using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PropertyManagement.API.Data;
using PropertyManagement.API.Hubs;
using PropertyManagement.API.Services;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
    // Allow SignalR to pass token via query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSignalR();
// Background service flags overdue payments hourly — runs in the API (single source of truth)
builder.Services.AddHostedService<PropertyManagement.API.Services.OverduePaymentService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Rate limiting — protects the login endpoint from brute-force attacks.
// Fixed window: max 10 login attempts per IP per minute.
// Uses Microsoft.AspNetCore.RateLimiting (built into .NET 7+ Web SDK — no extra package needed).
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit          = 10,
                Window               = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0   // reject immediately, no queue
            }
        ));
    options.RejectionStatusCode = 429; // Too Many Requests
});
// Response caching: reduces repeated identical GET requests hitting the DB.
// Controllers opt-in using [ResponseCache(Duration = N)] attributes.
builder.Services.AddResponseCaching();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Property Management API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    // Restrict to known client origins — explicitly listed instead of open wildcard
    // allow-list so the API does not accept requests from arbitrary third-party domains.
    // Azure URLs are included alongside the local dev ports.
    var allowedOrigins = new[]
    {
        "https://localhost:7143",                           // MVC dev (HTTPS)
        "http://localhost:5134",                            // MVC dev (HTTP)
        "https://localhost:7200",                           // Reporting dev (HTTPS)
        "http://localhost:5200",                            // Reporting dev (HTTP)
        "https://propertymgmt-mvc.azurewebsites.net",       // MVC Azure
        "https://propertymgmt-reporting.azurewebsites.net", // Reporting Azure
    };

    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader());

    // Hub uses the same allow-list. No AllowCredentials() — the hub has no [Authorize]
    // so credentials are never needed. Both policies share the same origin list so
    // any future change only needs to be made in one place (allowedOrigins above).
    options.AddPolicy("SignalR", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// ── Startup configuration guard ───────────────────────────────────────────────
// Fail fast if critical settings were not set in Azure App Service configuration.
// The sentinel value "AZURE_OVERRIDE_VIA_ENV_VAR" means the admin forgot to set
// the environment variable — better to crash immediately than fail silently later.
{
    var jwtKey  = builder.Configuration["Jwt:Key"] ?? "";
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";

    if (jwtKey is "" or "AZURE_OVERRIDE_VIA_ENV_VAR")
        throw new InvalidOperationException(
            "Jwt:Key is not configured. " +
            "Set the 'Jwt__Key' environment variable in Azure App Service → Configuration.");

    if (connStr is "" or "AZURE_OVERRIDE_VIA_ENV_VAR")
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is not configured. " +
            "Set 'ConnectionStrings__DefaultConnection' in Azure App Service → Configuration.");
}

// Apply any pending EF Core migrations automatically on startup.
// This ensures the Azure SQL database schema is always up to date
// without requiring manual "dotnet ef database update" commands in the pipeline.
using (var scope = app.Services.CreateScope())
{
    var db     = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await db.Database.MigrateAsync();   // Applies pending migrations (idempotent)
        await SeedData.Initialize(scope.ServiceProvider); // Seeds roles + demo accounts
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration or seeding.");
        // Do not rethrow — allow the app to start even if seeding fails on subsequent runs
    }
}

// Swagger: exposed in Development only to avoid leaking the API schema in production.
// To access Swagger on Azure during testing, temporarily set ASPNETCORE_ENVIRONMENT=Development.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Property Management API v1"));
}

app.UseHttpsRedirection();
// Middleware order matters for SignalR + CORS:
// UseRouting must be explicit so UseCors can run between routing and endpoint execution.
// SignalR WebSocket upgrades happen at the endpoint level — if CORS runs before routing
// resolves the hub endpoint, RequireCors("SignalR") on MapHub is never applied.
app.UseRouting();
app.UseRateLimiter();
app.UseResponseCaching();
app.UseCors("AllowAll");        // Applied to all REST controllers
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// Hub uses its own CORS policy (same origin allow-list, no credentials needed)
// so browsers allow the cross-origin WebSocket upgrade from the MVC app.
app.MapHub<MaintenanceHub>("/hubs/maintenance").RequireCors("SignalR");
app.Run();

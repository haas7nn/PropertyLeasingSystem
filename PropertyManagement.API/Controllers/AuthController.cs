using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PropertyManagement.API.DTOs;
using PropertyManagement.API.Services;

namespace PropertyManagement.API.Controllers
{
    /// <summary>
    /// API Controller: Authentication
    ///
    /// Provides JWT token issuance for all three roles:
    ///   • Tenant (Role A)           — email/password login → JWT with "Tenant" role claim
    ///   • Maintenance Staff (Role B) — email/password login → JWT with "MaintenanceStaff" claim
    ///   • Property Manager (Role C)  — email/password login → JWT with "PropertyManager" claim
    ///
    /// The returned JWT is used by:
    ///   1. The Reporting Application — stored in memory, attached to every API call header.
    ///   2. External API clients (e.g., Swagger) — pasted into the Authorize dialog.
    ///
    /// The MVC application uses ASP.NET Core Identity cookie authentication instead —
    /// it does NOT call this endpoint. MVC login goes through AccountController → SignInManager.
    ///
    /// Token format: Bearer {JWT}
    /// Token lifetime: configured via Jwt:ExpiryInMinutes in appsettings.json (default: 60 min).
    ///
    /// Security: passwords are never returned or logged. ASP.NET Core Identity handles
    /// hashing (PBKDF2 with HMAC-SHA256) internally — we only call CheckPasswordSignInAsync.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly TokenService _tokenService;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            TokenService tokenService,
            IConfiguration configuration)
        {
            _userManager   = userManager;
            _signInManager = signInManager;
            _tokenService  = tokenService;
            _configuration = configuration;
        }

        /// <summary>
        /// POST /api/Auth/login
        /// Body: { email, password }
        ///
        /// Validates credentials via ASP.NET Core Identity, then issues a signed JWT.
        /// Returns 401 Unauthorized for both "user not found" and "wrong password"
        /// to avoid disclosing which emails are registered (security best practice).
        ///
        /// Response body: { token, email, userId, roles[], expiration }
        /// The token must be included in subsequent API requests as:
        ///   Authorization: Bearer {token}
        /// </summary>
        [HttpPost("login")]
        [EnableRateLimiting("login")]  // max 10 login attempts per IP per minute
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Look up user by email — returns null if not found (same response as wrong password)
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password." });

            // CheckPasswordSignInAsync does NOT create a cookie — only validates the hash
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid email or password." });

            var roles       = await _userManager.GetRolesAsync(user);
            var token       = _tokenService.GenerateToken(user, roles);
            var expiryMins  = Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"] ?? "60");

            return Ok(new LoginResponseDto
            {
                Token      = token,
                Email      = user.Email ?? string.Empty,
                UserId     = user.Id,
                Roles      = roles.ToList(),
                Expiration = DateTime.UtcNow.AddMinutes(expiryMins)
            });
        }
    }
}

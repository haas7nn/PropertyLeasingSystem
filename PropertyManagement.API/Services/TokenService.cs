using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PropertyManagement.API.Services
{
    /// <summary>
    /// Generates signed JWT access tokens for authenticated users.
    ///
    /// Token contents (claims):
    ///   • NameIdentifier — user's Identity GUID (used throughout API to filter by owner)
    ///   • Name           — user's email (used as display name in MVC and Reporting App)
    ///   • Email          — explicitly included for client convenience
    ///   • Role           — one per role claim; supports [Authorize(Roles = "...")] filtering
    ///   • Jti            — unique token ID; useful for future token revocation
    ///
    /// Configuration (appsettings.json → "Jwt" section):
    ///   Key            : HMAC-SHA256 signing key (≥ 32 characters required)
    ///   Issuer         : Identifies the API as the token issuer
    ///   Audience       : Identifies the intended token consumers (MVC + Reporting App)
    ///   ExpiryInMinutes: Token lifetime; 60 minutes by default
    ///
    /// Security note: The signing key is read from configuration at call time —
    /// never hardcoded. In Azure, this is overridden via App Service environment
    /// variables (Jwt__Key) without touching appsettings.json.
    /// </summary>
    public class TokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Builds and signs a JWT token for the given user and their roles.
        /// Called by AuthController immediately after a successful password check.
        /// </summary>
        /// <param name="user">The authenticated IdentityUser (Tenant, Staff, or Manager).</param>
        /// <param name="roles">Role names assigned to the user in Identity.</param>
        /// <returns>Compact serialised JWT string ready to include in Authorization header.</returns>
        public string GenerateToken(IdentityUser user, IList<string> roles)
        {
            // Build the claims that will be embedded in the token payload
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),              // User GUID
                new Claim(ClaimTypes.Name,           user.UserName ?? ""),  // Email as username
                new Claim(ClaimTypes.Email,          user.Email    ?? ""),  // Explicit email
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique token ID
            };

            // Add one Role claim per role (users may have multiple roles in edge cases)
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "Jwt:Key is not configured. Set the Jwt__Key environment variable.");

            var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            // Fall back to 60 min if not configured — a missing non-critical setting
            // should degrade gracefully rather than prevent all logins.
            var expiryMinutes = double.TryParse(_configuration["Jwt:ExpiryInMinutes"], out var parsed)
                ? parsed : 60.0;
            var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                issuer:            _configuration["Jwt:Issuer"],
                audience:          _configuration["Jwt:Audience"],
                claims:            claims,
                expires:           expiry,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

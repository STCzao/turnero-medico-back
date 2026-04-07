using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace turnero_medico_backend.Tests.Integration
{
    /// <summary>
    /// Genera JWT firmados con la misma clave que usa la factory.
    /// El token resultante es aceptado por el middleware real de JwtBearer.
    /// </summary>
    public static class JwtTestHelper
    {
        public static string GenerateToken(
            string userId,
            string role,
            string? secret   = null,
            string? issuer   = null,
            string? audience = null)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret ?? CustomWebApplicationFactory.TestJwtSecret));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer:             issuer   ?? CustomWebApplicationFactory.TestIssuer,
                audience:           audience ?? CustomWebApplicationFactory.TestAudience,
                claims:             claims,
                expires:            DateTime.UtcNow.AddHours(1),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Devuelve un HttpClient con el header Authorization ya configurado.
        /// </summary>
        public static HttpClient WithRole(this HttpClient client, string role, string userId = "test-user-id")
        {
            var token = GenerateToken(userId, role);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}

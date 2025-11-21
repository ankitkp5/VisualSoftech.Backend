using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Services
{
    public class JwtService
    {
        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;

        public JwtService(string key, string issuer, string audience, int expiryMinutes)
        {
            _key = key;
            _issuer = issuer;
            _audience = audience;
            _expiryMinutes = expiryMinutes;
        }

        public string GenerateToken(string username)
        {
            // Security key
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));

            // Credentials
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Claims (you can add more whenever needed)
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username)
            };

            // Token structure
            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
                signingCredentials: credentials
            );

            // Write token
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

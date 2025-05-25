using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using jool_backend.Models;
using jool_backend.DTOs;

namespace jool_backend.Services
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public TokenDto GenerateJwtToken(User user)
        {
            // Obtener la configuración de JWT
            var jwtSection = _configuration.GetSection("JWT");
            var secretKey = jwtSection["SecretKey"];
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var expirationMinutes = int.Parse(jwtSection["DurationInMinutes"]);

            // Configurar las reclamaciones del token
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.user_id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("first_name", user.first_name),
                new Claim("last_name", user.last_name)
            };

            // Configurar las credenciales de firma
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Calcular fecha de expiración
            var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

            // Crear el token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            // Serializar el token a string
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(token);

            // Devolver el DTO del token
            return new TokenDto
            {
                AccessToken = tokenString,
                ExpiresAt = expires
            };
        }
    }
} 
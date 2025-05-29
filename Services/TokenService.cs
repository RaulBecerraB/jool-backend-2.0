using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using jool_backend.Models;
using jool_backend.DTOs;
using jool_backend.Utils;

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
            try
            {
                // Obtener la configuración de JWT
                var jwtSection = _configuration.GetSection("JWT");
                var secretKey = jwtSection["SecretKey"];
                var issuer = jwtSection["Issuer"];
                var audience = jwtSection["Audience"];
                var expirationMinutes = int.Parse(jwtSection["DurationInMinutes"]);

                // Validar que exista la configuración necesaria
                if (string.IsNullOrEmpty(secretKey))
                {
                    LoggingUtils.LogError("La clave secreta JWT no está configurada", nameof(TokenService));
                    throw new InvalidOperationException("La configuración JWT no está completa: falta SecretKey");
                }

                // Configurar las reclamaciones del token
                var claims = CreateClaims(user);

                // Configurar las credenciales de firma
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Calcular fecha de expiración
                var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

                // Crear el token
                var token = CreateToken(claims, creds, issuer, audience, expires);

                // Serializar el token a string
                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenString = tokenHandler.WriteToken(token);

                LoggingUtils.LogInfo($"Token JWT generado para usuario {user.email}", nameof(TokenService));

                // Devolver el DTO del token
                return new TokenDto
                {
                    AccessToken = tokenString,
                    ExpiresAt = expires
                };
            }
            catch (Exception ex)
            {
                LoggingUtils.LogException(ex, nameof(TokenService), "Error al generar token JWT");
                throw;
            }
        }

        private List<Claim> CreateClaims(User user)
        {
            return new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.user_id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("first_name", user.first_name),
                new Claim("last_name", user.last_name)
            };
        }

        private JwtSecurityToken CreateToken(List<Claim> claims, SigningCredentials credentials, 
                                            string issuer, string audience, DateTime expires)
        {
            return new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );
        }
    }
} 
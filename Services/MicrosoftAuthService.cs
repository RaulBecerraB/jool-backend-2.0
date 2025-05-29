using jool_backend.DTOs;
using jool_backend.Models;
using jool_backend.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Generic;

namespace jool_backend.Services
{
    public class MicrosoftAuthService
    {
        private readonly UserRepository _userRepository;
        private readonly TokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;

        public MicrosoftAuthService(
            UserRepository userRepository,
            TokenService tokenService,
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
        }

        // Método para manejar manualmente el código de autorización
        public async Task<UserDto> HandleAuthorizationCodeAsync(string code, string redirectUri)
        {
            try
            {
                Console.WriteLine($"Procesando código de autorización recibido...");

                // 1. Intercambiar el código por un token de acceso
                var httpClient = _httpClientFactory.CreateClient();
                
                var tokenRequestParams = new Dictionary<string, string>
                {
                    ["client_id"] = Environment.GetEnvironmentVariable("MS_CLIENT_ID"),
                    ["client_secret"] = Environment.GetEnvironmentVariable("MS_CLIENT_SECRET"),
                    ["code"] = code,
                    ["redirect_uri"] = redirectUri,
                    ["grant_type"] = "authorization_code"
                };

                // Imprimir parámetros para debug (excepto client_secret)
                foreach (var param in tokenRequestParams.Where(p => p.Key != "client_secret"))
                {
                    Console.WriteLine($"Param: {param.Key}={param.Value}");
                }

                var tokenRequestContent = new FormUrlEncodedContent(tokenRequestParams);

                Console.WriteLine("Enviando solicitud para obtener token...");
                var tokenResponse = await httpClient.PostAsync(
                    "https://login.microsoftonline.com/common/oauth2/v2.0/token", 
                    tokenRequestContent);

                var responseContent = await tokenResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Respuesta del token: StatusCode={tokenResponse.StatusCode}");
                
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error al obtener token: {responseContent}");
                    return null;
                }

                Console.WriteLine("Token obtenido exitosamente");
                
                // 2. Parsear la respuesta JSON para obtener el token
                var tokenData = System.Text.Json.JsonDocument.Parse(responseContent);
                var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("Error: Token de acceso vacío");
                    return null;
                }
                
                Console.WriteLine($"Token de acceso válido obtenido");

                // 3. Obtener información del usuario con el token
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                
                Console.WriteLine("Solicitando información del usuario...");
                var userResponse = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");
                
                var userResponseContent = await userResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Respuesta del usuario: StatusCode={userResponse.StatusCode}");
                
                if (!userResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error al obtener información del usuario: {userResponseContent}");
                    return null;
                }

                Console.WriteLine("Información del usuario obtenida exitosamente");
                
                // 4. Parsear los datos del usuario
                var userData = System.Text.Json.JsonDocument.Parse(userResponseContent);
                
                string email = null;
                if (userData.RootElement.TryGetProperty("mail", out var mailProperty) && !mailProperty.ValueKind.Equals(JsonValueKind.Null))
                {
                    email = mailProperty.GetString();
                }
                else if (userData.RootElement.TryGetProperty("userPrincipalName", out var upnProperty))
                {
                    email = upnProperty.GetString();
                }
                
                var firstName = userData.RootElement.TryGetProperty("givenName", out var givenNameProperty) 
                    ? givenNameProperty.GetString() : "Usuario";
                
                var lastName = userData.RootElement.TryGetProperty("surname", out var surnameProperty)
                    ? surnameProperty.GetString() : "Microsoft";
                
                if (string.IsNullOrEmpty(email))
                {
                    Console.WriteLine("Error: No se pudo obtener el email del usuario");
                    return null;
                }
                
                Console.WriteLine($"Datos obtenidos de Microsoft: Email={email}, Nombre={firstName} {lastName}");
                
                // 5. Buscar o crear usuario
                var existingUser = await _userRepository.GetUserByEmailAsync(email);
                
                if (existingUser == null)
                {
                    Console.WriteLine("Creando nuevo usuario...");
                    // Crear un nuevo usuario con una contraseña aleatoria
                    string randomPassword = Guid.NewGuid().ToString();
                    
                    var newUser = new User
                    {
                        email = email,
                        first_name = firstName ?? "Usuario",
                        last_name = lastName ?? "Microsoft",
                        password = HashPassword(randomPassword),
                        is_active = true
                    };
                    
                    existingUser = await _userRepository.CreateUserAsync(newUser);
                    Console.WriteLine($"Nuevo usuario creado con ID: {existingUser?.user_id}");
                }
                else
                {
                    Console.WriteLine($"Usuario existente encontrado con ID: {existingUser.user_id}");
                }
                
                // 6. Generar JWT
                var token = _tokenService.GenerateJwtToken(existingUser);
                Console.WriteLine("Token JWT generado correctamente");
                
                // 7. Devolver DTO
                return new UserDto
                {
                    user_id = existingUser.user_id,
                    email = existingUser.email,
                    first_name = existingUser.first_name,
                    last_name = existingUser.last_name,
                    is_active = existingUser.is_active,
                    Token = token
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en HandleAuthorizationCodeAsync: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        // Método original que ahora no usaremos
        public async Task<UserDto> ProcessMicrosoftAuthAsync()
        {
            try
            {
                Console.WriteLine("Procesando autenticación con Microsoft...");
                
                if (_httpContextAccessor.HttpContext == null)
                {
                    Console.WriteLine("Error: HttpContext es null");
                    return null;
                }

                // Es importante usar el mismo esquema que se usa en Challenge
                var authenticateResult = await _httpContextAccessor.HttpContext.AuthenticateAsync("Microsoft");
                
                if (!authenticateResult.Succeeded)
                {
                    Console.WriteLine($"Error: Autenticación fallida. {authenticateResult.Failure?.Message}");
                    return null;
                }

                // Obtener claims del token
                var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
                var firstName = authenticateResult.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "Usuario";
                var lastName = authenticateResult.Principal.FindFirstValue(ClaimTypes.Surname) ?? "Microsoft";
                
                Console.WriteLine($"Datos recibidos de Microsoft - Email: {email}, Nombre: {firstName} {lastName}");
                
                if (string.IsNullOrEmpty(email))
                {
                    Console.WriteLine("Error: No se pudo obtener el email del usuario");
                    return null;
                }
                
                // Verificar si el usuario ya existe
                var existingUser = await _userRepository.GetUserByEmailAsync(email);
                
                if (existingUser == null)
                {
                    Console.WriteLine("Creando nuevo usuario...");
                    // Crear un nuevo usuario con una contraseña aleatoria
                    // (el usuario nunca la usará porque se autenticará con Microsoft)
                    string randomPassword = Guid.NewGuid().ToString();
                    
                    var newUser = new User
                    {
                        email = email,
                        first_name = firstName,
                        last_name = lastName,
                        password = HashPassword(randomPassword),
                        is_active = true
                    };
                    
                    existingUser = await _userRepository.CreateUserAsync(newUser);
                    Console.WriteLine($"Nuevo usuario creado con ID: {existingUser?.user_id}");
                }
                else
                {
                    Console.WriteLine($"Usuario existente encontrado con ID: {existingUser.user_id}");
                }
                
                // Generar el token JWT
                var token = _tokenService.GenerateJwtToken(existingUser);
                Console.WriteLine("Token JWT generado correctamente");
                
                // Devolver el DTO del usuario con el token
                return new UserDto
                {
                    user_id = existingUser.user_id,
                    email = existingUser.email,
                    first_name = existingUser.first_name,
                    last_name = existingUser.last_name,
                    is_active = existingUser.is_active,
                    Token = token
                };
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error en ProcessMicrosoftAuthAsync: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Convertir la contraseña a bytes
                byte[] bytes = Encoding.UTF8.GetBytes(password);

                // Calcular el hash
                byte[] hash = sha256.ComputeHash(bytes);

                // Convertir el hash a string en formato hexadecimal
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
} 
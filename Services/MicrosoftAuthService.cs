using jool_backend.DTOs;
using jool_backend.Models;
using jool_backend.Repository;
using jool_backend.Utils;
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
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace jool_backend.Services
{
    public class MicrosoftAuthService
    {
        private readonly UserRepository _userRepository;
        private readonly TokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public MicrosoftAuthService(
            UserRepository userRepository,
            TokenService tokenService,
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // Método para manejar manualmente el código de autorización
        public async Task<UserDto> HandleAuthorizationCodeAsync(string code, string redirectUri)
        {
            try
            {
                LoggingUtils.LogInfo("Procesando código de autorización recibido...", nameof(MicrosoftAuthService));

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
                    LoggingUtils.LogInfo($"Param: {param.Key}={param.Value}", nameof(MicrosoftAuthService));
                }

                var tokenRequestContent = new FormUrlEncodedContent(tokenRequestParams);

                LoggingUtils.LogInfo("Enviando solicitud para obtener token...", nameof(MicrosoftAuthService));
                var tokenResponse = await httpClient.PostAsync(
                    "https://login.microsoftonline.com/common/oauth2/v2.0/token",
                    tokenRequestContent);

                var responseContent = await tokenResponse.Content.ReadAsStringAsync();
                LoggingUtils.LogInfo($"Respuesta del token: StatusCode={tokenResponse.StatusCode}", nameof(MicrosoftAuthService));

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    LoggingUtils.LogError($"Error al obtener token: {responseContent}", nameof(MicrosoftAuthService));
                    return null;
                }

                LoggingUtils.LogInfo("Token obtenido exitosamente", nameof(MicrosoftAuthService));

                // 2. Parsear la respuesta JSON para obtener el token
                var tokenData = JsonDocument.Parse(responseContent);
                var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();

                if (string.IsNullOrEmpty(accessToken))
                {
                    LoggingUtils.LogError("Error: Token de acceso vacío", nameof(MicrosoftAuthService));
                    return null;
                }

                LoggingUtils.LogInfo("Token de acceso válido obtenido", nameof(MicrosoftAuthService));

                // 3. Obtener información del usuario con el token
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                LoggingUtils.LogInfo("Solicitando información del usuario...", nameof(MicrosoftAuthService));
                var userResponse = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");

                var userResponseContent = await userResponse.Content.ReadAsStringAsync();
                LoggingUtils.LogInfo($"Respuesta del usuario: StatusCode={userResponse.StatusCode}", nameof(MicrosoftAuthService));

                if (!userResponse.IsSuccessStatusCode)
                {
                    LoggingUtils.LogError($"Error al obtener información del usuario: {userResponseContent}", nameof(MicrosoftAuthService));
                    return null;
                }

                LoggingUtils.LogInfo("Información del usuario obtenida exitosamente", nameof(MicrosoftAuthService));

                // 4. Parsear los datos del usuario
                var userData = JsonDocument.Parse(userResponseContent);

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
                    LoggingUtils.LogError("Error: No se pudo obtener el email del usuario", nameof(MicrosoftAuthService));
                    return null;
                }

                LoggingUtils.LogInfo($"Datos obtenidos de Microsoft: Email={email}, Nombre={firstName} {lastName}", nameof(MicrosoftAuthService));

                // 5. Buscar o crear usuario
                var existingUser = await _userRepository.GetUserByEmailAsync(email);

                if (existingUser == null)
                {
                    LoggingUtils.LogInfo("Creando nuevo usuario...", nameof(MicrosoftAuthService));
                    // Crear un nuevo usuario con una contraseña aleatoria
                    string randomPassword = SecurityUtils.GenerateRandomPassword();

                    var newUser = new User
                    {
                        email = email,
                        first_name = firstName ?? "Usuario",
                        last_name = lastName ?? "Microsoft",
                        password = SecurityUtils.HashPassword(randomPassword),
                        is_active = true
                    };

                    existingUser = await _userRepository.CreateUserAsync(newUser);
                    LoggingUtils.LogInfo($"Nuevo usuario creado con ID: {existingUser?.user_id}", nameof(MicrosoftAuthService));
                }
                else
                {
                    LoggingUtils.LogInfo($"Usuario existente encontrado con ID: {existingUser.user_id}", nameof(MicrosoftAuthService));
                }

                // 6. Generar JWT
                var token = _tokenService.GenerateJwtToken(existingUser);
                LoggingUtils.LogInfo("Token JWT generado correctamente", nameof(MicrosoftAuthService));

                // 7. Devolver DTO
                return MappingUtils.MapToUserDtoWithToken(existingUser, token);
            }
            catch (Exception ex)
            {
                LoggingUtils.LogException(ex, nameof(MicrosoftAuthService), "Error en HandleAuthorizationCodeAsync");
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

        // Nuevo método para procesar el código de autorización y devolver el usuario y token
        public async Task<(User User, TokenDto Token)> ProcessAuthorizationCodeAsync(string code)
        {
            try
            {
                LoggingUtils.LogInfo("Procesando código de autorización...", nameof(MicrosoftAuthService));

                // Usar localhost como URL de redirección para Azure AD
                string redirectUri = "https://refined-portion-substance-rendered.trycloudflare.com/auth/microsoft-callback";

                LoggingUtils.LogInfo($"URL de redirección: {redirectUri}", nameof(MicrosoftAuthService));

                // Crear cliente HTTP
                var httpClient = _httpClientFactory.CreateClient();

                // Obtener credenciales
                var clientId = Environment.GetEnvironmentVariable("MS_CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("MS_CLIENT_SECRET");

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    LoggingUtils.LogError("Credenciales de Microsoft no configuradas correctamente", nameof(MicrosoftAuthService));
                    throw new InvalidOperationException("Las credenciales de Microsoft no están configuradas correctamente");
                }

                // Preparar datos para solicitar token
                var tokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
                var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = clientId,
                    ["scope"] = "https://graph.microsoft.com/User.Read",
                    ["code"] = code,
                    ["redirect_uri"] = redirectUri,
                    ["grant_type"] = "authorization_code",
                    ["client_secret"] = clientSecret
                });

                // Solicitar token
                LoggingUtils.LogInfo("Solicitando token a Microsoft...", nameof(MicrosoftAuthService));
                var tokenResponse = await httpClient.PostAsync(tokenEndpoint, tokenRequestContent);

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                    LoggingUtils.LogError($"Error al obtener token: {errorContent}", nameof(MicrosoftAuthService));
                    throw new InvalidOperationException($"Error al obtener token de Microsoft: {errorContent}");
                }

                // Parsear respuesta
                var tokenResponseJson = await tokenResponse.Content.ReadAsStringAsync();
                var tokenData = JsonDocument.Parse(tokenResponseJson);
                var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();

                // Configurar cliente para solicitar datos de usuario
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                // Solicitar datos de usuario
                LoggingUtils.LogInfo("Solicitando información del usuario...", nameof(MicrosoftAuthService));
                var userInfoResponse = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");

                if (!userInfoResponse.IsSuccessStatusCode)
                {
                    var errorContent = await userInfoResponse.Content.ReadAsStringAsync();
                    LoggingUtils.LogError($"Error al obtener información del usuario: {errorContent}", nameof(MicrosoftAuthService));
                    throw new InvalidOperationException($"Error al obtener información del usuario: {errorContent}");
                }

                // Parsear datos del usuario
                var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
                var userInfo = JsonDocument.Parse(userInfoJson);

                // Extraer datos relevantes
                string email = null;
                if (userInfo.RootElement.TryGetProperty("mail", out var mailProperty) &&
                    mailProperty.ValueKind != JsonValueKind.Null)
                {
                    email = mailProperty.GetString();
                }
                else if (userInfo.RootElement.TryGetProperty("userPrincipalName", out var upnProperty))
                {
                    email = upnProperty.GetString();
                }

                if (string.IsNullOrEmpty(email))
                {
                    LoggingUtils.LogError("No se pudo obtener el email del usuario", nameof(MicrosoftAuthService));
                    throw new InvalidOperationException("No se pudo obtener el email del usuario desde Microsoft");
                }

                var firstName = userInfo.RootElement.TryGetProperty("givenName", out var givenNameProperty)
                    ? givenNameProperty.GetString()
                    : "Usuario";

                var lastName = userInfo.RootElement.TryGetProperty("surname", out var surnameProperty)
                    ? surnameProperty.GetString()
                    : "Microsoft";

                LoggingUtils.LogInfo($"Datos obtenidos: Email={email}, Nombre={firstName} {lastName}", nameof(MicrosoftAuthService));

                // Buscar o crear usuario
                var existingUser = await _userRepository.GetUserByEmailAsync(email);

                if (existingUser == null)
                {
                    LoggingUtils.LogInfo("Usuario no encontrado, creando nuevo usuario...", nameof(MicrosoftAuthService));

                    // Generar contraseña aleatoria
                    string randomPassword = SecurityUtils.GenerateRandomPassword();

                    // Crear nuevo usuario
                    var newUser = new User
                    {
                        email = email,
                        first_name = firstName,
                        last_name = lastName,
                        password = SecurityUtils.HashPassword(randomPassword),
                        is_active = true
                    };

                    existingUser = await _userRepository.CreateUserAsync(newUser);
                    LoggingUtils.LogInfo($"Nuevo usuario creado con ID: {existingUser.user_id}", nameof(MicrosoftAuthService));
                }
                else
                {
                    LoggingUtils.LogInfo($"Usuario encontrado con ID: {existingUser.user_id}", nameof(MicrosoftAuthService));
                }

                // Generar token JWT
                var jwtToken = _tokenService.GenerateJwtToken(existingUser);
                LoggingUtils.LogInfo("Token JWT generado correctamente", nameof(MicrosoftAuthService));

                return (existingUser, jwtToken);
            }
            catch (Exception ex)
            {
                LoggingUtils.LogException(ex, nameof(MicrosoftAuthService), "Error en ProcessAuthorizationCodeAsync");
                throw;
            }
        }

        // Método para generar la URL de autorización de Microsoft
        public string GetAuthorizationUrl(string redirectUrl = null)
        {
            try
            {
                LoggingUtils.LogInfo("Generando URL de autorización de Microsoft...", nameof(MicrosoftAuthService));

                // Obtener parámetros necesarios
                var clientId = Environment.GetEnvironmentVariable("MS_CLIENT_ID");

                if (string.IsNullOrEmpty(clientId))
                {
                    LoggingUtils.LogError("ClientID de Microsoft no está configurado", nameof(MicrosoftAuthService));
                    throw new InvalidOperationException("El client_id de Microsoft no está configurado");
                }

                // Siempre usar localhost para la URL de redirección en Azure AD
                string redirectUri = "https://refined-portion-substance-rendered.trycloudflare.com/auth/microsoft-callback";

                // Guardar la URL real para usar después en el callback
                var request = _httpContextAccessor.HttpContext.Request;
                var realRedirectUri = $"{request.Scheme}://{request.Host}/auth/microsoft-callback";

                // Guardar la URL real en la sesión
                _httpContextAccessor.HttpContext.Session.Set(
                    "RealRedirectUri",
                    System.Text.Encoding.UTF8.GetBytes(realRedirectUri)
                );
                LoggingUtils.LogInfo($"URL real guardada: {realRedirectUri}", nameof(MicrosoftAuthService));

                // Si se proporcionó una URL de redirección personalizada, guardarla en la sesión
                if (!string.IsNullOrEmpty(redirectUrl))
                {
                    _httpContextAccessor.HttpContext.Session.Set(
                        "MicrosoftAuthRedirectUrl",
                        System.Text.Encoding.UTF8.GetBytes(redirectUrl)
                    );
                    LoggingUtils.LogInfo($"URL de redirección personalizada guardada: {redirectUrl}", nameof(MicrosoftAuthService));
                }

                // Crear la URL de autorización
                var authUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize" +
                              $"?client_id={Uri.EscapeDataString(clientId)}" +
                              "&response_type=code" +
                              $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                              "&response_mode=query" +
                              "&scope=user.read%20openid%20profile%20email" +
                              "&prompt=select_account";

                LoggingUtils.LogInfo($"URL de autorización generada: {authUrl}", nameof(MicrosoftAuthService));
                return authUrl;
            }
            catch (Exception ex)
            {
                LoggingUtils.LogException(ex, nameof(MicrosoftAuthService), "Error en GetAuthorizationUrl");
                throw;
            }
        }
    }
}
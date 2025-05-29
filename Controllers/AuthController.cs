using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using jool_backend.DTOs;
using jool_backend.Services;
using jool_backend.Repository;
using jool_backend.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace jool_backend.Controllers
{
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly MicrosoftAuthService _microsoftAuthService;
        private readonly TokenService _tokenService;
        private readonly UserRepository _userRepository;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(
            AuthService authService, 
            MicrosoftAuthService microsoftAuthService,
            TokenService tokenService,
            UserRepository userRepository,
            IHttpClientFactory httpClientFactory)
        {
            _authService = authService;
            _microsoftAuthService = microsoftAuthService;
            _tokenService = tokenService;
            _userRepository = userRepository;
            _httpClientFactory = httpClientFactory;
        }

        // POST: /auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDto>> Register(RegisterUserDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterUserAsync(registerDto);
            if (result == null)
            {
                return BadRequest("El correo electrónico ya está registrado");
            }

            return CreatedAtAction(nameof(Register), result);
        }

        // POST: /auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(loginDto);
            if (result == null)
            {
                return Unauthorized("Correo o contraseña incorrectos");
            }

            return Ok(result);
        }
        
        // GET: /auth/profile
        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<string> GetProfile()
        {
            // Obtener el ID del usuario desde las claims del token
            var userId = User.FindFirst("sub")?.Value;
            var email = User.FindFirst("email")?.Value;
            var firstName = User.FindFirst("first_name")?.Value;
            var lastName = User.FindFirst("last_name")?.Value;
            
            return Ok(new { 
                userId, 
                email, 
                firstName, 
                lastName,
                message = "Perfil obtenido correctamente"
            });
        }

        // GET: /auth/login-microsoft
        [HttpGet("login-microsoft")]
        [AllowAnonymous]
        public IActionResult LoginWithMicrosoft()
        {
            try 
            {
                // Configurar la URL de redirección - importante usar minúsculas en 'auth'
                var redirectUri = $"{Request.Scheme}://{Request.Host}/auth/microsoft-callback";
                
                // Generar la URL de autorización de Microsoft
                var clientId = Environment.GetEnvironmentVariable("MS_CLIENT_ID");
                var scope = "https://graph.microsoft.com/user.read";
                
                var authorizationUrl = $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize" + 
                    $"?client_id={clientId}" +
                    $"&response_type=code" +
                    $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                    $"&response_mode=query" +
                    $"&scope={Uri.EscapeDataString(scope)}";
                
                // Devolver la URL en lugar de redireccionar
                return Ok(new { 
                    redirect_url = authorizationUrl
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error al generar URL para autenticación con Microsoft" });
            }
        }

        // GET: /auth/microsoft-callback
        [HttpGet("microsoft-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> MicrosoftCallback()
        {
            try
            {
                // Verificar si hay errores en la respuesta
                if (Request.Query.ContainsKey("error") && 
                    Request.Query["error"] != "invalid_state") // Ignorar errores de estado
                {
                    var error = Request.Query["error"];
                    var errorDescription = Request.Query["error_description"];
                    return BadRequest(new { message = $"Error durante la autenticación: {errorDescription}" });
                }
                
                // Obtener el código de autorización
                if (!Request.Query.ContainsKey("code"))
                {
                    return BadRequest(new { message = "No se recibió el código de autorización" });
                }
                
                var code = Request.Query["code"];
                
                // Intercambiar el código por un token manualmente
                var redirectUri = $"{Request.Scheme}://{Request.Host}/auth/microsoft-callback";
                var client = _httpClientFactory.CreateClient();
                
                var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = Environment.GetEnvironmentVariable("MS_CLIENT_ID"),
                    ["client_secret"] = Environment.GetEnvironmentVariable("MS_CLIENT_SECRET"),
                    ["code"] = code,
                    ["redirect_uri"] = redirectUri,
                    ["grant_type"] = "authorization_code"
                });
                
                var tokenResponse = await client.PostAsync(
                    "https://login.microsoftonline.com/common/oauth2/v2.0/token", 
                    tokenRequestContent);
                
                var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
                
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    return BadRequest(new { message = "Error al obtener token de acceso" });
                }
                
                // Extraer el token de acceso
                var tokenData = JsonDocument.Parse(tokenResponseContent);
                var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    return BadRequest(new { message = "Token de acceso vacío" });
                }
                
                // Obtener información del usuario
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                
                var userResponse = await client.GetAsync("https://graph.microsoft.com/v1.0/me");
                var userResponseContent = await userResponse.Content.ReadAsStringAsync();
                
                if (!userResponse.IsSuccessStatusCode)
                {
                    return BadRequest(new { message = "Error al obtener información del usuario" });
                }
                
                // Extraer datos del usuario
                var userData = JsonDocument.Parse(userResponseContent);
                
                string email = null;
                if (userData.RootElement.TryGetProperty("mail", out var mailProperty) && 
                    mailProperty.ValueKind != JsonValueKind.Null)
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
                    return BadRequest(new { message = "No se pudo obtener el email del usuario" });
                }
                
                // Buscar o crear usuario
                var existingUser = await _userRepository.GetUserByEmailAsync(email);
                
                if (existingUser == null)
                {
                    // Crear un nuevo usuario con una contraseña aleatoria
                    string randomPassword = Guid.NewGuid().ToString();
                    string passwordHash = HashPassword(randomPassword);
                    
                    var newUser = new Models.User
                    {
                        email = email,
                        first_name = firstName ?? "Usuario",
                        last_name = lastName ?? "Microsoft",
                        password = passwordHash,
                        is_active = true
                    };
                    
                    existingUser = await _userRepository.CreateUserAsync(newUser);
                }
                
                // Generar JWT
                var token = _tokenService.GenerateJwtToken(existingUser);
                
                // Devolver los datos del usuario y el token en formato JSON
                return Ok(new { 
                    user_id = existingUser.user_id,
                    first_name = existingUser.first_name,
                    last_name = existingUser.last_name,
                    email = existingUser.email,
                    is_active = existingUser.is_active,
                    phone = existingUser.phone,
                    has_image = existingUser.image != null && existingUser.image.Length > 0,
                    token = new {
                        accessToken = token.AccessToken,
                        expiresAt = token.ExpiresAt
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error durante la autenticación con Microsoft" });
            }
        }

        // GET: /auth/login-error
        [HttpGet("login-error")]
        [AllowAnonymous]
        public IActionResult LoginError()
        {
            return BadRequest(new { message = "Error durante la autenticación con Microsoft. Por favor, intente de nuevo." });
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
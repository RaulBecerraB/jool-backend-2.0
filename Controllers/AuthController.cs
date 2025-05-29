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
using Microsoft.Extensions.Configuration;

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
        private readonly IConfiguration _configuration;

        public AuthController(
            AuthService authService, 
            MicrosoftAuthService microsoftAuthService,
            TokenService tokenService,
            UserRepository userRepository,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _authService = authService;
            _microsoftAuthService = microsoftAuthService;
            _tokenService = tokenService;
            _userRepository = userRepository;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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
        public IActionResult LoginWithMicrosoft([FromQuery] string redirectUrl = null)
        {
            try 
            {
                // Generar la URL de autorización de Microsoft
                var authorizationUrl = _microsoftAuthService.GetAuthorizationUrl(redirectUrl);
                
                // Devolver la URL en lugar de redireccionar
                // No usar tipo anónimo, usar un diccionario
                var response = new Dictionary<string, string>
                {
                    ["redirect_url"] = authorizationUrl
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en LoginWithMicrosoft: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                
                return BadRequest(new Dictionary<string, string>
                {
                    ["error"] = "Error al generar URL para autenticación con Microsoft"
                });
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
                if (Request.Query.ContainsKey("error"))
                {
                    var error = Request.Query["error"];
                    var errorDescription = Request.Query["error_description"];
                    
                    // Obtener la URL de error del frontend desde la configuración
                    string frontendErrorUrl = _configuration["Authentication:Microsoft:FrontendErrorUrl"] ?? 
                                             $"{Request.Scheme}://{Request.Host}/auth/login-error";
                    
                    // Redirigir al frontend con el error
                    return Redirect($"{frontendErrorUrl}?error={Uri.EscapeDataString(errorDescription)}");
                }
                
                // Obtener el código de autorización
                if (!Request.Query.ContainsKey("code"))
                {
                    string frontendErrorUrl = _configuration["Authentication:Microsoft:FrontendErrorUrl"] ?? 
                                             $"{Request.Scheme}://{Request.Host}/auth/login-error";
                    
                    return Redirect($"{frontendErrorUrl}?error=No se recibió el código de autorización");
                }
                
                var code = Request.Query["code"];
                
                // Procesar el código de autorización
                var (user, token) = await _microsoftAuthService.ProcessAuthorizationCodeAsync(code);
                
                // Construir objeto de respuesta usando Dictionary en lugar de tipo anónimo
                var authResult = new Dictionary<string, object>
                {
                    ["user_id"] = user.user_id,
                    ["first_name"] = user.first_name,
                    ["last_name"] = user.last_name,
                    ["email"] = user.email,
                    ["is_active"] = user.is_active,
                    ["phone"] = user.phone,
                    ["has_image"] = user.image != null && user.image.Length > 0,
                    ["token"] = new Dictionary<string, object>
                    {
                        ["accessToken"] = token.AccessToken,
                        ["expiresAt"] = token.ExpiresAt
                    }
                };

                // Verificar si hay una URL de redirección personalizada en la sesión
                string customRedirectUrl = null;
                if (HttpContext.Session.TryGetValue("MicrosoftAuthRedirectUrl", out var redirectUrlBytes))
                {
                    customRedirectUrl = Encoding.UTF8.GetString(redirectUrlBytes);
                    HttpContext.Session.Remove("MicrosoftAuthRedirectUrl");
                }
                
                // Obtener la URL de callback del frontend desde la configuración
                string frontendCallbackUrl = customRedirectUrl ?? 
                                           _configuration["Authentication:Microsoft:FrontendCallbackUrl"] ?? 
                                           $"{Request.Scheme}://{Request.Host}/auth/login-success";
                
                // Serializar los datos y codificar para URL
                var serializedData = JsonSerializer.Serialize(authResult);
                var encodedData = Uri.EscapeDataString(serializedData);
                
                // Redirigir al frontend con los datos en el fragmento hash (#)
                return Redirect($"{frontendCallbackUrl}#{encodedData}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en MicrosoftCallback: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                
                // Obtener la URL de error del frontend desde la configuración
                string frontendErrorUrl = _configuration["Authentication:Microsoft:FrontendErrorUrl"] ?? 
                                         $"{Request.Scheme}://{Request.Host}/auth/login-error";
                
                // Redirigir al frontend con el error
                return Redirect($"{frontendErrorUrl}?error={Uri.EscapeDataString("Error durante la autenticación con Microsoft")}");
            }
        }

        // GET: /auth/login-error
        [HttpGet("login-error")]
        [AllowAnonymous]
        public IActionResult LoginError()
        {
            return BadRequest(new Dictionary<string, string>
            {
                ["message"] = "Error durante la autenticación con Microsoft. Por favor, intente de nuevo."
            });
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
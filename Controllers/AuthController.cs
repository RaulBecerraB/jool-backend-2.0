using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using jool_backend.DTOs;
using jool_backend.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace jool_backend.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
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
    }
}
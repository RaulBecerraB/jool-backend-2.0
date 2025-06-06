using jool_backend.Models;
using jool_backend.Repository;
using jool_backend.DTOs;
using jool_backend.Validations;
using jool_backend.Utils;
using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using FluentValidation.Results;

namespace jool_backend.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;
        private readonly RegisterUserValidatorAsync _asyncValidator;
        private readonly TokenService _tokenService;

        public AuthService(UserRepository userRepository, RegisterUserValidatorAsync asyncValidator, TokenService tokenService)
        {
            _userRepository = userRepository;
            _asyncValidator = asyncValidator;
            _tokenService = tokenService;
        }

        public async Task<UserDto?> RegisterUserAsync(RegisterUserDto registerDto)
        {
            // Verificar si el email ya existe
            var existingUser = await _userRepository.GetUserByEmailAsync(registerDto.email);
            if (existingUser != null)
            {
                LoggingUtils.LogInfo($"Intento de registro con email ya existente: {registerDto.email}", nameof(AuthService));
                return null;
            }

            // Crear hash de la contraseña
            string passwordHash = SecurityUtils.HashPassword(registerDto.password);

            // Crear nuevo usuario
            var user = new User
            {
                first_name = registerDto.first_name,
                last_name = registerDto.last_name,
                email = registerDto.email,
                password = passwordHash,
                phone = registerDto.phone,
                is_active = true
            };

            // Guardar en la base de datos
            var createdUser = await _userRepository.CreateUserAsync(user);
            LoggingUtils.LogInfo($"Usuario registrado exitosamente: {createdUser.email}", nameof(AuthService));

            // Generar token JWT
            var token = _tokenService.GenerateJwtToken(createdUser);

            // Mapear a DTO y retornar
            return MappingUtils.MapToUserDtoWithToken(createdUser, token);
        }

        public async Task<UserDto?> LoginAsync(LoginDto loginDto)
        {
            // Buscar usuario por email
            var user = await _userRepository.GetUserByEmailAsync(loginDto.email);

            // Verificar que el usuario existe y está activo
            if (user == null || !user.is_active)
            {
                LoggingUtils.LogInfo($"Intento de login fallido: usuario no existe o inactivo - {loginDto.email}", nameof(AuthService));
                return null;
            }

            // Verificar contraseña
            if (!SecurityUtils.VerifyPassword(loginDto.password, user.password))
            {
                LoggingUtils.LogInfo($"Intento de login fallido: contraseña incorrecta - {loginDto.email}", nameof(AuthService));
                return null;
            }

            LoggingUtils.LogInfo($"Login exitoso: {user.email}", nameof(AuthService));

            // Generar token JWT
            var token = _tokenService.GenerateJwtToken(user);

            // Mapear a DTO y retornar
            return MappingUtils.MapToUserDtoWithToken(user, token);
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

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            // Calcular el hash de la contraseña ingresada
            string inputHash = HashPassword(inputPassword);

            // Comparar con el hash almacenado
            return string.Equals(inputHash, storedHash, StringComparison.OrdinalIgnoreCase);
        }

        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                user_id = user.user_id,
                first_name = user.first_name,
                last_name = user.last_name,
                email = user.email,
                is_active = user.is_active,
                phone = user.phone,
                has_image = user.image != null && user.image.Length > 0,
                Token = null
            };
        }
    }
}
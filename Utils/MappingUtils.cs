using jool_backend.Models;
using jool_backend.DTOs;
using System.Collections.Generic;
using System.Linq;

namespace jool_backend.Utils
{
    public static class MappingUtils
    {
        /// <summary>
        /// Mapea un objeto User a UserDto
        /// </summary>
        /// <param name="user">Objeto User a mapear</param>
        /// <returns>UserDto mapeado</returns>
        public static UserDto MapToUserDto(User user)
        {
            if (user == null) return null;
            
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

        /// <summary>
        /// Mapea un objeto User a UserDto incluyendo un token
        /// </summary>
        /// <param name="user">Objeto User a mapear</param>
        /// <param name="token">Token a incluir</param>
        /// <returns>UserDto mapeado con token</returns>
        public static UserDto MapToUserDtoWithToken(User user, TokenDto token)
        {
            var userDto = MapToUserDto(user);
            if (userDto != null)
            {
                userDto.Token = token;
            }
            return userDto;
        }

        /// <summary>
        /// Crea un diccionario de respuesta para la autenticación de Microsoft
        /// </summary>
        /// <param name="user">Objeto User</param>
        /// <param name="token">Objeto TokenDto</param>
        /// <returns>Diccionario con los datos de autenticación</returns>
        public static Dictionary<string, object> CreateMicrosoftAuthResponse(User user, TokenDto token)
        {
            return new Dictionary<string, object>
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
        }
    }
} 
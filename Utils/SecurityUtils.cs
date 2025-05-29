using System;
using System.Security.Cryptography;
using System.Text;

namespace jool_backend.Utils
{
    public static class SecurityUtils
    {
        /// <summary>
        /// Genera un hash SHA256 para una contraseña
        /// </summary>
        /// <param name="password">Contraseña a hashear</param>
        /// <returns>String hexadecimal del hash</returns>
        public static string HashPassword(string password)
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

        /// <summary>
        /// Verifica si una contraseña coincide con un hash almacenado
        /// </summary>
        /// <param name="inputPassword">Contraseña ingresada</param>
        /// <param name="storedHash">Hash almacenado</param>
        /// <returns>True si la contraseña coincide, false en caso contrario</returns>
        public static bool VerifyPassword(string inputPassword, string storedHash)
        {
            // Calcular el hash de la contraseña ingresada
            string inputHash = HashPassword(inputPassword);

            // Comparar con el hash almacenado
            return string.Equals(inputHash, storedHash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Genera una contraseña aleatoria
        /// </summary>
        /// <returns>Contraseña aleatoria como string</returns>
        public static string GenerateRandomPassword()
        {
            return Guid.NewGuid().ToString();
        }
    }
} 
using System;

namespace jool_backend.Utils
{
    public static class LoggingUtils
    {
        /// <summary>
        /// Registra un mensaje informativo en la consola
        /// </summary>
        /// <param name="message">Mensaje a registrar</param>
        /// <param name="source">Nombre de la clase o componente origen</param>
        public static void LogInfo(string message, string source = null)
        {
            string logMessage = FormatLogMessage(message, source, "INFO");
            Console.WriteLine(logMessage);
        }

        /// <summary>
        /// Registra un mensaje de error en la consola
        /// </summary>
        /// <param name="message">Mensaje de error</param>
        /// <param name="source">Nombre de la clase o componente origen</param>
        public static void LogError(string message, string source = null)
        {
            string logMessage = FormatLogMessage(message, source, "ERROR");
            Console.WriteLine(logMessage);
        }

        /// <summary>
        /// Registra una excepción completa en la consola
        /// </summary>
        /// <param name="ex">Excepción a registrar</param>
        /// <param name="source">Nombre de la clase o componente origen</param>
        /// <param name="additionalMessage">Mensaje adicional opcional</param>
        public static void LogException(Exception ex, string source = null, string additionalMessage = null)
        {
            string message = additionalMessage != null 
                ? $"{additionalMessage}: {ex.Message}" 
                : ex.Message;
                
            string logMessage = FormatLogMessage(message, source, "ERROR");
            Console.WriteLine(logMessage);
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"InnerException: {ex.InnerException.Message}");
            }
        }

        private static string FormatLogMessage(string message, string source, string level)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            return source != null 
                ? $"[{timestamp}] [{level}] [{source}] {message}" 
                : $"[{timestamp}] [{level}] {message}";
        }
    }
} 
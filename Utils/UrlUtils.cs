using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;

namespace jool_backend.Utils
{
    public static class UrlUtils
    {
        /// <summary>
        /// Obtiene la URL de error para redireccionamiento en caso de fallo de autenticación
        /// </summary>
        /// <param name="configuration">Objeto de configuración</param>
        /// <param name="request">Objeto HttpRequest actual</param>
        /// <param name="errorMessage">Mensaje de error a incluir</param>
        /// <returns>URL completa para redireccionamiento</returns>
        public static string GetMicrosoftErrorRedirectUrl(IConfiguration configuration, HttpRequest request, string errorMessage)
        {
            string frontendErrorUrl = configuration["Authentication:Microsoft:FrontendErrorUrl"] ?? 
                                     $"{request.Scheme}://{request.Host}/auth/login-error";
            
            return $"{frontendErrorUrl}?error={Uri.EscapeDataString(errorMessage)}";
        }

        /// <summary>
        /// Obtiene la URL de éxito para redireccionamiento tras autenticación exitosa
        /// </summary>
        /// <param name="configuration">Objeto de configuración</param>
        /// <param name="request">Objeto HttpRequest actual</param>
        /// <param name="customRedirectUrl">URL personalizada (opcional)</param>
        /// <returns>URL completa para redireccionamiento</returns>
        public static string GetMicrosoftSuccessRedirectUrl(IConfiguration configuration, HttpRequest request, string customRedirectUrl = null)
        {
            return customRedirectUrl ?? 
                   configuration["Authentication:Microsoft:FrontendCallbackUrl"] ?? 
                   $"{request.Scheme}://{request.Host}/auth/login-success";
        }

        /// <summary>
        /// Extrae y verifica una URL de redirección personalizada almacenada en la sesión
        /// </summary>
        /// <param name="httpContext">Contexto HTTP actual</param>
        /// <returns>URL personalizada o null si no existe</returns>
        public static string ExtractCustomRedirectUrl(HttpContext httpContext)
        {
            string customRedirectUrl = null;
            if (httpContext.Session.TryGetValue("MicrosoftAuthRedirectUrl", out var redirectUrlBytes))
            {
                customRedirectUrl = System.Text.Encoding.UTF8.GetString(redirectUrlBytes);
                httpContext.Session.Remove("MicrosoftAuthRedirectUrl");
            }
            
            return customRedirectUrl;
        }
        
        /// <summary>
        /// Obtiene la URL real de redirección almacenada en la sesión
        /// </summary>
        /// <param name="httpContext">Contexto HTTP actual</param>
        /// <returns>URL real o null si no existe</returns>
        public static string GetRealRedirectUri(HttpContext httpContext)
        {
            string realRedirectUri = null;
            if (httpContext.Session.TryGetValue("RealRedirectUri", out var redirectUriBytes))
            {
                realRedirectUri = System.Text.Encoding.UTF8.GetString(redirectUriBytes);
                // No eliminamos la URL real de la sesión para poder usarla en solicitudes subsiguientes
            }
            
            return realRedirectUri;
        }
    }
} 
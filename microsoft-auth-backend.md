# Modificaciones en el Backend para Simplificar la Autenticación Microsoft

## Objetivo

Modificar el backend para que redirija al frontend con los datos de autenticación de forma segura y fácil de consumir.

## Cambios Requeridos

### 1. Modificar el Endpoint `/auth/microsoft-callback`

```csharp
[HttpGet("microsoft-callback")]
public async Task<IActionResult> MicrosoftCallback(string code, string state = null)
{
    try
    {
        // 1. Procesar el código de autorización y obtener los datos de usuario
        var (user, token) = await _microsoftAuthService.ProcessAuthorizationCodeAsync(code);
        
        // 2. Construir objeto de respuesta
        var authResult = new
        {
            user_id = user.Id,
            first_name = user.FirstName,
            last_name = user.LastName,
            email = user.Email,
            is_active = user.IsActive,
            phone = user.Phone,
            has_image = user.Image != null && user.Image.Length > 0,
            token = new
            {
                accessToken = token.AccessToken,
                expiresAt = token.ExpiresAt
            }
        };

        // 3. Serializar los datos y codificar para URL
        var serializedData = JsonSerializer.Serialize(authResult);
        var encodedData = Uri.EscapeDataString(serializedData);
        
        // 4. Obtener URL de redirección del frontend desde la configuración
        string frontendCallbackUrl = _configuration["Authentication:Microsoft:FrontendCallbackUrl"];
        
        // 5. Redirigir al frontend con los datos en el fragmento hash (#)
        return Redirect($"{frontendCallbackUrl}#{encodedData}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error en el callback de Microsoft");
        return Redirect($"{_configuration["Authentication:Microsoft:FrontendErrorUrl"]}?error={Uri.EscapeDataString("Error en la autenticación con Microsoft")}");
    }
}
```

### 2. Actualizar el Archivo `appsettings.json`

Agregar las siguientes configuraciones:

```json
{
  "Authentication": {
    "Microsoft": {
      "ClientId": "tu-client-id",
      "ClientSecret": "tu-client-secret",
      "TenantId": "common",
      "FrontendCallbackUrl": "https://tu-frontend.com/auth/callback",
      "FrontendErrorUrl": "https://tu-frontend.com/auth/error"
    }
  }
}
```

### 3. Modificar el Endpoint `/auth/login-microsoft`

```csharp
[HttpGet("login-microsoft")]
public IActionResult LoginWithMicrosoft([FromQuery] string redirectUrl = null)
{
    try
    {
        // 1. Guardar URL de redirección personalizada si se proporciona
        string callbackUrl = string.IsNullOrEmpty(redirectUrl) 
            ? _configuration["Authentication:Microsoft:FrontendCallbackUrl"]
            : redirectUrl;
            
        // 2. Almacenar en la configuración temporal
        HttpContext.Session.SetString("MicrosoftAuthRedirectUrl", callbackUrl);
        
        // 3. Generar URL de autorización de Microsoft
        string authorizationUrl = _microsoftAuthService.GetAuthorizationUrl();
        
        // 4. Redirigir al usuario a la página de login de Microsoft
        return Redirect(authorizationUrl);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error generando URL de autorización Microsoft");
        return BadRequest(new { error = "Error generando URL de autorización" });
    }
}
```

## Instrucciones para el Frontend

Con estos cambios, el frontend solo necesita:

1. Redirigir al usuario a `/auth/login-microsoft`
2. Tener una página de callback que extraiga los datos del fragmento hash (URL después del `#`)

### Ejemplo de Página de Callback en Next.js

```jsx
// pages/auth/callback.js
import { useEffect } from 'react';
import { useRouter } from 'next/router';
import Cookies from 'js-cookie';

export default function AuthCallback() {
  const router = useRouter();
  
  useEffect(() => {
    // Función para extraer y procesar los datos del hash
    const processAuthData = () => {
      if (typeof window !== 'undefined' && window.location.hash) {
        try {
          // 1. Obtener datos del hash (quitar el # inicial)
          const encodedData = window.location.hash.substring(1);
          
          // 2. Decodificar y parsear JSON
          const jsonStr = decodeURIComponent(encodedData);
          const authData = JSON.parse(jsonStr);
          
          // 3. Guardar token en cookies
          Cookies.set('auth_token', authData.token.accessToken, {
            expires: new Date(authData.token.expiresAt),
            secure: process.env.NODE_ENV === 'production',
            sameSite: 'Lax'
          });
          
          // 4. Guardar datos de expiración
          Cookies.set('auth_expiry', authData.token.expiresAt);
          
          // 5. Opcionalmente guardar datos del usuario
          localStorage.setItem('user_data', JSON.stringify({
            id: authData.user_id,
            email: authData.email,
            firstName: authData.first_name,
            lastName: authData.last_name,
            isActive: authData.is_active,
            hasImage: authData.has_image
          }));
          
          // 6. Redirigir al dashboard o página principal
          router.replace('/dashboard');
        } catch (error) {
          console.error('Error procesando datos de autenticación:', error);
          router.replace('/login?error=auth_failed');
        }
      }
    };
    
    processAuthData();
  }, [router]);
  
  return (
    <div className="auth-processing">
      <div className="spinner"></div>
      <p>Completando autenticación...</p>
    </div>
  );
}
```

## Ventajas de esta Implementación

1. **Seguridad mejorada**: Los tokens se transmiten a través del fragmento hash, que no se envía al servidor
2. **Facilidad de uso**: El frontend solo necesita redirigir y procesar los datos del hash
3. **Experiencia de usuario fluida**: No hay necesidad de ventanas emergentes o iframes
4. **Simplicidad**: El flujo de autenticación es claro y directo
5. **Compatibilidad**: Funciona bien con Next.js y otras frameworks SPA

## Notas de Seguridad

- Asegúrate de que todas las comunicaciones sean a través de HTTPS
- Valida el origen de las redirecciones para prevenir ataques de redirección
- Considera implementar CSRF tokens para mayor seguridad
- Asegúrate de que los tokens tengan una expiración adecuada
- Implementa CORS correctamente para permitir solo los orígenes necesarios 
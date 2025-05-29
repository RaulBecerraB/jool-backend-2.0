# Tutorial: Implementación de Autenticación con Microsoft en ASP.NET Core

Este tutorial explica paso a paso cómo implementar la autenticación con Microsoft en una aplicación ASP.NET Core, basado en la implementación realizada en el proyecto Jool.

## Índice
1. [Requisitos Previos](#requisitos-previos)
2. [Registro de la Aplicación en Azure](#registro-de-la-aplicación-en-azure)
3. [Configuración del Backend](#configuración-del-backend)
4. [Implementación del Flujo de Autenticación](#implementación-del-flujo-de-autenticación)
5. [Integración con el Frontend](#integración-con-el-frontend)
6. [Solución de Problemas Comunes](#solución-de-problemas-comunes)

## Requisitos Previos

- Una cuenta de Azure (puedes crear una cuenta gratuita)
- .NET 6.0 o superior
- Un proyecto ASP.NET Core existente
- Conocimientos básicos de OAuth 2.0

## Registro de la Aplicación en Azure

1. Inicia sesión en el [Portal de Azure](https://portal.azure.com)
2. Navega a **Azure Active Directory** > **Registros de aplicaciones**
3. Haz clic en **Nuevo registro**
4. Completa el formulario:
   - **Nombre**: Nombre de tu aplicación (ej. "Jool Auth")
   - **Tipos de cuenta compatibles**: "Cuentas en cualquier directorio organizativo y cuentas personales de Microsoft"
   - **URI de redirección**: Selecciona "Web" y agrega `https://tu-dominio.com/auth/microsoft-callback` (para producción) y `http://localhost:8080/auth/microsoft-callback` (para desarrollo)
5. Haz clic en **Registrar**

Después del registro:

1. Anota el **ID de aplicación (cliente)** - este será tu `MS_CLIENT_ID`
2. Navega a **Certificados y secretos**
3. Crea un **Nuevo secreto de cliente**
4. Anota el **Valor** del secreto creado - este será tu `MS_CLIENT_SECRET`
5. Navega a **Permisos de API** y agrega los siguientes permisos:
   - Microsoft Graph: `User.Read` (delegado)

## Configuración del Backend

### 1. Instalar paquetes NuGet necesarios

```bash
dotnet add package DotNetEnv
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### 2. Configurar appsettings.json

Añade la siguiente sección a tu archivo `appsettings.json`:

```json
"Authentication": {
  "Microsoft": {
    "ClientId": "",
    "ClientSecret": "",
    "TenantId": "common",
    "FrontendCallbackUrl": "http://localhost:3000/auth/callback",
    "FrontendErrorUrl": "http://localhost:3000/auth/error",
    "Scope": "https://graph.microsoft.com/user.read"
  }
}
```

### 3. Configurar variables de entorno

Crea un archivo `.env` en la raíz del proyecto:

```
MS_CLIENT_ID=tu-client-id-de-azure
MS_CLIENT_SECRET=tu-client-secret-de-azure
```

### 4. Configurar Program.cs

Actualiza `Program.cs` para cargar las variables de entorno y configurar la sesión:

```csharp
// Cargar variables de entorno desde el archivo .env
try
{
    Env.Load();
    Console.WriteLine("Variables de entorno cargadas desde .env");
    
    // Verificar si las variables críticas están presentes
    var clientId = Environment.GetEnvironmentVariable("MS_CLIENT_ID");
    var clientSecret = Environment.GetEnvironmentVariable("MS_CLIENT_SECRET");
    
    Console.WriteLine($"MS_CLIENT_ID está {(string.IsNullOrEmpty(clientId) ? "AUSENTE" : "presente")}");
    Console.WriteLine($"MS_CLIENT_SECRET está {(string.IsNullOrEmpty(clientSecret) ? "AUSENTE" : "presente")}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error al cargar variables de entorno: {ex.Message}");
}

var builder = WebApplication.CreateBuilder(args);

// Configuración de sesión
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Resto de la configuración...

// Registrar servicios necesarios
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<MicrosoftAuthService>();

var app = builder.Build();

// Middleware
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
```

## Implementación del Flujo de Autenticación

### 1. Crear TokenService

Crea `Services/TokenService.cs` para manejar la generación de tokens JWT:

```csharp
public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TokenDto GenerateJwtToken(User user)
    {
        // Obtener la configuración de JWT
        var jwtSection = _configuration.GetSection("JWT");
        var secretKey = jwtSection["SecretKey"];
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var expirationMinutes = int.Parse(jwtSection["DurationInMinutes"]);

        // Configurar las reclamaciones del token
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.user_id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("first_name", user.first_name),
            new Claim("last_name", user.last_name)
        };

        // Configurar las credenciales de firma
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Calcular fecha de expiración
        var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

        // Crear el token
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        // Serializar el token a string
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenString = tokenHandler.WriteToken(token);

        // Devolver el DTO del token
        return new TokenDto
        {
            AccessToken = tokenString,
            ExpiresAt = expires
        };
    }
}
```

### 2. Crear TokenDto

Crea `DTOs/TokenDto.cs` para representar la respuesta del token:

```csharp
public class TokenDto
{
    public string AccessToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

### 3. Crear MicrosoftAuthService

Crea `Services/MicrosoftAuthService.cs` para manejar la autenticación con Microsoft:

```csharp
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

    // Método para generar la URL de autorización de Microsoft
    public string GetAuthorizationUrl(string redirectUrl = null)
    {
        try
        {
            // 1. Obtener la configuración
            string clientId = Environment.GetEnvironmentVariable("MS_CLIENT_ID");
            
            // Si no está en las variables de entorno, intentar con la configuración
            if (string.IsNullOrEmpty(clientId))
            {
                clientId = _configuration["Authentication:Microsoft:ClientId"];
                Console.WriteLine($"ClientId obtenido de configuración: {clientId}");
            }
            else
            {
                Console.WriteLine($"ClientId obtenido de variable de entorno");
            }
            
            // Verificar que tengamos un clientId
            if (string.IsNullOrEmpty(clientId))
            {
                throw new Exception("No se encontró el ClientId en la configuración ni en las variables de entorno");
            }
            
            Console.WriteLine($"Usando ClientId: {clientId}");
            
            string scope = _configuration["Authentication:Microsoft:Scope"];
            
            // Si no está en la configuración, usar el valor predeterminado
            if (string.IsNullOrEmpty(scope))
            {
                scope = "https://graph.microsoft.com/user.read";
            }
            
            // 2. Configurar la URL de redirección
            var request = _httpContextAccessor.HttpContext.Request;
            var callbackUri = $"{request.Scheme}://{request.Host}/auth/microsoft-callback";
            
            // 3. Generar la URL de autorización
            var authorizationUrl = $"https://login.microsoftonline.com/common/oauth2/v2.0/authorize" + 
                $"?client_id={clientId}" +
                $"&response_type=code" +
                $"&redirect_uri={Uri.EscapeDataString(callbackUri)}" +
                $"&response_mode=query" +
                $"&scope={Uri.EscapeDataString(scope)}";
            
            if (!string.IsNullOrEmpty(redirectUrl))
            {
                // Guardar la URL de redirección personalizada en la sesión
                if (_httpContextAccessor.HttpContext.Session != null)
                {
                    _httpContextAccessor.HttpContext.Session.SetString("MicrosoftAuthRedirectUrl", redirectUrl);
                }
            }
            
            return authorizationUrl;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetAuthorizationUrl: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw new Exception($"Error generando URL de autorización: {ex.Message}", ex);
        }
    }

    // Método para procesar el código de autorización y devolver el usuario y token
    public async Task<(User User, TokenDto Token)> ProcessAuthorizationCodeAsync(string code)
    {
        try
        {
            // 1. Obtener la configuración de Microsoft
            string clientId = Environment.GetEnvironmentVariable("MS_CLIENT_ID");
            
            // Si no está en las variables de entorno, intentar con la configuración
            if (string.IsNullOrEmpty(clientId))
            {
                clientId = _configuration["Authentication:Microsoft:ClientId"];
                Console.WriteLine($"ClientId obtenido de configuración: {clientId}");
            }
            else
            {
                Console.WriteLine($"ClientId obtenido de variable de entorno");
            }
            
            // Verificar que tengamos un clientId
            if (string.IsNullOrEmpty(clientId))
            {
                throw new Exception("No se encontró el ClientId en la configuración ni en las variables de entorno");
            }
            
            string clientSecret = Environment.GetEnvironmentVariable("MS_CLIENT_SECRET");
            
            // Si no está en las variables de entorno, intentar con la configuración
            if (string.IsNullOrEmpty(clientSecret))
            {
                clientSecret = _configuration["Authentication:Microsoft:ClientSecret"];
                Console.WriteLine($"ClientSecret obtenido de configuración");
            }
            else
            {
                Console.WriteLine($"ClientSecret obtenido de variable de entorno");
            }
            
            // Verificar que tengamos un clientSecret
            if (string.IsNullOrEmpty(clientSecret))
            {
                throw new Exception("No se encontró el ClientSecret en la configuración ni en las variables de entorno");
            }
            
            // 2. Configurar la URL de redirección
            var request = _httpContextAccessor.HttpContext.Request;
            var redirectUri = $"{request.Scheme}://{request.Host}/auth/microsoft-callback";
            
            // 3. Intercambiar el código por un token de acceso
            var httpClient = _httpClientFactory.CreateClient();
            
            var tokenRequestParams = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            };

            var tokenRequestContent = new FormUrlEncodedContent(tokenRequestParams);
            
            var tokenResponse = await httpClient.PostAsync(
                "https://login.microsoftonline.com/common/oauth2/v2.0/token", 
                tokenRequestContent);

            var responseContent = await tokenResponse.Content.ReadAsStringAsync();
            
            if (!tokenResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error al obtener token: {responseContent}");
                throw new Exception($"Error al obtener token: {responseContent}");
            }
            
            // 4. Parsear la respuesta JSON para obtener el token
            var tokenData = JsonDocument.Parse(responseContent);
            var accessToken = tokenData.RootElement.GetProperty("access_token").GetString();
            
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception("Token de acceso vacío");
            }

            // 5. Obtener información del usuario con el token
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            
            var userResponse = await httpClient.GetAsync("https://graph.microsoft.com/v1.0/me");
            var userResponseContent = await userResponse.Content.ReadAsStringAsync();
            
            if (!userResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error al obtener información del usuario: {userResponseContent}");
                throw new Exception($"Error al obtener información del usuario: {userResponseContent}");
            }
            
            // 6. Parsear los datos del usuario
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
            
            string firstName = userData.RootElement.TryGetProperty("givenName", out var givenNameProperty) 
                ? givenNameProperty.GetString() : "Usuario";
            
            string lastName = userData.RootElement.TryGetProperty("surname", out var surnameProperty)
                ? surnameProperty.GetString() : "Microsoft";
            
            if (string.IsNullOrEmpty(email))
            {
                throw new Exception("No se pudo obtener el email del usuario");
            }
            
            Console.WriteLine($"Datos obtenidos de Microsoft: Email={email}, Nombre={firstName} {lastName}");
            
            // 7. Buscar o crear usuario
            var existingUser = await _userRepository.GetUserByEmailAsync(email);
            
            if (existingUser == null)
            {
                Console.WriteLine("Creando nuevo usuario...");
                
                // Crear un nuevo usuario con una contraseña aleatoria
                string randomPassword = Guid.NewGuid().ToString();
                
                var newUser = new User
                {
                    email = email,
                    first_name = firstName ?? "Usuario",
                    last_name = lastName ?? "Microsoft",
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
            
            // 8. Generar JWT
            var token = _tokenService.GenerateJwtToken(existingUser);
            Console.WriteLine("Token JWT generado correctamente");
            
            // 9. Devolver usuario y token
            return (existingUser, token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en ProcessAuthorizationCodeAsync: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw new Exception($"Error procesando código de autorización: {ex.Message}", ex);
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
}
```

### 4. Crear AuthController

Crea `Controllers/AuthController.cs` con los endpoints para la autenticación de Microsoft:

```csharp
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
}
```

## Integración con el Frontend

Para integrar con un frontend (como Next.js), crea un servicio de autenticación que maneje:

1. El inicio del flujo de autenticación
2. El procesamiento del callback
3. El almacenamiento de tokens

Ejemplo para un cliente Next.js:

```javascript
// services/authService.js
import axios from 'axios';
import Cookies from 'js-cookie';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
const TOKEN_COOKIE = 'auth_token';
const EXPIRY_COOKIE = 'auth_expiry';
const USER_DATA_KEY = 'user_data';

export const authService = {
  // Inicia el flujo de autenticación con Microsoft
  loginWithMicrosoft: async () => {
    try {
      const response = await axios.get(`${API_URL}/auth/login-microsoft`);
      const { redirect_url } = response.data;
      
      // Redirige al usuario a la página de login de Microsoft
      window.location.href = redirect_url;
    } catch (error) {
      console.error('Error iniciando autenticación con Microsoft:', error);
      throw error;
    }
  },

  // Procesa y guarda los datos de autenticación del hash fragment
  processAuthHash: () => {
    if (typeof window === 'undefined' || !window.location.hash) return null;
    
    try {
      // Extraer y decodificar los datos del hash (quitar el # inicial)
      const encodedData = window.location.hash.substring(1);
      const jsonStr = decodeURIComponent(encodedData);
      const authData = JSON.parse(jsonStr);
      
      // Guardar token en cookies
      Cookies.set(TOKEN_COOKIE, authData.token.accessToken, {
        expires: new Date(authData.token.expiresAt),
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'Lax'
      });
      
      // Guardar fecha de expiración
      Cookies.set(EXPIRY_COOKIE, authData.token.expiresAt);
      
      // Guardar datos del usuario en localStorage
      const userData = {
        id: authData.user_id,
        email: authData.email,
        firstName: authData.first_name,
        lastName: authData.last_name,
        isActive: authData.is_active,
        hasImage: authData.has_image,
        phone: authData.phone
      };
      
      localStorage.setItem(USER_DATA_KEY, JSON.stringify(userData));
      
      // Limpiar el hash de la URL para evitar que los datos queden expuestos
      window.history.replaceState(null, '', window.location.pathname);
      
      return userData;
    } catch (error) {
      console.error('Error procesando datos de autenticación:', error);
      return null;
    }
  },

  // Verifica si el usuario está autenticado
  isAuthenticated: () => {
    if (typeof window === 'undefined') return false;
    
    const token = Cookies.get(TOKEN_COOKIE);
    const expiry = Cookies.get(EXPIRY_COOKIE);
    
    if (!token || !expiry) return false;
    
    // Verificar si el token ha expirado
    return new Date(expiry) > new Date();
  },

  // Cierra la sesión
  logout: () => {
    Cookies.remove(TOKEN_COOKIE);
    Cookies.remove(EXPIRY_COOKIE);
    localStorage.removeItem(USER_DATA_KEY);
  }
};
```

## Solución de Problemas Comunes

### 1. Error de aplicación no encontrada

```
AADSTS700016: Application with identifier 'ID_INCORRECTO' was not found in the directory
```

**Solución**: Verifica que el ClientId sea el correcto y que las variables de entorno se estén cargando.

### 2. Error de URL de redirección

```
AADSTS50011: The reply URL specified in the request does not match the reply URLs configured
```

**Solución**: Asegúrate de que la URL de redirección en el código coincida exactamente con la configurada en Azure, incluyendo el protocolo (http/https).

### 3. Error de análisis de JSON

Si obtienes errores al analizar la respuesta JSON, verifica:
- Que no estés usando tipos anónimos que puedan causar problemas de serialización
- Que las propiedades en el objeto JSON coincidan con las propiedades esperadas en el frontend

### 4. Problemas con variables de entorno

Si las variables de entorno no se cargan correctamente, intenta:
- Verificar que el paquete DotNetEnv esté instalado y funcionando
- Asegurarte de que el archivo `.env` esté en la raíz del proyecto
- Comprobar que el archivo `.env` no esté en `.gitignore` y se haya incluido en la publicación

## Conclusión

La implementación de autenticación con Microsoft en ASP.NET Core es un proceso que combina OAuth 2.0, manejo de tokens JWT y gestión de sesiones. Con el enfoque descrito en este tutorial, obtendrás un sistema robusto que:

1. Inicia el flujo de autenticación redirigiendo al usuario a Microsoft
2. Procesa el callback de Microsoft y obtiene información del usuario
3. Crea o actualiza el usuario en tu base de datos
4. Genera un token JWT para la autenticación continua
5. Devuelve los datos del usuario y el token al frontend de manera segura

Este enfoque proporciona una autenticación segura mientras mantiene una experiencia de usuario fluida. 
# Configuración de Autenticación Microsoft

Este documento explica cómo configurar correctamente la autenticación con Microsoft en la aplicación.

## Prerequisitos

1. Debes tener una aplicación registrada en el portal de Azure AD:
   - Ir a [portal.azure.com](https://portal.azure.com)
   - Navegar a "Azure Active Directory" > "Registros de aplicaciones"
   - Crear una nueva aplicación o usar una existente

## Configuración de Variables de Entorno

Existen dos formas de configurar las credenciales de Microsoft:

### 1. Usando Variables de Entorno (Recomendado)

Crea un archivo `.env` en la raíz del proyecto con el siguiente contenido:

```
MS_CLIENT_ID=tu-client-id-de-azure
MS_CLIENT_SECRET=tu-client-secret-de-azure
```

Reemplaza `tu-client-id-de-azure` y `tu-client-secret-de-azure` con los valores reales de tu aplicación en Azure.

### 2. Usando appsettings.json

Alternativamente, puedes configurar los valores directamente en el archivo `appsettings.json`:

```json
"Authentication": {
  "Microsoft": {
    "ClientId": "tu-client-id-de-azure",
    "ClientSecret": "tu-client-secret-de-azure",
    "TenantId": "common",
    "FrontendCallbackUrl": "http://localhost:3000/auth/callback",
    "FrontendErrorUrl": "http://localhost:3000/auth/error",
    "Scope": "https://graph.microsoft.com/user.read"
  }
}
```

## Configuración de Redirección en Azure

Es crucial que configures correctamente las URLs de redirección en tu aplicación de Azure:

1. En el portal de Azure, ve a tu aplicación registrada
2. Navega a "Autenticación"
3. En "URI de redirección", añade:
   - `https://tu-dominio.com/auth/microsoft-callback` (producción)
   - `http://localhost:8080/auth/microsoft-callback` (desarrollo)
4. Asegúrate de seleccionar el tipo "Web" para estas redirecciones
5. Guarda los cambios

## Verificación de la Configuración

Para verificar que las variables de entorno están correctamente configuradas, puedes iniciar la aplicación y revisar los logs. Deberías ver mensajes como:

```
ClientId obtenido de variable de entorno
Usando ClientId: [tu-client-id]
```

## Solución de Problemas Comunes

### Error: "Application with identifier was not found in the directory"

Este error indica que el ClientId no es válido o no pertenece al directorio (tenant) al que estás intentando acceder.

**Solución:**
1. Verifica que estás usando el ClientId correcto
2. Asegúrate de que la aplicación está registrada en el directorio correcto
3. Confirma que las variables de entorno se están cargando correctamente

### Error: "The reply URL specified in the request does not match the reply URLs configured for the application"

Este error ocurre cuando la URL de redirección que usa tu aplicación no coincide con las configuradas en Azure.

**Solución:**
1. Verifica la URL exacta que estás usando (incluyendo http/https)
2. Añade esta URL a la lista de redirecciones permitidas en Azure
3. Ten en cuenta que Azure distingue entre mayúsculas y minúsculas en las URLs

### Error: "AADSTS7000215: Invalid client secret provided"

Este error indica que el ClientSecret no es válido.

**Solución:**
1. Genera un nuevo secreto en el portal de Azure
2. Actualiza la variable de entorno o el appsettings.json con el nuevo valor
3. Recuerda que los secretos de cliente tienen fecha de expiración 
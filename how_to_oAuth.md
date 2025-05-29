# Autenticación con Microsoft en Next.js (Popup)

Este proyecto implementa autenticación con Microsoft Identity en una aplicación Next.js utilizando una ventana emergente (popup) para manejar la respuesta JSON directa del backend.

## Requisitos Previos

- Node.js 14.x o superior
- npm o yarn
- Una aplicación Next.js existente
- Un backend ASP.NET Core con los endpoints de autenticación Microsoft configurados

## Instalación

1. Instala las dependencias necesarias:

```bash
npm install js-cookie
# o
yarn add js-cookie
```

2. Copia los siguientes archivos a tu proyecto:

- `services/authService.js` - Servicio de autenticación
- `components/MicrosoftLoginButton.jsx` - Componente de botón de login
- `public/auth-callback.html` - Página de callback para la ventana emergente
- `pages/login.jsx` (opcional) - Ejemplo de página de login

## Estructura de Archivos

```
📁 tu-proyecto-next
├── 📁 components
│   └── 📄 MicrosoftLoginButton.jsx
├── 📁 pages
│   └── 📄 login.jsx
├── 📁 public
│   └── 📄 auth-callback.html
├── 📁 services
│   └── 📄 authService.js
```

## Configuración

1. Configura la variable de entorno para la URL de tu API:

```
# .env.local
NEXT_PUBLIC_API_URL=https://tu-api.com
```

2. Asegúrate de que la API tenga el endpoint `/auth/login-microsoft` configurado para devolver la URL de login de Microsoft.

## Uso

### Integración Básica

```jsx
import MicrosoftLoginButton from '../components/MicrosoftLoginButton';

export default function TuComponente() {
  const handleSuccess = (userData) => {
    console.log('Usuario autenticado:', userData);
    // Redirigir o actualizar el estado de la aplicación
  };
  
  const handleError = (error) => {
    console.error('Error de autenticación:', error);
    // Mostrar mensaje de error
  };

  return (
    <div>
      <MicrosoftLoginButton 
        onSuccess={handleSuccess}
        onError={handleError}
        text="Iniciar sesión con Microsoft"
      />
    </div>
  );
}
```

### Autenticación Programática

También puedes iniciar la autenticación programáticamente:

```jsx
import { useEffect } from 'react';
import authService from '../services/authService';

export default function TuComponente() {
  const iniciarLogin = async () => {
    try {
      const userData = await authService.loginWithMicrosoftPopup();
      console.log('Usuario autenticado:', userData);
    } catch (error) {
      console.error('Error de autenticación:', error);
    }
  };

  return (
    <button onClick={iniciarLogin}>
      Iniciar sesión
    </button>
  );
}
```

### Verificar Autenticación

Para verificar si el usuario está autenticado:

```jsx
import { useEffect, useState } from 'react';
import authService from '../services/authService';

export default function Dashboard() {
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [userData, setUserData] = useState(null);
  
  useEffect(() => {
    // Verificar si el usuario está autenticado
    const auth = authService.isAuthenticated();
    setIsLoggedIn(auth);
    
    if (auth) {
      // Obtener datos del usuario
      const user = authService.getUserData();
      setUserData(user);
    }
  }, []);

  if (!isLoggedIn) {
    return <div>No has iniciado sesión</div>;
  }

  return (
    <div>
      <h1>Bienvenido, {userData?.first_name}</h1>
      {/* Contenido del dashboard */}
    </div>
  );
}
```

## Cómo Funciona

1. El usuario hace clic en el botón "Iniciar sesión con Microsoft"
2. Se abre una ventana emergente con la URL `/auth/login-microsoft`
3. El backend redirige al usuario a la página de login de Microsoft
4. Después de la autenticación, Microsoft redirige al endpoint `/auth/microsoft-callback` del backend
5. El backend procesa la autenticación y devuelve los datos de usuario y token en formato JSON
6. La página `auth-callback.html` en la ventana emergente extrae este JSON y lo envía a la ventana principal
7. La ventana principal recibe los datos, guarda el token y cierra la ventana emergente
8. El usuario queda autenticado en la aplicación

## Solución de Problemas

### Ventana emergente bloqueada

Si el navegador bloquea la ventana emergente, asegúrate de:
- Iniciar la ventana emergente como respuesta a una acción del usuario (clic en botón)
- Agregar tu dominio a la lista de sitios permitidos en el navegador

### No se reciben datos de autenticación

Si la ventana emergente no recibe los datos de autenticación:
1. Verifica que el backend está devolviendo un JSON válido
2. Comprueba la consola del navegador en la ventana emergente para ver errores
3. Asegúrate de que `auth-callback.html` está en la carpeta `public`

### Problemas de CORS

Si hay problemas de CORS:
1. Configura el backend para permitir solicitudes desde tu dominio
2. Verifica que estás usando el mismo origen en la comunicación entre ventanas

## Personalización

### Estilos del Botón

Puedes personalizar el botón pasando un className:

```jsx
<MicrosoftLoginButton 
  className="mi-estilo-personalizado"
  text="Continuar con cuenta institucional"
/>
```

### Redireccionamiento después del Login

Modifica la función `handleLoginSuccess` para redirigir a la página deseada:

```jsx
const handleLoginSuccess = (userData) => {
  // Redirigir a una página específica
  router.push('/mi-pagina');
  
  // O redirigir de vuelta a la página anterior
  if (router.query.returnUrl) {
    router.push(router.query.returnUrl);
  } else {
    router.push('/dashboard');
  }
};
```

## Seguridad

- Asegúrate de almacenar tokens en cookies seguras (HttpOnly, Secure)
- Implementa verificación de token en el lado del servidor para rutas protegidas
- Configura correctamente la política de CORS en tu backend
- Valida el origen de los mensajes en la comunicación entre ventanas 
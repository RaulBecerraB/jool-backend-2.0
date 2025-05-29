# Guía de Integración de Autenticación Microsoft en Frontend Next.js

## Introducción

Esta guía explica cómo integrar la autenticación con Microsoft en una aplicación Next.js utilizando el backend ASP.NET Core actualizado.

## Prerrequisitos

1. Instala las dependencias necesarias:

```bash
npm install axios js-cookie
# o
yarn add axios js-cookie
```

## Implementación

### 1. Crear el Servicio de Autenticación

Crea un archivo `services/authService.js`:

```javascript
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
  },

  // Obtiene el token actual
  getToken: () => {
    return Cookies.get(TOKEN_COOKIE);
  },

  // Obtiene los datos del usuario desde localStorage
  getUserData: () => {
    if (typeof window === 'undefined') return null;
    
    const userData = localStorage.getItem(USER_DATA_KEY);
    return userData ? JSON.parse(userData) : null;
  }
};

export default authService;
```

### 2. Crear una Página de Callback

Crea un archivo `pages/auth/callback.js`:

```jsx
import { useEffect, useState } from 'react';
import { useRouter } from 'next/router';
import authService from '../../services/authService';

export default function AuthCallback() {
  const router = useRouter();
  const [error, setError] = useState('');
  
  useEffect(() => {
    // Procesar los datos de autenticación del hash fragment
    const userData = authService.processAuthHash();
    
    if (userData) {
      // Redireccionar al dashboard o página principal
      router.replace('/dashboard');
    } else {
      setError('Error procesando la autenticación. Por favor, intente nuevamente.');
    }
  }, [router]);
  
  // Si hay un error, mostrar mensaje
  if (error) {
    return (
      <div className="auth-error-container">
        <h2>Error de Autenticación</h2>
        <p>{error}</p>
        <button onClick={() => router.push('/login')}>
          Volver a intentar
        </button>
      </div>
    );
  }
  
  // Mostrar indicador de carga mientras se procesa la autenticación
  return (
    <div className="auth-loading-container">
      <div className="spinner"></div>
      <p>Completando autenticación...</p>
    </div>
  );
}
```

### 3. Crear una Página de Error

Crea un archivo `pages/auth/error.js`:

```jsx
import { useEffect, useState } from 'react';
import { useRouter } from 'next/router';

export default function AuthError() {
  const router = useRouter();
  const [errorMessage, setErrorMessage] = useState('Error durante la autenticación');
  
  useEffect(() => {
    // Obtener el mensaje de error de los query params
    if (router.isReady && router.query.error) {
      setErrorMessage(router.query.error);
    }
  }, [router.isReady, router.query]);
  
  return (
    <div className="auth-error-container">
      <h2>Error de Autenticación</h2>
      <p>{errorMessage}</p>
      <button onClick={() => router.push('/login')}>
        Volver a intentar
      </button>
    </div>
  );
}
```

### 4. Crear un Botón de Login con Microsoft

Crea un componente `components/MicrosoftLoginButton.jsx`:

```jsx
import React from 'react';
import authService from '../services/authService';

const MicrosoftLoginButton = ({ className, text = 'Iniciar sesión con Microsoft' }) => {
  const handleLogin = async () => {
    try {
      await authService.loginWithMicrosoft();
    } catch (error) {
      console.error('Error al iniciar sesión con Microsoft:', error);
      // Manejar el error según necesidades de la UI
    }
  };

  return (
    <button 
      onClick={handleLogin}
      className={className || 'ms-login-btn'}
    >
      <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 23 23">
        <path fill="#f3f3f3" d="M0 0h23v23H0z" />
        <path fill="#f35325" d="M1 1h10v10H1z" />
        <path fill="#81bc06" d="M12 1h10v10H12z" />
        <path fill="#05a6f0" d="M1 12h10v10H1z" />
        <path fill="#ffba08" d="M12 12h10v10H12z" />
      </svg>
      {text}
    </button>
  );
};

export default MicrosoftLoginButton;
```

### 5. Crear un Cliente HTTP con Interceptor para Token

Crea un archivo `utils/api.js`:

```javascript
import axios from 'axios';
import authService from '../services/authService';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

const api = axios.create({
  baseURL: API_URL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor para agregar el token a todas las peticiones
api.interceptors.request.use(
  (config) => {
    const token = authService.getToken();
    
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    
    return config;
  },
  (error) => Promise.reject(error)
);

// Interceptor para manejar errores de autenticación
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Si recibimos un 401 Unauthorized, el token ha expirado
    if (error.response && error.response.status === 401) {
      authService.logout();
      // Redirigir a la página de login
      if (typeof window !== 'undefined') {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

export default api;
```

### 6. Crear un HOC para Proteger Rutas

Crea un archivo `components/withAuth.js`:

```jsx
import { useEffect, useState } from 'react';
import { useRouter } from 'next/router';
import authService from '../services/authService';

const withAuth = (WrappedComponent) => {
  const WithAuth = (props) => {
    const router = useRouter();
    const [loading, setLoading] = useState(true);
    
    useEffect(() => {
      // Verificar autenticación
      const isAuthenticated = authService.isAuthenticated();
      
      if (!isAuthenticated) {
        // Redirigir a login si no está autenticado
        router.replace('/login');
      } else {
        setLoading(false);
      }
    }, []);
    
    if (loading) {
      return (
        <div className="auth-loading-container">
          <div className="spinner"></div>
          <p>Cargando...</p>
        </div>
      );
    }
    
    return <WrappedComponent {...props} />;
  };
  
  return WithAuth;
};

export default withAuth;
```

## Ejemplos de Uso

### Página de Login

```jsx
import { useState } from 'react';
import MicrosoftLoginButton from '../components/MicrosoftLoginButton';

export default function Login() {
  return (
    <div className="login-container">
      <h1>Iniciar Sesión</h1>
      
      <MicrosoftLoginButton 
        className="ms-btn primary-btn" 
        text="Continuar con Microsoft" 
      />
      
      {/* Otros métodos de login si los hay */}
    </div>
  );
}
```

### Página Protegida (Dashboard)

```jsx
import withAuth from '../components/withAuth';
import authService from '../services/authService';

function Dashboard() {
  const userData = authService.getUserData();
  
  return (
    <div className="dashboard-container">
      <h1>Bienvenido, {userData?.firstName}</h1>
      <div className="dashboard-content">
        {/* Contenido del dashboard */}
      </div>
    </div>
  );
}

export default withAuth(Dashboard);
```

## Pasos para Completar la Integración

1. Configura la variable de entorno en tu archivo `.env.local`:

```
NEXT_PUBLIC_API_URL=http://localhost:5000
```

2. Asegúrate de actualizar la URL del frontend en el archivo `appsettings.json` del backend:

```json
"Authentication": {
  "Microsoft": {
    "FrontendCallbackUrl": "http://tudominio.com/auth/callback",
    "FrontendErrorUrl": "http://tudominio.com/auth/error"
  }
}
```

3. Implementa las páginas y componentes descritos anteriormente.

4. Prueba el flujo completo de autenticación.

## Solución de Problemas

- **Error CORS**: Asegúrate de que el backend permita solicitudes desde el dominio de tu aplicación Next.js.
- **Redirección incorrecta**: Verifica que las URLs en `appsettings.json` sean correctas.
- **Error al procesar el hash**: Comprueba la consola del navegador para ver errores específicos.

## Ventajas de esta Implementación

1. **Seguridad mejorada**: Los tokens se transmiten a través del fragmento hash, que no se envía al servidor.
2. **Experiencia de usuario fluida**: Redirecciones automáticas y manejo de errores.
3. **Fácil mantenimiento**: Código modular y bien estructurado.
4. **Gestión de tokens centralizada**: Todo el manejo de tokens está en un solo servicio. 
# Integración de Autenticación con Microsoft en Next.js

Esta guía explica cómo integrar la autenticación con Microsoft en una aplicación Next.js utilizando el backend ASP.NET Core.

## Configuración Inicial

1. Instala las dependencias necesarias:

```bash
npm install axios js-cookie
# o
yarn add axios js-cookie
```

## Implementación

### 1. Crear Servicio de Autenticación

Crea un archivo `services/authService.js`:

```javascript
import axios from 'axios';
import Cookies from 'js-cookie';

const API_URL = process.env.NEXT_PUBLIC_API_URL || 'https://tu-api.com';
const TOKEN_COOKIE = 'auth_token';
const EXPIRY_COOKIE = 'auth_expiry';

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

  // Procesa el callback de Microsoft
  handleMicrosoftCallback: async (code) => {
    try {
      // La API procesa el código automáticamente en la URL de callback
      const response = await axios.get(`${API_URL}/auth/microsoft-callback${window.location.search}`);
      
      const { token, ...userData } = response.data;
      
      // Guardar token en cookies
      Cookies.set(TOKEN_COOKIE, token.accessToken, { 
        expires: new Date(token.expiresAt),
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'Lax'
      });
      
      Cookies.set(EXPIRY_COOKIE, token.expiresAt);
      
      return userData;
    } catch (error) {
      console.error('Error en callback de Microsoft:', error);
      throw error;
    }
  },

  // Verifica si el usuario está autenticado
  isAuthenticated: () => {
    const token = Cookies.get(TOKEN_COOKIE);
    const expiry = Cookies.get(EXPIRY_COOKIE);
    
    if (!token || !expiry) return false;
    
    // Verifica si el token ha expirado
    return new Date(expiry) > new Date();
  },

  // Cierra la sesión
  logout: () => {
    Cookies.remove(TOKEN_COOKIE);
    Cookies.remove(EXPIRY_COOKIE);
  },

  // Obtiene el token actual
  getToken: () => {
    return Cookies.get(TOKEN_COOKIE);
  },

  // Obtiene los datos del usuario desde el localStorage o API
  getUserData: async () => {
    // Implementar según necesidades
  }
};
```

### 2. Crear Componente de Botón de Login

Crea un componente `components/MicrosoftLoginButton.jsx`:

```jsx
import React from 'react';
import { authService } from '../services/authService';

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

### 3. Crear Página de Callback

Crea `pages/auth/microsoft-callback.js`:

```jsx
import { useEffect, useState } from 'react';
import { useRouter } from 'next/router';
import { authService } from '../../services/authService';

export default function MicrosoftCallback() {
  const router = useRouter();
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Esperar a que la URL esté disponible en el cliente
    if (!router.isReady) return;
    
    const { code, error: urlError } = router.query;
    
    if (urlError) {
      setError('Error durante la autenticación con Microsoft');
      setLoading(false);
      return;
    }
    
    if (!code) {
      setError('No se recibió código de autorización');
      setLoading(false);
      return;
    }
    
    const processCallback = async () => {
      try {
        // Procesar el callback (la URL ya incluye el código)
        const userData = await authService.handleMicrosoftCallback();
        
        // Guardar datos del usuario si es necesario
        // Por ejemplo: dispatch(setUser(userData)) si usas Redux
        
        // Redirigir al usuario a la página principal o dashboard
        router.push('/dashboard');
      } catch (error) {
        console.error('Error procesando callback:', error);
        setError('Error durante la autenticación. Por favor, intente nuevamente.');
        setLoading(false);
      }
    };
    
    processCallback();
  }, [router.isReady, router.query]);

  if (loading) {
    return (
      <div className="auth-callback-container">
        <div className="spinner"></div>
        <p>Completando autenticación...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="auth-callback-container error">
        <p>{error}</p>
        <button onClick={() => router.push('/login')}>
          Volver a intentar
        </button>
      </div>
    );
  }

  return null;
}
```

### 4. Actualizar Configuración de Next.js

En `next.config.js`, asegúrate de configurar las redirecciones:

```javascript
module.exports = {
  // Otras configuraciones...
  async rewrites() {
    return [
      {
        source: '/auth/microsoft-callback',
        destination: '/auth/microsoft-callback',
      },
    ];
  },
};
```

### 5. Crear Cliente HTTP con Interceptor para Token

Crea un archivo `utils/api.js`:

```javascript
import axios from 'axios';
import { authService } from '../services/authService';

const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL,
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

## Ejemplo de Uso

### Página de Login

```jsx
import MicrosoftLoginButton from '../components/MicrosoftLoginButton';

export default function Login() {
  return (
    <div className="login-container">
      <h1>Iniciar Sesión</h1>
      
      <MicrosoftLoginButton 
        className="ms-btn primary-btn" 
        text="Continuar con Microsoft" 
      />
      
      {/* Otros métodos de login */}
    </div>
  );
}
```

### Página Protegida con HOC

Crea un HOC (Higher Order Component) para proteger rutas en `components/withAuth.jsx`:

```jsx
import { useEffect, useState } from 'react';
import { useRouter } from 'next/router';
import { authService } from '../services/authService';

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
      return <div>Cargando...</div>;
    }
    
    return <WrappedComponent {...props} />;
  };
  
  return WithAuth;
};

export default withAuth;
```

Uso del HOC:

```jsx
import withAuth from '../components/withAuth';

function Dashboard() {
  return (
    <div>
      <h1>Dashboard</h1>
      {/* Contenido del dashboard */}
    </div>
  );
}

export default withAuth(Dashboard);
```

## Consideraciones de Seguridad

1. Asegúrate de que todas las comunicaciones se realicen sobre HTTPS.
2. Configura correctamente las opciones de cookies para prevenir ataques CSRF.
3. Nunca almacenes información sensible en localStorage, prefiere el uso de cookies seguras.
4. Implementa un mecanismo de refresco de token si es necesario para sesiones largas.

## Solución de Problemas

### Problemas comunes:

1. **Error de CORS**: Asegúrate de que el backend permita solicitudes desde el dominio de tu aplicación Next.js.
2. **Tokens inválidos**: Verifica que los tokens se estén guardando y enviando correctamente.
3. **Problemas de redirección**: Confirma que las URL de redirección coincidan exactamente con las configuradas en el portal de Azure.

## Recursos Adicionales

- [Documentación de Microsoft Identity Platform](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [Documentación de Next.js](https://nextjs.org/docs)
- [Documentación de js-cookie](https://github.com/js-cookie/js-cookie) 
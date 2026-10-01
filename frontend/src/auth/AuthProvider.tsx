import { createContext, ReactNode, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import {
  AuthenticationResult,
  EventType,
  InteractionRequiredAuthError,
  InteractionStatus,
} from '@azure/msal-browser';
import { MsalProvider, useIsAuthenticated, useMsal } from '@azure/msal-react';
import { config } from '../config';
import { loginRequest, msalInstance } from './msalConfig';

export interface PortalUser {
  name: string;
  username: string;
  roles: string[];
}

interface AuthContextValue {
  user: PortalUser | null;
  isAdmin: boolean;
  getAccessToken: () => Promise<string>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth debe usarse dentro de <AuthProvider>');
  return ctx;
}

/** Proveedor de autenticación de desarrollo: sin Azure AD, usuario admin ficticio. */
function MockAuthProvider({ children }: { children: ReactNode }) {
  const value = useMemo<AuthContextValue>(
    () => ({
      user: { name: 'Usuario Demo', username: 'demo@empresa.com', roles: [config.adminRole] },
      isAdmin: true,
      getAccessToken: async () => 'mock-token',
      logout: () => window.location.reload(),
    }),
    [],
  );
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/** Fuerza el login con Azure AD (redirect) y expone el usuario y sus app roles. */
function AzureAdGate({ children }: { children: ReactNode }) {
  const { instance, accounts, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const account = instance.getActiveAccount() ?? accounts[0] ?? null;

  useEffect(() => {
    if (!isAuthenticated && inProgress === InteractionStatus.None) {
      instance.loginRedirect(loginRequest).catch((e) => console.error(e));
    }
  }, [isAuthenticated, inProgress, instance]);

  const getAccessToken = useCallback(async () => {
    if (!account) throw new Error('No hay una sesión activa');
    try {
      const result = await instance.acquireTokenSilent({ ...loginRequest, account });
      return result.accessToken;
    } catch (e) {
      if (e instanceof InteractionRequiredAuthError) {
        await instance.acquireTokenRedirect({ ...loginRequest, account });
      }
      throw e;
    }
  }, [instance, account]);

  const value = useMemo<AuthContextValue | null>(() => {
    if (!account) return null;
    const roles = ((account.idTokenClaims?.roles as string[] | undefined) ?? []).slice();
    return {
      user: { name: account.name ?? account.username, username: account.username, roles },
      isAdmin: roles.includes(config.adminRole),
      getAccessToken,
      logout: () => instance.logoutRedirect({ account }),
    };
  }, [account, getAccessToken, instance]);

  if (!value) {
    return <div className="fullscreen-message">Redirigiendo a Azure AD…</div>;
  }
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

let msalReady: Promise<void> | null = null;
function initMsal(): Promise<void> {
  msalReady ??= msalInstance.initialize().then(async () => {
    const result = await msalInstance.handleRedirectPromise();
    if (result?.account) msalInstance.setActiveAccount(result.account);
    msalInstance.addEventCallback((event) => {
      if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
        msalInstance.setActiveAccount((event.payload as AuthenticationResult).account);
      }
    });
  });
  return msalReady;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [ready, setReady] = useState(config.useMocks);

  useEffect(() => {
    if (config.useMocks) return;
    initMsal()
      .then(() => setReady(true))
      .catch((e) => console.error('Error inicializando MSAL', e));
  }, []);

  if (config.useMocks) return <MockAuthProvider>{children}</MockAuthProvider>;
  if (!ready) return <div className="fullscreen-message">Cargando…</div>;

  return (
    <MsalProvider instance={msalInstance}>
      <AzureAdGate>{children}</AzureAdGate>
    </MsalProvider>
  );
}

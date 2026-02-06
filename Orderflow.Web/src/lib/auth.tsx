import { createContext, useContext, useState, useEffect, useCallback, type ReactNode } from "react";
import { api } from "./api";
import { tokenStorage } from "./storage";

interface AuthUser {
  userId: string;
  email: string;
  roles: string[];
  userName?: string;
}

interface AuthContextType {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  loading: boolean;
  login: (token: string, userData: { userId: string; email: string; roles: string[] }) => void;
  logout: () => void;
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

function parseJwt(token: string): Record<string, unknown> | null {
  try {
    const base64Url = token.split(".")[1];
    const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split("")
        .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
        .join("")
    );
    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

function extractUserFromToken(token: string): AuthUser | null {
  const payload = parseJwt(token);
  if (!payload) return null;

  // Handle role claim - can be string or array
  const roleClaim =
    payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ??
    payload["role"] ??
    [];
  const roles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];

  return {
    userId:
      (payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] as string) ??
      (payload["sub"] as string) ??
      "",
    email:
      (payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] as string) ??
      (payload["email"] as string) ??
      "",
    userName:
      (payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] as string) ??
      undefined,
    roles: roles as string[],
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  // Initialize from stored token
  useEffect(() => {
    const token = tokenStorage.get();
    if (token) {
      const parsed = extractUserFromToken(token);
      if (parsed) {
        api.defaults.headers.common["Authorization"] = `Bearer ${token}`;
        setUser(parsed);
      } else {
        tokenStorage.clear();
      }
    }
    setLoading(false);
  }, []);

  // Set up axios interceptor for 401 responses
  useEffect(() => {
    const interceptor = api.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          tokenStorage.clear();
          delete api.defaults.headers.common["Authorization"];
          setUser(null);
        }
        return Promise.reject(error);
      }
    );
    return () => api.interceptors.response.eject(interceptor);
  }, []);

  const login = useCallback(
    (token: string, userData: { userId: string; email: string; roles: string[] }) => {
      tokenStorage.set(token);
      api.defaults.headers.common["Authorization"] = `Bearer ${token}`;
      setUser({
        userId: userData.userId,
        email: userData.email,
        roles: userData.roles,
      });
    },
    []
  );

  const logout = useCallback(() => {
    tokenStorage.clear();
    delete api.defaults.headers.common["Authorization"];
    setUser(null);
  }, []);

  const refreshProfile = useCallback(async () => {
    try {
      const response = await api.get("/api/v1/users/me");
      setUser((prev) =>
        prev
          ? {
              ...prev,
              email: response.data.email,
              userName: response.data.userName,
              roles: response.data.roles,
            }
          : prev
      );
    } catch {
      // Silently fail - interceptor handles 401
    }
  }, []);

  const isAuthenticated = !!user;
  const isAdmin = user?.roles.includes("Admin") ?? false;

  return (
    <AuthContext.Provider
      value={{ user, isAuthenticated, isAdmin, loading, login, logout, refreshProfile }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}

import { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../../../lib/auth";
import { api } from "../../../lib/api";

export function AuthCallbackPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { login } = useAuth();
  const [error, setError] = useState("");

  useEffect(() => {
    const token = searchParams.get("token");
    const userId = searchParams.get("userId");
    const isNewUser = searchParams.get("isNewUser") === "True" || searchParams.get("isNewUser") === "true";

    if (!token || !userId) {
      setError("No se recibieron los datos de autenticacion.");
      return;
    }

    // Set the token temporarily to fetch the user profile
    api.defaults.headers.common["Authorization"] = `Bearer ${token}`;

    api
      .get("/api/v1/users/me")
      .then((response) => {
        const { email, roles } = response.data;
        login(token, { userId, email, roles });

        if (isNewUser) {
          navigate("/profile", {
            state: { message: "Cuenta creada con Google. Completa tu perfil." },
          });
        } else {
          navigate("/");
        }
      })
      .catch(() => {
        // Fallback: try to decode the JWT for basic info
        try {
          const base64Url = token.split(".")[1];
          const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
          const payload = JSON.parse(
            decodeURIComponent(
              atob(base64)
                .split("")
                .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
                .join("")
            )
          );

          const email =
            payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] ??
            payload["email"] ??
            "";
          const roleClaim =
            payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ??
            payload["role"] ??
            [];
          const roles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];

          login(token, { userId, email, roles: roles as string[] });
          navigate("/");
        } catch {
          setError("Error al procesar la autenticacion.");
        }
      });
  }, [searchParams, login, navigate]);

  if (error) {
    return (
      <div className="max-w-md mx-auto mt-16 text-center">
        <div className="bg-red-50 border border-red-200 text-red-700 px-6 py-4 rounded-lg">
          <p>{error}</p>
          <button
            onClick={() => navigate("/login")}
            className="mt-4 text-sm font-medium text-red-600 hover:text-red-800 underline"
          >
            Volver al inicio de sesion
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-[400px]">
      <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600 mb-4"></div>
      <p className="text-gray-500">Procesando autenticacion...</p>
    </div>
  );
}

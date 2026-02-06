import { useEffect, useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { api } from "../../../lib/api";
import { useAuth } from "../../../lib/auth";

interface UserProfile {
  userId: string;
  email: string;
  userName: string;
  emailConfirmed: boolean;
  phoneNumber: string | null;
  phoneNumberConfirmed: boolean;
  twoFactorEnabled: boolean;
  roles: string[];
}

export function ProfilePage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { isAuthenticated, logout, refreshProfile } = useAuth();

  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [successMsg, setSuccessMsg] = useState(location.state?.message || "");

  // Edit profile state
  const [editing, setEditing] = useState(false);
  const [editForm, setEditForm] = useState({ userName: "", phoneNumber: "" });
  const [editLoading, setEditLoading] = useState(false);
  const [editError, setEditError] = useState("");

  // Change password state
  const [changingPassword, setChangingPassword] = useState(false);
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: "",
    newPassword: "",
    confirmNewPassword: "",
  });
  const [passwordLoading, setPasswordLoading] = useState(false);
  const [passwordError, setPasswordError] = useState("");

  useEffect(() => {
    if (!isAuthenticated) {
      navigate("/login");
      return;
    }
    loadProfile();
  }, [isAuthenticated, navigate]);

  const loadProfile = () => {
    setLoading(true);
    api
      .get("/api/v1/users/me")
      .then((response) => {
        setProfile(response.data);
        setEditForm({
          userName: response.data.userName,
          phoneNumber: response.data.phoneNumber || "",
        });
        setLoading(false);
      })
      .catch((err) => {
        if (err.response?.status === 401) {
          navigate("/login");
        } else {
          setError(err.response?.data?.message || "Error al cargar el perfil");
          setLoading(false);
        }
      });
  };

  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setEditError("");
    setEditLoading(true);

    try {
      const response = await api.put("/api/v1/users/me", {
        userName: editForm.userName,
        phoneNumber: editForm.phoneNumber || null,
      });
      setProfile((prev) =>
        prev
          ? {
              ...prev,
              userName: response.data.userName ?? editForm.userName,
              phoneNumber: response.data.phoneNumber ?? editForm.phoneNumber,
            }
          : prev
      );
      setEditing(false);
      setSuccessMsg("Perfil actualizado correctamente.");
      refreshProfile();
      setTimeout(() => setSuccessMsg(""), 3000);
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { detail?: string; title?: string } } };
      setEditError(axiosErr.response?.data?.detail || axiosErr.response?.data?.title || "Error al actualizar el perfil");
    } finally {
      setEditLoading(false);
    }
  };

  const handlePasswordSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordError("");

    if (passwordForm.newPassword !== passwordForm.confirmNewPassword) {
      setPasswordError("Las contrasenas no coinciden.");
      return;
    }

    if (passwordForm.newPassword.length < 8) {
      setPasswordError("La nueva contrasena debe tener al menos 8 caracteres.");
      return;
    }

    setPasswordLoading(true);

    try {
      await api.post("/api/v1/users/me/password", {
        currentPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword,
        confirmNewPassword: passwordForm.confirmNewPassword,
      });
      setChangingPassword(false);
      setPasswordForm({ currentPassword: "", newPassword: "", confirmNewPassword: "" });
      setSuccessMsg("Contrasena cambiada correctamente.");
      setTimeout(() => setSuccessMsg(""), 3000);
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { detail?: string; title?: string }; status?: number } };
      if (axiosErr.response?.status === 401) {
        setPasswordError("La contrasena actual es incorrecta.");
      } else {
        setPasswordError(
          axiosErr.response?.data?.detail || axiosErr.response?.data?.title || "Error al cambiar la contrasena"
        );
      }
    } finally {
      setPasswordLoading(false);
    }
  };

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center min-h-[400px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="max-w-4xl mx-auto mt-8">
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">{error}</div>
      </div>
    );
  }

  if (!profile) return null;

  return (
    <div className="max-w-4xl mx-auto mt-8">
      {successMsg && (
        <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg mb-4 flex items-center gap-2">
          <svg className="w-5 h-5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
          </svg>
          {successMsg}
        </div>
      )}

      <div className="bg-white shadow-md rounded-lg overflow-hidden">
        <div className="bg-gradient-to-r from-blue-500 to-blue-600 px-6 py-8">
          <div className="flex items-center gap-4">
            <div className="bg-white/20 backdrop-blur-sm p-4 rounded-full">
              <svg className="w-10 h-10 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
              </svg>
            </div>
            <div>
              <h1 className="text-3xl font-bold text-white">Mi Perfil</h1>
              <p className="text-blue-100 mt-1">{profile.email}</p>
            </div>
          </div>
        </div>

        <div className="p-6">
          {/* Profile Info (view mode) */}
          {!editing && (
            <div className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Email</label>
                  <div className="flex items-center gap-2">
                    <span className="text-gray-900">{profile.email}</span>
                    {profile.emailConfirmed ? (
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
                        Verificado
                      </span>
                    ) : (
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-yellow-100 text-yellow-800">
                        No verificado
                      </span>
                    )}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Nombre de Usuario</label>
                  <span className="text-gray-900">{profile.userName}</span>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Telefono</label>
                  <span className="text-gray-900">{profile.phoneNumber || "No especificado"}</span>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Roles</label>
                  <div className="flex gap-2">
                    {profile.roles.map((role) => (
                      <span
                        key={role}
                        className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                          role === "Admin" ? "bg-purple-100 text-purple-800" : "bg-blue-100 text-blue-800"
                        }`}
                      >
                        {role}
                      </span>
                    ))}
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">ID de Usuario</label>
                  <span className="text-gray-500 text-sm font-mono">{profile.userId}</span>
                </div>
              </div>
            </div>
          )}

          {/* Edit Profile Form */}
          {editing && (
            <form onSubmit={handleEditSubmit} className="space-y-4">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Editar Perfil</h3>

              {editError && (
                <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
                  {editError}
                </div>
              )}

              <div>
                <label htmlFor="userName" className="block text-sm font-medium text-gray-700 mb-1">
                  Nombre de Usuario
                </label>
                <input
                  type="text"
                  id="userName"
                  value={editForm.userName}
                  onChange={(e) => setEditForm({ ...editForm, userName: e.target.value })}
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div>
                <label htmlFor="phoneNumber" className="block text-sm font-medium text-gray-700 mb-1">
                  Telefono
                </label>
                <input
                  type="tel"
                  id="phoneNumber"
                  value={editForm.phoneNumber}
                  onChange={(e) => setEditForm({ ...editForm, phoneNumber: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="+54 11 1234-5678"
                />
              </div>

              <div className="flex gap-3 pt-2">
                <button
                  type="submit"
                  disabled={editLoading}
                  className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded-md transition disabled:opacity-50"
                >
                  {editLoading ? "Guardando..." : "Guardar Cambios"}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setEditing(false);
                    setEditError("");
                    setEditForm({ userName: profile.userName, phoneNumber: profile.phoneNumber || "" });
                  }}
                  className="bg-gray-100 hover:bg-gray-200 text-gray-700 font-medium py-2 px-4 rounded-md transition"
                >
                  Cancelar
                </button>
              </div>
            </form>
          )}

          {/* Change Password Form */}
          {changingPassword && (
            <form onSubmit={handlePasswordSubmit} className="mt-6 pt-6 border-t border-gray-200 space-y-4">
              <h3 className="text-lg font-semibold text-gray-900 mb-4">Cambiar Contrasena</h3>

              {passwordError && (
                <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
                  {passwordError}
                </div>
              )}

              <div>
                <label htmlFor="currentPassword" className="block text-sm font-medium text-gray-700 mb-1">
                  Contrasena Actual
                </label>
                <input
                  type="password"
                  id="currentPassword"
                  value={passwordForm.currentPassword}
                  onChange={(e) => setPasswordForm({ ...passwordForm, currentPassword: e.target.value })}
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div>
                <label htmlFor="newPassword" className="block text-sm font-medium text-gray-700 mb-1">
                  Nueva Contrasena
                </label>
                <input
                  type="password"
                  id="newPassword"
                  value={passwordForm.newPassword}
                  onChange={(e) => setPasswordForm({ ...passwordForm, newPassword: e.target.value })}
                  required
                  minLength={8}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <p className="mt-1 text-xs text-gray-500">
                  Minimo 8 caracteres, mayusculas, minusculas, numero y caracter especial.
                </p>
              </div>

              <div>
                <label htmlFor="confirmNewPassword" className="block text-sm font-medium text-gray-700 mb-1">
                  Confirmar Nueva Contrasena
                </label>
                <input
                  type="password"
                  id="confirmNewPassword"
                  value={passwordForm.confirmNewPassword}
                  onChange={(e) => setPasswordForm({ ...passwordForm, confirmNewPassword: e.target.value })}
                  required
                  minLength={8}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div className="flex gap-3 pt-2">
                <button
                  type="submit"
                  disabled={passwordLoading}
                  className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded-md transition disabled:opacity-50"
                >
                  {passwordLoading ? "Cambiando..." : "Cambiar Contrasena"}
                </button>
                <button
                  type="button"
                  onClick={() => {
                    setChangingPassword(false);
                    setPasswordError("");
                    setPasswordForm({ currentPassword: "", newPassword: "", confirmNewPassword: "" });
                  }}
                  className="bg-gray-100 hover:bg-gray-200 text-gray-700 font-medium py-2 px-4 rounded-md transition"
                >
                  Cancelar
                </button>
              </div>
            </form>
          )}

          {/* Action Buttons */}
          <div className="border-t border-gray-200 pt-6 mt-6">
            <div className="flex flex-wrap gap-4">
              {!editing && (
                <button
                  onClick={() => setEditing(true)}
                  className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded-md transition"
                >
                  Editar Perfil
                </button>
              )}
              {!changingPassword && (
                <button
                  onClick={() => setChangingPassword(true)}
                  className="bg-gray-100 hover:bg-gray-200 text-gray-700 font-medium py-2 px-4 rounded-md transition"
                >
                  Cambiar Contrasena
                </button>
              )}
              <button
                onClick={handleLogout}
                className="ml-auto bg-red-50 hover:bg-red-100 text-red-700 font-medium py-2 px-4 rounded-md transition"
              >
                Cerrar Sesion
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

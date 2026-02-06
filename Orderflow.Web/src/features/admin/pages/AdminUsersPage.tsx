import { useEffect, useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../../../lib/api";
import { useAuth } from "../../../lib/auth";

interface User {
  userId: string;
  email: string;
  userName: string;
  emailConfirmed: boolean;
  lockoutEnd: string | null;
  lockoutEnabled: boolean;
  accessFailedCount: number;
  roles: string[];
}

interface PaginatedResponse {
  data: User[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

interface UserDetail {
  userId: string;
  email: string;
  userName: string;
  emailConfirmed: boolean;
  phoneNumber: string | null;
  phoneNumberConfirmed: boolean;
  twoFactorEnabled: boolean;
  lockoutEnd: string | null;
  lockoutEnabled: boolean;
  accessFailedCount: number;
  roles: string[];
}

type ModalType = "create" | "edit" | "delete" | "roles" | null;

export function AdminUsersPage() {
  const navigate = useNavigate();
  const { isAuthenticated, isAdmin } = useAuth();

  const [users, setUsers] = useState<User[]>([]);
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [successMsg, setSuccessMsg] = useState("");
  const [searchTerm, setSearchTerm] = useState("");

  // Modal state
  const [modalType, setModalType] = useState<ModalType>(null);
  const [selectedUser, setSelectedUser] = useState<UserDetail | null>(null);
  const [modalLoading, setModalLoading] = useState(false);
  const [modalError, setModalError] = useState("");

  // Create user form
  const [createForm, setCreateForm] = useState({
    email: "",
    password: "",
    userName: "",
    phoneNumber: "",
    roles: ["Customer"],
  });

  // Edit user form
  const [editForm, setEditForm] = useState({
    email: "",
    userName: "",
    phoneNumber: "",
    emailConfirmed: false,
    lockoutEnabled: true,
  });

  // Roles management
  const [userRoles, setUserRoles] = useState<string[]>([]);
  const availableRoles = ["Admin", "Customer"];

  useEffect(() => {
    if (!isAuthenticated) {
      navigate("/login");
      return;
    }
    if (!isAdmin) {
      navigate("/");
      return;
    }
  }, [isAuthenticated, isAdmin, navigate]);

  const loadUsers = useCallback(
    (page = 1, search = searchTerm) => {
      setLoading(true);
      api
        .get("/api/v1/admin/users", {
          params: { page, pageSize: 10, search: search || undefined },
        })
        .then((response: { data: PaginatedResponse }) => {
          setUsers(response.data.data);
          setPagination(response.data.pagination);
          setLoading(false);
        })
        .catch((err) => {
          if (err.response?.status === 401 || err.response?.status === 403) {
            navigate("/login");
          } else {
            setError(err.response?.data?.message || "Error al cargar los usuarios");
            setLoading(false);
          }
        });
    },
    [searchTerm, navigate]
  );

  useEffect(() => {
    if (isAuthenticated && isAdmin) {
      loadUsers();
    }
  }, [isAuthenticated, isAdmin, loadUsers]);

  const showSuccess = (msg: string) => {
    setSuccessMsg(msg);
    setTimeout(() => setSuccessMsg(""), 3000);
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    loadUsers(1, searchTerm);
  };

  const isUserLocked = (lockoutEnd: string | null) => {
    if (!lockoutEnd) return false;
    return new Date(lockoutEnd) > new Date();
  };

  // --- Create User ---
  const openCreateModal = () => {
    setCreateForm({ email: "", password: "", userName: "", phoneNumber: "", roles: ["Customer"] });
    setModalError("");
    setModalType("create");
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setModalError("");
    setModalLoading(true);

    try {
      await api.post("/api/v1/admin/users", {
        email: createForm.email,
        password: createForm.password,
        userName: createForm.userName || undefined,
        phoneNumber: createForm.phoneNumber || undefined,
        roles: createForm.roles,
      });
      setModalType(null);
      showSuccess("Usuario creado correctamente.");
      loadUsers(pagination.page);
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { detail?: string; title?: string } } };
      setModalError(axiosErr.response?.data?.detail || axiosErr.response?.data?.title || "Error al crear usuario");
    } finally {
      setModalLoading(false);
    }
  };

  // --- Edit User ---
  const openEditModal = async (userId: string) => {
    setModalError("");
    setModalLoading(true);
    setModalType("edit");

    try {
      const response = await api.get(`/api/v1/admin/users/${userId}`);
      const user: UserDetail = response.data;
      setSelectedUser(user);
      setEditForm({
        email: user.email,
        userName: user.userName,
        phoneNumber: user.phoneNumber || "",
        emailConfirmed: user.emailConfirmed,
        lockoutEnabled: user.lockoutEnabled,
      });
    } catch {
      setModalError("Error al cargar datos del usuario.");
    } finally {
      setModalLoading(false);
    }
  };

  const handleEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedUser) return;
    setModalError("");
    setModalLoading(true);

    try {
      await api.put(`/api/v1/admin/users/${selectedUser.userId}`, {
        email: editForm.email,
        userName: editForm.userName,
        phoneNumber: editForm.phoneNumber || null,
        emailConfirmed: editForm.emailConfirmed,
        lockoutEnabled: editForm.lockoutEnabled,
      });
      setModalType(null);
      showSuccess("Usuario actualizado correctamente.");
      loadUsers(pagination.page);
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { detail?: string; title?: string } } };
      setModalError(axiosErr.response?.data?.detail || axiosErr.response?.data?.title || "Error al actualizar usuario");
    } finally {
      setModalLoading(false);
    }
  };

  // --- Delete User ---
  const openDeleteModal = (user: User) => {
    setSelectedUser(user as unknown as UserDetail);
    setModalError("");
    setModalType("delete");
  };

  const handleDelete = async () => {
    if (!selectedUser) return;
    setModalError("");
    setModalLoading(true);

    try {
      await api.delete(`/api/v1/admin/users/${selectedUser.userId}`);
      setModalType(null);
      showSuccess("Usuario eliminado correctamente.");
      loadUsers(pagination.page);
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { detail?: string; title?: string } } };
      setModalError(axiosErr.response?.data?.detail || axiosErr.response?.data?.title || "Error al eliminar usuario");
    } finally {
      setModalLoading(false);
    }
  };

  // --- Lock / Unlock ---
  const handleToggleLock = async (user: User) => {
    try {
      if (isUserLocked(user.lockoutEnd)) {
        await api.post(`/api/v1/admin/users/${user.userId}/unlock`);
        showSuccess(`Usuario ${user.email} desbloqueado.`);
      } else {
        await api.post(`/api/v1/admin/users/${user.userId}/lock`, { lockoutEnd: null });
        showSuccess(`Usuario ${user.email} bloqueado.`);
      }
      loadUsers(pagination.page);
    } catch {
      setError("Error al cambiar el estado de bloqueo.");
    }
  };

  // --- Roles ---
  const openRolesModal = async (userId: string) => {
    setModalError("");
    setModalLoading(true);
    setModalType("roles");

    try {
      const [userResponse, rolesResponse] = await Promise.all([
        api.get(`/api/v1/admin/users/${userId}`),
        api.get(`/api/v1/admin/users/${userId}/roles`),
      ]);
      setSelectedUser(userResponse.data);
      setUserRoles(rolesResponse.data.roles);
    } catch {
      setModalError("Error al cargar roles.");
    } finally {
      setModalLoading(false);
    }
  };

  const handleAddRole = async (roleName: string) => {
    if (!selectedUser) return;
    setModalLoading(true);
    try {
      await api.post(`/api/v1/admin/users/${selectedUser.userId}/roles/${roleName}`);
      setUserRoles((prev) => [...prev, roleName]);
      loadUsers(pagination.page);
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { detail?: string; title?: string } } };
      setModalError(axiosErr.response?.data?.detail || "Error al asignar rol");
    } finally {
      setModalLoading(false);
    }
  };

  const handleRemoveRole = async (roleName: string) => {
    if (!selectedUser) return;
    setModalLoading(true);
    try {
      await api.delete(`/api/v1/admin/users/${selectedUser.userId}/roles/${roleName}`);
      setUserRoles((prev) => prev.filter((r) => r !== roleName));
      loadUsers(pagination.page);
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { detail?: string; title?: string } } };
      setModalError(axiosErr.response?.data?.detail || "Error al remover rol");
    } finally {
      setModalLoading(false);
    }
  };

  if (loading && users.length === 0) {
    return (
      <div className="flex justify-center items-center min-h-[400px]">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto mt-8">
      <div className="mb-6">
        <h1 className="text-3xl font-bold">Panel de Administracion - Usuarios</h1>
        <p className="text-gray-600 mt-2">Gestiona todos los usuarios del sistema</p>
      </div>

      {successMsg && (
        <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg mb-4 flex items-center gap-2">
          <svg className="w-5 h-5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
          </svg>
          {successMsg}
        </div>
      )}

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-4">{error}</div>
      )}

      <div className="bg-white shadow-md rounded-lg overflow-hidden">
        <div className="p-4 border-b border-gray-200">
          <div className="flex gap-4">
            <form onSubmit={handleSearch} className="flex-1 flex gap-2">
              <input
                type="text"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Buscar por email o nombre de usuario..."
                className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <button
                type="submit"
                className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded-md transition"
              >
                Buscar
              </button>
            </form>
            <button
              onClick={openCreateModal}
              className="bg-green-600 hover:bg-green-700 text-white font-medium py-2 px-4 rounded-md transition"
            >
              + Nuevo Usuario
            </button>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Usuario
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Email
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Roles
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Estado
                </th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Acciones
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {users.map((user) => (
                <tr key={user.userId} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm font-medium text-gray-900">{user.userName}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-500">{user.email}</div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex gap-1">
                      {user.roles.map((role) => (
                        <span
                          key={role}
                          className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${
                            role === "Admin" ? "bg-purple-100 text-purple-800" : "bg-blue-100 text-blue-800"
                          }`}
                        >
                          {role}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex gap-2">
                      {user.emailConfirmed ? (
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
                          Verificado
                        </span>
                      ) : (
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-yellow-100 text-yellow-800">
                          No verificado
                        </span>
                      )}
                      {isUserLocked(user.lockoutEnd) && (
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-red-100 text-red-800">
                          Bloqueado
                        </span>
                      )}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <div className="flex justify-end gap-2">
                      <button
                        onClick={() => openEditModal(user.userId)}
                        className="text-blue-600 hover:text-blue-900"
                      >
                        Editar
                      </button>
                      <button
                        onClick={() => openRolesModal(user.userId)}
                        className="text-purple-600 hover:text-purple-900"
                      >
                        Roles
                      </button>
                      <button
                        onClick={() => handleToggleLock(user)}
                        className={isUserLocked(user.lockoutEnd) ? "text-green-600 hover:text-green-900" : "text-orange-600 hover:text-orange-900"}
                      >
                        {isUserLocked(user.lockoutEnd) ? "Desbloquear" : "Bloquear"}
                      </button>
                      <button
                        onClick={() => openDeleteModal(user)}
                        className="text-red-600 hover:text-red-900"
                      >
                        Eliminar
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
              {users.length === 0 && !loading && (
                <tr>
                  <td colSpan={5} className="px-6 py-8 text-center text-gray-500">
                    No se encontraron usuarios.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {pagination.totalPages > 1 && (
          <div className="bg-gray-50 px-4 py-3 border-t border-gray-200 sm:px-6">
            <div className="flex items-center justify-between">
              <div className="text-sm text-gray-700">
                Mostrando{" "}
                <span className="font-medium">{(pagination.page - 1) * pagination.pageSize + 1}</span> a{" "}
                <span className="font-medium">
                  {Math.min(pagination.page * pagination.pageSize, pagination.totalCount)}
                </span>{" "}
                de <span className="font-medium">{pagination.totalCount}</span> resultados
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => loadUsers(pagination.page - 1)}
                  disabled={pagination.page === 1 || loading}
                  className="px-3 py-1 border border-gray-300 rounded-md text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Anterior
                </button>
                <span className="px-3 py-1 text-sm text-gray-700">
                  Pagina {pagination.page} de {pagination.totalPages}
                </span>
                <button
                  onClick={() => loadUsers(pagination.page + 1)}
                  disabled={pagination.page === pagination.totalPages || loading}
                  className="px-3 py-1 border border-gray-300 rounded-md text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Siguiente
                </button>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* ========== MODALS ========== */}
      {modalType && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="fixed inset-0 bg-black/50" onClick={() => !modalLoading && setModalType(null)} />
          <div className="relative bg-white rounded-lg shadow-xl w-full max-w-md mx-4 max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              {/* Create User Modal */}
              {modalType === "create" && (
                <>
                  <h2 className="text-xl font-bold text-gray-900 mb-4">Crear Nuevo Usuario</h2>
                  {modalError && (
                    <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm mb-4">
                      {modalError}
                    </div>
                  )}
                  <form onSubmit={handleCreate} className="space-y-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">Email *</label>
                      <input
                        type="email"
                        required
                        value={createForm.email}
                        onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">Contrasena *</label>
                      <input
                        type="password"
                        required
                        minLength={8}
                        value={createForm.password}
                        onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                      <p className="mt-1 text-xs text-gray-500">Min 8 chars, mayusculas, minusculas, numero y especial.</p>
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">Nombre de Usuario</label>
                      <input
                        type="text"
                        value={createForm.userName}
                        onChange={(e) => setCreateForm({ ...createForm, userName: e.target.value })}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        placeholder="Se usara el email si se deja vacio"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">Telefono</label>
                      <input
                        type="tel"
                        value={createForm.phoneNumber}
                        onChange={(e) => setCreateForm({ ...createForm, phoneNumber: e.target.value })}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">Rol</label>
                      <select
                        value={createForm.roles[0]}
                        onChange={(e) => setCreateForm({ ...createForm, roles: [e.target.value] })}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      >
                        <option value="Customer">Customer</option>
                        <option value="Admin">Admin</option>
                      </select>
                    </div>
                    <div className="flex justify-end gap-3 pt-2">
                      <button
                        type="button"
                        onClick={() => setModalType(null)}
                        disabled={modalLoading}
                        className="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-md transition"
                      >
                        Cancelar
                      </button>
                      <button
                        type="submit"
                        disabled={modalLoading}
                        className="px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-md transition disabled:opacity-50"
                      >
                        {modalLoading ? "Creando..." : "Crear Usuario"}
                      </button>
                    </div>
                  </form>
                </>
              )}

              {/* Edit User Modal */}
              {modalType === "edit" && (
                <>
                  <h2 className="text-xl font-bold text-gray-900 mb-4">Editar Usuario</h2>
                  {modalError && (
                    <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm mb-4">
                      {modalError}
                    </div>
                  )}
                  {selectedUser && (
                    <form onSubmit={handleEdit} className="space-y-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                        <input
                          type="email"
                          required
                          value={editForm.email}
                          onChange={(e) => setEditForm({ ...editForm, email: e.target.value })}
                          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Nombre de Usuario</label>
                        <input
                          type="text"
                          required
                          value={editForm.userName}
                          onChange={(e) => setEditForm({ ...editForm, userName: e.target.value })}
                          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Telefono</label>
                        <input
                          type="tel"
                          value={editForm.phoneNumber}
                          onChange={(e) => setEditForm({ ...editForm, phoneNumber: e.target.value })}
                          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />
                      </div>
                      <div className="flex items-center gap-4">
                        <label className="flex items-center gap-2 cursor-pointer">
                          <input
                            type="checkbox"
                            checked={editForm.emailConfirmed}
                            onChange={(e) => setEditForm({ ...editForm, emailConfirmed: e.target.checked })}
                            className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                          />
                          <span className="text-sm text-gray-700">Email verificado</span>
                        </label>
                        <label className="flex items-center gap-2 cursor-pointer">
                          <input
                            type="checkbox"
                            checked={editForm.lockoutEnabled}
                            onChange={(e) => setEditForm({ ...editForm, lockoutEnabled: e.target.checked })}
                            className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                          />
                          <span className="text-sm text-gray-700">Bloqueo habilitado</span>
                        </label>
                      </div>
                      <div className="flex justify-end gap-3 pt-2">
                        <button
                          type="button"
                          onClick={() => setModalType(null)}
                          disabled={modalLoading}
                          className="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-md transition"
                        >
                          Cancelar
                        </button>
                        <button
                          type="submit"
                          disabled={modalLoading}
                          className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md transition disabled:opacity-50"
                        >
                          {modalLoading ? "Guardando..." : "Guardar Cambios"}
                        </button>
                      </div>
                    </form>
                  )}
                </>
              )}

              {/* Delete User Modal */}
              {modalType === "delete" && selectedUser && (
                <>
                  <h2 className="text-xl font-bold text-gray-900 mb-4">Eliminar Usuario</h2>
                  {modalError && (
                    <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm mb-4">
                      {modalError}
                    </div>
                  )}
                  <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-4">
                    <p className="text-sm text-red-800">
                      Estas seguro de que deseas eliminar al usuario <strong>{selectedUser.email}</strong>?
                      Esta accion no se puede deshacer.
                    </p>
                  </div>
                  <div className="flex justify-end gap-3">
                    <button
                      onClick={() => setModalType(null)}
                      disabled={modalLoading}
                      className="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-md transition"
                    >
                      Cancelar
                    </button>
                    <button
                      onClick={handleDelete}
                      disabled={modalLoading}
                      className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-md transition disabled:opacity-50"
                    >
                      {modalLoading ? "Eliminando..." : "Eliminar"}
                    </button>
                  </div>
                </>
              )}

              {/* Roles Management Modal */}
              {modalType === "roles" && selectedUser && (
                <>
                  <h2 className="text-xl font-bold text-gray-900 mb-2">Gestionar Roles</h2>
                  <p className="text-sm text-gray-500 mb-4">{selectedUser.email}</p>
                  {modalError && (
                    <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm mb-4">
                      {modalError}
                    </div>
                  )}

                  <div className="space-y-3">
                    <h3 className="text-sm font-medium text-gray-700">Roles actuales:</h3>
                    {userRoles.length === 0 ? (
                      <p className="text-sm text-gray-400 italic">Sin roles asignados</p>
                    ) : (
                      <div className="flex flex-wrap gap-2">
                        {userRoles.map((role) => (
                          <span
                            key={role}
                            className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-sm font-medium ${
                              role === "Admin" ? "bg-purple-100 text-purple-800" : "bg-blue-100 text-blue-800"
                            }`}
                          >
                            {role}
                            <button
                              onClick={() => handleRemoveRole(role)}
                              disabled={modalLoading}
                              className="ml-1 hover:text-red-600 transition"
                              title="Remover rol"
                            >
                              &times;
                            </button>
                          </span>
                        ))}
                      </div>
                    )}

                    <h3 className="text-sm font-medium text-gray-700 mt-4">Agregar rol:</h3>
                    <div className="flex flex-wrap gap-2">
                      {availableRoles
                        .filter((r) => !userRoles.includes(r))
                        .map((role) => (
                          <button
                            key={role}
                            onClick={() => handleAddRole(role)}
                            disabled={modalLoading}
                            className="px-3 py-1 border border-dashed border-gray-300 rounded-full text-sm text-gray-600 hover:bg-gray-50 hover:border-gray-400 transition disabled:opacity-50"
                          >
                            + {role}
                          </button>
                        ))}
                      {availableRoles.filter((r) => !userRoles.includes(r)).length === 0 && (
                        <p className="text-sm text-gray-400 italic">Todos los roles asignados</p>
                      )}
                    </div>
                  </div>

                  <div className="flex justify-end pt-4 mt-4 border-t">
                    <button
                      onClick={() => setModalType(null)}
                      className="px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-md transition"
                    >
                      Cerrar
                    </button>
                  </div>
                </>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

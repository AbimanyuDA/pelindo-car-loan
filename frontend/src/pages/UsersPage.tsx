import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Users,
  Plus,
  Edit2,
  Trash2,
  Upload,
  Download,
  Search,
  AlertTriangle,
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Modal } from "@/components/ui/Modal";
import { Alert } from "@/components/ui/Alert";
import { PageLoading } from "@/components/ui/Loading";
import {
  userService,
  type UserType,
  type CreateUserRequest,
  type UpdateUserRequest,
  type BulkImportResult,
} from "@/services";

export default function UsersPage() {
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState("");
  const [showModal, setShowModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [showImportModal, setShowImportModal] = useState(false);
  const [showImportResult, setShowImportResult] = useState(false);
  const [editingUser, setEditingUser] = useState<UserType | null>(null);
  const [deletingUser, setDeletingUser] = useState<UserType | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [importResult, setImportResult] = useState<BulkImportResult | null>(
    null
  );
  const [formData, setFormData] = useState<
    CreateUserRequest | UpdateUserRequest
  >({
    fullName: "",
    email: "",
    password: "",
    role: "PEMOHON",
    division: "",
    unitKerja: "",
    phoneNumber: "",
  });
  const [formError, setFormError] = useState<string | null>(null);

  const {
    data: users,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["users"],
    queryFn: async () => {
      const response = await userService.getAll();
      // Backend returns ApiResponse<User[]>, so data might be double-nested
      return response.data || [];
    },
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateUserRequest) => userService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      setShowModal(false);
      resetForm();
    },
    onError: (error: any) => {
      setFormError(error.response?.data?.message || "Gagal membuat user");
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateUserRequest }) =>
      userService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      setShowModal(false);
      resetForm();
    },
    onError: (error: any) => {
      setFormError(error.response?.data?.message || "Gagal mengupdate user");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => userService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      setShowDeleteModal(false);
      setDeletingUser(null);
    },
  });

  const importMutation = useMutation({
    mutationFn: (file: File) => userService.importFromExcel(file),
    onSuccess: (response) => {
      queryClient.invalidateQueries({ queryKey: ["users"] });
      // Backend returns ApiResponse<BulkImportResult>
      setImportResult(response.data || null);
      setShowImportModal(false);
      setShowImportResult(true);
      setSelectedFile(null);
    },
  });

  const handleOpenCreateModal = () => {
    setEditingUser(null);
    resetForm();
    setShowModal(true);
  };

  const handleOpenEditModal = (user: UserType) => {
    setEditingUser(user);
    setFormData({
      fullName: user.fullName,
      email: user.email,
      role: user.role,
      division: user.division || "",
      unitKerja: user.unitKerja || "",
      phoneNumber: user.phoneNumber || "",
      isActive: user.isActive,
    });
    setShowModal(true);
  };

  const handleOpenDeleteModal = (user: UserType) => {
    setDeletingUser(user);
    setShowDeleteModal(true);
  };

  const resetForm = () => {
    setFormData({
      fullName: "",
      email: "",
      password: "",
      role: "PEMOHON",
      division: "",
      unitKerja: "",
      phoneNumber: "",
    });
    setFormError(null);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);

    // Validation
    if (!formData.fullName.trim() || !formData.email.trim()) {
      setFormError("Nama lengkap dan email wajib diisi");
      return;
    }

    if (!editingUser && !(formData as CreateUserRequest).password) {
      setFormError("Password wajib diisi");
      return;
    }

    if (editingUser) {
      updateMutation.mutate({
        id: editingUser.id,
        data: formData as UpdateUserRequest,
      });
    } else {
      createMutation.mutate(formData as CreateUserRequest);
    }
  };

  const handleDelete = () => {
    if (deletingUser) {
      deleteMutation.mutate(deletingUser.id);
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      if (!file.name.endsWith(".xlsx")) {
        alert("File harus berformat .xlsx");
        return;
      }
      setSelectedFile(file);
    }
  };

  const handleImport = () => {
    if (selectedFile) {
      importMutation.mutate(selectedFile);
    }
  };

  const handleDownloadTemplate = async () => {
    try {
      await userService.downloadTemplate();
    } catch (error) {
      console.error("Error downloading template:", error);
      alert("Gagal mendownload template");
    }
  };

  const filteredUsers = users?.filter(
    (user: UserType) =>
      user.fullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.role.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const getRoleLabel = (role: string) => {
    const labels: Record<string, string> = {
      PEMOHON: "Pemohon",
      PIC_APPROVAL_L1: "PIC Approval L1",
      PIC_APPROVAL_L2: "PIC Approval L2",
      DRIVER: "Driver",
      ADMIN: "Administrator",
    };
    return labels[role] || role;
  };

  const getRoleBadgeColor = (role: string) => {
    const colors: Record<string, string> = {
      ADMIN: "bg-purple-100 text-purple-700",
      PIC_APPROVAL_L1: "bg-blue-100 text-blue-700",
      PIC_APPROVAL_L2: "bg-indigo-100 text-indigo-700",
      DRIVER: "bg-green-100 text-green-700",
      PEMOHON: "bg-gray-100 text-gray-700",
    };
    return colors[role] || "bg-gray-100 text-gray-700";
  };

  if (isLoading) return <PageLoading />;

  if (error) {
    return (
      <Alert variant="error">Terjadi kesalahan saat memuat data users</Alert>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Manajemen User</h1>
          <p className="text-gray-600 mt-1">
            Kelola user sistem dan import data dari Excel
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button
            variant="outline"
            onClick={() => setShowImportModal(true)}
            className="flex items-center gap-2"
          >
            <Upload className="w-4 h-4" />
            Import Excel
          </Button>
          <Button
            onClick={handleOpenCreateModal}
            className="flex items-center gap-2"
          >
            <Plus className="w-4 h-4" />
            Tambah User
          </Button>
        </div>
      </div>

      {/* Search */}
      <Card>
        <CardContent className="pt-6">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-5 h-5" />
            <input
              type="text"
              placeholder="Cari user (nama, email, role)..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
            />
          </div>
        </CardContent>
      </Card>

      {/* Users Table */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Users className="w-5 h-5" />
            Daftar User ({filteredUsers?.length || 0})
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-gray-200 bg-gray-50">
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">
                    Nama
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">
                    Email
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">
                    Role
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">
                    Divisi
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">
                    Unit Kerja
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">
                    No. HP
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-semibold text-gray-600 uppercase">
                    Status
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-gray-600 uppercase">
                    Aksi
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {filteredUsers?.map((user: UserType) => (
                  <tr key={user.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3">
                      <div className="font-medium text-gray-900">
                        {user.fullName}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-gray-600">{user.email}</td>
                    <td className="px-4 py-3">
                      <span
                        className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getRoleBadgeColor(
                          user.role
                        )}`}
                      >
                        {getRoleLabel(user.role)}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-gray-600">
                      {user.division || "-"}
                    </td>
                    <td className="px-4 py-3 text-gray-600">
                      {user.unitKerja || "-"}
                    </td>
                    <td className="px-4 py-3 text-gray-600">
                      {user.phoneNumber || "-"}
                    </td>
                    <td className="px-4 py-3">
                      <div
                        className={
                          user.isActive
                            ? "bg-green-100 text-green-700 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                            : "bg-red-100 text-red-700 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
                        }
                      >
                        {user.isActive ? "Aktif" : "Nonaktif"}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-2">
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleOpenEditModal(user)}
                        >
                          <Edit2 className="w-4 h-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          onClick={() => handleOpenDeleteModal(user)}
                          className="text-red-600 hover:text-red-700 hover:bg-red-50"
                        >
                          <Trash2 className="w-4 h-4" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {filteredUsers?.length === 0 && (
              <div className="text-center py-12">
                <Users className="w-12 h-12 text-gray-300 mx-auto mb-3" />
                <p className="text-gray-500">Tidak ada user ditemukan</p>
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Create/Edit Modal */}
      <Modal
        isOpen={showModal}
        onClose={() => {
          setShowModal(false);
          resetForm();
        }}
        title={editingUser ? "Edit User" : "Tambah User Baru"}
      >
        <form onSubmit={handleSubmit} className="space-y-4">
          {formError && (
            <Alert variant="error" className="mb-4">
              {formError}
            </Alert>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Nama Lengkap *
            </label>
            <input
              type="text"
              value={formData.fullName}
              onChange={(e) =>
                setFormData({ ...formData, fullName: e.target.value })
              }
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Email *
            </label>
            <input
              type="email"
              value={formData.email}
              onChange={(e) =>
                setFormData({ ...formData, email: e.target.value })
              }
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              required
            />
          </div>

          {!editingUser && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Password *
              </label>
              <input
                type="password"
                value={(formData as CreateUserRequest).password || ""}
                onChange={(e) =>
                  setFormData({ ...formData, password: e.target.value })
                }
                className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                required={!editingUser}
                minLength={6}
              />
              <p className="text-xs text-gray-500 mt-1">Minimal 6 karakter</p>
            </div>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Role *
            </label>
            <select
              value={formData.role}
              onChange={(e) =>
                setFormData({ ...formData, role: e.target.value })
              }
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              required
            >
              <option value="PEMOHON">Pemohon</option>
              <option value="PIC_APPROVAL_L1">PIC Approval L1</option>
              <option value="PIC_APPROVAL_L2">PIC Approval L2</option>
              <option value="DRIVER">Driver</option>
              <option value="ADMIN">Administrator</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Divisi
            </label>
            <input
              type="text"
              value={formData.division || ""}
              onChange={(e) =>
                setFormData({ ...formData, division: e.target.value })
              }
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Unit Kerja
            </label>
            <input
              type="text"
              value={formData.unitKerja || ""}
              onChange={(e) =>
                setFormData({ ...formData, unitKerja: e.target.value })
              }
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              No. HP
            </label>
            <input
              type="tel"
              value={formData.phoneNumber || ""}
              onChange={(e) =>
                setFormData({ ...formData, phoneNumber: e.target.value })
              }
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              placeholder="08123456789"
            />
          </div>

          {editingUser && (
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="isActive"
                checked={(formData as UpdateUserRequest).isActive ?? true}
                onChange={(e) =>
                  setFormData({ ...formData, isActive: e.target.checked })
                }
                className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
              />
              <label
                htmlFor="isActive"
                className="text-sm font-medium text-gray-700"
              >
                User Aktif
              </label>
            </div>
          )}

          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setShowModal(false);
                resetForm();
              }}
            >
              Batal
            </Button>
            <Button
              type="submit"
              disabled={createMutation.isPending || updateMutation.isPending}
            >
              {createMutation.isPending || updateMutation.isPending
                ? "Menyimpan..."
                : editingUser
                ? "Update"
                : "Simpan"}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={showDeleteModal}
        onClose={() => {
          setShowDeleteModal(false);
          setDeletingUser(null);
        }}
        title="Hapus User"
      >
        <div className="space-y-4">
          <div className="flex items-start gap-3 p-4 bg-red-50 rounded-lg">
            <AlertTriangle className="w-5 h-5 text-red-600 flex-shrink-0 mt-0.5" />
            <div>
              <p className="text-sm text-red-800 font-medium">
                Apakah Anda yakin ingin menghapus user ini?
              </p>
              <p className="text-sm text-red-700 mt-1">
                User: <strong>{deletingUser?.fullName}</strong> (
                {deletingUser?.email})
              </p>
              <p className="text-xs text-red-600 mt-2">
                User akan dinonaktifkan dan tidak bisa login lagi.
              </p>
            </div>
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setShowDeleteModal(false);
                setDeletingUser(null);
              }}
            >
              Batal
            </Button>
            <Button
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
              className="bg-red-600 hover:bg-red-700"
            >
              {deleteMutation.isPending ? "Menghapus..." : "Hapus User"}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Import Modal */}
      <Modal
        isOpen={showImportModal}
        onClose={() => {
          setShowImportModal(false);
          setSelectedFile(null);
        }}
        title="Import User dari Excel"
      >
        <div className="space-y-4">
          <Alert variant="info">
            <p className="text-sm">
              Download template Excel terlebih dahulu, isi data user, kemudian
              upload file tersebut.
            </p>
          </Alert>

          <div>
            <Button
              variant="outline"
              onClick={handleDownloadTemplate}
              className="w-full flex items-center justify-center gap-2"
            >
              <Download className="w-4 h-4" />
              Download Template Excel
            </Button>
          </div>

          <div className="border-t pt-4">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Upload File Excel (.xlsx)
            </label>
            <input
              type="file"
              accept=".xlsx"
              onChange={handleFileSelect}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
            />
            {selectedFile && (
              <p className="text-sm text-gray-600 mt-2">
                File dipilih: <strong>{selectedFile.name}</strong>
              </p>
            )}
          </div>

          <div className="bg-gray-50 p-3 rounded-lg">
            <p className="text-xs text-gray-600 font-semibold mb-1">
              Format Excel:
            </p>
            <p className="text-xs text-gray-600">
              FullName | Email | Role | Division | PhoneNumber | Password
            </p>
            <p className="text-xs text-gray-500 mt-2">
              <strong>Valid Roles:</strong> PEMOHON, PIC_APPROVAL_L1,
              PIC_APPROVAL_L2, DRIVER, ADMIN
            </p>
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setShowImportModal(false);
                setSelectedFile(null);
              }}
            >
              Batal
            </Button>
            <Button
              onClick={handleImport}
              disabled={!selectedFile || importMutation.isPending}
            >
              {importMutation.isPending ? "Mengimport..." : "Import"}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Import Result Modal */}
      <Modal
        isOpen={showImportResult}
        onClose={() => {
          setShowImportResult(false);
          setImportResult(null);
        }}
        title="Hasil Import"
      >
        <div className="space-y-4">
          {importResult && (
            <>
              <div className="grid grid-cols-3 gap-4">
                <div className="bg-gray-50 p-4 rounded-lg text-center">
                  <p className="text-2xl font-bold text-gray-900">
                    {importResult.totalRows}
                  </p>
                  <p className="text-sm text-gray-600">Total Baris</p>
                </div>
                <div className="bg-green-50 p-4 rounded-lg text-center">
                  <p className="text-2xl font-bold text-green-700">
                    {importResult.successCount}
                  </p>
                  <p className="text-sm text-green-600">Berhasil</p>
                </div>
                <div className="bg-red-50 p-4 rounded-lg text-center">
                  <p className="text-2xl font-bold text-red-700">
                    {importResult.failedCount}
                  </p>
                  <p className="text-sm text-red-600">Gagal</p>
                </div>
              </div>

              {importResult.errors.length > 0 && (
                <div>
                  <h4 className="font-semibold text-gray-900 mb-2">
                    Error Details:
                  </h4>
                  <div className="max-h-60 overflow-y-auto space-y-2">
                    {importResult.errors.map((error, index) => (
                      <div
                        key={index}
                        className="p-3 bg-red-50 border border-red-200 rounded-lg"
                      >
                        <p className="text-sm font-medium text-red-900">
                          Baris {error.rowNumber}: {error.email}
                        </p>
                        <p className="text-xs text-red-700 mt-1">
                          {error.errorMessage}
                        </p>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </>
          )}

          <div className="flex justify-end pt-4 border-t">
            <Button
              onClick={() => {
                setShowImportResult(false);
                setImportResult(null);
              }}
            >
              Tutup
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}

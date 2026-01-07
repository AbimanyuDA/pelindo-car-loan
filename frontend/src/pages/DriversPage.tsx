import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Plus, Pencil, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Table, Column } from "@/components/ui/Table";
import { Badge } from "@/components/ui/Badge";
import { Modal, ConfirmModal } from "@/components/ui/Modal";
import { Input } from "@/components/ui/Input";
import { Select } from "@/components/ui/Select";
import { PageLoading } from "@/components/ui/Loading";
import { Alert } from "@/components/ui/Alert";
import { driverService } from "@/services";
import type { Driver } from "@/types";

const driverSchema = z.object({
  name: z
    .string()
    .min(2, "Nama minimal 2 karakter")
    .max(100, "Maksimal 100 karakter"),
  phoneNumber: z
    .string()
    .min(10, "Nomor HP minimal 10 digit")
    .max(20, "Maksimal 20 karakter"),
  licenseNumber: z
    .string()
    .min(5, "Nomor SIM minimal 5 karakter")
    .max(50, "Maksimal 50 karakter"),
  status: z.string().min(1, "Status wajib diisi"),
});

type DriverFormData = z.infer<typeof driverSchema>;

export default function DriversPage() {
  const queryClient = useQueryClient();
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingDriver, setEditingDriver] = useState<Driver | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<DriverFormData>({
    resolver: zodResolver(driverSchema),
    defaultValues: {
      status: "AVAILABLE",
    },
  });

  const { data: drivers, isLoading } = useQuery({
    queryKey: ["drivers"],
    queryFn: async () => {
      const response = await driverService.getAll();
      return response.data || [];
    },
  });

  const createMutation = useMutation({
    mutationFn: driverService.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["drivers"] });
      closeForm();
    },
    onError: (err: unknown) => {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || "Gagal menambah driver");
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: DriverFormData }) =>
      driverService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["drivers"] });
      closeForm();
    },
    onError: (err: unknown) => {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || "Gagal mengupdate driver");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: driverService.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["drivers"] });
      setDeleteId(null);
    },
  });

  const openCreateForm = () => {
    setEditingDriver(null);
    setError(null);
    reset({
      name: "",
      phoneNumber: "",
      licenseNumber: "",
      status: "AVAILABLE",
    });
    setIsFormOpen(true);
  };

  const openEditForm = (driver: Driver) => {
    setEditingDriver(driver);
    setError(null);
    reset({
      name: driver.name,
      phoneNumber: driver.phoneNumber,
      licenseNumber: driver.licenseNumber,
      status: driver.status,
    });
    setIsFormOpen(true);
  };

  const closeForm = () => {
    setIsFormOpen(false);
    setEditingDriver(null);
    setError(null);
    reset();
  };

  const onSubmit = (data: DriverFormData) => {
    if (editingDriver) {
      updateMutation.mutate({ id: editingDriver.id, data });
    } else {
      createMutation.mutate(data);
    }
  };

  const statusOptions = [
    { value: "AVAILABLE", label: "Tersedia" },
    { value: "ON_DUTY", label: "Bertugas" },
    { value: "OFF_DUTY", label: "Tidak Bertugas" },
    { value: "ON_LEAVE", label: "Cuti" },
  ];

  const columns: Column<Driver>[] = [
    {
      key: "id",
      header: "ID",
      render: (item) => <span className="font-mono text-xs">#{item.id}</span>,
    },
    {
      key: "name",
      header: "Nama",
      render: (item) => <span className="font-medium">{item.name}</span>,
    },
    {
      key: "phoneNumber",
      header: "No. HP",
      render: (item) => <span>{item.phoneNumber}</span>,
    },
    {
      key: "licenseNumber",
      header: "No. SIM",
      render: (item) => (
        <span className="font-mono text-sm">{item.licenseNumber}</span>
      ),
    },
    {
      key: "status",
      header: "Status",
      render: (item) => <Badge status={item.status} />,
    },
    {
      key: "actions",
      header: "Aksi",
      render: (item) => (
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="sm" onClick={() => openEditForm(item)}>
            <Pencil className="w-4 h-4" />
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setDeleteId(item.id)}
            disabled={item.status === "ON_DUTY"}
          >
            <Trash2 className="w-4 h-4 text-red-500" />
          </Button>
        </div>
      ),
    },
  ];

  if (isLoading) return <PageLoading />;

  const stats = {
    total: drivers?.length || 0,
    available: drivers?.filter((d) => d.status === "AVAILABLE").length || 0,
    onDuty: drivers?.filter((d) => d.status === "ON_DUTY").length || 0,
    onLeave: drivers?.filter((d) => d.status === "ON_LEAVE").length || 0,
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Driver</h1>
          <p className="text-gray-600">Kelola data driver kendaraan</p>
        </div>
        <Button
          onClick={openCreateForm}
          leftIcon={<Plus className="w-4 h-4" />}
        >
          Tambah Driver
        </Button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Total</p>
            <p className="text-2xl font-bold text-gray-900">{stats.total}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Tersedia</p>
            <p className="text-2xl font-bold text-green-600">
              {stats.available}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Bertugas</p>
            <p className="text-2xl font-bold text-blue-600">{stats.onDuty}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Cuti</p>
            <p className="text-2xl font-bold text-orange-600">
              {stats.onLeave}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Table */}
      <Card>
        <CardHeader>
          <CardTitle>Daftar Driver</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <Table
            columns={columns}
            data={drivers || []}
            keyExtractor={(item) => item.id}
            emptyMessage="Belum ada driver terdaftar"
          />
        </CardContent>
      </Card>

      {/* Form Modal */}
      <Modal
        isOpen={isFormOpen}
        onClose={closeForm}
        title={editingDriver ? "Edit Driver" : "Tambah Driver"}
      >
        {error && (
          <Alert
            variant="error"
            className="mb-4"
            onClose={() => setError(null)}
          >
            {error}
          </Alert>
        )}

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <Input
            label="Nama Lengkap *"
            placeholder="Contoh: Budi Santoso"
            error={errors.name?.message}
            {...register("name")}
          />

          <Input
            label="Nomor HP *"
            placeholder="Contoh: 081234567890"
            error={errors.phoneNumber?.message}
            {...register("phoneNumber")}
          />

          <Input
            label="Nomor SIM *"
            placeholder="Contoh: 1234567890123456"
            error={errors.licenseNumber?.message}
            {...register("licenseNumber")}
          />

          <Select
            label="Status *"
            options={statusOptions}
            error={errors.status?.message}
            {...register("status")}
          />

          <div className="flex justify-end gap-3 mt-6">
            <Button variant="ghost" type="button" onClick={closeForm}>
              Batal
            </Button>
            <Button
              type="submit"
              isLoading={createMutation.isPending || updateMutation.isPending}
            >
              {editingDriver ? "Update" : "Simpan"}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Delete Confirmation */}
      <ConfirmModal
        isOpen={deleteId !== null}
        onClose={() => setDeleteId(null)}
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
        title="Hapus Driver"
        message="Apakah Anda yakin ingin menghapus driver ini? Tindakan ini tidak dapat dibatalkan."
        confirmText="Ya, Hapus"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  );
}

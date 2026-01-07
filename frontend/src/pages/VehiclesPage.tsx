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
import { vehicleService } from "@/services";
import type { Vehicle } from "@/types";

const vehicleSchema = z.object({
  plateNumber: z
    .string()
    .min(1, "Nomor plat wajib diisi")
    .max(20, "Maksimal 20 karakter"),
  model: z
    .string()
    .min(1, "Model wajib diisi")
    .max(100, "Maksimal 100 karakter"),
  capacity: z.coerce
    .number()
    .min(1, "Minimal 1 kursi")
    .max(50, "Maksimal 50 kursi"),
  status: z.string().min(1, "Status wajib diisi"),
});

type VehicleFormData = z.infer<typeof vehicleSchema>;

export default function VehiclesPage() {
  const queryClient = useQueryClient();
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingVehicle, setEditingVehicle] = useState<Vehicle | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<VehicleFormData>({
    resolver: zodResolver(vehicleSchema),
    defaultValues: {
      status: "AVAILABLE",
    },
  });

  const { data: vehicles, isLoading } = useQuery({
    queryKey: ["vehicles"],
    queryFn: async () => {
      const response = await vehicleService.getAll();
      return response.data || [];
    },
  });

  const createMutation = useMutation({
    mutationFn: vehicleService.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
      closeForm();
    },
    onError: (err: unknown) => {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || "Gagal menambah kendaraan");
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: VehicleFormData }) =>
      vehicleService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
      closeForm();
    },
    onError: (err: unknown) => {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || "Gagal mengupdate kendaraan");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: vehicleService.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
      setDeleteId(null);
    },
  });

  const openCreateForm = () => {
    setEditingVehicle(null);
    setError(null);
    reset({
      plateNumber: "",
      model: "",
      capacity: 4,
      status: "AVAILABLE",
    });
    setIsFormOpen(true);
  };

  const openEditForm = (vehicle: Vehicle) => {
    setEditingVehicle(vehicle);
    setError(null);
    reset({
      plateNumber: vehicle.plateNumber,
      model: vehicle.model,
      capacity: vehicle.capacity,
      status: vehicle.status,
    });
    setIsFormOpen(true);
  };

  const closeForm = () => {
    setIsFormOpen(false);
    setEditingVehicle(null);
    setError(null);
    reset();
  };

  const onSubmit = (data: VehicleFormData) => {
    if (editingVehicle) {
      updateMutation.mutate({ id: editingVehicle.id, data });
    } else {
      createMutation.mutate(data);
    }
  };

  const statusOptions = [
    { value: "AVAILABLE", label: "Tersedia" },
    { value: "IN_USE", label: "Sedang Digunakan" },
    { value: "MAINTENANCE", label: "Maintenance" },
    { value: "RETIRED", label: "Tidak Aktif" },
  ];

  const columns: Column<Vehicle>[] = [
    {
      key: "id",
      header: "ID",
      render: (item) => <span className="font-mono text-xs">#{item.id}</span>,
    },
    {
      key: "plateNumber",
      header: "Nomor Plat",
      render: (item) => <span className="font-medium">{item.plateNumber}</span>,
    },
    {
      key: "model",
      header: "Model",
      render: (item) => <span>{item.model}</span>,
    },
    {
      key: "capacity",
      header: "Kapasitas",
      render: (item) => <span>{item.capacity} kursi</span>,
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
            disabled={item.status === "IN_USE"}
          >
            <Trash2 className="w-4 h-4 text-red-500" />
          </Button>
        </div>
      ),
    },
  ];

  if (isLoading) return <PageLoading />;

  const stats = {
    total: vehicles?.length || 0,
    available: vehicles?.filter((v) => v.status === "AVAILABLE").length || 0,
    inUse: vehicles?.filter((v) => v.status === "IN_USE").length || 0,
    maintenance:
      vehicles?.filter((v) => v.status === "MAINTENANCE").length || 0,
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Kendaraan</h1>
          <p className="text-gray-600">Kelola data kendaraan operasional</p>
        </div>
        <Button
          onClick={openCreateForm}
          leftIcon={<Plus className="w-4 h-4" />}
        >
          Tambah Kendaraan
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
            <p className="text-sm text-gray-500">Sedang Digunakan</p>
            <p className="text-2xl font-bold text-blue-600">{stats.inUse}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Maintenance</p>
            <p className="text-2xl font-bold text-orange-600">
              {stats.maintenance}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Table */}
      <Card>
        <CardHeader>
          <CardTitle>Daftar Kendaraan</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <Table
            columns={columns}
            data={vehicles || []}
            keyExtractor={(item) => item.id}
            emptyMessage="Belum ada kendaraan terdaftar"
          />
        </CardContent>
      </Card>

      {/* Form Modal */}
      <Modal
        isOpen={isFormOpen}
        onClose={closeForm}
        title={editingVehicle ? "Edit Kendaraan" : "Tambah Kendaraan"}
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
            label="Nomor Plat *"
            placeholder="Contoh: B 1234 CD"
            error={errors.plateNumber?.message}
            {...register("plateNumber")}
          />

          <Input
            label="Model *"
            placeholder="Contoh: Toyota Innova"
            error={errors.model?.message}
            {...register("model")}
          />

          <Input
            type="number"
            label="Kapasitas (kursi) *"
            placeholder="Contoh: 7"
            error={errors.capacity?.message}
            {...register("capacity")}
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
              {editingVehicle ? "Update" : "Simpan"}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Delete Confirmation */}
      <ConfirmModal
        isOpen={deleteId !== null}
        onClose={() => setDeleteId(null)}
        onConfirm={() => deleteId && deleteMutation.mutate(deleteId)}
        title="Hapus Kendaraan"
        message="Apakah Anda yakin ingin menghapus kendaraan ini? Tindakan ini tidak dapat dibatalkan."
        confirmText="Ya, Hapus"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  );
}

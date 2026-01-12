import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Plus, Pencil, Trash2, Upload, Download } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Table, Column } from "@/components/ui/Table";
import { Badge } from "@/components/ui/Badge";
import { Modal, ConfirmModal } from "@/components/ui/Modal";
import { Input } from "@/components/ui/Input";
import { PageLoading } from "@/components/ui/Loading";
import { Alert } from "@/components/ui/Alert";
import { vehicleService } from "@/services";
import type { Vehicle } from "@/types";

const vehicleSchema = z.object({
  plateNumber: z
    .string()
    .min(1, "Nomor plat wajib diisi")
    .max(20, "Maksimal 20 karakter"),
  brand: z
    .string()
    .min(1, "Merek wajib diisi")
    .max(50, "Maksimal 50 karakter"),
  type: z
    .string()
    .min(1, "Tipe wajib diisi")
    .max(50, "Maksimal 50 karakter"),
  capacity: z.coerce
    .number()
    .min(1, "Minimal 1 kursi")
    .max(50, "Maksimal 50 kursi"),
  notes: z.string().optional(),
});

type VehicleFormData = z.infer<typeof vehicleSchema>;

export default function VehiclesPage() {
  const queryClient = useQueryClient();
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingVehicle, setEditingVehicle] = useState<Vehicle | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Import states
  const [isImportOpen, setIsImportOpen] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [importResult, setImportResult] = useState<any>(null);
  const [showImportResult, setShowImportResult] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<VehicleFormData>({
    resolver: zodResolver(vehicleSchema),
    defaultValues: {
      plateNumber: "",
      brand: "",
      type: "",
      capacity: 4,
      notes: "",
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

  const importMutation = useMutation({
    mutationFn: vehicleService.importFromExcel,
    onSuccess: (response) => {
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
      setImportResult(response.data);
      setShowImportResult(true);
      setIsImportOpen(false);
      setSelectedFile(null);
    },
    onError: (err: unknown) => {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || "Gagal import data");
      setIsImportOpen(false);
      setSelectedFile(null);
    },
  });

  const openCreateForm = () => {
    setEditingVehicle(null);
    setError(null);
    reset({
      plateNumber: "",
      brand: "",
      type: "",
      capacity: 4,
      notes: "",
    });
    setIsFormOpen(true);
  };

  const openEditForm = (vehicle: Vehicle) => {
    setEditingVehicle(vehicle);
    setError(null);
    reset({
      plateNumber: vehicle.plateNumber,
      brand: vehicle.brand,
      type: vehicle.type,
      capacity: vehicle.capacity,
      notes: vehicle.notes || "",
    });
    setIsFormOpen(true);
  };

  const closeForm = () => {
    setIsFormOpen(false);
    setEditingVehicle(null);
    setError(null);
    reset();
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      if (!file.name.endsWith(".xlsx")) {
        setError("File harus berformat .xlsx");
        return;
      }
      setSelectedFile(file);
      setError(null);
    }
  };

  const handleImport = () => {
    if (!selectedFile) {
      setError("Pilih file terlebih dahulu");
      return;
    }
    importMutation.mutate(selectedFile);
  };

  const handleDownloadTemplate = () => {
    vehicleService.downloadTemplate();
  };

  const onSubmit = (data: VehicleFormData) => {
    if (editingVehicle) {
      updateMutation.mutate({ id: editingVehicle.id, data });
    } else {
      createMutation.mutate(data);
    }
  };

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
      key: "brand",
      header: "Merek",
      render: (item) => <span>{item.brand}</span>,
    },
    {
      key: "type",
      header: "Tipe",
      render: (item) => <span>{item.type}</span>,
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
    <div className="space-y-4 sm:space-y-6">
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 sm:gap-0">
        <div>
          <h1 className="text-xl sm:text-2xl font-bold text-gray-900">
            Kendaraan
          </h1>
          <p className="text-sm sm:text-base text-gray-600">
            Kelola data kendaraan operasional
          </p>
        </div>
        <div className="flex gap-2 w-full sm:w-auto">
          <Button
            onClick={() => setIsImportOpen(true)}
            leftIcon={<Upload className="w-4 h-4" />}
            variant="outline"
            className="flex-1 sm:flex-initial"
          >
            Import Excel
          </Button>
          <Button
            onClick={openCreateForm}
            leftIcon={<Plus className="w-4 h-4" />}
            className="flex-1 sm:flex-initial"
          >
            Tambah Kendaraan
          </Button>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-2 sm:gap-4">
        <Card>
          <CardContent className="py-3 sm:py-4">
            <p className="text-xs sm:text-sm text-gray-500">Total</p>
            <p className="text-xl sm:text-2xl font-bold text-gray-900">
              {stats.total}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-3 sm:py-4">
            <p className="text-xs sm:text-sm text-gray-500">Tersedia</p>
            <p className="text-xl sm:text-2xl font-bold text-green-600">
              {stats.available}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-3 sm:py-4">
            <p className="text-xs sm:text-sm text-gray-500">Sedang Digunakan</p>
            <p className="text-xl sm:text-2xl font-bold text-blue-600">
              {stats.inUse}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-3 sm:py-4">
            <p className="text-xs sm:text-sm text-gray-500">Maintenance</p>
            <p className="text-xl sm:text-2xl font-bold text-orange-600">
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
            label="Merek *"
            placeholder="Contoh: Toyota"
            error={errors.brand?.message}
            {...register("brand")}
          />

          <Input
            label="Tipe *"
            placeholder="Contoh: Sedan, SUV, MPV"
            error={errors.type?.message}
            {...register("type")}
          />

          <Input
            type="number"
            label="Kapasitas (kursi) *"
            placeholder="Contoh: 7"
            error={errors.capacity?.message}
            {...register("capacity")}
          />

          <Input
            label="Catatan"
            placeholder="Catatan tambahan (opsional)"
            error={errors.notes?.message}
            {...register("notes")}
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

      {/* Import Modal */}
      <Modal
        isOpen={isImportOpen}
        onClose={() => {
          setIsImportOpen(false);
          setSelectedFile(null);
          setError(null);
        }}
        title="Import Data Kendaraan"
      >
        <div className="space-y-4">
          <div className="bg-blue-50 border border-blue-200 rounded-md p-4">
            <p className="text-sm text-blue-800 mb-2">
              <strong>Format Excel:</strong>
            </p>
            <ul className="text-sm text-blue-700 space-y-1 list-disc list-inside">
              <li>
                Kolom: PlateNumber | Brand | Type | Model | Capacity | Status
              </li>
              <li>Status: AVAILABLE, IN_USE, MAINTENANCE, RETIRED</li>
              <li>Capacity: Angka (jumlah kursi)</li>
            </ul>
          </div>

          <Button
            onClick={handleDownloadTemplate}
            leftIcon={<Download className="w-4 h-4" />}
            variant="outline"
            className="w-full"
          >
            Download Template Excel
          </Button>

          {error && (
            <Alert
              variant="error"
              className="mb-4"
              onClose={() => setError(null)}
            >
              {error}
            </Alert>
          )}

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Pilih File Excel
            </label>
            <input
              type="file"
              accept=".xlsx"
              onChange={handleFileSelect}
              className="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100"
            />
            {selectedFile && (
              <p className="mt-2 text-sm text-gray-600">
                File terpilih: {selectedFile.name}
              </p>
            )}
          </div>

          <div className="flex justify-end gap-3 mt-6">
            <Button
              variant="ghost"
              type="button"
              onClick={() => {
                setIsImportOpen(false);
                setSelectedFile(null);
                setError(null);
              }}
            >
              Batal
            </Button>
            <Button
              onClick={handleImport}
              isLoading={importMutation.isPending}
              disabled={!selectedFile}
            >
              Import Data
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
        {importResult && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <Card>
                <CardContent className="py-4">
                  <p className="text-sm text-gray-500">Berhasil</p>
                  <p className="text-2xl font-bold text-green-600">
                    {importResult.successCount}
                  </p>
                </CardContent>
              </Card>
              <Card>
                <CardContent className="py-4">
                  <p className="text-sm text-gray-500">Gagal</p>
                  <p className="text-2xl font-bold text-red-600">
                    {importResult.failedCount}
                  </p>
                </CardContent>
              </Card>
            </div>

            {importResult.errors && importResult.errors.length > 0 && (
              <div>
                <h4 className="font-medium text-gray-900 mb-2">
                  Detail Error:
                </h4>
                <div className="bg-red-50 border border-red-200 rounded-md p-4 max-h-60 overflow-y-auto">
                  <ul className="text-sm text-red-700 space-y-1">
                    {importResult.errors.map((error: any, index: number) => (
                      <li key={index}>
                        Baris {error.rowNumber}: {error.errorMessage}
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            )}

            <div className="flex justify-end">
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
        )}
      </Modal>
    </div>
  );
}

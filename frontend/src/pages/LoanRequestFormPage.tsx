import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQuery } from "@tanstack/react-query";
import { ArrowLeft, Send } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Textarea } from "@/components/ui/Textarea";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardDescription,
} from "@/components/ui/Card";
import { Alert } from "@/components/ui/Alert";
import { loanRequestService, vehicleService, driverService } from "@/services";
import api from "@/services/api";

const loanRequestSchema = z.object({
  serviceLetterBasis: z
    .string()
    .min(5, "Dasar Surat Pelayanan minimal 5 karakter")
    .max(100, "Dasar Surat Pelayanan maksimal 100 karakter"),
  serviceLetterFile: z
    .instanceof(FileList)
    .refine((files) => files.length > 0, "Upload surat pelayanan wajib diisi"),
  purpose: z
    .string()
    .min(5, "Tujuan minimal 5 karakter")
    .max(200, "Keperluan peminjaman maksimal 200 karakter"),
  destination: z
    .string()
    .min(3, "Destinasi minimal 3 karakter")
    .max(200, "Destinasi maksimal 200 karakter"),
  guestList: z
    .string()
    .min(3, "Daftar tamu minimal 3 karakter")
    .max(500, "Daftar tamu maksimal 500 karakter"),
  hotelAccommodation: z
    .string()
    .max(200, "Hotel maksimal 200 karakter")
    .optional(),
  resourceSelectionMode: z.enum(["self", "assigned"]),
  vehicleId: z.number().optional(),
  driverId: z.number().optional(),
  departureDate: z.string().min(1, "Tanggal keberangkatan wajib diisi"),
  departureTime: z.string().min(1, "Waktu keberangkatan wajib diisi"),
  returnDate: z.string().min(1, "Tanggal kembali wajib diisi"),
  returnTime: z.string().min(1, "Waktu kembali wajib diisi"),
  notes: z.string().max(500, "Catatan maksimal 500 karakter").optional(),
});

type LoanRequestFormData = z.infer<typeof loanRequestSchema>;

export default function LoanRequestFormPage() {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const [selectionMode, setSelectionMode] = useState<"self" | "assigned">(
    "assigned"
  );

  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue,
    watch,
  } = useForm<LoanRequestFormData>({
    resolver: zodResolver(loanRequestSchema),
    mode: "onSubmit",
    defaultValues: {
      resourceSelectionMode: "assigned",
      guestList: "",
      hotelAccommodation: "",
      notes: "",
    },
  });

  // Watch date changes for availability filtering
  const departureDate = watch("departureDate");
  const departureTime = watch("departureTime");
  const returnDate = watch("returnDate");
  const returnTime = watch("returnTime");

  // Compute start and end datetime for filtering
  const startDatetime =
    departureDate && departureTime
      ? `${departureDate}T${departureTime}:00`
      : undefined;
  const endDatetime =
    returnDate && returnTime ? `${returnDate}T${returnTime}:00` : undefined;

  // Fetch available vehicles based on selected dates
  const { data: vehiclesData } = useQuery({
    queryKey: ["vehicles", "available", startDatetime, endDatetime],
    queryFn: async () => {
      if (startDatetime && endDatetime) {
        // Fetch with date filter to exclude vehicles already scheduled
        const response = await vehicleService.getAvailable(
          startDatetime,
          endDatetime
        );
        return response.data;
      } else {
        // No dates selected, fetch all vehicles
        const response = await vehicleService.getAll();
        return response.data;
      }
    },
    enabled: true,
  });

  // Fetch available drivers based on selected dates
  const { data: driversData } = useQuery({
    queryKey: ["drivers", "available", startDatetime, endDatetime],
    queryFn: async () => {
      if (startDatetime && endDatetime) {
        // Fetch with date filter to exclude drivers already scheduled
        const response = await driverService.getAvailable(
          startDatetime,
          endDatetime
        );
        return response.data;
      } else {
        // No dates selected, fetch all drivers
        const response = await driverService.getAll();
        return response.data;
      }
    },
    enabled: true,
  });

  const createMutation = useMutation({
    mutationFn: loanRequestService.create,
    onSuccess: () => {
      navigate("/loan-requests");
    },
    onError: (err: unknown) => {
      const error = err as {
        response?: {
          data?: { message?: string; errors?: any; title?: string };
        };
      };
      console.error("Create mutation error:", error.response?.data);
      console.error(
        "Validation errors:",
        JSON.stringify(error.response?.data?.errors, null, 2)
      );
      const errorMsg =
        error.response?.data?.title ||
        error.response?.data?.message ||
        "Gagal membuat pengajuan";
      setError(errorMsg);
    },
  });

  // Normalize select values to finite numbers for Zod validation
  const parseNumberValue = (value: string) => {
    const num = Number(value);
    return Number.isFinite(num) ? num : undefined;
  };

  const onSubmit = async (data: LoanRequestFormData) => {
    setError(null);

    // Validate if self mode but no vehicle/driver selected
    if (data.resourceSelectionMode === "self") {
      if (!data.vehicleId || !data.driverId) {
        setError("Mohon pilih kendaraan dan driver");
        return;
      }
    }

    // Validate file upload
    if (!data.serviceLetterFile || data.serviceLetterFile.length === 0) {
      setError("Upload surat pelayanan wajib diisi");
      return;
    }

    try {
      // Upload file first
      let serviceLetterFilePath: string | undefined;
      if (data.serviceLetterFile && data.serviceLetterFile.length > 0) {
        const formData = new FormData();
        formData.append("file", data.serviceLetterFile[0]);

        const uploadResponse = await api.post(
          "/LoanRequests/upload-service-letter",
          formData,
          {
            headers: {
              "Content-Type": "multipart/form-data",
            },
          }
        );

        serviceLetterFilePath = uploadResponse.data.data;
      }

      // Transform form data to API format
      const requestData = {
        serviceLetterBasis: data.serviceLetterBasis,
        serviceLetterFilePath: serviceLetterFilePath || null,
        purpose: data.purpose,
        destination: data.destination,
        guestList: data.guestList,
        hotelAccommodation: data.hotelAccommodation || null,
        vehicleId:
          data.resourceSelectionMode === "self" ? data.vehicleId : null,
        driverId: data.resourceSelectionMode === "self" ? data.driverId : null,
        startDatetime: `${data.departureDate}T${data.departureTime}:00`,
        endDatetime: `${data.returnDate}T${data.returnTime}:00`,
        notes: data.notes || null,
      };

      console.log("Request data:", requestData);
      createMutation.mutate(requestData as any);
    } catch (err: any) {
      console.error("Error submitting:", err);
      setError(
        err.response?.data?.message ||
          err.message ||
          "Terjadi kesalahan saat memproses pengajuan"
      );
    }
  };

  // Get tomorrow's date as minimum date
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  const minDate = tomorrow.toISOString().split("T")[0];

  return (
    <div className="max-w-2xl mx-auto space-y-4 sm:space-y-6 px-2 sm:px-0">
      <div className="flex flex-col sm:flex-row items-start sm:items-center gap-3 sm:gap-4">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate(-1)}
          className="flex-shrink-0"
        >
          <ArrowLeft className="w-4 h-4" />
        </Button>
        <div className="min-w-0">
          <h1 className="text-xl sm:text-2xl font-bold text-gray-900">
            Ajukan Peminjaman
          </h1>
          <p className="text-sm sm:text-base text-gray-600">
            Isi form berikut untuk mengajukan peminjaman kendaraan
          </p>
        </div>
      </div>

      {error && (
        <Alert variant="error" onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Form Pengajuan</CardTitle>
          <CardDescription>
            Pastikan semua informasi yang diisi sudah benar sebelum mengirim
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
            <Input
              label="Dasar Surat Pelayanan (Wajib) *"
              placeholder="Tuliskan Nomor SPPD yang telah anda terima"
              error={errors.serviceLetterBasis?.message}
              {...register("serviceLetterBasis")}
            />

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Upload Surat Pelayanan (PDF) *
              </label>
              <input
                type="file"
                accept=".pdf"
                className="block w-full text-sm text-gray-500
                  file:mr-4 file:py-2 file:px-4
                  file:rounded-lg file:border-0
                  file:text-sm file:font-semibold
                  file:bg-blue-50 file:text-blue-700
                  hover:file:bg-blue-100
                  cursor-pointer"
                {...register("serviceLetterFile")}
              />
              {errors.serviceLetterFile && (
                <p className="mt-1 text-sm text-red-600">
                  {errors.serviceLetterFile.message as string}
                </p>
              )}
              <p className="mt-1 text-xs text-gray-500">
                File PDF maksimal 5MB (wajib)
              </p>
            </div>

            <Input
              label="Tujuan Peminjaman *"
              placeholder="Contoh: Kunjungan ke Terminal Petikemas"
              error={errors.purpose?.message}
              {...register("purpose")}
            />

            <Input
              label="Destinasi *"
              placeholder="Contoh: Terminal Petikemas Surabaya"
              error={errors.destination?.message}
              {...register("destination")}
            />

            <Input
              label="Daftar Tamu yang Dilayani *"
              placeholder="Isikan nama tamu"
              error={errors.guestList?.message}
              {...register("guestList")}
            />

            <Input
              label="Hotel Menginap"
              placeholder="Kosongkan Apabila Tidak Menginap"
              error={errors.hotelAccommodation?.message}
              {...register("hotelAccommodation")}
            />

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
              <Input
                type="date"
                label="Tanggal Keberangkatan *"
                min={minDate}
                error={errors.departureDate?.message}
                {...register("departureDate")}
              />

              <Input
                type="time"
                label="Waktu Berangkat *"
                error={errors.departureTime?.message}
                {...register("departureTime")}
              />

              <Input
                type="date"
                label="Tanggal Kembali *"
                min={minDate}
                error={errors.returnDate?.message}
                {...register("returnDate")}
              />

              <Input
                type="time"
                label="Waktu Kembali *"
                error={errors.returnTime?.message}
                {...register("returnTime")}
              />
            </div>

            {/* Resource Selection Mode */}
            <div className="space-y-3 p-3 sm:p-4 bg-gray-50 rounded-lg border border-gray-200">
              <label className="block text-xs sm:text-sm font-medium text-gray-800 mb-3">
                Pemilihan Kendaraan & Driver *
              </label>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 sm:gap-3">
                <label
                  className={`relative flex items-start sm:items-center p-3 sm:p-4 rounded-lg border-2 cursor-pointer transition-all ${
                    selectionMode === "assigned"
                      ? "border-blue-500 bg-blue-50"
                      : "border-gray-200 bg-white hover:border-gray-300"
                  }`}
                >
                  <input
                    type="radio"
                    value="assigned"
                    {...register("resourceSelectionMode")}
                    checked={selectionMode === "assigned"}
                    onChange={(_e) => {
                      setSelectionMode("assigned");
                      setValue("resourceSelectionMode", "assigned");
                      setValue("vehicleId", undefined);
                      setValue("driverId", undefined);
                    }}
                    className="w-4 h-4 text-blue-600 focus:ring-blue-500"
                  />
                  <div className="ml-2 sm:ml-3 flex-1">
                    <p className="text-xs sm:text-sm font-semibold text-gray-900">
                      Dipilihkan Oleh Approval
                    </p>
                    <p className="text-xs text-gray-600 mt-0.5 hidden sm:block">
                      Sistem akan meneruskan ke approval untuk dipilihkan
                    </p>
                  </div>
                  {selectionMode === "assigned" && (
                    <svg
                      className="w-5 h-5 text-blue-600"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                    >
                      <path
                        fillRule="evenodd"
                        d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                        clipRule="evenodd"
                      />
                    </svg>
                  )}
                </label>

                <label
                  className={`relative flex items-start sm:items-center p-3 sm:p-4 rounded-lg border-2 cursor-pointer transition-all ${
                    selectionMode === "self"
                      ? "border-blue-500 bg-blue-50"
                      : "border-gray-200 bg-white hover:border-gray-300"
                  }`}
                >
                  <input
                    type="radio"
                    value="self"
                    {...register("resourceSelectionMode")}
                    checked={selectionMode === "self"}
                    onChange={(_e) => {
                      setSelectionMode("self");
                      setValue("resourceSelectionMode", "self");
                    }}
                    className="w-4 h-4 text-blue-600 focus:ring-blue-500 flex-shrink-0"
                  />
                  <div className="ml-2 sm:ml-3 flex-1">
                    <p className="text-xs sm:text-sm font-semibold text-gray-900">
                      Pilih Sendiri
                    </p>
                    <p className="text-xs text-gray-600 mt-0.5 hidden sm:block">
                      Saya ingin memilih kendaraan & driver sendiri
                    </p>
                  </div>
                  {selectionMode === "self" && (
                    <svg
                      className="w-5 h-5 text-blue-600"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                    >
                      <path
                        fillRule="evenodd"
                        d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                        clipRule="evenodd"
                      />
                    </svg>
                  )}
                </label>
              </div>
            </div>

            {/* Vehicle & Driver Selection - Only show when "self" mode */}
            {selectionMode === "self" && (
              <div className="space-y-4 p-4 bg-blue-50 rounded-lg border border-blue-200">
                <div className="flex items-center gap-2 text-blue-700 mb-2">
                  <svg
                    className="w-5 h-5"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                  </svg>
                  <span className="text-sm font-medium">
                    Pilih kendaraan dan driver yang tersedia
                  </span>
                </div>

                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <label className="block text-sm font-medium text-gray-800">
                      Pilih Kendaraan *
                    </label>
                    <span className="text-xs text-gray-500">
                      Hanya yang tersedia bisa dipilih
                    </span>
                  </div>
                  <select
                    {...register("vehicleId", { setValueAs: parseNumberValue })}
                    className="w-full px-3 py-2.5 border border-gray-200 rounded-lg bg-white shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition text-sm"
                  >
                    <option value="">Pilih kendaraan tersedia</option>
                    {vehiclesData && vehiclesData.length > 0 ? (
                      vehiclesData.map((vehicle, index) => {
                        const value = vehicle.id;
                        if (!value) return null; // skip items tanpa id
                        return (
                          <option
                            key={`vehicle-${value}-${index}`}
                            value={value}
                          >
                            {vehicle.brand} {vehicle.type} •{" "}
                            {vehicle.plateNumber} — Tersedia
                          </option>
                        );
                      })
                    ) : (
                      <option disabled>
                        {startDatetime && endDatetime
                          ? "Tidak ada kendaraan tersedia di waktu yang dipilih"
                          : "Pilih tanggal terlebih dahulu"}
                      </option>
                    )}
                  </select>
                  {errors.vehicleId && (
                    <p className="text-sm text-red-600">
                      {errors.vehicleId.message}
                    </p>
                  )}
                </div>

                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <label className="block text-sm font-medium text-gray-800">
                      Pilih Driver *
                    </label>
                    <span className="text-xs text-gray-500">
                      Hanya yang tersedia bisa dipilih
                    </span>
                  </div>
                  <select
                    {...register("driverId", { setValueAs: parseNumberValue })}
                    className="w-full px-3 py-2.5 border border-gray-200 rounded-lg bg-white shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition text-sm"
                  >
                    <option value="">Pilih driver tersedia</option>
                    {driversData && driversData.length > 0 ? (
                      driversData.map((driver, index) => {
                        const value = driver.id;
                        if (!value) return null;
                        const displayName =
                          (driver as any).name ||
                          (driver as any).driverName ||
                          `Driver ${value}`;
                        return (
                          <option
                            key={`driver-${value}-${index}`}
                            value={value}
                          >
                            {displayName} — Tersedia
                          </option>
                        );
                      })
                    ) : (
                      <option disabled>
                        {startDatetime && endDatetime
                          ? "Tidak ada driver tersedia di waktu yang dipilih"
                          : "Pilih tanggal terlebih dahulu"}
                      </option>
                    )}
                  </select>
                  {errors.driverId && (
                    <p className="text-sm text-red-600">
                      {errors.driverId.message}
                    </p>
                  )}
                </div>
              </div>
            )}

            <Textarea
              label="Catatan Tambahan"
              placeholder="Catatan tambahan jika ada (opsional)"
              rows={4}
              error={errors.notes?.message}
              {...register("notes")}
            />

            <div className="flex flex-col-reverse sm:flex-row justify-end gap-2 sm:gap-3 pt-4 border-t">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate(-1)}
                className="w-full sm:w-auto"
              >
                Batal
              </Button>
              <Button
                type="submit"
                isLoading={createMutation.isPending}
                leftIcon={<Send className="w-4 h-4" />}
                className="w-full sm:w-auto"
              >
                Kirim Pengajuan
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Info Card */}
      <Card className="bg-blue-50 border-blue-200">
        <CardContent className="py-4">
          <h4 className="font-medium text-blue-900 mb-2">
            Informasi Proses Approval
          </h4>
          <ul className="text-sm text-blue-800 space-y-1">
            <li>• Pengajuan akan diteruskan ke PIC Approval Level 1</li>
            <li>
              • Setelah disetujui L1, akan diteruskan ke PIC Approval Level 2
            </li>
            <li>
              • Kendaraan dan driver akan dijadwalkan otomatis setelah approval
              L2
            </li>
            <li>
              • Anda akan mendapat notifikasi untuk setiap perubahan status
            </li>
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}

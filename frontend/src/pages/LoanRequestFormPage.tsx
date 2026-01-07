import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation } from "@tanstack/react-query";
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
import { loanRequestService } from "@/services";

const loanRequestSchema = z.object({
  purpose: z
    .string()
    .min(5, "Tujuan minimal 5 karakter")
    .max(200, "Tujuan maksimal 200 karakter"),
  destination: z
    .string()
    .min(3, "Destinasi minimal 3 karakter")
    .max(200, "Destinasi maksimal 200 karakter"),
  departureDate: z.string().min(1, "Tanggal keberangkatan wajib diisi"),
  departureTime: z.string().min(1, "Waktu keberangkatan wajib diisi"),
  returnTime: z.string().min(1, "Waktu kembali wajib diisi"),
  passengerCount: z.coerce
    .number()
    .min(1, "Minimal 1 penumpang")
    .max(20, "Maksimal 20 penumpang"),
  notes: z.string().max(500, "Catatan maksimal 500 karakter").optional(),
});

type LoanRequestFormData = z.infer<typeof loanRequestSchema>;

export default function LoanRequestFormPage() {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoanRequestFormData>({
    resolver: zodResolver(loanRequestSchema),
    defaultValues: {
      passengerCount: 1,
      notes: "",
    },
  });

  const createMutation = useMutation({
    mutationFn: loanRequestService.create,
    onSuccess: () => {
      navigate("/loan-requests");
    },
    onError: (err: unknown) => {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || "Gagal membuat pengajuan");
    },
  });

  const onSubmit = (data: LoanRequestFormData) => {
    setError(null);
    createMutation.mutate(data);
  };

  // Get tomorrow's date as minimum date
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  const minDate = tomorrow.toISOString().split("T")[0];

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" onClick={() => navigate(-1)}>
          <ArrowLeft className="w-4 h-4" />
        </Button>
        <div>
          <h1 className="text-2xl font-bold text-gray-900">
            Ajukan Peminjaman
          </h1>
          <p className="text-gray-600">
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

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
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
                type="time"
                label="Waktu Kembali *"
                error={errors.returnTime?.message}
                {...register("returnTime")}
              />
            </div>

            <Input
              type="number"
              label="Jumlah Penumpang *"
              min={1}
              max={20}
              error={errors.passengerCount?.message}
              {...register("passengerCount")}
            />

            <Textarea
              label="Catatan Tambahan"
              placeholder="Catatan tambahan jika ada (opsional)"
              rows={4}
              error={errors.notes?.message}
              {...register("notes")}
            />

            <div className="flex justify-end gap-3 pt-4 border-t">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate(-1)}
              >
                Batal
              </Button>
              <Button
                type="submit"
                isLoading={createMutation.isPending}
                leftIcon={<Send className="w-4 h-4" />}
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

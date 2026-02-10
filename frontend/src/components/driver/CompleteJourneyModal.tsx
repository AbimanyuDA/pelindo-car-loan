import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Camera, CheckCircle2 } from "lucide-react";
import { scheduleService } from "@/services";
import toast from "react-hot-toast";
import type { DriverSchedule } from "@/types";

interface CompleteJourneyModalProps {
  isOpen: boolean;
  onClose: () => void;
  schedule: DriverSchedule | null;
}

export default function CompleteJourneyModal({
  isOpen,
  onClose,
  schedule,
}: CompleteJourneyModalProps) {
  const queryClient = useQueryClient();
  const [finalFuelCondition, setFinalFuelCondition] = useState("");
  const [isRefueled, setIsRefueled] = useState(false);
  const [refuelAmount, setRefuelAmount] = useState("");
  const [refuelReceipt, setRefuelReceipt] = useState<File | null>(null);
  const [refuelReceiptPreview, setRefuelReceiptPreview] = useState<string | null>(null);
  const [isConfirmModalOpen, setIsConfirmModalOpen] = useState(false);
  const [confirmationChecked, setConfirmationChecked] = useState(false);

  const completeJourneyMutation = useMutation({
    mutationFn: async () => {
      if (!schedule) throw new Error("Schedule not found");
      
      const formData = new FormData();
      // ISO datetime format for DateTime binding
      formData.append("actualEndTime", new Date().toISOString());
      
      if (finalFuelCondition) {
        formData.append("finalFuelCondition", finalFuelCondition);
      }
      
      // Boolean format for bool binding (lowercase)
      formData.append("isRefueled", isRefueled.toString());
      
      // Decimal format - just the number without Rp symbol
      if (isRefueled && refuelAmount) {
        const cleanAmount = refuelAmount.replace(/[^0-9]/g, "");
        formData.append("refuelAmount", cleanAmount);
      }
      
      // File upload
      if (refuelReceipt) {
        formData.append("refuelReceipt", refuelReceipt);
      }

      return scheduleService.completeJourney(schedule.scheduleId, formData);
    },
    onSuccess: () => {
      toast.success("Perjalanan selesai");
      queryClient.invalidateQueries({ queryKey: ["driver-schedules"] });
      handleClose();
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || "Gagal menyelesaikan perjalanan");
    },
  });

  const handleClose = () => {
    setFinalFuelCondition("");
    setIsRefueled(false);
    setRefuelAmount("");
    setRefuelReceipt(null);
    setRefuelReceiptPreview(null);
    onClose();
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setRefuelReceipt(file);
      const reader = new FileReader();
      reader.onloadend = () => {
        setRefuelReceiptPreview(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (isRefueled && !refuelAmount) {
      toast.error("Mohon masukkan jumlah bensin yang diisi");
      return;
    }

    if (isRefueled && !refuelReceipt) {
      toast.error("Mohon upload nota bensin");
      return;
    }

    // Open confirmation modal instead of confirm dialog
    setIsConfirmModalOpen(true);
  };

  const handleConfirmComplete = () => {
    if (!confirmationChecked) {
      toast.error("Mohon centang konfirmasi terlebih dahulu");
      return;
    }
    completeJourneyMutation.mutate();
    setIsConfirmModalOpen(false);
  };

  if (!schedule) return null;

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Selesaikan Perjalanan"
      size="lg"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <h3 className="font-semibold text-blue-900 mb-2">Informasi Perjalanan</h3>
          <div className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-600">Pemohon:</span>
              <span className="font-medium text-gray-900">{schedule.requesterName}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">Destinasi:</span>
              <span className="font-medium text-gray-900">{schedule.destination}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">Kendaraan:</span>
              <span className="font-medium text-gray-900">{schedule.vehiclePlate}</span>
            </div>
            {schedule.fuelCondition && (
              <div className="flex justify-between">
                <span className="text-gray-600">Bensin Awal:</span>
                <span className="font-medium text-gray-900">{schedule.fuelCondition}</span>
              </div>
            )}
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Kondisi Bensin Akhir (Opsional)
          </label>
          <Input
            type="text"
            value={finalFuelCondition}
            onChange={(e) => setFinalFuelCondition(e.target.value)}
            placeholder="contoh: 3 strip, Penuh"
          />
          <p className="text-xs text-gray-500 mt-1">
            Masukkan kondisi bensin setelah perjalanan selesai
          </p>
        </div>

        <div className="space-y-3">
          <div className="flex items-center gap-3">
            <input
              type="checkbox"
              id="isRefueled"
              checked={isRefueled}
              onChange={(e) => setIsRefueled(e.target.checked)}
              className="w-4 h-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
            />
            <label htmlFor="isRefueled" className="text-sm font-medium text-gray-700 cursor-pointer">
              Mengisi bensin selama perjalanan
            </label>
          </div>

          {isRefueled && (
            <div className="ml-7 space-y-3 border-l-2 border-blue-200 pl-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Jumlah Bensin (Rupiah) <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <span className="absolute left-3 top-2.5 text-gray-500 text-sm font-medium">Rp</span>
                  <input
                    type="text"
                    value={refuelAmount ? parseInt(refuelAmount).toLocaleString('id-ID') : ''}
                    onChange={(e) => {
                      const value = e.target.value.replace(/\D/g, '');
                      setRefuelAmount(value);
                    }}
                    placeholder="100.000"
                    required={isRefueled}
                    className="w-full pl-10 pr-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <p className="text-xs text-gray-500 mt-1">
                  Format otomatis dengan pemisah ribuan
                </p>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Upload Nota Bensin <span className="text-red-500">*</span>
                </label>
                <div className="flex items-center gap-3">
                  <label className="flex-1 cursor-pointer">
                    <div className="flex items-center justify-center gap-2 px-4 py-2 border-2 border-dashed border-gray-300 rounded-lg hover:border-blue-500 transition-colors">
                      <Camera className="w-5 h-5 text-gray-400" />
                      <span className="text-sm text-gray-600">
                        {refuelReceipt ? refuelReceipt.name : "Pilih foto nota"}
                      </span>
                    </div>
                    <input
                      type="file"
                      accept="image/*"
                      onChange={handleFileChange}
                      required={isRefueled}
                      className="hidden"
                    />
                  </label>
                </div>
                {refuelReceiptPreview && (
                  <div className="mt-3">
                    <img
                      src={refuelReceiptPreview}
                      alt="Preview nota"
                      className="max-w-full h-auto max-h-48 rounded-lg border border-gray-200"
                    />
                  </div>
                )}
              </div>
            </div>
          )}
        </div>

        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
          <p className="text-sm text-yellow-800">
            <strong>Perhatian:</strong> Setelah Anda menyelesaikan perjalanan, 
            status akan berubah menjadi "Selesai" dan tidak dapat diubah kembali.
          </p>
        </div>

        <div className="flex gap-3 pt-4">
          <Button
            type="button"
            variant="outline"
            onClick={handleClose}
            className="flex-1"
            disabled={completeJourneyMutation.isPending}
          >
            Batal
          </Button>
          <Button
            type="submit"
            variant="primary"
            className="flex-1 bg-gradient-to-r from-orange-600 to-red-600 hover:from-orange-700 hover:to-red-700"
            disabled={completeJourneyMutation.isPending}
          >
            {completeJourneyMutation.isPending
              ? "Memproses..."
              : "Ya, Selesaikan Perjalanan"}
          </Button>
        </div>
      </form>

      {/* Confirmation Modal */}
      {isConfirmModalOpen && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full mx-4">
            <div className="p-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold text-gray-900">
                  Konfirmasi Penyelesaian Perjalanan
                </h3>
                <button
                  onClick={() => {
                    setIsConfirmModalOpen(false);
                    setConfirmationChecked(false);
                  }}
                  className="text-gray-400 hover:text-gray-600"
                >
                  <svg
                    className="w-6 h-6"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M6 18L18 6M6 6l12 12"
                    />
                  </svg>
                </button>
              </div>

              <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-4">
                <p className="text-sm text-blue-900">
                  Pastikan semua data yang Anda masukkan sudah benar. Setelah konfirmasi, 
                  status perjalanan tidak dapat diubah kembali.
                </p>
              </div>

              <div className="space-y-3 mb-6">
                <div className="flex items-start gap-3 p-3 bg-gray-50 rounded-lg">
                  <CheckCircle2 className="w-5 h-5 text-green-600 flex-shrink-0 mt-0.5" />
                  <div className="text-sm">
                    <p className="font-medium text-gray-900">Kondisi Bensin Akhir</p>
                    <p className="text-gray-600">{finalFuelCondition || "Tidak diisi"}</p>
                  </div>
                </div>

                {isRefueled && (
                  <>
                    <div className="flex items-start gap-3 p-3 bg-gray-50 rounded-lg">
                      <CheckCircle2 className="w-5 h-5 text-green-600 flex-shrink-0 mt-0.5" />
                      <div className="text-sm">
                        <p className="font-medium text-gray-900">Jumlah Pengisian Bensin</p>
                        <p className="text-gray-600">Rp {parseInt(refuelAmount || "0").toLocaleString("id-ID")}</p>
                      </div>
                    </div>
                    <div className="flex items-start gap-3 p-3 bg-gray-50 rounded-lg">
                      <CheckCircle2 className="w-5 h-5 text-green-600 flex-shrink-0 mt-0.5" />
                      <div className="text-sm">
                        <p className="font-medium text-gray-900">Nota Bensin</p>
                        <p className="text-gray-600">{refuelReceipt?.name || "Belum diupload"}</p>
                      </div>
                    </div>
                  </>
                )}
              </div>

              <div className="flex items-start gap-3 mb-6 p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
                <input
                  type="checkbox"
                  id="confirmCheckbox"
                  checked={confirmationChecked}
                  onChange={(e) => setConfirmationChecked(e.target.checked)}
                  className="w-4 h-4 text-orange-600 border-gray-300 rounded focus:ring-orange-500 mt-0.5 flex-shrink-0"
                />
                <label htmlFor="confirmCheckbox" className="text-sm text-gray-700 cursor-pointer">
                  Saya yakin data sudah benar dan siap menyelesaikan perjalanan
                </label>
              </div>

              <div className="flex gap-3">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => {
                    setIsConfirmModalOpen(false);
                    setConfirmationChecked(false);
                  }}
                  className="flex-1"
                  disabled={completeJourneyMutation.isPending}
                >
                  Batal
                </Button>
                <Button
                  type="button"
                  variant="primary"
                  onClick={handleConfirmComplete}
                  className="flex-1 bg-gradient-to-r from-orange-600 to-red-600 hover:from-orange-700 hover:to-red-700"
                  disabled={!confirmationChecked || completeJourneyMutation.isPending}
                >
                  {completeJourneyMutation.isPending ? "Memproses..." : "Ya, Selesaikan"}
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </Modal>
  );
}

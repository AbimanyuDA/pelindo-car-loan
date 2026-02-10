import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { scheduleService } from "@/services";
import toast from "react-hot-toast";

interface PreDepartureModalProps {
  scheduleId: number;
  vehicleInfo: {
    id: number;
    plateNumber: string;
    brand: string;
    model: string;
    type: string;
  };
  onClose: () => void;
  onEmergency: () => void;
}

export default function PreDepartureModal({
  scheduleId,
  vehicleInfo,
  onClose,
  onEmergency,
}: PreDepartureModalProps) {
  const queryClient = useQueryClient();
  const [fuelCondition, setFuelCondition] = useState("");
  const [kmPhoto, setKmPhoto] = useState<File | null>(null);
  const [kmPhotoPreview, setKmPhotoPreview] = useState<string>("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleKmPhotoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      if (file.type !== "image/jpeg" && file.type !== "image/jpg") {
        toast.error("Hanya gambar JPEG/JPG yang diperbolehkan");
        return;
      }
      setKmPhoto(file);
      const reader = new FileReader();
      reader.onloadend = () => {
        setKmPhotoPreview(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const handleContinue = async () => {
    try {
      setIsSubmitting(true);

      // Upload KM photo if provided
      if (kmPhoto) {
        await scheduleService.uploadKmPhoto(scheduleId, kmPhoto);
      }

      // Send confirmation with pre-departure data
      const confirmationData: any = {};

      if (fuelCondition) {
        confirmationData.fuelCondition = fuelCondition;
      }

      await scheduleService.driverConfirmation(scheduleId, confirmationData);

      toast.success(
        "Data perjalanan berhasil dikonfirmasi. Silakan klik tombol Start untuk memulai perjalanan.",
      );
      queryClient.invalidateQueries({ queryKey: ["driver-schedules"] });
      onClose();
    } catch (error: any) {
      toast.error(error.response?.data?.message || "Gagal memulai perjalanan");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full max-h-[90vh] overflow-y-auto">
        <div className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold text-gray-900">
              Persiapan Sebelum Keberangkatan
            </h2>
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600 transition-colors"
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

          <div className="space-y-4">
            {/* Vehicle Info */}
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <label className="block text-sm font-semibold text-gray-700 mb-2">
                Kendaraan Yang Digunakan
              </label>
              <div className="bg-white rounded-md p-3 space-y-2">
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600">Nomor Polisi:</span>
                  <span className="text-sm font-semibold text-gray-900">
                    {vehicleInfo.plateNumber}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600">Merek:</span>
                  <span className="text-sm font-semibold text-gray-900">
                    {vehicleInfo.brand}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600">Model:</span>
                  <span className="text-sm font-semibold text-gray-900">
                    {vehicleInfo.model || "-"}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-sm text-gray-600">Tipe:</span>
                  <span className="text-sm font-semibold text-gray-900">
                    {vehicleInfo.type}
                  </span>
                </div>
              </div>
              <p className="text-xs text-amber-700 mt-2 flex items-start gap-1">
                <span>
                  Jika kendaraan bermasalah, klik tombol Darurat di bawah
                </span>
              </p>
            </div>

            {/* Fuel Condition */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Kondisi Bensin (Opsional)
              </label>
              <input
                type="text"
                value={fuelCondition}
                onChange={(e) => setFuelCondition(e.target.value)}
                placeholder="contoh: 3 strip, Penuh"
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <p className="text-xs text-gray-500 mt-1">
                Masukkan level bensin sebelum berangkat
              </p>
            </div>

            {/* KM Photo Upload */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Foto KM (Opsional)
              </label>
              <input
                type="file"
                accept="image/jpeg,image/jpg"
                onChange={handleKmPhotoChange}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
              <p className="text-xs text-gray-500 mt-1">
                Unggah foto odometer (hanya JPEG/JPG)
              </p>
              {kmPhotoPreview && (
                <div className="mt-2">
                  <img
                    src={kmPhotoPreview}
                    alt="KM Preview"
                    className="w-full h-48 object-contain border border-gray-200 rounded"
                  />
                </div>
              )}
            </div>

            {/* Emergency Warning */}
            <div className="bg-red-50 border border-red-200 rounded-md p-3">
              <p className="text-sm text-red-800">
                <strong>Darurat?</strong> Jika ada masalah sebelum
                keberangkatan, klik tombol Darurat di bawah.
              </p>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="mt-6 flex gap-3">
            <button
              onClick={onEmergency}
              disabled={isSubmitting}
              className="flex-1 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:bg-gray-300 disabled:cursor-not-allowed"
            >
              Darurat
            </button>
            <button
              onClick={handleContinue}
              disabled={isSubmitting}
              className="flex-1 px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 disabled:bg-gray-300 disabled:cursor-not-allowed"
            >
              {isSubmitting ? "Mengkonfirmasi..." : "Konfirmasi Perjalanan"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

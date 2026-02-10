import { useMutation, useQueryClient } from "@tanstack/react-query";
import { scheduleService } from "@/services";
import toast from "react-hot-toast";
import { DriverSchedule } from "@/types";
import { formatDateTime } from "@/lib/utils";
import { useState } from "react";

interface StartJourneyModalProps {
  schedule: DriverSchedule;
  onClose: () => void;
}

export default function StartJourneyModal({
  schedule,
  onClose,
}: StartJourneyModalProps) {
  const queryClient = useQueryClient();
  const [metPemohon, setMetPemohon] = useState(false);

  const startJourneyMutation = useMutation({
    mutationFn: () =>
      scheduleService.startJourney(schedule.scheduleId, {
        actualStartTime: new Date().toISOString(),
      }),
    onSuccess: () => {
      toast.success("Perjalanan berhasil dimulai!");
      queryClient.invalidateQueries({ queryKey: ["driver-schedules"] });
      onClose();
    },
    onError: (error: any) => {
      toast.error(
        error.response?.data?.message || "Gagal memulai perjalanan"
      );
    },
  });

  const handleStart = () => {
    if (!metPemohon) {
      toast.error("Anda harus memastikan sudah bertemu dengan pemohon!");
      return;
    }
    startJourneyMutation.mutate();
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl max-w-lg w-full max-h-[90vh] overflow-y-auto">
        <div className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold text-gray-900">
              Konfirmasi Mulai Perjalanan
            </h2>
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600 transition-colors"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div className="space-y-4">
            {/* Waktu Saat Ini */}
            <div className="bg-purple-50 border border-purple-200 rounded-lg p-4">
              <h3 className="font-semibold text-gray-900 mb-2">
                ⏰ Waktu Saat Ini
              </h3>
              <p className="text-lg font-bold text-purple-700">
                {formatDateTime(new Date().toISOString())}
              </p>
            </div>

            {/* Detail Perjalanan */}
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <h3 className="font-semibold text-gray-900 mb-3">
                Detail Perjalanan
              </h3>
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-gray-600">Nomor Pengajuan:</span>
                  <span className="font-medium">{schedule.requestNumber}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Pemohon:</span>
                  <span className="font-medium">{schedule.requesterName}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Tujuan:</span>
                  <span className="font-medium">{schedule.destination}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Keperluan:</span>
                  <span className="font-medium">{schedule.purpose}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Waktu Berangkat:</span>
                  <span className="font-medium">
                    {formatDateTime(schedule.startDatetime)}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Waktu Kembali:</span>
                  <span className="font-medium">
                    {formatDateTime(schedule.endDatetime)}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Kendaraan:</span>
                  <span className="font-medium">
                    {schedule.vehiclePlate} - {schedule.vehicleBrand}
                  </span>
                </div>
                {schedule.fuelCondition && (
                  <div className="flex justify-between">
                    <span className="text-gray-600">Kondisi Bensin:</span>
                    <span className="font-medium">{schedule.fuelCondition}</span>
                  </div>
                )}
              </div>
            </div>

            {/* Peringatan Penting */}
            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
              <div className="flex items-start gap-3">
                <span className="text-2xl">⚠️</span>
                <div>
                  <h4 className="font-semibold text-yellow-900 mb-3">
                    Checklist Keberangkatan:
                  </h4>
                  <div className="space-y-2">
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={metPemohon}
                        onChange={(e) => setMetPemohon(e.target.checked)}
                        className="w-4 h-4 rounded border-gray-300"
                      />
                      <span className="text-sm text-yellow-800">
                        ✓ Sudah bertemu dengan pemohon
                      </span>
                    </label>
                    <div className="text-sm text-yellow-800 space-y-1 list-disc list-inside pl-6">
                      <li>Kendaraan dalam kondisi baik dan siap digunakan</li>
                      <li>Semua penumpang sudah naik</li>
                      <li>Rute perjalanan sudah jelas</li>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Konfirmasi Kontak */}
            <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
              <h4 className="font-semibold text-gray-900 mb-2">
                Kontak Pemohon:
              </h4>
              <div className="space-y-2 text-sm">
                <div className="flex items-center gap-2">
                  <span className="text-gray-600">📧</span>
                  <a
                    href={`mailto:${schedule.requesterEmail}`}
                    className="text-blue-600 hover:underline"
                  >
                    {schedule.requesterEmail}
                  </a>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-gray-600">📱</span>
                  <a
                    href={`https://wa.me/${schedule.requesterPhone.replace(
                      /[^0-9]/g,
                      ""
                    )}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-green-600 hover:underline"
                  >
                    {schedule.requesterPhone}
                  </a>
                </div>
              </div>
            </div>

            {/* Info Tambahan */}
            <div className="bg-green-50 border border-green-200 rounded-lg p-3">
              <p className="text-sm text-green-800">
                <strong>📍 Setelah memulai:</strong> Status akan berubah
                menjadi "Dalam Perjalanan". Jangan lupa klik tombol "Stop"
                setelah perjalanan selesai.
              </p>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="mt-6 flex gap-3">
            <button
              onClick={onClose}
              disabled={startJourneyMutation.isPending}
              className="flex-1 px-4 py-2 bg-gray-200 text-gray-700 rounded-md hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-gray-500 disabled:bg-gray-100 disabled:cursor-not-allowed"
            >
              Batal
            </button>
            <button
              onClick={handleStart}
              disabled={!metPemohon || startJourneyMutation.isPending}
              className="flex-1 px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-green-500 disabled:bg-gray-300 disabled:cursor-not-allowed"
            >
              {startJourneyMutation.isPending
                ? "Memulai..."
                : "✓ Ya, Mulai Perjalanan"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

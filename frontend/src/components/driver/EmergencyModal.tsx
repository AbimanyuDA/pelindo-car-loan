import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { scheduleService } from "@/services";
import toast from "react-hot-toast";

interface EmergencyModalProps {
  scheduleId: number;
  onClose: () => void;
  onBack: () => void;
}

export default function EmergencyModal({
  scheduleId,
  onClose,
  onBack,
}: EmergencyModalProps) {
  const queryClient = useQueryClient();
  const [emergencyType, setEmergencyType] = useState("Mobil Bermasalah");
  const [emergencyReason, setEmergencyReason] = useState("");

  const reportEmergencyMutation = useMutation({
    mutationFn: () =>
      scheduleService.reportEmergency(scheduleId, {
        emergencyReason: `[${emergencyType}] ${emergencyReason}`,
      }),
    onSuccess: () => {
      toast.success(
        "Darurat dilaporkan. Pengajuan dikembalikan ke approver L1."
      );
      queryClient.invalidateQueries({ queryKey: ["driver-schedules"] });
      onClose();
    },
    onError: (error: any) => {
      toast.error(
        error.response?.data?.message || "Gagal melaporkan darurat"
      );
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!emergencyReason.trim()) {
      toast.error("Mohon jelaskan detail situasi darurat");
      return;
    }
    reportEmergencyMutation.mutate();
  };

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl max-w-md w-full">
        <div className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-xl font-semibold text-red-600">
              Laporkan Darurat
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

          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Emergency Type */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Jenis Darurat <span className="text-red-500">*</span>
              </label>
              <select
                value={emergencyType}
                onChange={(e) => setEmergencyType(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-red-500"
              >
                <option value="Mobil Bermasalah">Mobil Bermasalah</option>
                <option value="Alasan Lain">Alasan Lain</option>
              </select>
            </div>

            {/* Emergency Reason */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Penjelasan Detail <span className="text-red-500">*</span>
              </label>
              <textarea
                value={emergencyReason}
                onChange={(e) => setEmergencyReason(e.target.value)}
                placeholder={emergencyType === "Mobil Bermasalah" ? "Jelaskan masalah pada kendaraan..." : "Jelaskan situasi darurat..."}
                rows={4}
                required
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-red-500"
              />
            </div>

            {/* Warning */}
            <div className="bg-yellow-50 border border-yellow-200 rounded-md p-3">
              <p className="text-sm text-yellow-800">
                <strong>Catatan:</strong> Ini akan mengembalikan pengajuan ke
                approver L1 untuk evaluasi ulang. Jadwal akan ditandai sebagai darurat.
              </p>
            </div>

            {/* Action Buttons */}
            <div className="flex gap-3">
              <button
                type="button"
                onClick={onBack}
                disabled={reportEmergencyMutation.isPending}
                className="flex-1 px-4 py-2 bg-gray-200 text-gray-700 rounded-md hover:bg-gray-300 focus:outline-none focus:ring-2 focus:ring-gray-500 disabled:bg-gray-100 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
                </svg>
                Kembali
              </button>
              <button
                type="submit"
                disabled={reportEmergencyMutation.isPending}
                className="flex-1 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:bg-gray-300 disabled:cursor-not-allowed"
              >
                {reportEmergencyMutation.isPending
                  ? "Melaporkan..."
                  : "Laporkan Darurat"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

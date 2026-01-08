import { useParams, useNavigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import {
  ArrowLeft,
  MapPin,
  Calendar,
  Clock,
  FileText,
  CheckCircle,
  XCircle,
  Clock3,
  MessageSquare,
  Ban,
} from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { PageLoading } from "@/components/ui/Loading";
import { Alert } from "@/components/ui/Alert";
import { Modal } from "@/components/ui/Modal";
import {
  loanRequestService,
  approvalService,
  vehicleService,
  driverService,
  scheduleService,
} from "@/services";
import { formatDateTime } from "@/lib/utils";

export default function LoanRequestDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [cancellationReason, setCancellationReason] = useState("");
  const [cancelError, setCancelError] = useState<string | null>(null);

  const {
    data: request,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["loan-request", id],
    queryFn: async () => {
      const response = await loanRequestService.getById(Number(id));
      return response.data;
    },
    enabled: !!id,
  });

  const { data: approvalHistory } = useQuery({
    queryKey: ["approval-history", id],
    queryFn: async () => {
      const response = await approvalService.getHistory(Number(id));
      return response.data || [];
    },
    enabled: !!id,
  });

  // Lookup data untuk nama kendaraan/driver yang dipilih
  const { data: vehicles } = useQuery({
    queryKey: ["vehicles"],
    queryFn: async () => {
      const response = await vehicleService.getAll();
      return response.data || [];
    },
  });

  const { data: drivers } = useQuery({
    queryKey: ["drivers"],
    queryFn: async () => {
      const response = await driverService.getAll();
      return response.data || [];
    },
  });

  const matchedVehicle = vehicles?.find(
    (v: any) =>
      v.id === request?.vehicleId || v.vehicleId === request?.vehicleId
  );
  const vehicleName = matchedVehicle
    ? `${matchedVehicle.brand || ""} ${matchedVehicle.type || ""} • ${
        matchedVehicle.plateNumber || ""
      }`.trim()
    : request?.vehicleId
    ? `Kendaraan #${request.vehicleId}`
    : "-";

  const matchedDriver = drivers?.find(
    (d: any) => d.id === request?.driverId || d.driverId === request?.driverId
  );
  const driverName =
    matchedDriver?.name ||
    matchedDriver?.driverName ||
    (request?.driverId ? `Driver #${request.driverId}` : "-");

  const schedule = request?.schedule;

  const cancelMutation = useMutation({
    mutationFn: async () => {
      if (!schedule?.id) throw new Error("Schedule not found");
      const response = await scheduleService.cancelSchedule(schedule.id, {
        cancellationReason,
      });
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["loan-request", id] });
      setShowCancelModal(false);
      setCancellationReason("");
      setCancelError(null);
    },
    onError: (error: any) => {
      setCancelError(
        error.response?.data?.message || "Gagal membatalkan jadwal"
      );
    },
  });

  const handleCancelSchedule = () => {
    if (!cancellationReason.trim()) {
      setCancelError("Alasan pembatalan harus diisi");
      return;
    }
    cancelMutation.mutate();
  };

  if (isLoading) return <PageLoading />;

  if (error || !request) {
    return (
      <div className="space-y-6">
        <Button variant="ghost" onClick={() => navigate(-1)}>
          <ArrowLeft className="w-4 h-4 mr-2" />
          Kembali
        </Button>
        <Alert variant="error">Pengajuan tidak ditemukan</Alert>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" onClick={() => navigate(-1)}>
          <ArrowLeft className="w-4 h-4" />
        </Button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-900">
              Detail Pengajuan
            </h1>
            <Badge status={request.status} />
          </div>
          <p className="text-gray-600">ID: #{request.id}</p>
        </div>
      </div>

      {/* Main Info */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="w-5 h-5" />
            Informasi Pengajuan
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-4">
              <div>
                <p className="text-sm text-gray-500">
                  Dasar Surat Pelayanan (SPPD)
                </p>
                <p className="font-medium text-gray-900">
                  {request.serviceLetterBasis || "-"}
                </p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Tujuan Peminjaman</p>
                <p className="font-medium text-gray-900">{request.purpose}</p>
              </div>
              <div className="flex items-start gap-2">
                <MapPin className="w-4 h-4 text-gray-400 mt-1" />
                <div>
                  <p className="text-sm text-gray-500">Destinasi</p>
                  <p className="font-medium text-gray-900">
                    {request.destination}
                  </p>
                </div>
              </div>
              <div>
                <p className="text-sm text-gray-500">
                  Daftar Tamu yang Dilayani
                </p>
                <p className="font-medium text-gray-900 whitespace-pre-line">
                  {request.guestList}
                </p>
              </div>
              {request.hotelAccommodation && (
                <div>
                  <p className="text-sm text-gray-500">Hotel Menginap</p>
                  <p className="font-medium text-gray-900">
                    {request.hotelAccommodation}
                  </p>
                </div>
              )}
            </div>
            <div className="space-y-4">
              <div className="flex items-start gap-2">
                <Calendar className="w-4 h-4 text-gray-400 mt-1" />
                <div>
                  <p className="text-sm text-gray-500">Jadwal Berangkat</p>
                  <p className="font-medium text-gray-900">
                    {formatDateTime(request.startDatetime)}
                  </p>
                </div>
              </div>
              <div className="flex items-start gap-2">
                <Clock className="w-4 h-4 text-gray-400 mt-1" />
                <div>
                  <p className="text-sm text-gray-500">Jadwal Kembali</p>
                  <p className="font-medium text-gray-900">
                    {formatDateTime(request.endDatetime)}
                  </p>
                </div>
              </div>
              <div>
                <p className="text-sm text-gray-500">Kendaraan</p>
                <p className="font-medium text-gray-900">{vehicleName}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Driver</p>
                <p className="font-medium text-gray-900">{driverName}</p>
              </div>
              {request.notes && (
                <div>
                  <p className="text-sm text-gray-500">Catatan</p>
                  <p className="text-gray-700">{request.notes}</p>
                </div>
              )}
            </div>
          </div>
          <div className="mt-6 pt-6 border-t">
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <p className="text-gray-500">Dibuat pada</p>
                <p className="text-gray-900">
                  {formatDateTime(request.createdAt)}
                </p>
              </div>
              <div>
                <p className="text-gray-500">Terakhir diupdate</p>
                <p className="text-gray-900">
                  {formatDateTime(request.updatedAt)}
                </p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Approval History */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="w-5 h-5" />
            Riwayat Approval
          </CardTitle>
        </CardHeader>
        <CardContent>
          {!approvalHistory || approvalHistory.length === 0 ? (
            <div className="text-center py-8">
              <Clock3 className="w-12 h-12 text-gray-300 mx-auto mb-2" />
              <p className="text-gray-500">Belum ada riwayat approval</p>
              <p className="text-sm text-gray-400 mt-1">
                Menunggu proses approval
              </p>
            </div>
          ) : (
            <div className="space-y-6">
              {approvalHistory.map((approval, index) => {
                const isApproved = approval.status === "APPROVED";
                const isRejected = approval.status === "REJECTED";
                const isPending = !isApproved && !isRejected;

                return (
                  <div key={approval.id} className="relative">
                    {/* Timeline Line */}
                    {index < approvalHistory.length - 1 && (
                      <div className="absolute left-4 top-10 w-0.5 h-[calc(100%+0.5rem)] bg-gray-200" />
                    )}

                    <div className="flex gap-4">
                      {/* Timeline Icon */}
                      <div className="flex-shrink-0">
                        <div
                          className={`
                          w-8 h-8 rounded-full flex items-center justify-center
                          ${
                            isApproved
                              ? "bg-green-100 text-green-600 ring-2 ring-green-200"
                              : isRejected
                              ? "bg-red-100 text-red-600 ring-2 ring-red-200"
                              : "bg-gray-100 text-gray-400 ring-2 ring-gray-200"
                          }
                        `}
                        >
                          {isApproved ? (
                            <CheckCircle className="w-5 h-5" />
                          ) : isRejected ? (
                            <XCircle className="w-5 h-5" />
                          ) : (
                            <Clock3 className="w-5 h-5" />
                          )}
                        </div>
                      </div>

                      {/* Content */}
                      <div className="flex-1 pb-2">
                        <div
                          className={`
                          rounded-lg border p-4
                          ${
                            isApproved
                              ? "bg-green-50 border-green-200"
                              : isRejected
                              ? "bg-red-50 border-red-200"
                              : "bg-gray-50 border-gray-200"
                          }
                        `}
                        >
                          <div className="flex items-start justify-between gap-4 mb-2">
                            <div>
                              <div className="flex items-center gap-2">
                                <h3 className="font-semibold text-gray-900">
                                  Approval Level {approval.approvalLevel}
                                </h3>
                                <Badge status={approval.status} />
                              </div>
                              <p className="text-sm text-gray-600 mt-1">
                                oleh{" "}
                                <span className="font-medium text-gray-900">
                                  {approval.approverName}
                                </span>
                              </p>
                            </div>
                            <p className="text-xs text-gray-500 whitespace-nowrap">
                              {formatDateTime(approval.approvedAt)}
                            </p>
                          </div>

                          {approval.notes && (
                            <div
                              className={`
                              mt-3 pt-3 border-t flex gap-2
                              ${
                                isApproved
                                  ? "border-green-200"
                                  : isRejected
                                  ? "border-red-200"
                                  : "border-gray-200"
                              }
                            `}
                            >
                              <MessageSquare
                                className={`
                                w-4 h-4 flex-shrink-0 mt-0.5
                                ${
                                  isApproved
                                    ? "text-green-600"
                                    : isRejected
                                    ? "text-red-600"
                                    : "text-gray-500"
                                }
                              `}
                              />
                              <div className="flex-1">
                                <p className="text-xs text-gray-500 mb-1">
                                  Pesan dari approver:
                                </p>
                                <p className="text-sm text-gray-700 italic">
                                  "{approval.notes}"
                                </p>
                              </div>
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Schedule Info (if approved) */}
      {schedule && (
        <Card className="bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50 border-indigo-200 shadow-lg">
          <CardHeader className="border-b border-indigo-100">
            <div className="flex items-center justify-between">
              <CardTitle className="text-indigo-900 flex items-center gap-3 text-xl">
                <div className="p-2 bg-indigo-100 rounded-lg">
                  <Calendar className="w-6 h-6 text-indigo-600" />
                </div>
                Jadwal Perjalanan
              </CardTitle>
              <Badge status={schedule.status} />
            </div>
            <p className="text-sm text-indigo-600 mt-2">
              Jadwal telah dikonfirmasi dan siap untuk perjalanan
            </p>
          </CardHeader>
          <CardContent className="pt-6">
            {/* Tanggal Section */}
            <div className="mb-6 p-4 bg-white/60 rounded-xl border border-indigo-100">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="flex items-start gap-3">
                  <div className="p-2 bg-blue-100 rounded-lg mt-1">
                    <Calendar className="w-5 h-5 text-blue-600" />
                  </div>
                  <div>
                    <p className="text-xs text-gray-500 uppercase tracking-wide mb-1">
                      Jadwal Berangkat
                    </p>
                    <p className="font-bold text-gray-900 text-lg">
                      {formatDateTime(request.startDatetime)}
                    </p>
                  </div>
                </div>
                <div className="flex items-start gap-3">
                  <div className="p-2 bg-purple-100 rounded-lg mt-1">
                    <Clock className="w-5 h-5 text-purple-600" />
                  </div>
                  <div>
                    <p className="text-xs text-gray-500 uppercase tracking-wide mb-1">
                      Jadwal Kembali
                    </p>
                    <p className="font-bold text-gray-900 text-lg">
                      {formatDateTime(request.endDatetime)}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            {/* Vehicle & Driver Section */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
              {/* Vehicle Card */}
              <div className="p-4 bg-white/60 rounded-xl border border-indigo-100">
                <div className="flex items-center gap-2 mb-3">
                  <div className="p-1.5 bg-indigo-100 rounded-lg">
                    <svg
                      className="w-4 h-4 text-indigo-600"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4"
                      />
                    </svg>
                  </div>
                  <p className="text-xs text-gray-500 uppercase tracking-wide font-semibold">
                    Kendaraan
                  </p>
                </div>
                <p className="font-bold text-gray-900 text-base">
                  {schedule.vehicle
                    ? `${schedule.vehicle.brand} ${schedule.vehicle.type}`
                    : vehicleName}
                </p>
                {schedule.vehicle && (
                  <p className="text-sm text-gray-600 mt-1">
                    {schedule.vehicle.plateNumber}
                  </p>
                )}
              </div>

              {/* Driver Card */}
              <div className="p-4 bg-white/60 rounded-xl border border-indigo-100">
                <div className="flex items-center gap-2 mb-3">
                  <div className="p-1.5 bg-indigo-100 rounded-lg">
                    <svg
                      className="w-4 h-4 text-indigo-600"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                      />
                    </svg>
                  </div>
                  <p className="text-xs text-gray-500 uppercase tracking-wide font-semibold">
                    Driver
                  </p>
                </div>
                <p className="font-bold text-gray-900 text-base mb-2">
                  {schedule.driver?.driverName || driverName}
                </p>
                {schedule.driver?.phoneNumber && (
                  <a
                    href={`https://wa.me/${schedule.driver.phoneNumber.replace(
                      /\D/g,
                      ""
                    )}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-2 px-3 py-1.5 text-sm bg-green-500 text-white rounded-lg hover:bg-green-600 transition-colors"
                  >
                    <svg
                      className="w-4 h-4"
                      fill="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413Z" />
                    </svg>
                    Hubungi via WhatsApp
                  </a>
                )}
              </div>
            </div>

            {/* Meta Info */}
            <div className="p-4 bg-white/60 rounded-xl border border-indigo-100">
              <div className="flex items-center justify-between text-sm">
                <div className="flex items-center gap-2 text-gray-600">
                  <Clock className="w-4 h-4" />
                  <span>Ditugaskan pada</span>
                </div>
                <span className="font-medium text-gray-900">
                  {formatDateTime(schedule.assignedAt)}
                </span>
              </div>
            </div>

            {schedule.notes && (
              <div className="mt-4 p-4 bg-amber-50 rounded-xl border border-amber-200">
                <div className="flex items-start gap-2">
                  <MessageSquare className="w-4 h-4 text-amber-600 mt-0.5" />
                  <div>
                    <p className="text-xs text-amber-700 font-semibold uppercase tracking-wide mb-1">
                      Catatan Jadwal
                    </p>
                    <p className="text-sm text-gray-700">{schedule.notes}</p>
                  </div>
                </div>
              </div>
            )}

            {/* Cancel Button (only if status is CONFIRMED) */}
            {schedule.status === "CONFIRMED" && (
              <div className="mt-6 pt-4 border-t border-indigo-100 flex justify-end">
                <Button
                  variant="outline"
                  onClick={() => setShowCancelModal(true)}
                  className="text-red-600 border-red-300 hover:bg-red-50"
                >
                  <Ban className="w-4 h-4 mr-2" />
                  Batalkan Jadwal
                </Button>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Cancel Schedule Modal */}
      <Modal
        isOpen={showCancelModal}
        onClose={() => {
          setShowCancelModal(false);
          setCancellationReason("");
          setCancelError(null);
        }}
        title="Batalkan Jadwal Perjalanan"
        description="Apakah Anda yakin ingin membatalkan jadwal ini? Tindakan ini tidak dapat dibatalkan."
      >
        {cancelError && (
          <Alert variant="error" className="mb-4">
            {cancelError}
          </Alert>
        )}

        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Alasan Pembatalan *
            </label>
            <textarea
              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-red-500 focus:border-transparent resize-none"
              rows={4}
              placeholder="Masukkan alasan pembatalan..."
              value={cancellationReason}
              onChange={(e) => setCancellationReason(e.target.value)}
            />
            <p className="text-xs text-gray-500 mt-1">
              Alasan pembatalan akan disimpan dan terlihat oleh admin dan driver
            </p>
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button
              type="button"
              variant="outline"
              onClick={() => {
                setShowCancelModal(false);
                setCancellationReason("");
                setCancelError(null);
              }}
            >
              Batal
            </Button>
            <Button
              type="button"
              onClick={handleCancelSchedule}
              disabled={cancelMutation.isPending}
              className="bg-red-600 hover:bg-red-700 text-white"
            >
              {cancelMutation.isPending
                ? "Membatalkan..."
                : "Ya, Batalkan Jadwal"}
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}

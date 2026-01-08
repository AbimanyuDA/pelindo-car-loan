import { useQuery } from "@tanstack/react-query";
import { Eye, Calendar, MapPin, Users, Hotel, Phone, Mail } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Table, Column } from "@/components/ui/Table";
import { Badge } from "@/components/ui/Badge";
import { PageLoading } from "@/components/ui/Loading";
import { Alert } from "@/components/ui/Alert";
import { Modal } from "@/components/ui/Modal";
import { scheduleService } from "@/services";
import { formatDate, formatDateTime } from "@/lib/utils";
import type { DriverSchedule } from "@/types";

export default function DriverSchedulePage() {
  const [selectedSchedule, setSelectedSchedule] =
    useState<DriverSchedule | null>(null);
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);

  const {
    data: schedules,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["my-schedules"],
    queryFn: async () => {
      const response = await scheduleService.getMySchedules();
      return response.data || [];
    },
  });

  const handleViewDetail = (schedule: DriverSchedule) => {
    setSelectedSchedule(schedule);
    setIsDetailModalOpen(true);
  };

  const columns: Column<DriverSchedule>[] = [
    {
      key: "scheduleId",
      header: "ID",
      render: (item) => (
        <span className="font-mono text-xs">#{item.scheduleId}</span>
      ),
    },
    {
      key: "requesterName",
      header: "Pemohon",
      render: (item) => (
        <div>
          <p className="font-medium text-gray-900">{item.requesterName}</p>
          <p className="text-sm text-gray-500">{item.purpose}</p>
        </div>
      ),
    },
    {
      key: "destination",
      header: "Destinasi",
      render: (item) => <span className="text-sm">{item.destination}</span>,
    },
    {
      key: "startDatetime",
      header: "Tanggal",
      render: (item) => (
        <div>
          <p className="text-sm">{formatDate(item.startDatetime)}</p>
          <p className="text-xs text-gray-500">
            {new Date(item.startDatetime).toLocaleTimeString("id-ID", {
              hour: "2-digit",
              minute: "2-digit",
            })}{" "}
            -{" "}
            {new Date(item.endDatetime).toLocaleTimeString("id-ID", {
              hour: "2-digit",
              minute: "2-digit",
            })}
          </p>
        </div>
      ),
    },
    {
      key: "vehiclePlate",
      header: "Kendaraan",
      render: (item) => (
        <div>
          <p className="font-medium text-gray-900">{item.vehiclePlate}</p>
          <p className="text-sm text-gray-500">
            {item.vehicleBrand} {item.vehicleType}
          </p>
        </div>
      ),
    },
    {
      key: "status",
      header: "Status",
      render: (item) => (
        <div>
          <Badge status={item.status} />
          {item.status === "CANCELLED" && item.notes && (
            <p className="text-xs text-red-600 mt-1">{item.notes}</p>
          )}
        </div>
      ),
    },
    {
      key: "actions",
      header: "Aksi",
      render: (item) => (
        <Button
          variant="primary"
          size="sm"
          onClick={() => handleViewDetail(item)}
          leftIcon={<Eye className="w-4 h-4" />}
          className="bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 shadow-md"
        >
          Lihat Detail
        </Button>
      ),
    },
  ];

  if (isLoading) return <PageLoading />;

  if (error) {
    return <Alert variant="error">Gagal memuat data jadwal</Alert>;
  }

  const stats = {
    total: schedules?.length || 0,
    confirmed: schedules?.filter((s) => s.status === "CONFIRMED").length || 0,
    completed: schedules?.filter((s) => s.status === "COMPLETED").length || 0,
    cancelled: schedules?.filter((s) => s.status === "CANCELLED").length || 0,
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-gradient-to-r from-blue-600 via-indigo-600 to-purple-600 rounded-2xl p-8 text-white shadow-xl">
        <div className="flex items-center gap-3 mb-2">
          <div className="p-3 bg-white/20 rounded-xl backdrop-blur-sm">
            <Calendar className="w-6 h-6" />
          </div>
          <h1 className="text-3xl font-bold">Jadwal Perjalanan Saya</h1>
        </div>
        <p className="text-blue-100 ml-[60px]">
          Kelola dan pantau jadwal perjalanan yang telah ditugaskan kepada Anda
        </p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
        <Card className="border-none shadow-lg hover:shadow-xl transition-all duration-300 bg-gradient-to-br from-gray-50 to-gray-100">
          <CardContent className="py-6">
            <div className="flex items-center justify-between mb-2">
              <p className="text-sm font-medium text-gray-600">Total Jadwal</p>
              <div className="p-2 bg-gray-200 rounded-lg">
                <Calendar className="w-4 h-4 text-gray-700" />
              </div>
            </div>
            <p className="text-3xl font-bold text-gray-900">{stats.total}</p>
            <p className="text-xs text-gray-500 mt-1">Semua perjalanan</p>
          </CardContent>
        </Card>
        <Card className="border-none shadow-lg hover:shadow-xl transition-all duration-300 bg-gradient-to-br from-blue-50 to-indigo-100">
          <CardContent className="py-6">
            <div className="flex items-center justify-between mb-2">
              <p className="text-sm font-medium text-blue-700">Terkonfirmasi</p>
              <div className="p-2 bg-blue-200 rounded-lg">
                <Users className="w-4 h-4 text-blue-700" />
              </div>
            </div>
            <p className="text-3xl font-bold text-blue-600">
              {stats.confirmed}
            </p>
            <p className="text-xs text-blue-600 mt-1">Siap berangkat</p>
          </CardContent>
        </Card>
        <Card className="border-none shadow-lg hover:shadow-xl transition-all duration-300 bg-gradient-to-br from-green-50 to-emerald-100">
          <CardContent className="py-6">
            <div className="flex items-center justify-between mb-2">
              <p className="text-sm font-medium text-green-700">Selesai</p>
              <div className="p-2 bg-green-200 rounded-lg">
                <MapPin className="w-4 h-4 text-green-700" />
              </div>
            </div>
            <p className="text-3xl font-bold text-green-600">
              {stats.completed}
            </p>
            <p className="text-xs text-green-600 mt-1">Perjalanan selesai</p>
          </CardContent>
        </Card>
        <Card className="border-none shadow-lg hover:shadow-xl transition-all duration-300 bg-gradient-to-br from-red-50 to-rose-100">
          <CardContent className="py-6">
            <div className="flex items-center justify-between mb-2">
              <p className="text-sm font-medium text-red-700">Dibatalkan</p>
              <div className="p-2 bg-red-200 rounded-lg">
                <Calendar className="w-4 h-4 text-red-700" />
              </div>
            </div>
            <p className="text-3xl font-bold text-red-600">{stats.cancelled}</p>
            <p className="text-xs text-red-600 mt-1">Tidak jadi</p>
          </CardContent>
        </Card>
      </div>

      {/* Table */}
      <Card className="border-none shadow-xl">
        <CardHeader className="bg-gradient-to-r from-gray-50 to-gray-100 border-b">
          <div className="flex items-center gap-2">
            <div className="p-2 bg-blue-100 rounded-lg">
              <Calendar className="w-5 h-5 text-blue-600" />
            </div>
            <CardTitle className="text-xl">Daftar Jadwal Perjalanan</CardTitle>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          <Table
            columns={columns}
            data={schedules || []}
            keyExtractor={(item) => item.scheduleId}
            emptyMessage="Belum ada jadwal yang ditugaskan"
          />
        </CardContent>
      </Card>

      {/* Detail Modal */}
      <Modal
        isOpen={isDetailModalOpen}
        onClose={() => setIsDetailModalOpen(false)}
        title="📋 Detail Jadwal Perjalanan"
        size="lg"
      >
        {selectedSchedule && (
          <div className="space-y-6">
            {/* Status Badge */}
            <div className="flex items-center justify-between bg-gradient-to-r from-gray-50 to-gray-100 rounded-xl p-4">
              <Badge status={selectedSchedule.status} />
              <span className="text-sm font-mono font-semibold text-gray-600 bg-white px-3 py-1 rounded-lg shadow-sm">
                #{selectedSchedule.scheduleId}
              </span>
            </div>

            {/* Cancellation Notice */}
            {selectedSchedule.status === "CANCELLED" &&
              selectedSchedule.notes && (
                <Alert variant="error" title="Jadwal Dibatalkan">
                  {selectedSchedule.notes}
                </Alert>
              )}

            {/* Requester Info */}
            <div className="bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50 rounded-2xl p-6 border border-blue-100 shadow-md">
              <h3 className="text-base font-bold text-gray-900 mb-4 flex items-center gap-2">
                <div className="p-2 bg-blue-100 rounded-lg">
                  <Users className="w-5 h-5 text-blue-600" />
                </div>
                Informasi Pemohon
              </h3>
              <div className="space-y-4">
                <div className="bg-white/50 rounded-lg p-3">
                  <p className="text-xs font-semibold text-gray-600 uppercase tracking-wide mb-1">
                    Nama Lengkap
                  </p>
                  <p className="text-base font-semibold text-gray-900">
                    {selectedSchedule.requesterName}
                  </p>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div className="bg-white/50 rounded-lg p-3">
                    <p className="text-xs font-semibold text-gray-600 uppercase tracking-wide mb-2 flex items-center gap-1">
                      <Mail className="w-3 h-3" /> Email
                    </p>
                    <a
                      href={`mailto:${selectedSchedule.requesterEmail}`}
                      className="text-sm text-blue-600 hover:text-blue-800 hover:underline font-medium break-all"
                    >
                      {selectedSchedule.requesterEmail}
                    </a>
                  </div>
                  <div className="bg-white/50 rounded-lg p-3">
                    <p className="text-xs font-semibold text-gray-600 uppercase tracking-wide mb-2 flex items-center gap-1">
                      <Phone className="w-3 h-3" /> WhatsApp
                    </p>
                    <a
                      href={`https://wa.me/${selectedSchedule.requesterPhone.replace(
                        /[^0-9]/g,
                        ""
                      )}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-sm text-green-600 hover:text-green-800 hover:underline font-medium flex items-center gap-1"
                    >
                      {selectedSchedule.requesterPhone}
                      <span className="text-xs">💬</span>
                    </a>
                  </div>
                </div>
              </div>
            </div>

            {/* Trip Details */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm hover:shadow-md transition-shadow">
                <div className="flex items-center gap-3 mb-2">
                  <div className="p-2 bg-blue-100 rounded-lg">
                    <MapPin className="w-5 h-5 text-blue-600" />
                  </div>
                  <p className="text-xs font-semibold text-gray-600 uppercase tracking-wide">
                    Destinasi
                  </p>
                </div>
                <p className="text-sm text-gray-900 whitespace-pre-line ml-[52px]">
                  {selectedSchedule.destination}
                </p>
              </div>

              <div className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm hover:shadow-md transition-shadow">
                <div className="flex items-center gap-3 mb-2">
                  <div className="p-2 bg-purple-100 rounded-lg">
                    <Calendar className="w-5 h-5 text-purple-600" />
                  </div>
                  <p className="text-xs font-semibold text-gray-600 uppercase tracking-wide">
                    Keperluan
                  </p>
                </div>
                <p className="text-sm text-gray-900 whitespace-pre-line ml-[52px]">
                  {selectedSchedule.purpose}
                </p>
              </div>

              <div className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm hover:shadow-md transition-shadow">
                <div className="flex items-center gap-3 mb-2">
                  <div className="p-2 bg-green-100 rounded-lg">
                    <Users className="w-5 h-5 text-green-600" />
                  </div>
                  <p className="text-xs font-semibold text-gray-600 uppercase tracking-wide">
                    TAmu yang Dilayani
                  </p>
                </div>
                <p className="text-sm text-gray-900 whitespace-pre-line ml-[52px]">
                  {selectedSchedule.guestList || "Tidak ada data"}
                </p>
              </div>

              <div className="bg-white border border-gray-200 rounded-xl p-4 shadow-sm hover:shadow-md transition-shadow">
                <div className="flex items-center gap-3 mb-2">
                  <div className="p-2 bg-orange-100 rounded-lg">
                    <Hotel className="w-5 h-5 text-orange-600" />
                  </div>
                  <p className="text-xs font-semibold text-gray-600 uppercase tracking-wide">
                    Hotel
                  </p>
                </div>
                <p className="text-sm text-gray-900 whitespace-pre-line ml-[52px]">
                  {selectedSchedule.hotelName || "Tidak menginap"}
                </p>
              </div>
            </div>

            {/* Schedule Info */}
            <div className="bg-gradient-to-r from-indigo-50 to-purple-50 rounded-2xl p-6 border border-indigo-100 shadow-md">
              <h3 className="text-base font-bold text-gray-900 mb-4 flex items-center gap-2">
                <div className="p-2 bg-indigo-100 rounded-lg">
                  <Calendar className="w-5 h-5 text-indigo-600" />
                </div>
                Jadwal Perjalanan
              </h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="bg-white/50 rounded-lg p-4">
                  <p className="text-xs font-semibold text-gray-600 uppercase tracking-wide mb-2">
                    🚀 Keberangkatan
                  </p>
                  <p className="text-base font-bold text-gray-900">
                    {formatDateTime(selectedSchedule.startDatetime)}
                  </p>
                </div>
                <div className="bg-white/50 rounded-lg p-4">
                  <p className="text-xs font-semibold text-gray-600 uppercase tracking-wide mb-2">
                    🏁 Kembali
                  </p>
                  <p className="text-base font-bold text-gray-900">
                    {formatDateTime(selectedSchedule.endDatetime)}
                  </p>
                </div>
              </div>
            </div>

            {/* Vehicle Info */}
            <div className="bg-gradient-to-r from-gray-50 to-slate-100 rounded-2xl p-6 border border-gray-200 shadow-md">
              <h3 className="text-base font-bold text-gray-900 mb-4 flex items-center gap-2">
                <div className="p-2 bg-gray-200 rounded-lg">
                  <MapPin className="w-5 h-5 text-gray-700" />
                </div>
                Kendaraan Yang Digunakan
              </h3>
              <div className="bg-white/70 rounded-xl p-4 flex items-center gap-4">
                <div className="p-3 bg-blue-100 rounded-xl">
                  <span className="text-2xl">🚗</span>
                </div>
                <div>
                  <p className="text-lg font-bold text-gray-900">
                    {selectedSchedule.vehiclePlate}
                  </p>
                  <p className="text-sm text-gray-600 font-medium">
                    {selectedSchedule.vehicleBrand}{" "}
                    {selectedSchedule.vehicleType}
                  </p>
                </div>
              </div>
            </div>

            {/* Notes */}
            {selectedSchedule.notes &&
              selectedSchedule.status !== "CANCELLED" && (
                <div className="bg-gradient-to-r from-yellow-50 to-amber-50 rounded-2xl p-6 border border-yellow-200 shadow-md">
                  <h3 className="text-base font-bold text-gray-900 mb-3 flex items-center gap-2">
                    <div className="p-2 bg-yellow-200 rounded-lg">
                      <span className="text-lg">📝</span>
                    </div>
                    Catatan Penting
                  </h3>
                  <p className="text-sm text-gray-800 leading-relaxed bg-white/50 rounded-lg p-4">
                    {selectedSchedule.notes}
                  </p>
                </div>
              )}
          </div>
        )}
      </Modal>
    </div>
  );
}

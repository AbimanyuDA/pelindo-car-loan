import { useQuery } from "@tanstack/react-query";
import {
  FileText,
  CheckSquare,
  Car,
  Users,
  Calendar,
  Clock,
  TrendingUp,
  AlertCircle,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { PageLoading } from "@/components/ui/Loading";
import { Alert } from "@/components/ui/Alert";
import { useAuthStore } from "@/store/authStore";
import {
  dashboardService,
  loanRequestService,
  approvalService,
  scheduleService,
} from "@/services";
import { formatDate } from "@/lib/utils";

export default function DashboardPage() {
  const { user } = useAuthStore();

  if (!user) return null;

  // Render different dashboards based on role
  switch (user.role) {
    case "PEMOHON":
      return <PemohonDashboard />;
    case "PIC_APPROVAL_L1":
    case "PIC_APPROVAL_L2":
      return <ApprovalDashboard role={user.role} />;
    case "DRIVER":
      return <DriverDashboard />;
    case "ADMIN":
      return <AdminDashboard />;
    default:
      return <div>Role tidak dikenali</div>;
  }
}

// Pemohon Dashboard
function PemohonDashboard() {
  const { data: requests, isLoading } = useQuery({
    queryKey: ["my-requests"],
    queryFn: async () => {
      const response = await loanRequestService.getMyRequests();
      return response.data || [];
    },
  });

  if (isLoading) return <PageLoading />;

  const stats = {
    total: requests?.length || 0,
    pending: requests?.filter((r) => r.status.includes("PENDING")).length || 0,
    approved: requests?.filter((r) => r.status === "APPROVED").length || 0,
    rejected: requests?.filter((r) => r.status === "REJECTED").length || 0,
  };

  const recentRequests = requests?.slice(0, 5) || [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-gray-600">
          Selamat datang di Sistem Peminjaman Kendaraan
        </p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-6">
        <StatCard
          title="Total Pengajuan"
          value={stats.total}
          icon={FileText}
          color="blue"
        />
        <StatCard
          title="Menunggu Approval"
          value={stats.pending}
          icon={Clock}
          color="yellow"
        />
        <StatCard
          title="Disetujui"
          value={stats.approved}
          icon={CheckSquare}
          color="green"
        />
        <StatCard
          title="Ditolak"
          value={stats.rejected}
          icon={AlertCircle}
          color="red"
        />
      </div>

      {/* Recent Requests */}
      <Card>
        <CardHeader>
          <CardTitle>Pengajuan Terbaru</CardTitle>
        </CardHeader>
        <CardContent>
          {recentRequests.length === 0 ? (
            <p className="text-gray-500 text-center py-8">
              Belum ada pengajuan
            </p>
          ) : (
            <div className="space-y-4">
              {recentRequests.map((request) => (
                <div
                  key={request.id}
                  className="flex items-center justify-between p-4 bg-gray-50 rounded-lg"
                >
                  <div>
                    <p className="font-medium text-gray-900">
                      {request.purpose}
                    </p>
                    <p className="text-sm text-gray-500">
                      {formatDate(request.departureDate)} •{" "}
                      {request.destination}
                    </p>
                  </div>
                  <Badge status={request.status} />
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

// Approval Dashboard
function ApprovalDashboard({ role }: { role: string }) {
  const level = role === "PIC_APPROVAL_L1" ? "l1" : "l2";

  const { data: pendingApprovals, isLoading } = useQuery({
    queryKey: ["pending-approvals", level],
    queryFn: async () => {
      const response =
        level === "l1"
          ? await approvalService.getPendingL1()
          : await approvalService.getPendingL2();
      return response.data || [];
    },
  });

  const { data: counts } = useQuery({
    queryKey: ["approval-counts"],
    queryFn: async () => {
      const response = await approvalService.getPendingCount();
      return response.data;
    },
  });

  if (isLoading) return <PageLoading />;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Dashboard Approval</h1>
        <p className="text-gray-600">
          {role === "PIC_APPROVAL_L1" ? "Approval Level 1" : "Approval Level 2"}
        </p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 sm:gap-6">
        <StatCard
          title="Menunggu Approval"
          value={level === "l1" ? counts?.level1 || 0 : counts?.level2 || 0}
          icon={Clock}
          color="yellow"
        />
        <StatCard
          title="Total Pending"
          value={counts?.total || 0}
          icon={FileText}
          color="blue"
        />
        <StatCard
          title="Perlu Ditindaklanjuti"
          value={pendingApprovals?.length || 0}
          icon={AlertCircle}
          color="red"
        />
      </div>

      {/* Pending Approvals */}
      <Card>
        <CardHeader>
          <CardTitle>Pengajuan Menunggu Approval</CardTitle>
        </CardHeader>
        <CardContent>
          {!pendingApprovals || pendingApprovals.length === 0 ? (
            <Alert variant="info">
              Tidak ada pengajuan yang menunggu approval
            </Alert>
          ) : (
            <div className="space-y-4">
              {pendingApprovals.map((approval) => (
                <div
                  key={approval.loanRequestId}
                  className="flex items-center justify-between p-4 bg-gray-50 rounded-lg hover:bg-gray-100 cursor-pointer transition-colors"
                >
                  <div>
                    <p className="font-medium text-gray-900">
                      {approval.purpose}
                    </p>
                    <p className="text-sm text-gray-500">
                      {approval.requesterName} • {approval.department}
                    </p>
                    <p className="text-sm text-gray-500">
                      {formatDate(approval.departureDate)} •{" "}
                      {approval.destination}
                    </p>
                  </div>
                  <Badge status={approval.status} />
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

// Driver Dashboard
function DriverDashboard() {
  const { data: schedules, isLoading } = useQuery({
    queryKey: ["my-schedules"],
    queryFn: async () => {
      const response = await scheduleService.getMySchedules();
      return response.data || [];
    },
  });

  const { data: upcomingSchedules } = useQuery({
    queryKey: ["upcoming-schedules"],
    queryFn: async () => {
      const response = await scheduleService.getUpcoming();
      return response.data || [];
    },
  });

  if (isLoading) return <PageLoading />;

  const stats = {
    total: schedules?.length || 0,
    scheduled: schedules?.filter((s) => s.status === "SCHEDULED").length || 0,
    inProgress:
      schedules?.filter((s) => s.status === "IN_PROGRESS").length || 0,
    completed: schedules?.filter((s) => s.status === "COMPLETED").length || 0,
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Dashboard Driver</h1>
        <p className="text-gray-600">Kelola jadwal perjalanan Anda</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          title="Total Jadwal"
          value={stats.total}
          icon={Calendar}
          color="blue"
        />
        <StatCard
          title="Terjadwal"
          value={stats.scheduled}
          icon={Clock}
          color="yellow"
        />
        <StatCard
          title="Dalam Perjalanan"
          value={stats.inProgress}
          icon={Car}
          color="purple"
        />
        <StatCard
          title="Selesai"
          value={stats.completed}
          icon={CheckSquare}
          color="green"
        />
      </div>

      {/* Upcoming Schedules */}
      <Card>
        <CardHeader>
          <CardTitle>Jadwal Mendatang</CardTitle>
        </CardHeader>
        <CardContent>
          {!upcomingSchedules || upcomingSchedules.length === 0 ? (
            <Alert variant="info">Tidak ada jadwal mendatang</Alert>
          ) : (
            <div className="space-y-4">
              {upcomingSchedules.map((schedule) => (
                <div
                  key={schedule.id}
                  className="flex items-center justify-between p-4 bg-gray-50 rounded-lg"
                >
                  <div>
                    <p className="font-medium text-gray-900">
                      {schedule.purpose}
                    </p>
                    <p className="text-sm text-gray-500">
                      {formatDate(schedule.departureDate)} •{" "}
                      {schedule.destination}
                    </p>
                    <p className="text-sm text-gray-500">
                      {schedule.vehiclePlateNumber} • {schedule.requesterName}
                    </p>
                  </div>
                  <Badge status={schedule.status} />
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

// Admin Dashboard
function AdminDashboard() {
  const { data: dashboard, isLoading } = useQuery({
    queryKey: ["dashboard"],
    queryFn: async () => {
      const response = await dashboardService.getDashboard();
      return response.data;
    },
  });

  if (isLoading) return <PageLoading />;

  const stats = dashboard?.stats;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Dashboard Admin</h1>
        <p className="text-gray-600">Overview sistem peminjaman kendaraan</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <StatCard
          title="Total Pengajuan"
          value={stats?.totalRequests || 0}
          icon={FileText}
          color="blue"
        />
        <StatCard
          title="Menunggu Approval"
          value={stats?.pendingApprovals || 0}
          icon={Clock}
          color="yellow"
        />
        <StatCard
          title="Kendaraan Tersedia"
          value={stats?.availableVehicles || 0}
          icon={Car}
          color="green"
        />
        <StatCard
          title="Driver Aktif"
          value={stats?.availableDrivers || 0}
          icon={Users}
          color="purple"
        />
      </div>

      {/* Additional Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <StatCard
          title="Jadwal Hari Ini"
          value={stats?.todaySchedules || 0}
          icon={Calendar}
          color="teal"
        />
        <StatCard
          title="Jadwal Aktif"
          value={stats?.activeSchedules || 0}
          icon={TrendingUp}
          color="orange"
        />
        <StatCard
          title="Menunggu Resource"
          value={stats?.waitingResources || 0}
          icon={AlertCircle}
          color="red"
        />
      </div>

      {/* Recent Activities */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Pengajuan Terbaru</CardTitle>
          </CardHeader>
          <CardContent>
            {!dashboard?.recentRequests ||
            dashboard.recentRequests.length === 0 ? (
              <p className="text-gray-500 text-center py-8">Tidak ada data</p>
            ) : (
              <div className="space-y-4">
                {dashboard.recentRequests.map((request) => (
                  <div
                    key={request.id}
                    className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
                  >
                    <div>
                      <p className="font-medium text-gray-900 text-sm">
                        {request.purpose}
                      </p>
                      <p className="text-xs text-gray-500">
                        {request.requesterName}
                      </p>
                    </div>
                    <Badge status={request.status} />
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Jadwal Hari Ini</CardTitle>
          </CardHeader>
          <CardContent>
            {!dashboard?.todaySchedulesList ||
            dashboard.todaySchedulesList.length === 0 ? (
              <p className="text-gray-500 text-center py-8">
                Tidak ada jadwal hari ini
              </p>
            ) : (
              <div className="space-y-4">
                {dashboard.todaySchedulesList.map((schedule) => (
                  <div
                    key={schedule.id}
                    className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
                  >
                    <div>
                      <p className="font-medium text-gray-900 text-sm">
                        {schedule.destination}
                      </p>
                      <p className="text-xs text-gray-500">
                        {schedule.vehiclePlateNumber} • {schedule.driverName}
                      </p>
                    </div>
                    <Badge status={schedule.status} />
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

// Stat Card Component
interface StatCardProps {
  title: string;
  value: number;
  icon: React.ElementType;
  color: "blue" | "green" | "yellow" | "red" | "purple" | "teal" | "orange";
}

function StatCard({ title, value, icon: Icon, color }: StatCardProps) {
  const colors = {
    blue: "bg-blue-100 text-blue-600",
    green: "bg-green-100 text-green-600",
    yellow: "bg-yellow-100 text-yellow-600",
    red: "bg-red-100 text-red-600",
    purple: "bg-purple-100 text-purple-600",
    teal: "bg-secondary-100 text-secondary-600",
    orange: "bg-orange-100 text-orange-600",
  };

  return (
    <Card>
      <CardContent className="flex flex-col sm:flex-row items-start sm:items-center gap-3 sm:gap-4">
        <div className={`p-2 sm:p-3 rounded-lg flex-shrink-0 ${colors[color]}`}>
          <Icon className="w-5 h-5 sm:w-6 sm:h-6" />
        </div>
        <div className="min-w-0">
          <p className="text-xs sm:text-sm text-gray-500">{title}</p>
          <p className="text-xl sm:text-2xl font-bold text-gray-900">{value}</p>
        </div>
      </CardContent>
    </Card>
  );
}

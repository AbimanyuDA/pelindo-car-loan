import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { Plus, Eye, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Table, Column } from "@/components/ui/Table";
import { Badge } from "@/components/ui/Badge";
import { ConfirmModal } from "@/components/ui/Modal";
import { PageLoading } from "@/components/ui/Loading";
import { Alert } from "@/components/ui/Alert";
import { loanRequestService } from "@/services";
import { formatDate } from "@/lib/utils";
import type { LoanRequestListItem } from "@/types";

export default function LoanRequestsPage() {
  const queryClient = useQueryClient();
  const [deleteId, setDeleteId] = useState<number | null>(null);

  const {
    data: requests,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["my-requests"],
    queryFn: async () => {
      const response = await loanRequestService.getMyRequests();
      return response.data || [];
    },
  });

  const cancelMutation = useMutation({
    mutationFn: (id: number) => loanRequestService.cancel(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["my-requests"] });
      setDeleteId(null);
    },
  });

  const columns: Column<LoanRequestListItem>[] = [
    {
      key: "id",
      header: "ID",
      render: (item) => <span className="font-mono text-xs">#{item.id}</span>,
    },
    {
      key: "purpose",
      header: "Tujuan",
      render: (item) => (
        <div>
          <p className="font-medium text-gray-900">{item.purpose}</p>
          <p className="text-sm text-gray-500">{item.destination}</p>
        </div>
      ),
    },
    {
      key: "departureDate",
      header: "Tanggal",
      render: (item) => (
        <div>
          <p className="text-sm">{formatDate(item.departureDate)}</p>
          <p className="text-xs text-gray-500">
            {item.departureTime} - {item.returnTime}
          </p>
        </div>
      ),
    },
    {
      key: "passengerCount",
      header: "Penumpang",
      render: (item) => (
        <span className="text-sm">{item.passengerCount} orang</span>
      ),
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
          <Link to={`/loan-requests/${item.id}`}>
            <Button variant="ghost" size="sm">
              <Eye className="w-4 h-4" />
            </Button>
          </Link>
          {(item.status === "PENDING" || item.status === "PENDING_L1") && (
            <Button
              variant="ghost"
              size="sm"
              onClick={(e) => {
                e.preventDefault();
                setDeleteId(item.id);
              }}
            >
              <Trash2 className="w-4 h-4 text-red-500" />
            </Button>
          )}
        </div>
      ),
    },
  ];

  if (isLoading) return <PageLoading />;

  if (error) {
    return <Alert variant="error">Gagal memuat data pengajuan</Alert>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Pengajuan Saya</h1>
          <p className="text-gray-600">
            Kelola pengajuan peminjaman kendaraan Anda
          </p>
        </div>
        <Link to="/loan-requests/new">
          <Button leftIcon={<Plus className="w-4 h-4" />}>Ajukan Baru</Button>
        </Link>
      </div>

      {/* Stats Summary */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Total</p>
            <p className="text-2xl font-bold text-gray-900">
              {requests?.length || 0}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Pending</p>
            <p className="text-2xl font-bold text-yellow-600">
              {requests?.filter((r) => r.status.includes("PENDING")).length ||
                0}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Disetujui</p>
            <p className="text-2xl font-bold text-green-600">
              {requests?.filter((r) => r.status === "APPROVED").length || 0}
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="py-4">
            <p className="text-sm text-gray-500">Ditolak</p>
            <p className="text-2xl font-bold text-red-600">
              {requests?.filter((r) => r.status === "REJECTED").length || 0}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Table */}
      <Card>
        <CardHeader>
          <CardTitle>Daftar Pengajuan</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <Table
            columns={columns}
            data={requests || []}
            keyExtractor={(item) => item.id}
            emptyMessage="Belum ada pengajuan. Klik 'Ajukan Baru' untuk membuat pengajuan."
          />
        </CardContent>
      </Card>

      {/* Cancel Confirmation Modal */}
      <ConfirmModal
        isOpen={deleteId !== null}
        onClose={() => setDeleteId(null)}
        onConfirm={() => deleteId && cancelMutation.mutate(deleteId)}
        title="Batalkan Pengajuan"
        message="Apakah Anda yakin ingin membatalkan pengajuan ini? Tindakan ini tidak dapat dibatalkan."
        confirmText="Ya, Batalkan"
        variant="danger"
        isLoading={cancelMutation.isPending}
      />
    </div>
  );
}

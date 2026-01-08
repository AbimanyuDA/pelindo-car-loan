import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Check, X, Eye, Clock } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Table, Column } from "@/components/ui/Table";
import { Badge } from "@/components/ui/Badge";
import { Modal } from "@/components/ui/Modal";
import { Textarea } from "@/components/ui/Textarea";
import { PageLoading } from "@/components/ui/Loading";
import { Alert } from "@/components/ui/Alert";
import {
  approvalService,
  loanRequestService,
  vehicleService,
  driverService,
} from "@/services";
import { formatDate } from "@/lib/utils";
import type {
  PendingApproval,
  LoanRequest,
  Approval,
  Vehicle,
  Driver,
} from "@/types";

interface ApprovalPageProps {
  level: "l1" | "l2";
}

const approvalSchema = z.object({
  notes: z.string().max(500, "Catatan maksimal 500 karakter").optional(),
  vehicleId: z.number().optional(),
  driverId: z.number().optional(),
});

type ApprovalFormData = z.infer<typeof approvalSchema>;

export default function ApprovalPage({ level }: ApprovalPageProps) {
  const queryClient = useQueryClient();
  const [selectedRequest, setSelectedRequest] =
    useState<PendingApproval | null>(null);
  const [detailRequest, setDetailRequest] = useState<LoanRequest | null>(null);
  const [actionType, setActionType] = useState<"approve" | "reject" | null>(
    null
  );
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<"pending" | "history">("pending");
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [drivers, setDrivers] = useState<Driver[]>([]);
  const [changeSelection, setChangeSelection] = useState<boolean>(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
    setValue,
  } = useForm<ApprovalFormData>({
    resolver: zodResolver(approvalSchema),
  });

  const { data: pendingApprovals, isLoading } = useQuery({
    queryKey: ["pending-approvals", level],
    queryFn: async () => {
      const response =
        level === "l1"
          ? await approvalService.getPendingL1()
          : await approvalService.getPendingL2();

      console.log("=== DEBUG API Response ===");
      console.log("Level:", level);
      console.log("Response Data:", response.data);
      if (response.data && response.data.length > 0) {
        console.log("First Item:", response.data[0]);
        console.log(
          "First Item Service Letter Basis:",
          response.data[0].serviceLetterBasis
        );
        console.log(
          "First Item Service Letter File:",
          response.data[0].serviceLetterFilePath
        );
      }
      console.log("========================");

      return response.data || [];
    },
  });

  const { data: allLoanRequests } = useQuery({
    queryKey: ["all-loan-requests"],
    queryFn: async () => {
      const response = await loanRequestService.getAll();
      return response.data || [];
    },
  });

  const { data: allVehicles } = useQuery({
    queryKey: ["vehicles"],
    queryFn: async () => {
      const response = await vehicleService.getAll();
      setVehicles(response.data || []);
      return response.data || [];
    },
  });

  const { data: allDrivers } = useQuery({
    queryKey: ["drivers"],
    queryFn: async () => {
      const response = await driverService.getAll();
      setDrivers(response.data || []);
      return response.data || [];
    },
  });

  const approveMutation = useMutation({
    mutationFn: async ({
      loanRequestId,
      notes,
      vehicleId,
      driverId,
    }: {
      loanRequestId: number;
      notes?: string;
      vehicleId?: number;
      driverId?: number;
    }) => {
      const processApproval =
        level === "l1" ? approvalService.processL1 : approvalService.processL2;
      return processApproval({
        loanRequestId,
        status: "APPROVED",
        notes,
        vehicleId,
        driverId,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["pending-approvals", level] });
      closeModal();
    },
    onError: (err: unknown) => {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || "Gagal memproses approval");
    },
  });

  const rejectMutation = useMutation({
    mutationFn: async ({
      loanRequestId,
      notes,
    }: {
      loanRequestId: number;
      notes?: string;
    }) => {
      const processApproval =
        level === "l1" ? approvalService.processL1 : approvalService.processL2;
      return processApproval({
        loanRequestId,
        status: "REJECTED",
        notes,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["pending-approvals", level] });
      closeModal();
    },
    onError: (err: unknown) => {
      const error = err as { response?: { data?: { message?: string } } };
      setError(error.response?.data?.message || "Gagal memproses penolakan");
    },
  });

  const handleViewDetail = async (item: PendingApproval) => {
    try {
      const response = await loanRequestService.getById(item.loanRequestId);
      setDetailRequest(response.data || null);
    } catch {
      setError("Gagal memuat detail pengajuan");
    }
  };

  const openApprovalModal = (
    item: PendingApproval,
    type: "approve" | "reject"
  ) => {
    console.log("=== DEBUG Approval Modal Data ===");
    console.log("Selected Request:", item);
    console.log("Service Letter Basis:", item.serviceLetterBasis);
    console.log("Service Letter File Path:", item.serviceLetterFilePath);
    console.log("================================");

    setSelectedRequest(item);
    setActionType(type);
    setError(null);
    setChangeSelection(false); // Reset to default (keep user selection)
    reset();
  };

  const closeModal = () => {
    setSelectedRequest(null);
    setActionType(null);
    setError(null);
    setChangeSelection(false);
    reset();
  };

  const onSubmit = (data: ApprovalFormData) => {
    if (!selectedRequest || !actionType) return;

    if (actionType === "approve") {
      // If user already selected and approver chose to keep, use existing selection
      let finalVehicleId = data.vehicleId;
      let finalDriverId = data.driverId;

      if (
        selectedRequest.vehicleId != null &&
        selectedRequest.driverId != null &&
        !changeSelection
      ) {
        // Keep user's original selection
        finalVehicleId = selectedRequest.vehicleId;
        finalDriverId = selectedRequest.driverId;
      }

      // Validate vehicle and driver selection for approval
      if (!finalVehicleId || !finalDriverId) {
        setError(
          "Kendaraan dan Driver harus dipilih untuk menyetujui pengajuan"
        );
        return;
      }

      approveMutation.mutate({
        loanRequestId: selectedRequest.loanRequestId,
        notes: data.notes,
        vehicleId: finalVehicleId,
        driverId: finalDriverId,
      });
    } else {
      rejectMutation.mutate({
        loanRequestId: selectedRequest.loanRequestId,
        notes: data.notes,
      });
    }
  };

  const columns: Column<PendingApproval>[] = [
    {
      key: "loanRequestId",
      header: "ID",
      render: (item) => (
        <span className="font-mono text-xs">#{item.loanRequestId}</span>
      ),
    },
    {
      key: "requesterName",
      header: "Pemohon",
      render: (item) => (
        <div>
          <p className="font-medium text-gray-900">{item.requesterName}</p>
          <p className="text-sm text-gray-500">{item.requesterDivision}</p>
        </div>
      ),
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
      key: "startDatetime",
      header: "Waktu",
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
      key: "guestList",
      header: "Daftar Tamu",
      render: (item) => (
        <span className="text-sm truncate max-w-[150px] block">
          {item.guestList}
        </span>
      ),
    },
    {
      key: "driver",
      header: "Driver",
      render: (item) => (
        <div className="flex items-center gap-1">
          <span className="text-sm">{item.driverName || "-"}</span>
          {item.driverPhone && (
            <a
              href={`https://wa.me/${item.driverPhone.replace(/\D/g, "")}`}
              target="_blank"
              rel="noopener noreferrer"
              className="text-green-600 hover:text-green-700"
              title="Hubungi via WhatsApp"
            >
              <svg
                className="w-3.5 h-3.5"
                fill="currentColor"
                viewBox="0 0 24 24"
              >
                <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413Z" />
              </svg>
            </a>
          )}
        </div>
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
          <Button
            variant="ghost"
            size="sm"
            onClick={() => handleViewDetail(item)}
          >
            <Eye className="w-4 h-4" />
          </Button>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => openApprovalModal(item, "approve")}
          >
            <Check className="w-4 h-4" />
          </Button>
          <Button
            variant="danger"
            size="sm"
            onClick={() => openApprovalModal(item, "reject")}
          >
            <X className="w-4 h-4" />
          </Button>
        </div>
      ),
    },
  ];

  const getVehicleName = (vehicleId: number) => {
    const vehicle = vehicles.find((v) => v.id === vehicleId);
    return vehicle
      ? `${vehicle.brand} ${vehicle.type} (${vehicle.plateNumber})`
      : "-";
  };

  const getDriverName = (driverId: number) => {
    const driver = drivers.find((d) => d.id === driverId);
    return driver ? driver.driverName || `Driver ${driverId}` : "-";
  };

  const approvedRequests = (allLoanRequests || []).filter(
    (req) =>
      req.status === (level === "l1" ? "APPROVED_L1" : "APPROVED_L2") ||
      req.status === "APPROVED_L2" ||
      req.status === "SCHEDULED" ||
      req.status === "COMPLETED"
  );

  const rejectedRequests = (allLoanRequests || []).filter(
    (req) => req.status === (level === "l1" ? "REJECTED_L1" : "REJECTED_L2")
  );

  if (isLoading) return <PageLoading />;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">
          Persetujuan Level {level === "l1" ? "1" : "2"}
        </h1>
        <p className="text-gray-600">
          Kelola pengajuan yang memerlukan persetujuan Anda
        </p>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          <button
            onClick={() => setActiveTab("pending")}
            className={`${
              activeTab === "pending"
                ? "border-blue-500 text-blue-600"
                : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
            } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm`}
          >
            Menunggu Persetujuan
            {pendingApprovals && pendingApprovals.length > 0 && (
              <span className="ml-2 bg-blue-100 text-blue-600 py-0.5 px-2 rounded-full text-xs">
                {pendingApprovals.length}
              </span>
            )}
          </button>
          <button
            onClick={() => setActiveTab("history")}
            className={`${
              activeTab === "history"
                ? "border-blue-500 text-blue-600"
                : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
            } whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm flex items-center gap-2`}
          >
            <Clock className="w-4 h-4" />
            Riwayat
          </button>
        </nav>
      </div>

      {activeTab === "pending" && (
        <>
          {/* Stats */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Card>
              <CardContent className="py-4">
                <p className="text-sm text-gray-500">Menunggu Review</p>
                <p className="text-2xl font-bold text-yellow-600">
                  {pendingApprovals?.length || 0}
                </p>
              </CardContent>
            </Card>
          </div>

          {/* Table */}
          <Card>
            <CardHeader>
              <CardTitle>Pengajuan Menunggu Approval</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <Table
                columns={columns}
                data={pendingApprovals || []}
                keyExtractor={(item) => item.loanRequestId}
                emptyMessage="Tidak ada pengajuan yang menunggu approval"
              />
            </CardContent>
          </Card>
        </>
      )}

      {activeTab === "history" && (
        <div className="space-y-6">
          {/* Approved Requests */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Check className="w-5 h-5 text-green-600" />
                Pengajuan Disetujui
              </CardTitle>
            </CardHeader>
            <CardContent>
              {approvedRequests.length === 0 ? (
                <p className="text-center text-gray-500 py-8">
                  Belum ada pengajuan yang disetujui
                </p>
              ) : (
                <div className="space-y-4">
                  {approvedRequests.map((req) => (
                    <div
                      key={req.id}
                      className="border rounded-lg p-4 hover:bg-gray-50 transition-colors"
                    >
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-2">
                            <span className="font-mono text-sm text-gray-500">
                              #{req.id}
                            </span>
                            <Badge status={req.status} />
                          </div>
                          <h3 className="font-medium text-gray-900 mb-1">
                            {req.purpose}
                          </h3>
                          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm text-gray-600">
                            <div>
                              <p className="text-gray-500">Pemohon</p>
                              <p className="font-medium">{req.requesterName}</p>
                            </div>
                            <div>
                              <p className="text-gray-500">Destinasi</p>
                              <p className="font-medium">{req.destination}</p>
                            </div>
                            <div>
                              <p className="text-gray-500">Waktu</p>
                              <p className="font-medium">
                                {formatDate(req.startDatetime)}
                              </p>
                            </div>
                            <div>
                              <p className="text-gray-500">Daftar Tamu</p>
                              <p className="font-medium truncate">
                                {req.guestList}
                              </p>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

          {/* Rejected Requests */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <X className="w-5 h-5 text-red-600" />
                Pengajuan Ditolak
              </CardTitle>
            </CardHeader>
            <CardContent>
              {rejectedRequests.length === 0 ? (
                <p className="text-center text-gray-500 py-8">
                  Belum ada pengajuan yang ditolak
                </p>
              ) : (
                <div className="space-y-4">
                  {rejectedRequests.map((req) => (
                    <div
                      key={req.id}
                      className="border rounded-lg p-4 hover:bg-gray-50 transition-colors"
                    >
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-2">
                            <span className="font-mono text-sm text-gray-500">
                              #{req.id}
                            </span>
                            <Badge status={req.status} />
                          </div>
                          <h3 className="font-medium text-gray-900 mb-1">
                            {req.purpose}
                          </h3>
                          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm text-gray-600">
                            <div>
                              <p className="text-gray-500">Pemohon</p>
                              <p className="font-medium">{req.requesterName}</p>
                            </div>
                            <div>
                              <p className="text-gray-500">Destinasi</p>
                              <p className="font-medium">{req.destination}</p>
                            </div>
                            <div>
                              <p className="text-gray-500">Waktu</p>
                              <p className="font-medium">
                                {formatDate(req.startDatetime)}
                              </p>
                            </div>
                            <div>
                              <p className="text-gray-500">Daftar Tamu</p>
                              <p className="font-medium truncate">
                                {req.guestList}
                              </p>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {/* Approval/Reject Modal */}
      <Modal
        isOpen={selectedRequest !== null && actionType !== null}
        onClose={closeModal}
        title={
          actionType === "approve" ? "Setujui Pengajuan" : "Tolak Pengajuan"
        }
        description={`Pengajuan #${selectedRequest?.loanRequestId} - ${selectedRequest?.purpose}`}
      >
        {error && (
          <Alert
            variant="error"
            className="mb-4"
            onClose={() => setError(null)}
          >
            {error}
          </Alert>
        )}

        <form onSubmit={handleSubmit(onSubmit)}>
          <div className="mb-4 p-4 bg-gray-50 rounded-lg">
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <p className="text-gray-500">Pemohon</p>
                <p className="font-medium">{selectedRequest?.requesterName}</p>
              </div>
              <div>
                <p className="text-gray-500">Email Pemohon</p>
                <p className="font-medium text-blue-600">
                  <a href={`mailto:${selectedRequest?.requesterEmail}`}>
                    {selectedRequest?.requesterEmail}
                  </a>
                </p>
              </div>
              <div>
                <p className="text-gray-500">Kontak Pemohon</p>
                {selectedRequest?.requesterPhone ? (
                  <a
                    href={`https://wa.me/${selectedRequest.requesterPhone.replace(
                      /\D/g,
                      ""
                    )}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="font-medium text-green-600 hover:underline flex items-center gap-1"
                  >
                    {selectedRequest.requesterPhone}
                    <svg
                      className="w-4 h-4"
                      fill="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413Z" />
                    </svg>
                  </a>
                ) : (
                  <p className="font-medium text-gray-400">-</p>
                )}
              </div>
              <div>
                <p className="text-gray-500">Divisi</p>
                <p className="font-medium">
                  {selectedRequest?.requesterDivision}
                </p>
              </div>
              <div>
                <p className="text-gray-500">Dasar Surat Pelayanan</p>
                <p className="font-medium">
                  {selectedRequest?.serviceLetterBasis || "-"}
                </p>
              </div>
              <div>
                <p className="text-gray-500">File Surat Pelayanan</p>
                {selectedRequest &&
                (selectedRequest as any).serviceLetterFilePath ? (
                  <a
                    href={`http://localhost:5000/api/LoanRequests/download-service-letter/${(
                      selectedRequest as any
                    ).serviceLetterFilePath
                      .split(/[/\\]/)
                      .pop()}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="font-medium text-blue-600 hover:underline flex items-center gap-1"
                  >
                    <svg
                      className="w-4 h-4"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                      />
                    </svg>
                    Unduh Surat
                  </a>
                ) : (
                  <p className="font-medium text-gray-400">Tidak ada file</p>
                )}
              </div>
              <div>
                <p className="text-gray-500">Tujuan</p>
                <p className="font-medium">{selectedRequest?.purpose}</p>
              </div>
              <div>
                <p className="text-gray-500">Destinasi</p>
                <p className="font-medium">{selectedRequest?.destination}</p>
              </div>
              <div>
                <p className="text-gray-500">Daftar Tamu</p>
                <p className="font-medium">{selectedRequest?.guestList}</p>
              </div>
              <div>
                <p className="text-gray-500">Hotel</p>
                <p className="font-medium">
                  {selectedRequest?.hotelAccommodation || "-"}
                </p>
              </div>
              <div className="col-span-2">
                <p className="text-gray-500">Catatan dari Pemohon</p>
                <p className="font-medium text-gray-700 bg-blue-50 p-3 rounded border border-blue-200">
                  {selectedRequest?.notes || "-"}
                </p>
              </div>

              {/* Section Pilihan Pemohon - ALWAYS SHOW */}
              <div className="col-span-2 mt-2 p-3 bg-purple-50 rounded border border-purple-200">
                <p className="text-sm font-semibold text-purple-800 mb-2">
                  Assign untuk Pemohon
                </p>
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <p className="text-gray-500">Kendaraan untuk Pemohon</p>
                    <p className="font-medium">
                      {selectedRequest?.vehicleId ? (
                        getVehicleName(selectedRequest.vehicleId)
                      ) : (
                        <span className="text-amber-600 font-semibold">
                          Belum dipilih - Anda harus memilih
                        </span>
                      )}
                    </p>
                  </div>
                  <div>
                    <p className="text-gray-500">Driver untuk Pemohon</p>
                    <div className="flex items-center gap-2">
                      <p className="font-medium">
                        {selectedRequest?.driverName || (
                          <span className="text-amber-600 font-semibold">
                            Belum dipilih - Anda harus memilih
                          </span>
                        )}
                      </p>
                      {selectedRequest?.driverPhone && (
                        <a
                          href={`https://wa.me/${selectedRequest.driverPhone.replace(
                            /\D/g,
                            ""
                          )}`}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-green-600 hover:text-green-700"
                          title="Hubungi via WhatsApp"
                        >
                          <svg
                            className="w-4 h-4"
                            fill="currentColor"
                            viewBox="0 0 24 24"
                          >
                            <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413Z" />
                          </svg>
                        </a>
                      )}
                    </div>
                  </div>
                </div>
              </div>
              <div>
                <p className="text-gray-500">Waktu Mulai</p>
                <p className="font-medium">
                  {selectedRequest && formatDate(selectedRequest.startDatetime)}
                </p>
              </div>
              <div>
                <p className="text-gray-500">Waktu Selesai</p>
                <p className="font-medium">
                  {selectedRequest && formatDate(selectedRequest.endDatetime)}
                </p>
              </div>
            </div>
          </div>

          {actionType === "approve" && (
            <>
              <div className="space-y-4 p-4 bg-blue-50 rounded-lg border border-blue-200">
                {selectedRequest?.vehicleId != null &&
                selectedRequest?.driverId != null ? (
                  <>
                    <div className="mb-3 p-3 bg-green-50 border border-green-200 rounded-lg">
                      <div className="flex items-center gap-2 text-green-700 mb-2">
                        <svg
                          className="w-5 h-5"
                          fill="currentColor"
                          viewBox="0 0 20 20"
                        >
                          <path
                            fillRule="evenodd"
                            d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                            clipRule="evenodd"
                          />
                        </svg>
                        <span className="text-sm font-semibold">
                          {level === "l2"
                            ? "Approval L1 telah mengassign kendaraan & driver"
                            : "User sudah memilih kendaraan & driver"}
                        </span>
                      </div>
                      <p className="text-xs text-green-600 mb-3">
                        {level === "l2"
                          ? "Silakan cross-check assignment dari Approval L1 dan ubah jika diperlukan"
                          : "Pilih apakah akan mempertahankan pilihan user atau menggantinya"}
                      </p>

                      {/* Radio button untuk pilih keep atau change */}
                      <div className="space-y-2">
                        <label className="flex items-center gap-2 cursor-pointer">
                          <input
                            type="radio"
                            name="selectionChoice"
                            checked={!changeSelection}
                            onChange={() => setChangeSelection(false)}
                            className="w-4 h-4 text-green-600 focus:ring-green-500"
                          />
                          <span className="text-sm font-medium text-gray-700">
                            {level === "l2"
                              ? `Pertahankan assignment L1 (${getVehicleName(
                                  selectedRequest.vehicleId
                                )} - ${selectedRequest.driverName || "Driver"})`
                              : `Pertahankan pilihan user (${getVehicleName(
                                  selectedRequest.vehicleId
                                )} - ${
                                  selectedRequest.driverName || "Driver"
                                })`}
                          </span>
                        </label>
                        <label className="flex items-center gap-2 cursor-pointer">
                          <input
                            type="radio"
                            name="selectionChoice"
                            checked={changeSelection}
                            onChange={() => setChangeSelection(true)}
                            className="w-4 h-4 text-blue-600 focus:ring-blue-500"
                          />
                          <span className="text-sm font-medium text-gray-700">
                            Ganti dengan kendaraan & driver lain
                          </span>
                        </label>
                      </div>
                    </div>

                    {/* Show dropdown only if user chooses to change */}
                    {changeSelection && (
                      <>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-2">
                            Pilih Kendaraan Baru *
                          </label>
                          <select
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                            {...register("vehicleId", {
                              setValueAs: (value) =>
                                value ? parseInt(value, 10) : undefined,
                            })}
                            defaultValue={selectedRequest?.vehicleId || ""}
                          >
                            <option value="">-- Pilih Kendaraan --</option>
                            {vehicles && vehicles.length > 0 ? (
                              vehicles.map((v) => (
                                <option key={v.id} value={v.id}>
                                  {v.brand} {v.type} - {v.plateNumber}
                                </option>
                              ))
                            ) : (
                              <option disabled>Loading vehicles...</option>
                            )}
                          </select>
                          {errors.vehicleId && (
                            <p className="text-red-500 text-sm mt-1">
                              {errors.vehicleId.message}
                            </p>
                          )}
                        </div>

                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-2">
                            Pilih Driver Baru *
                          </label>
                          <select
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                            {...register("driverId", {
                              setValueAs: (value) =>
                                value ? parseInt(value, 10) : undefined,
                            })}
                            defaultValue={selectedRequest?.driverId || ""}
                          >
                            <option value="">-- Pilih Driver --</option>
                            {drivers && drivers.length > 0 ? (
                              drivers.map((d) => (
                                <option key={d.id} value={d.id}>
                                  {d.driverName || `Driver ${d.id}`}{" "}
                                  {d.phoneNumber ? `- ${d.phoneNumber}` : ""}
                                </option>
                              ))
                            ) : (
                              <option disabled>Loading drivers...</option>
                            )}
                          </select>
                          {errors.driverId && (
                            <p className="text-red-500 text-sm mt-1">
                              {errors.driverId.message}
                            </p>
                          )}
                        </div>
                      </>
                    )}
                  </>
                ) : (
                  <>
                    <div
                      className={`mb-3 p-3 rounded-lg border ${
                        level === "l2"
                          ? "bg-red-50 border-red-200"
                          : "bg-amber-50 border-amber-200"
                      }`}
                    >
                      <div
                        className={`flex items-center gap-2 mb-1 ${
                          level === "l2" ? "text-red-700" : "text-amber-700"
                        }`}
                      >
                        <svg
                          className="w-5 h-5"
                          fill="currentColor"
                          viewBox="0 0 20 20"
                        >
                          <path
                            fillRule="evenodd"
                            d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                            clipRule="evenodd"
                          />
                        </svg>
                        <span className="text-sm font-semibold">
                          {level === "l2"
                            ? "ERROR: Approval L1 belum mengassign!"
                            : "User belum memilih - Anda harus assign"}
                        </span>
                      </div>
                      <p
                        className={`text-xs ${
                          level === "l2" ? "text-red-600" : "text-amber-600"
                        }`}
                      >
                        {level === "l2"
                          ? "Data tidak valid. Seharusnya L1 sudah assign kendaraan & driver. Silakan pilih di bawah untuk melanjutkan."
                          : "Pilih kendaraan dan driver di bawah untuk melanjutkan approval"}
                      </p>
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Pilih Kendaraan *
                      </label>
                      <select
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        {...register("vehicleId", {
                          setValueAs: (value) =>
                            value ? parseInt(value, 10) : undefined,
                        })}
                        defaultValue={selectedRequest?.vehicleId || ""}
                      >
                        <option value="">-- Pilih Kendaraan --</option>
                        {vehicles.map((v) => (
                          <option key={v.id} value={v.id}>
                            {v.brand} {v.type} - {v.plateNumber}
                          </option>
                        ))}
                      </select>
                      {errors.vehicleId && (
                        <p className="text-red-500 text-sm mt-1">
                          {errors.vehicleId.message}
                        </p>
                      )}
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Pilih Driver *
                      </label>
                      <select
                        className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                        {...register("driverId", {
                          setValueAs: (value) =>
                            value ? parseInt(value, 10) : undefined,
                        })}
                        defaultValue={selectedRequest?.driverId || ""}
                      >
                        <option value="">-- Pilih Driver --</option>
                        {drivers.map((d) => (
                          <option key={d.id} value={d.id}>
                            {d.driverName || `Driver ${d.id}`} -{" "}
                            {d.phoneNumber || ""}
                          </option>
                        ))}
                      </select>
                      {errors.driverId && (
                        <p className="text-red-500 text-sm mt-1">
                          {errors.driverId.message}
                        </p>
                      )}
                    </div>
                  </>
                )}
              </div>
            </>
          )}

          <Textarea
            label={
              actionType === "approve"
                ? "Catatan (opsional)"
                : "Alasan Penolakan"
            }
            placeholder={
              actionType === "approve"
                ? "Tambahkan catatan jika diperlukan..."
                : "Jelaskan alasan penolakan..."
            }
            rows={3}
            error={errors.notes?.message}
            {...register("notes")}
          />

          <div className="flex justify-end gap-3 mt-6">
            <Button variant="ghost" type="button" onClick={closeModal}>
              Batal
            </Button>
            <Button
              type="submit"
              variant={actionType === "approve" ? "secondary" : "danger"}
              isLoading={approveMutation.isPending || rejectMutation.isPending}
            >
              {actionType === "approve" ? "Setujui" : "Tolak"}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Detail Modal */}
      <Modal
        isOpen={detailRequest !== null}
        onClose={() => setDetailRequest(null)}
        title="Detail Pengajuan"
        size="lg"
      >
        {detailRequest && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-sm text-gray-500">ID Pengajuan</p>
                <p className="font-medium">#{detailRequest.id}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Status</p>
                <Badge status={detailRequest.status} />
              </div>
              <div>
                <p className="text-sm text-gray-500">Pemohon</p>
                <p className="font-medium">{detailRequest.requesterName}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Email Pemohon</p>
                <a
                  href={`mailto:${detailRequest.requesterEmail}`}
                  className="font-medium text-blue-600 hover:underline"
                >
                  {detailRequest.requesterEmail}
                </a>
              </div>
              <div className="col-span-2">
                <p className="text-sm text-gray-500">Kontak Pemohon</p>
                {detailRequest.requesterPhone ? (
                  <a
                    href={`https://wa.me/${detailRequest.requesterPhone.replace(
                      /\D/g,
                      ""
                    )}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="font-medium text-green-600 hover:underline flex items-center gap-2"
                  >
                    {detailRequest.requesterPhone}
                    <svg
                      className="w-4 h-4"
                      fill="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413Z" />
                    </svg>
                  </a>
                ) : (
                  <p className="font-medium text-gray-400">-</p>
                )}
              </div>
              <div className="col-span-2">
                <p className="text-sm text-gray-500">
                  Dasar Surat Pelayanan (SPPD)
                </p>
                <p className="font-medium">
                  {detailRequest.serviceLetterBasis || "-"}
                </p>
              </div>
              <div className="col-span-2">
                <p className="text-sm text-gray-500">File Surat Pelayanan</p>
                {detailRequest.serviceLetterFilePath ? (
                  <a
                    href={`http://localhost:5000/api/LoanRequests/download-service-letter/${detailRequest.serviceLetterFilePath
                      .split(/[/\\]/)
                      .pop()}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="font-medium text-blue-600 hover:underline flex items-center gap-1"
                  >
                    <svg
                      className="w-4 h-4"
                      fill="none"
                      stroke="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                      />
                    </svg>
                    Unduh Surat Pelayanan
                  </a>
                ) : (
                  <p className="font-medium text-gray-400">Tidak ada file</p>
                )}
              </div>
              <div>
                <p className="text-sm text-gray-500">Tujuan</p>
                <p className="font-medium">{detailRequest.purpose}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Destinasi</p>
                <p className="font-medium">{detailRequest.destination}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">
                  Daftar Tamu yang Dilayani
                </p>
                <p className="font-medium">{detailRequest.guestList}</p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Hotel Menginap</p>
                <p className="font-medium">
                  {detailRequest.hotelAccommodation || "-"}
                </p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Kendaraan</p>
                <p className="font-medium">
                  {getVehicleName(detailRequest.vehicleId)}
                </p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Driver</p>
                <div className="flex items-center gap-2">
                  <p className="font-medium">
                    {detailRequest.driverName ||
                      getDriverName(detailRequest.driverId)}
                  </p>
                  {detailRequest.driverPhone && (
                    <a
                      href={`https://wa.me/${detailRequest.driverPhone.replace(
                        /\D/g,
                        ""
                      )}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-green-600 hover:text-green-700"
                      title="Hubungi via WhatsApp"
                    >
                      <svg
                        className="w-4 h-4"
                        fill="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413Z" />
                      </svg>
                    </a>
                  )}
                </div>
              </div>
              <div>
                <p className="text-sm text-gray-500">Waktu Mulai</p>
                <p className="font-medium">
                  {formatDate(detailRequest.startDatetime)}
                </p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Waktu Selesai</p>
                <p className="font-medium">
                  {formatDate(detailRequest.endDatetime)}
                </p>
              </div>
            </div>
            {detailRequest.notes && (
              <div>
                <p className="text-sm text-gray-500">Catatan</p>
                <p className="text-gray-700 bg-gray-50 p-3 rounded-lg mt-1">
                  {detailRequest.notes}
                </p>
              </div>
            )}
          </div>
        )}
      </Modal>
    </div>
  );
}

// Export named components for routes
export function ApprovalL1Page() {
  return <ApprovalPage level="l1" />;
}

export function ApprovalL2Page() {
  return <ApprovalPage level="l2" />;
}

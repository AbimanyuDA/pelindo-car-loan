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
import { useRealTimeUpdates } from "@/hooks/useRealTimeUpdates";
import {
  approvalService,
  loanRequestService,
  vehicleService,
  driverService,
} from "@/services";
import { formatDate, formatDateTime, formatTime } from "@/lib/utils";
import type { PendingApproval, LoanRequest, Vehicle, Driver } from "@/types";

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
  const [changeDriver, setChangeDriver] = useState<boolean>(false); // For optional driver change in mogok emergency

  // Subscribe to real-time approval updates
  const endpoint = level === "l1" ? "/approvals/subscribe" : "/approvals/subscribe";
  useRealTimeUpdates(endpoint, ["pending-approvals", level.toString()]);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
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

      const all = response.data || [];
      return all.filter((item) => !item.emergencyReason);
    },
  });

  const { data: emergencyApprovals, isLoading: isEmergencyLoading } = useQuery({
    queryKey: ["emergency-approvals", level],
    queryFn: async () => {
      const response =
        level === "l1"
          ? await approvalService.getEmergencyL1()
          : await approvalService.getEmergencyL2();
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

  // Fetch vehicles with date filtering if selectedRequest is available
  const { data: availableVehicles } = useQuery({
    queryKey: [
      "available-vehicles",
      selectedRequest?.startDatetime,
      selectedRequest?.endDatetime,
    ],
    queryFn: async () => {
      if (selectedRequest?.startDatetime && selectedRequest?.endDatetime) {
        const response = await vehicleService.getAvailable(
          selectedRequest.startDatetime,
          selectedRequest.endDatetime
        );
        return response.data || [];
      }
      return [];
    },
    enabled: !!selectedRequest && !!selectedRequest?.startDatetime && !!selectedRequest?.endDatetime,
  });

  // Fetch ALL vehicles to show in dropdown (booked ones will be disabled)
  useQuery({
    queryKey: ["all-vehicles"],
    queryFn: async () => {
      const response = await vehicleService.getAll();
      setVehicles(response.data || []);
      return response.data || [];
    },
    enabled: !!selectedRequest,
  });

  // Fetch drivers with date filtering if selectedRequest is available
  const { data: availableDrivers } = useQuery({
    queryKey: [
      "available-drivers",
      selectedRequest?.startDatetime,
      selectedRequest?.endDatetime,
    ],
    queryFn: async () => {
      if (selectedRequest?.startDatetime && selectedRequest?.endDatetime) {
        const response = await driverService.getAvailable(
          selectedRequest.startDatetime,
          selectedRequest.endDatetime
        );
        return response.data || [];
      }
      return [];
    },
    enabled: !!selectedRequest && !!selectedRequest?.startDatetime && !!selectedRequest?.endDatetime,
  });

  // Fetch ALL drivers to show in dropdown (booked ones will be disabled)
  useQuery({
    queryKey: ["all-drivers"],
    queryFn: async () => {
      const includeCurrentDriver =
        !!selectedRequest?.emergencyReason &&
        selectedRequest?.driverId != null;

      const mergeCurrentDriver = async (list: Driver[]) => {
        if (!includeCurrentDriver) return list;

        const exists = list.some((d) => d.id === selectedRequest!.driverId);
        if (exists) return list;

        try {
          const extra = await driverService.getById(selectedRequest!.driverId!);
          if (extra.data) {
            return [...list, extra.data];
          }
        } catch {
          // Ignore if driver not found
        }

        return list;
      };

      // Always fetch all drivers (regardless of date range)
      const response = await driverService.getAll();
      const list = await mergeCurrentDriver(response.data || []);
      setDrivers(list);
      return list;
    },
    enabled: !!selectedRequest, // Only enabled when modal is open
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
      queryClient.invalidateQueries({ queryKey: ["emergency-approvals", level] });
      closeModal();
      // Auto-refresh page after 500ms to ensure data is updated
      setTimeout(() => {
        window.location.reload();
      }, 500);
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
        status: "REJECTED",
        notes,
        vehicleId,
        driverId,
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["pending-approvals", level] });
      queryClient.invalidateQueries({ queryKey: ["emergency-approvals", level] });
      closeModal();
      // Auto-refresh page after 500ms to ensure data is updated
      setTimeout(() => {
        window.location.reload();
      }, 500);
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
    if (level === "l2" && type === "reject" && item.emergencyReason) {
      return;
    }
    console.log("=== DEBUG Approval Modal Data ===");
    console.log("Level:", level);
    console.log("Selected Request:", item);
    console.log("Emergency Reason:", item.emergencyReason);
    console.log("Emergency Type:", item.emergencyType);
    console.log("Vehicle ID:", item.vehicleId);
    console.log("Driver ID:", item.driverId);
    console.log("Driver Name:", item.driverName);
    console.log("Service Letter Basis:", item.serviceLetterBasis);
    console.log("Service Letter File Path:", item.serviceLetterFilePath);
    console.log("All keys in item:", Object.keys(item));
    console.log("================================");

    setSelectedRequest(item);
    setActionType(type);
    setError(null);
    setChangeSelection(false); // Reset to default (keep user selection)
    setChangeDriver(false); // Reset driver change option
    reset();
  };

  const closeModal = () => {
    setSelectedRequest(null);
    setActionType(null);
    setError(null);
    setChangeSelection(false);
    setChangeDriver(false);
    reset();
  };

  const onSubmit = (data: ApprovalFormData) => {
    if (!selectedRequest || !actionType) return;

    if (level === "l2" && actionType === "reject" && selectedRequest.emergencyReason) {
      return;
    }

    const isOtherEmergencyL1Reject =
      actionType === "reject" &&
      selectedRequest.emergencyReason &&
      selectedRequest.emergencyType === "LAINNYA" &&
      level === "l1";

    if (actionType === "approve" || isOtherEmergencyL1Reject) {
      // If user already selected and approver chose to keep, use existing selection
      let finalVehicleId = data.vehicleId;
      let finalDriverId = data.driverId;

      // Handle different scenarios
      const isMogokEmergencyL1 = selectedRequest.emergencyReason && 
        selectedRequest.emergencyType === "MOGOK" && 
        level === "l1";

      const isOtherEmergencyL1 = selectedRequest.emergencyReason && 
        selectedRequest.emergencyType === "LAINNYA" && 
        level === "l1";
      
      const isEmergencyL2 = selectedRequest.emergencyReason && level === "l2";

      if (level === "l2") {
        if (isEmergencyL2) {
          // Emergency L2: review only
          finalVehicleId = selectedRequest.vehicleId;
          finalDriverId = selectedRequest.driverId;
        } else if (!changeSelection) {
          // Non-emergency L2: keep L1 assignment
          finalVehicleId = selectedRequest.vehicleId;
          finalDriverId = selectedRequest.driverId;
        }
      } else if (isMogokEmergencyL1) {
        // MOGOK Emergency L1: Vehicle is mandatory, driver is optional
        if (!finalVehicleId) {
          setError("Kendaraan pengganti wajib dipilih untuk emergency mogok");
          return;
        }
        // If driver not changed, use existing driver
        if (!changeDriver || !finalDriverId) {
          finalDriverId = selectedRequest.driverId;
        }
      } else if (isOtherEmergencyL1) {
        if (isOtherEmergencyL1Reject) {
          // LAINNYA Emergency L1 Reject: keep existing assignment if no changes
          if (!finalVehicleId) {
            finalVehicleId = selectedRequest.vehicleId;
          }
          if (!finalDriverId) {
            finalDriverId = selectedRequest.driverId;
          }
        } else {
          // LAINNYA Emergency L1 Approve: Vehicle and driver must be assigned
          if (!finalVehicleId || !finalDriverId) {
            setError("Kendaraan dan driver wajib dipilih untuk emergency alasan lain");
            return;
          }
        }
      } else if (isEmergencyL2) {
        // Emergency L2: review only
        finalVehicleId = selectedRequest.vehicleId;
        finalDriverId = selectedRequest.driverId;
      } else if (
        selectedRequest.vehicleId != null &&
        selectedRequest.driverId != null &&
        !changeSelection
      ) {
        // Normal case: Keep user's original selection
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
            {formatTime(item.startDatetime)} - {formatTime(item.endDatetime)}
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
          {(level === "l1" || (level === "l2" && !item.emergencyReason)) && (
            <Button
              variant="danger"
              size="sm"
              onClick={() => openApprovalModal(item, "reject")}
            >
              <X className="w-4 h-4" />
            </Button>
          )}
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

  const parseEmergencyReason = (text?: string) => {
    if (!text) return { alasan: "", keterangan: "" };
    const trimmed = text.trim();
    const bracketMatch = trimmed.match(/^\[(.*?)\](.*)/s);
    if (bracketMatch) {
      return {
        alasan: bracketMatch[1].trim(),
        keterangan: bracketMatch[2].trim(),
      };
    }
    return { alasan: "", keterangan: trimmed };
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

  const isOtherEmergency =
    !!selectedRequest?.emergencyReason && selectedRequest?.emergencyType === "LAINNYA";
  const isOtherEmergencyL1Reject =
    actionType === "reject" &&
    level === "l1" &&
    !!selectedRequest?.emergencyReason &&
    selectedRequest?.emergencyType === "LAINNYA";
  const parsedEmergencyReason = parseEmergencyReason(
    selectedRequest?.emergencyReason
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

          {/* Emergency Segment */}
          {(isEmergencyLoading || (emergencyApprovals && emergencyApprovals.length > 0)) && (
            <Card className="border-2 border-red-500 bg-gradient-to-br from-red-50 to-orange-50 shadow-lg">
              <CardHeader className="bg-red-600 text-white rounded-t-lg">
                <CardTitle className="flex items-center gap-3">
                  <span className="w-8 h-8 flex items-center justify-center rounded-full bg-white text-red-600 text-lg font-bold animate-pulse">
                    !
                  </span>
                  <div>
                    <div className="text-lg font-bold">PENGAJUAN EMERGENCY</div>
                    <div className="text-sm font-normal opacity-90">Memerlukan Tindakan Segera</div>
                  </div>
                  <span className="ml-auto bg-white text-red-600 px-3 py-1 rounded-full text-sm font-bold">
                    {emergencyApprovals?.length || 0} Kasus
                  </span>
                </CardTitle>
              </CardHeader>
              <CardContent className="p-0">
                {isEmergencyLoading ? (
                  <div className="flex justify-center py-8">
                    <span className="text-gray-500">Memuat...</span>
                  </div>
                ) : emergencyApprovals && emergencyApprovals.length > 0 ? (
                  <div className="space-y-4 p-6">
                    {emergencyApprovals.map((req) => (
                      <div
                        key={req.loanRequestId}
                        className="border-2 border-red-400 rounded-xl p-5 bg-white shadow-md hover:shadow-xl transition-all duration-200"
                      >
                        {/* Header Row with Title, Pemohon, and Time */}
                        <div className="mb-4 pb-4 border-b border-gray-200">
                          <div className="flex items-start justify-between mb-3">
                            <div className="flex-1">
                              <div className="flex items-center gap-3 mb-2">
                                <span className="font-mono text-xs bg-gray-100 px-2 py-1 rounded text-gray-600">
                                  #{req.loanRequestId}
                                </span>
                                <Badge status="EMERGENCY" />
                                <span className="text-xs bg-orange-100 text-orange-800 px-2 py-1 rounded-full font-medium">
                                  Emergency dari Driver
                                </span>
                              </div>
                              <h3 className="text-lg font-bold text-gray-900 mb-1">
                                {req.purpose}
                              </h3>
                              <p className="text-sm text-gray-600">{req.destination}</p>
                            </div>
                            <div className="flex flex-col gap-2 ml-4">
                              <div className="text-right">
                                <p className="text-xs text-gray-500">Pemohon</p>
                                <p className="font-semibold text-gray-900 text-sm">{req.requesterName}</p>
                                <p className="text-xs text-gray-500">{req.requesterDivision}</p>
                              </div>
                              <div className="text-right">
                                <p className="text-xs text-gray-500">Waktu Keberangkatan</p>
                                <p className="font-semibold text-gray-900 text-sm">
                                  {formatDateTime(req.startDatetime)}
                                </p>
                              </div>
                            </div>
                          </div>
                        </div>

                        {/* Driver Section - Standalone */}
                        <div className="mb-4 bg-blue-50 rounded-lg p-4 border-2 border-blue-300">
                          <div className="flex items-center gap-2 mb-2">
                            <div className="w-6 h-6 bg-blue-600 rounded-full flex items-center justify-center">
                              <span className="text-white text-xs font-bold">D</span>
                            </div>
                            <p className="text-sm font-bold text-blue-900 uppercase">Driver</p>
                          </div>
                          <div className="mt-2">
                            <p className="font-bold text-gray-900 text-base">
                              {req.driverName || "Driver belum ditentukan"}
                            </p>
                            {req.driverPhone && (
                              <p className="text-sm text-gray-700 mt-1">☎ {req.driverPhone}</p>
                            )}
                          </div>
                        </div>

                        {/* Emergency Reason - Prominent Display */}
                        {req.emergencyReason && (
                          <div className="mb-4 bg-red-50 border-2 border-red-300 rounded-lg p-4">
                            <div className="mb-2">
                              <span className="inline-block bg-red-600 text-white text-xs font-bold px-3 py-1 rounded-full uppercase">
                                Laporan Emergency Driver
                              </span>
                            </div>
                            <div className="mt-3 space-y-3">
                              {(() => {
                                const text = req.emergencyReason.trim();
                                // Check if text contains [bracket] pattern
                                const bracketMatch = text.match(/^\[(.*?)\](.*)/s);
                                
                                if (bracketMatch) {
                                  // Format: [Alasan] Keterangan
                                  const alasan = bracketMatch[1].trim();
                                  const keterangan = bracketMatch[2].trim();
                                  
                                  return (
                                    <>
                                      {alasan && (
                                        <div>
                                          <p className="text-xs font-bold text-red-800 mb-1">Alasan:</p>
                                          <div className="bg-white rounded-lg p-3 border border-red-200">
                                            <p className="text-red-900 font-semibold">{alasan}</p>
                                          </div>
                                        </div>
                                      )}
                                      {keterangan && (
                                        <div>
                                          <p className="text-xs font-bold text-red-800 mb-1">Keterangan:</p>
                                          <div className="bg-white rounded-lg p-3 border border-red-200">
                                            <p className="text-red-900 leading-relaxed whitespace-pre-line">{keterangan}</p>
                                          </div>
                                        </div>
                                      )}
                                    </>
                                  );
                                } else {
                                  // No bracket pattern, display as is
                                  return (
                                    <div>
                                      <p className="text-xs font-bold text-red-800 mb-1">Keterangan:</p>
                                      <div className="bg-white rounded-lg p-3 border border-red-200">
                                        <p className="text-red-900 leading-relaxed whitespace-pre-line">{text}</p>
                                      </div>
                                    </div>
                                  );
                                }
                              })()}
                            </div>
                          </div>
                        )}

                        {/* Action Buttons */}
                        <div className="flex items-center gap-3 pt-4 border-t border-gray-200">
                          <button
                            onClick={() => {
                              console.log("🚨 EMERGENCY DEBUG - Opening modal:", {
                                emergencyReason: req.emergencyReason,
                                emergencyType: req.emergencyType,
                                vehicleId: req.vehicleId,
                                driverId: req.driverId,
                                driverName: req.driverName,
                                level: level
                              });
                              setSelectedRequest(req);
                              setChangeSelection(false); // Default: pertahankan assignment
                              setActionType(
                                level === "l2"
                                  ? "approve"
                                  : req.emergencyType === "LAINNYA"
                                    ? null
                                    : "approve"
                              );
                              setChangeDriver(false);
                            }}
                            className="flex-1 flex items-center justify-center gap-2 px-6 py-3 bg-red-600 hover:bg-red-700 text-white font-bold rounded-lg shadow-md hover:shadow-lg transition-all duration-200 transform hover:scale-105"
                          >
                            <span className="text-lg">⚠</span>
                            <span>TINDAK LANJUTI SEKARANG</span>
                          </button>
                          <button
                            onClick={() => {
                              setDetailRequest(null);
                              loanRequestService
                                .getById(req.loanRequestId)
                                .then((response) => {
                                  setDetailRequest(response.data ?? null);
                                })
                                .catch((error) => {
                                  console.error("Error fetching request details:", error);
                                });
                            }}
                            className="px-4 py-3 bg-gray-100 hover:bg-gray-200 text-gray-700 font-medium rounded-lg transition-colors"
                          >
                            Detail
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-center text-gray-500 py-8">
                    Tidak ada pengajuan emergency
                  </p>
                )}
              </CardContent>
            </Card>
          )}

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
                                {formatDateTime(req.startDatetime)}
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
                                {formatDateTime(req.startDatetime)}
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
        isOpen={selectedRequest !== null}
        onClose={closeModal}
        title={
          actionType === "approve" || isOtherEmergencyL1Reject
            ? "Setujui Pengajuan"
            : actionType === "reject"
              ? "Tolak Pengajuan"
              : isOtherEmergency
                ? "Tindak Lanjuti Emergency"
                : "Detail Pengajuan"
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

        {isOtherEmergency && selectedRequest && (
          <div className="mb-4 p-4 bg-red-50 border-2 border-red-200 rounded-lg">
            <div className="flex items-center gap-2 text-red-700 mb-3">
              <span className="text-sm font-bold">🚨 Emergency Alasan Lain</span>
              {level === "l1" && (
                <span className="text-xs text-red-600">
                  Persetujuan L1 akan membatalkan assignment driver sebelumnya
                </span>
              )}
            </div>

            {actionType === null && level === "l1" && (
              <div className="grid grid-cols-2 gap-3 mb-4">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => {
                    setActionType("approve");
                    setChangeSelection(false);
                    setChangeDriver(false);
                  }}
                >
                  Approve
                </Button>
                <Button
                  type="button"
                  variant="danger"
                  onClick={() => {
                    setActionType("reject");
                    setChangeSelection(false);
                    setChangeDriver(false);
                  }}
                >
                  Reject
                </Button>
              </div>
            )}

            <div className="grid grid-cols-1 gap-4 text-sm">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-gray-500">Driver</p>
                  <p className="font-medium text-gray-900">
                    {selectedRequest.driverName || "Driver"}
                  </p>
                </div>
                <div>
                  <p className="text-gray-500">Kontak Driver</p>
                  {selectedRequest.driverPhone ? (
                    <a
                      href={`https://wa.me/${selectedRequest.driverPhone.replace(
                        /\D/g,
                        ""
                      )}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="font-medium text-green-600 hover:underline flex items-center gap-1"
                    >
                      {selectedRequest.driverPhone}
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
              </div>

              <div>
                <p className="text-gray-500">Alasan Driver</p>
                <p className="font-medium text-gray-900 bg-white p-3 rounded border border-red-100">
                  {parsedEmergencyReason.alasan || "-"}
                </p>
              </div>
              <div>
                <p className="text-gray-500">Keterangan Driver</p>
                <p className="font-medium text-gray-700 bg-white p-3 rounded border border-red-100 whitespace-pre-line">
                  {parsedEmergencyReason.keterangan || "-"}
                </p>
              </div>
            </div>
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)}>
          <div className="mb-4 p-4 bg-gray-50 rounded-lg">
            <div className="grid grid-cols-1 gap-4 text-sm">
              {/* Nama Pemohon - Full Width */}
              <div>
                <p className="text-gray-500">Pemohon</p>
                <p className="font-medium text-lg">
                  {selectedRequest?.requesterName}
                </p>
              </div>

              {/* Email dan Kontak - Sejajar */}
              <div className="grid grid-cols-2 gap-4">
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
              </div>

              {/* Divisi dan Unit Kerja - Sejajar */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-gray-500">Divisi</p>
                  <p className="font-medium">
                    {selectedRequest?.requesterDivision}
                  </p>
                </div>
                <div>
                  <p className="text-gray-500">Unit Kerja</p>
                  <p className="font-medium">
                    {selectedRequest?.requesterUnitKerja || "-"}
                  </p>
                </div>
              </div>

              {/* Dasar Surat dan File - Sejajar */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-gray-500">Dasar Surat Pelayanan</p>
                  <p className="font-medium">
                    {selectedRequest?.serviceLetterBasis || "-"}
                  </p>
                </div>
                <div>
                  <p className="text-gray-500">File Surat Pelayanan</p>
                  {selectedRequest?.serviceLetterFilePath ? (
                    <a
                      href={`http://localhost:5000/api/LoanRequests/download-service-letter/${selectedRequest.serviceLetterFilePath
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
              </div>

              {/* Tujuan dan Destinasi - Sejajar */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-gray-500">Tujuan</p>
                  <p className="font-medium">{selectedRequest?.purpose}</p>
                </div>
                <div>
                  <p className="text-gray-500">Destinasi</p>
                  <p className="font-medium">{selectedRequest?.destination}</p>
                </div>
              </div>

              {/* Daftar Tamu dan Hotel - Sejajar */}
              <div className="grid grid-cols-2 gap-4">
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
              </div>

              <div>
                <p className="text-gray-500">Waktu Mulai</p>
                <p className="font-medium">
                  {selectedRequest &&
                    formatDateTime(selectedRequest.startDatetime)}
                </p>
              </div>
              <div>
                <p className="text-gray-500">Waktu Selesai</p>
                <p className="font-medium">
                  {selectedRequest &&
                    formatDateTime(selectedRequest.endDatetime)}
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
                    {/* Check if this is MOGOK emergency AND level 1 */}
                    {selectedRequest.emergencyReason && selectedRequest.emergencyType === "MOGOK" && level === "l1" ? (
                      // MOGOK Emergency L1 - Show previous driver, optional change, MUST change vehicle
                      <div className="mb-3 p-3 bg-red-50 border-2 border-red-400 rounded-lg">
                        <div className="flex items-center gap-2 text-red-700 mb-2">
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
                          <span className="text-sm font-bold">
                            🚨 Emergency Mogok - Kendaraan Bermasalah
                          </span>
                        </div>
                        <p className="text-xs text-red-700 mb-3">
                          Kendaraan mengalami masalah teknis. Wajib pilih kendaraan pengganti.
                        </p>
                        
                        {/* Info kendaraan bermasalah */}
                        <div className="mb-3 p-3 bg-white rounded border-2 border-red-300">
                          <p className="text-xs font-semibold text-red-800 mb-2">🚗 Kendaraan Bermasalah:</p>
                          <p className="text-sm font-bold text-red-900">
                            {selectedRequest.vehicleId ? getVehicleName(selectedRequest.vehicleId) : "Kendaraan tidak diketahui"}
                          </p>
                          <p className="text-xs text-red-600 mt-1 italic">
                            Kendaraan ini tidak dapat digunakan
                          </p>
                        </div>
                        
                        {/* Info driver sebelumnya */}
                        <div className="mb-3 p-2 bg-white rounded border border-red-200">
                          <p className="text-xs font-semibold text-gray-700 mb-1">👤 Driver Sebelumnya:</p>
                          <p className="text-sm font-medium text-gray-900">
                            {selectedRequest.driverName || "Driver"}
                            {selectedRequest.driverPhone && (
                              <span className="text-gray-500 ml-2">({selectedRequest.driverPhone})</span>
                            )}
                          </p>
                          <div className="mt-2">
                            <label className="flex items-center gap-2 cursor-pointer">
                              <input
                                type="checkbox"
                                checked={changeDriver}
                                onChange={(e) => setChangeDriver(e.target.checked)}
                                className="w-4 h-4 text-blue-600 focus:ring-blue-500 rounded"
                              />
                              <span className="text-xs text-gray-700">
                                Ganti driver (opsional)
                              </span>
                            </label>
                          </div>
                        </div>
                      </div>
                    ) : selectedRequest.emergencyReason && level === "l2" ? (
                      // Emergency L2 - Review only
                      <div className="mb-3 p-3 bg-blue-50 border-2 border-blue-400 rounded-lg">
                        <div className="flex items-center gap-2 text-blue-700 mb-2">
                          <svg
                            className="w-5 h-5"
                            fill="currentColor"
                            viewBox="0 0 20 20"
                          >
                            <path
                              fillRule="evenodd"
                              d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
                              clipRule="evenodd"
                            />
                          </svg>
                          <span className="text-sm font-bold">
                            Review Assignment Emergency dari L1
                          </span>
                        </div>
                        <p className="text-xs text-blue-700 mb-3">
                          L2 hanya review assignment L1 dan melakukan approval.
                        </p>

                        <div className="space-y-2 text-sm">
                          <div className="flex items-center justify-between p-2 bg-white rounded border border-blue-200">
                            <span className="text-gray-600">🚗 Kendaraan (L1):</span>
                            <span className="font-medium">{getVehicleName(selectedRequest.vehicleId)}</span>
                          </div>
                          <div className="flex items-center justify-between p-2 bg-white rounded border border-blue-200">
                            <span className="text-gray-600">👤 Driver (L1):</span>
                            <span className="font-medium">{selectedRequest.driverName || "Driver"}</span>
                          </div>
                        </div>
                      </div>
                    ) : selectedRequest.emergencyReason && selectedRequest.emergencyType === "LAINNYA" && level === "l1" ? (
                      // Emergency LAINNYA L1 - Must assign vehicle & driver
                      <div className="mb-3 p-3 bg-red-50 border-2 border-red-400 rounded-lg">
                        <div className="flex items-center gap-2 text-red-700 mb-2">
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
                          <span className="text-sm font-bold">
                            Emergency Alasan Lain - Wajib Assign
                          </span>
                        </div>
                        <p className="text-xs text-red-700">
                          Approver L1 wajib memilih kendaraan dan driver pengganti untuk melanjutkan.
                        </p>
                      </div>
                    ) : (
                      // Normal case
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
                          {level === "l2" && !selectedRequest.emergencyReason
                            ? "Pilih apakah akan mempertahankan assignment L1 atau menggantinya"
                            : level === "l2"
                              ? "L2 hanya review assignment L1 dan melakukan approval"
                              : "Pilih apakah akan mempertahankan pilihan user atau menggantinya"}
                        </p>

                        {(level === "l1" || (level === "l2" && !selectedRequest.emergencyReason)) && (
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
                                {`Pertahankan assignment (${getVehicleName(
                                  selectedRequest.vehicleId
                                )} - ${selectedRequest.driverName || "Driver"})`}
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
                        )}
                      </div>
                    )}

                    {/* Show dropdown based on context */}
                    {(level === "l1" || (level === "l2" && !selectedRequest.emergencyReason && changeSelection)) && (changeSelection || 
                      (selectedRequest.emergencyReason && selectedRequest.emergencyType === "MOGOK" && level === "l1") ||
                      (selectedRequest.emergencyReason && selectedRequest.emergencyType === "LAINNYA" && level === "l1")) && (
                      <>
                        <div>
                          <label className="block text-sm font-medium text-gray-700 mb-2">
                            {selectedRequest.emergencyReason && selectedRequest.emergencyType === "MOGOK" && level === "l1" 
                              ? "Pilih Kendaraan Pengganti * (Kendaraan bermasalah tidak dapat dipilih)"
                              : "Pilih Kendaraan Baru *"}
                          </label>
                          <select
                            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                            {...register("vehicleId", {
                              setValueAs: (value) =>
                                value ? parseInt(value, 10) : undefined,
                            })}
                            defaultValue={""}
                          >
                            <option value="">-- Pilih Kendaraan --</option>
                            {vehicles && vehicles.length > 0 ? (
                              vehicles.map((v) => {
                                // Disable kendaraan bermasalah jika emergency mogok L1
                                const isProblematicVehicle = selectedRequest.emergencyReason && 
                                  selectedRequest.emergencyType === "MOGOK" && 
                                  level === "l1" && 
                                  v.id === selectedRequest.vehicleId;
                                
                                // Check if vehicle is booked (not in available list)
                                const isBooked = availableVehicles && !availableVehicles.some((av: Vehicle) => av.id === v.id);
                                
                                return (
                                  <option 
                                    key={v.id} 
                                    value={v.id}
                                    disabled={isProblematicVehicle || isBooked}
                                    style={(isProblematicVehicle || isBooked) ? { 
                                      color: '#999', 
                                      fontStyle: 'italic',
                                      textDecoration: isProblematicVehicle ? 'line-through' : 'none'
                                    } : {}}
                                  >
                                    {v.brand} {v.type} - {v.plateNumber}
                                    {isProblematicVehicle ? ' 🚫 (Bermasalah)' : ''}
                                    {!isProblematicVehicle && isBooked ? ' 📅 (Sedang digunakan)' : ''}
                                  </option>
                                );
                              })
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

                        {/* Driver dropdown - show if NOT mogok emergency OR if changeDriver is checked */}
                        {!(selectedRequest.emergencyReason && selectedRequest.emergencyType === "MOGOK" && level === "l1") || changeDriver ? (
                          <div>
                            <label className="block text-sm font-medium text-gray-700 mb-2">
                              {selectedRequest.emergencyReason && selectedRequest.emergencyType === "MOGOK" && level === "l1"
                                ? "Pilih Driver Pengganti (Opsional)"
                                : "Pilih Driver Baru *"}
                            </label>
                            <select
                              className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                              {...register("driverId", {
                                setValueAs: (value) =>
                                  value ? parseInt(value, 10) : undefined,
                              })}
                              defaultValue={""}
                            >
                              <option value="">-- Pilih Driver --</option>
                              {drivers && drivers.length > 0 ? (
                                drivers.map((d) => {
                                  // Check if driver is booked (not in available list)
                                  const isBooked = availableDrivers && !availableDrivers.some((ad: Driver) => ad.id === d.id);
                                  
                                  return (
                                    <option 
                                      key={d.id} 
                                      value={d.id}
                                      disabled={isBooked}
                                      style={isBooked ? { 
                                        color: '#999', 
                                        fontStyle: 'italic'
                                      } : {}}
                                    >
                                      {d.driverName || `Driver ${d.id}`}{" "}
                                      {d.phoneNumber ? `- ${d.phoneNumber}` : ""}
                                      {isBooked ? ' 📅 (Sedang digunakan)' : ''}
                                    </option>
                                  );
                                })
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
                        ) : (
                          <div className="p-3 bg-gray-50 border border-gray-200 rounded-lg">
                            <p className="text-sm text-gray-600">
                              <span className="font-semibold">Driver tetap:</span> {selectedRequest.driverName || "Driver"}
                            </p>
                            <p className="text-xs text-gray-500 mt-1">
                              Centang opsi "Ganti driver" di atas jika ingin mengganti driver.
                            </p>
                          </div>
                        )}
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
                        {vehicles.map((v) => {
                          const isBooked =
                            availableVehicles &&
                            !availableVehicles.some(
                              (av: Vehicle) => av.id === v.id
                            );
                          return (
                            <option
                              key={v.id}
                              value={v.id}
                              disabled={isBooked}
                              style={
                                isBooked
                                  ? { color: "#999", fontStyle: "italic" }
                                  : {}
                              }
                            >
                              {v.brand} {v.type} - {v.plateNumber}
                              {isBooked ? " 📅 (Sedang digunakan)" : ""}
                            </option>
                          );
                        })}
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
                        {drivers.map((d) => {
                          const isBooked =
                            availableDrivers &&
                            !availableDrivers.some(
                              (ad: Driver) => ad.id === d.id
                            );
                          return (
                            <option
                              key={d.id}
                              value={d.id}
                              disabled={isBooked}
                              style={
                                isBooked
                                  ? { color: "#999", fontStyle: "italic" }
                                  : {}
                              }
                            >
                              {d.driverName || `Driver ${d.id}`} -{" "}
                              {d.phoneNumber || ""}
                              {isBooked ? " 📅 (Sedang digunakan)" : ""}
                            </option>
                          );
                        })}
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

          {actionType === "reject" &&
            selectedRequest?.emergencyReason &&
            selectedRequest?.emergencyType === "LAINNYA" &&
            level === "l1" && (
              <div className="space-y-4 p-4 bg-red-50 rounded-lg border border-red-200">
                <div className="p-3 bg-white rounded border border-red-100">
                  <p className="text-xs font-semibold text-red-800 mb-1">
                    Assignment Saat Ini
                  </p>
                  <p className="text-sm text-gray-900">
                    <span className="font-semibold">Kendaraan:</span>{" "}
                    {selectedRequest.vehicleId
                      ? getVehicleName(selectedRequest.vehicleId)
                      : "-"}
                  </p>
                  <p className="text-sm text-gray-900">
                    <span className="font-semibold">Driver:</span>{" "}
                    {selectedRequest.driverName || "Driver"}
                  </p>
                  <p className="text-xs text-gray-500 mt-2">
                    Secara default assignment tetap dipertahankan.
                  </p>
                </div>

                <div className="space-y-2">
                  <label className="flex items-center gap-2 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={changeSelection}
                      onChange={(e) => setChangeSelection(e.target.checked)}
                      className="w-4 h-4 text-red-600 focus:ring-red-500 rounded"
                    />
                    <span className="text-sm text-gray-700">
                      Ganti kendaraan (opsional)
                    </span>
                  </label>
                  {changeSelection && (
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
                        defaultValue={""}
                      >
                        <option value="">-- Pilih Kendaraan --</option>
                        {vehicles && vehicles.length > 0 ? (
                          vehicles.map((v) => {
                            const isBooked =
                              availableVehicles &&
                              !availableVehicles.some(
                                (av: Vehicle) => av.id === v.id
                              );
                            return (
                              <option
                                key={v.id}
                                value={v.id}
                                disabled={isBooked}
                                style={
                                  isBooked
                                    ? { color: "#999", fontStyle: "italic" }
                                    : {}
                                }
                              >
                                {v.brand} {v.type} - {v.plateNumber}
                                {isBooked ? " 📅 (Sedang digunakan)" : ""}
                              </option>
                            );
                          })
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
                  )}
                </div>

                <div className="space-y-2">
                  <label className="flex items-center gap-2 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={changeDriver}
                      onChange={(e) => setChangeDriver(e.target.checked)}
                      className="w-4 h-4 text-red-600 focus:ring-red-500 rounded"
                    />
                    <span className="text-sm text-gray-700">
                      Ganti driver (opsional)
                    </span>
                  </label>
                  {changeDriver && (
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
                        defaultValue={""}
                      >
                        <option value="">-- Pilih Driver --</option>
                        {drivers && drivers.length > 0 ? (
                          drivers.map((d) => {
                            const isBooked =
                              availableDrivers &&
                              !availableDrivers.some(
                                (ad: Driver) => ad.id === d.id
                              );
                            return (
                              <option
                                key={d.id}
                                value={d.id}
                                disabled={isBooked}
                                style={
                                  isBooked
                                    ? { color: "#999", fontStyle: "italic" }
                                    : {}
                                }
                              >
                                {d.driverName || `Driver ${d.id}`} {" "}
                                {d.phoneNumber ? `- ${d.phoneNumber}` : ""}
                                {isBooked ? " 📅 (Sedang digunakan)" : ""}
                              </option>
                            );
                          })
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
                  )}
                </div>
              </div>
            )}

          {actionType && (
            <>
              <Textarea
                label={
                  actionType === "approve" || isOtherEmergencyL1Reject
                    ? "Catatan (opsional)"
                    : "Alasan Penolakan"
                }
                placeholder={
                  actionType === "approve" || isOtherEmergencyL1Reject
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
                  variant={
                    actionType === "approve" || isOtherEmergencyL1Reject
                      ? "secondary"
                      : "danger"
                  }
                  isLoading={
                    approveMutation.isPending || rejectMutation.isPending
                  }
                >
                  {actionType === "approve" || isOtherEmergencyL1Reject
                    ? "Setujui"
                    : "Tolak"}
                </Button>
              </div>
            </>
          )}
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
            <div className="grid grid-cols-1 gap-4">
              {/* ID dan Status - Sejajar */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-sm text-gray-500">ID Pengajuan</p>
                  <p className="font-medium">#{detailRequest.id}</p>
                </div>
                <div>
                  <p className="text-sm text-gray-500">Status</p>
                  <Badge status={detailRequest.status} />
                </div>
              </div>

              {/* Nama Pemohon - Full Width */}
              <div>
                <p className="text-sm text-gray-500">Pemohon</p>
                <p className="font-medium text-lg">
                  {detailRequest.requesterName}
                </p>
              </div>

              {/* Email dan Kontak - Sejajar */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-sm text-gray-500">Email Pemohon</p>
                  <a
                    href={`mailto:${detailRequest.requesterEmail}`}
                    className="font-medium text-blue-600 hover:underline"
                  >
                    {detailRequest.requesterEmail}
                  </a>
                </div>
                <div>
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
              </div>

              {/* Divisi dan Unit Kerja - Sejajar */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-sm text-gray-500">Divisi</p>
                  <p className="font-medium">
                    {detailRequest.requesterDivision || "-"}
                  </p>
                </div>
                <div>
                  <p className="text-sm text-gray-500">Unit Kerja</p>
                  <p className="font-medium">
                    {detailRequest.requesterUnitKerja || "-"}
                  </p>
                </div>
              </div>

              {/* Dasar Surat dan File - Sejajar */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-sm text-gray-500">
                    Dasar Surat Pelayanan (SPPD)
                  </p>
                  <p className="font-medium">
                    {detailRequest.serviceLetterBasis || "-"}
                  </p>
                </div>
                <div>
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
              </div>

              {/* Tujuan dan Destinasi - Sejajar */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <p className="text-sm text-gray-500">Tujuan</p>
                  <p className="font-medium">{detailRequest.purpose}</p>
                </div>
                <div>
                  <p className="text-sm text-gray-500">Destinasi</p>
                  <p className="font-medium">{detailRequest.destination}</p>
                </div>
              </div>

              {/* Daftar Tamu dan Hotel - Sejajar */}
              <div className="grid grid-cols-2 gap-4">
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
              </div>

              {/* Kendaraan dan Driver - Sejajar */}
              <div className="grid grid-cols-2 gap-4">
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
              </div>

              <div>
                <p className="text-sm text-gray-500">Waktu Mulai</p>
                <p className="font-medium">
                  {formatDateTime(detailRequest.startDatetime)}
                </p>
              </div>
              <div>
                <p className="text-sm text-gray-500">Waktu Selesai</p>
                <p className="font-medium">
                  {formatDateTime(detailRequest.endDatetime)}
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

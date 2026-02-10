import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatDate(date: string | Date): string {
  // If it's a Date object, use it directly
  if (date instanceof Date) {
    return date.toLocaleDateString("id-ID", {
      day: "numeric",
      month: "long",
      year: "numeric",
    });
  }

  // Parse string manually to avoid timezone conversion
  const dateStr = String(date);
  const [datePart] = dateStr.split("T");
  const [year, month, day] = datePart.split("-").map(Number);

  const months = [
    "Januari",
    "Februari",
    "Maret",
    "April",
    "Mei",
    "Juni",
    "Juli",
    "Agustus",
    "September",
    "Oktober",
    "November",
    "Desember",
  ];

  return `${day} ${months[month - 1]} ${year}`;
}

export function formatDateTime(date: string | Date): string {
  // If it's a Date object, use it directly
  if (date instanceof Date) {
    return date.toLocaleDateString("id-ID", {
      day: "numeric",
      month: "long",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  }

  // Parse string manually to avoid timezone conversion
  const dateStr = String(date);
  const [datePart, timePart] = dateStr.split("T");
  const [year, month, day] = datePart.split("-").map(Number);
  const [hour, minute] = timePart.split(":").map(Number);

  const months = [
    "Januari",
    "Februari",
    "Maret",
    "April",
    "Mei",
    "Juni",
    "Juli",
    "Agustus",
    "September",
    "Oktober",
    "November",
    "Desember",
  ];

  return `${day} ${months[month - 1]} ${year}, ${String(hour).padStart(
    2,
    "0"
  )}.${String(minute).padStart(2, "0")}`;
}

export function formatTime(date: string | Date): string {
  // If it's a Date object, use it directly
  if (date instanceof Date) {
    return date.toLocaleTimeString("id-ID", {
      hour: "2-digit",
      minute: "2-digit",
    });
  }

  // Parse string manually to avoid timezone conversion
  const dateStr = String(date);
  const timePart = dateStr.includes("T") ? dateStr.split("T")[1] : dateStr;
  const [hour, minute] = timePart.split(":").map(Number);

  return `${String(hour).padStart(2, "0")}.${String(minute).padStart(2, "0")}`;
}

export function getStatusColor(status: string): string {
  const colors: Record<string, string> = {
    // Loan Request Statuses
    SUBMITTED: "bg-blue-900 text-white",
    PENDING: "bg-yellow-100 text-yellow-800",
    PENDING_L1: "bg-yellow-100 text-yellow-800",
    PENDING_L2: "bg-orange-100 text-orange-800",
    APPROVED_L1: "bg-orange-100 text-orange-800",
    APPROVED: "bg-green-100 text-green-800",
    REJECTED: "bg-red-100 text-red-800",
    REJECTED_L1: "bg-red-100 text-red-800",
    REJECTED_L2: "bg-red-100 text-red-800",
    CANCELLED: "bg-gray-100 text-gray-800",

    // Schedule Statuses
    SCHEDULED: "bg-green-100 text-green-800",
    CONFIRMED: "bg-blue-100 text-blue-800",
    WAITING_DRIVER: "bg-yellow-100 text-yellow-800",
    DRIVER_CONFIRMED: "bg-green-100 text-green-800",
    IN_PROGRESS: "bg-purple-100 text-purple-800",
    COMPLETED: "bg-green-100 text-green-800",
    WAITING_RESOURCE: "bg-amber-100 text-amber-800",
    EMERGENCY: "bg-red-500 text-white",

    // Vehicle Statuses
    AVAILABLE: "bg-green-100 text-green-800",
    IN_USE: "bg-blue-100 text-blue-800",
    MAINTENANCE: "bg-orange-100 text-orange-800",
    RETIRED: "bg-gray-100 text-gray-800",

    // Driver Statuses
    ON_DUTY: "bg-blue-100 text-blue-800",
    OFF_DUTY: "bg-gray-100 text-gray-800",
    ON_LEAVE: "bg-amber-100 text-amber-800",
  };
  return colors[status] || "bg-gray-100 text-gray-800";
}

export function getStatusLabel(status: string): string {
  const labels: Record<string, string> = {
    // Loan Request Statuses
    SUBMITTED: "Submitted",
    PENDING: "Pending",
    PENDING_L1: "Menunggu Approval L1",
    PENDING_L2: "Menunggu Approval L2",
    APPROVED_L1: "APPROVED_L1",
    APPROVED: "Disetujui",
    REJECTED: "Ditolak",
    REJECTED_L1: "Ditolak",
    REJECTED_L2: "Ditolak",
    CANCELLED: "Dibatalkan",

    // Schedule Statuses
    SCHEDULED: "Scheduled",
    CONFIRMED: "Menunggu Konfirmasi Driver",
    WAITING_DRIVER: "Menunggu Konfirmasi Driver",
    DRIVER_CONFIRMED: "Terkonfirmasi",
    IN_PROGRESS: "Dalam Perjalanan",
    COMPLETED: "Selesai",
    WAITING_RESOURCE: "Menunggu Resource",
    EMERGENCY: "Emergency Perjalanan",

    // Vehicle Statuses
    AVAILABLE: "Tersedia",
    IN_USE: "Sedang Digunakan",
    MAINTENANCE: "Maintenance",
    RETIRED: "Tidak Aktif",

    // Driver Statuses
    ON_DUTY: "Bertugas",
    OFF_DUTY: "Tidak Bertugas",
    ON_LEAVE: "Cuti",
  };
  return labels[status] || status;
}

export function getApprovalLevel(level: number): string {
  const levels: Record<number, string> = {
    1: "Approval Level 1",
    2: "Approval Level 2",
  };
  return levels[level] || `Level ${level}`;
}

export function getRoleLabel(role: string): string {
  const labels: Record<string, string> = {
    PEMOHON: "Pemohon",
    PIC_APPROVAL_L1: "PIC Approval L1",
    PIC_APPROVAL_L2: "PIC Approval L2",
    DRIVER: "Driver",
    ADMIN: "Administrator",
  };
  return labels[role] || role;
}

export function truncateText(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text;
  return text.slice(0, maxLength) + "...";
}

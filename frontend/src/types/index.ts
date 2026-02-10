// User Types
export interface User {
  id: number;
  name: string;
  email: string;
  role: UserRole;
  division?: string;
  isActive: boolean;
  createdAt: string;
}

export type UserRole =
  | "PEMOHON"
  | "PIC_APPROVAL_L1"
  | "PIC_APPROVAL_L2"
  | "DRIVER"
  | "ADMIN";

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: User;
}

// Loan Request Types
export interface LoanRequest {
  id: number;
  userId: number;
  requestNumber: string;
  requesterName?: string;
  requesterEmail?: string;
  requesterPhone?: string;
  requesterDivision?: string;
  requesterUnitKerja?: string;
  serviceLetterBasis: string;
  serviceLetterFilePath?: string;
  purpose: string;
  destination: string;
  guestList: string;
  hotelAccommodation?: string;
  vehicleId: number;
  driverId: number;
  driverName?: string;
  driverPhone?: string;
  startDatetime: string;
  endDatetime: string;
  status: LoanRequestStatus;
  notes?: string;
  createdAt: string;
  updatedAt: string;
  user?: User;
  approvals?: Approval[];
  schedule?: Schedule;
}

export interface LoanRequestListItem {
  id: number;
  requestNumber: string;
  requesterName: string;
  serviceLetterBasis: string;
  purpose: string;
  destination: string;
  guestList: string;
  hotelAccommodation?: string;
  vehicleId?: number;
  driverId?: number;
  startDatetime: string;
  endDatetime: string;
  departureDate?: string;
  status: LoanRequestStatus;
  createdAt: string;
}

export interface CreateLoanRequest {
  serviceLetterBasis: string;
  purpose: string;
  destination: string;
  guestList: string;
  hotelAccommodation?: string;
  vehicleId: number;
  driverId: number;
  startDatetime: string;
  endDatetime: string;
  notes?: string;
}

export type LoanRequestStatus =
  | "SUBMITTED"
  | "PENDING"
  | "PENDING_L1"
  | "APPROVED_L1"
  | "REJECTED_L1"
  | "APPROVED_L2"
  | "REJECTED_L2"
  | "APPROVED"
  | "REJECTED"
  | "SCHEDULED"
  | "WAITING_RESOURCE"
  | "IN_PROGRESS"
  | "COMPLETED"
  | "CANCELLED";

// Approval Types
export interface Approval {
  id: number;
  loanRequestId: number;
  approverId: number;
  approvalLevel: number;
  status: ApprovalStatus;
  notes?: string;
  approvedAt: string;
  approverName?: string;
}

export interface PendingApproval {
  loanRequestId: number;
  requestNumber: string;
  requesterName: string;
  requesterEmail: string;
  requesterPhone?: string;
  requesterDivision: string;
  department?: string;
  requesterUnitKerja?: string;
  serviceLetterBasis?: string;
  serviceLetterFilePath?: string;
  purpose: string;
  destination: string;
  guestList: string;
  hotelAccommodation?: string;
  vehicleId: number;
  driverId: number;
  driverName?: string;
  driverPhone?: string;
  startDatetime: string;
  endDatetime: string;
  departureDate?: string;
  status: LoanRequestStatus;
  notes?: string;
  createdAt: string;
  requiredApprovalLevel: number;
  emergencyReason?: string;
  emergencyType?: "MOGOK" | "LAINNYA";
}

export interface ProcessApproval {
  loanRequestId: number;
  status: ApprovalStatus;
  notes?: string;
  vehicleId?: number;
  driverId?: number;
}

export type ApprovalStatus = "APPROVED" | "REJECTED";

// Schedule Types
export interface Schedule {
  id: number;
  loanRequestId: number;
  driverId: number;
  vehicleId: number;
  assignedBy?: number;
  assignedAt: string;
  actualStartTime?: string;
  actualEndTime?: string;
  status: ScheduleStatus;
  notes?: string;
  loanRequest?: LoanRequest;
  driver?: Driver;
  vehicle?: Vehicle;
  assignedByName?: string;
  vehiclePlateNumber?: string;
  vehicleModel?: string;
  driverName?: string;
  // Driver pre-departure and journey tracking
  actualVehicleId?: number;
  actualVehicle?: Vehicle;
  fuelCondition?: string;
  kmPhotoPath?: string;
  emergencyReason?: string;
  driverMessage?: string;
}

export interface DriverSchedule {
  id?: number;
  scheduleId: number;
  requestNumber: string;
  requesterName: string;
  requesterEmail: string;
  requesterPhone: string;
  purpose: string;
  destination: string;
  guestList: string;
  hotelAccommodation: boolean;
  hotelName?: string;
  startDatetime: string;
  endDatetime: string;
  departureDate?: string;
  vehicleId: number;
  vehiclePlate: string;
  vehicleBrand: string;
  vehicleModel: string;
  vehicleType: string;
  status: string;
  notes?: string;
  // Driver pre-departure and journey tracking
  actualVehicleId?: number;
  actualVehiclePlate?: string;
  fuelCondition?: string;
  actualStartTime?: string;
  actualEndTime?: string;
  emergencyReason?: string;
}

export interface AssignSchedule {
  loanRequestId: number;
  driverId: number;
  vehicleId: number;
  notes?: string;
}

export interface UpdateScheduleStatus {
  status: ScheduleStatus;
  actualStartTime?: string;
  actualEndTime?: string;
  notes?: string;
}

export type ScheduleStatus =
  | "ASSIGNED"
  | "CONFIRMED"
  | "PENDING"
  | "WAITING_DRIVER"
  | "DRIVER_CONFIRMED"
  | "IN_PROGRESS"
  | "COMPLETED"
  | "CANCELLED"
  | "EMERGENCY";

// Vehicle Types
export interface Vehicle {
  id: number;
  plateNumber: string;
  brand: string;
  type: string;
  model?: string;
  year?: number;
  capacity: number;
  status: VehicleStatus;
  notes?: string;
  isActive: boolean;
}

export interface CreateVehicle {
  plateNumber: string;
  brand: string;
  type: string;
  capacity: number;
  notes?: string;
}

export type VehicleStatus = "AVAILABLE" | "IN_USE" | "MAINTENANCE" | "RETIRED";

// Driver Types
export interface Driver {
  id: number;
  userId?: number;
  name?: string;
  driverName?: string;
  phoneNumber?: string;
  licenseNumber: string;
  licenseExpiry: string;
  status: DriverStatus;
  isActive: boolean;
  user?: { name: string };
}

export interface CreateDriver {
  userId?: number;
  licenseNumber: string;
  licenseExpiry: string;
  phoneNumber?: string;
}

export type DriverStatus = "AVAILABLE" | "ON_DUTY" | "OFF_DUTY" | "LEAVE";

// Dashboard Types
export interface DashboardStats {
  totalRequests: number;
  pendingApprovals: number;
  scheduledTrips: number;
  completedTrips: number;
  availableVehicles: number;
  availableDrivers: number;
  waitingResources: number;
  todaySchedules?: number;
  activeSchedules?: number;
}

export interface RecentActivity {
  id: number;
  type: string;
  description: string;
  status: string;
  timestamp: string;
  actorName: string;
}

export interface Dashboard {
  stats: DashboardStats;
  recentActivities: RecentActivity[];
  myRecentRequests: LoanRequestListItem[];
  upcomingSchedules: DriverSchedule[];
  todaySchedulesList?: DriverSchedule[];
}

// API Response Types
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

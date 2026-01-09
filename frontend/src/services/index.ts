import api from "./api";
import type {
  ApiResponse,
  LoginRequest,
  LoginResponse,
  User,
  LoanRequest,
  LoanRequestListItem,
  CreateLoanRequest,
  PendingApproval,
  ProcessApproval,
  Approval,
  Schedule,
  DriverSchedule,
  AssignSchedule,
  UpdateScheduleStatus,
  Vehicle,
  CreateVehicle,
  Driver,
  CreateDriver,
  Dashboard,
  DashboardStats,
} from "@/types";

// Auth Services
export const authService = {
  login: async (data: LoginRequest): Promise<ApiResponse<LoginResponse>> => {
    const response = await api.post<ApiResponse<LoginResponse>>(
      "/auth/login",
      data
    );
    return response.data;
  },

  getCurrentUser: async (): Promise<ApiResponse<User>> => {
    const response = await api.get<ApiResponse<User>>("/auth/me");
    return response.data;
  },

  validateToken: async (): Promise<ApiResponse<{ valid: boolean }>> => {
    const response = await api.get<ApiResponse<{ valid: boolean }>>(
      "/auth/validate"
    );
    return response.data;
  },
};

// Loan Request Services
export const loanRequestService = {
  getAll: async (
    status?: string
  ): Promise<ApiResponse<LoanRequestListItem[]>> => {
    const params = status ? { status } : {};
    const response = await api.get<ApiResponse<LoanRequestListItem[]>>(
      "/loanrequests",
      { params }
    );
    return response.data;
  },

  getMyRequests: async (): Promise<ApiResponse<LoanRequestListItem[]>> => {
    const response = await api.get<ApiResponse<LoanRequestListItem[]>>(
      "/loanrequests/my-requests"
    );
    return response.data;
  },

  getById: async (id: number): Promise<ApiResponse<LoanRequest>> => {
    const response = await api.get<ApiResponse<LoanRequest>>(
      `/loanrequests/${id}`
    );
    return response.data;
  },

  create: async (
    data: CreateLoanRequest
  ): Promise<ApiResponse<LoanRequest>> => {
    const response = await api.post<ApiResponse<LoanRequest>>(
      "/loanrequests",
      data
    );
    return response.data;
  },

  update: async (
    id: number,
    data: CreateLoanRequest
  ): Promise<ApiResponse<LoanRequest>> => {
    const response = await api.put<ApiResponse<LoanRequest>>(
      `/loanrequests/${id}`,
      data
    );
    return response.data;
  },

  cancel: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/loanrequests/${id}`);
    return response.data;
  },
};

// Approval Services
export const approvalService = {
  getPendingL1: async (): Promise<ApiResponse<PendingApproval[]>> => {
    const response = await api.get<ApiResponse<PendingApproval[]>>(
      "/approvals/pending/l1"
    );
    return response.data;
  },

  getPendingL2: async (): Promise<ApiResponse<PendingApproval[]>> => {
    const response = await api.get<ApiResponse<PendingApproval[]>>(
      "/approvals/pending/l2"
    );
    return response.data;
  },

  processL1: async (data: ProcessApproval): Promise<ApiResponse<Approval>> => {
    const response = await api.post<ApiResponse<Approval>>(
      "/approvals/process/l1",
      data
    );
    return response.data;
  },

  processL2: async (data: ProcessApproval): Promise<ApiResponse<Approval>> => {
    const response = await api.post<ApiResponse<Approval>>(
      "/approvals/process/l2",
      data
    );
    return response.data;
  },

  getHistory: async (
    loanRequestId: number
  ): Promise<ApiResponse<Approval[]>> => {
    const response = await api.get<ApiResponse<Approval[]>>(
      `/approvals/history/${loanRequestId}`
    );
    return response.data;
  },

  getPendingCount: async (): Promise<
    ApiResponse<{ level1: number; level2: number; total: number }>
  > => {
    const response = await api.get<
      ApiResponse<{ level1: number; level2: number; total: number }>
    >("/approvals/pending/count");
    return response.data;
  },
};

// Schedule Services
export const scheduleService = {
  getAll: async (status?: string): Promise<ApiResponse<Schedule[]>> => {
    const params = status ? { status } : {};
    const response = await api.get<ApiResponse<Schedule[]>>("/schedules", {
      params,
    });
    return response.data;
  },

  getMySchedules: async (): Promise<ApiResponse<DriverSchedule[]>> => {
    const response = await api.get<ApiResponse<DriverSchedule[]>>(
      "/schedules/my-schedules"
    );
    return response.data;
  },

  getUpcoming: async (): Promise<ApiResponse<DriverSchedule[]>> => {
    const response = await api.get<ApiResponse<DriverSchedule[]>>(
      "/schedules/upcoming"
    );
    return response.data;
  },

  getById: async (id: number): Promise<ApiResponse<Schedule>> => {
    const response = await api.get<ApiResponse<Schedule>>(`/schedules/${id}`);
    return response.data;
  },

  getByLoanRequestId: async (
    loanRequestId: number
  ): Promise<ApiResponse<Schedule>> => {
    const response = await api.get<ApiResponse<Schedule>>(
      `/schedules/by-loan-request/${loanRequestId}`
    );
    return response.data;
  },

  assign: async (data: AssignSchedule): Promise<ApiResponse<Schedule>> => {
    const response = await api.post<ApiResponse<Schedule>>(
      "/schedules/assign",
      data
    );
    return response.data;
  },

  updateStatus: async (
    id: number,
    data: UpdateScheduleStatus
  ): Promise<ApiResponse<void>> => {
    const response = await api.patch<ApiResponse<void>>(
      `/schedules/${id}/status`,
      data
    );
    return response.data;
  },

  getWaitingResources: async (): Promise<ApiResponse<Schedule[]>> => {
    const response = await api.get<ApiResponse<Schedule[]>>(
      "/schedules/waiting-resources"
    );
    return response.data;
  },

  retryScheduling: async (
    loanRequestId: number
  ): Promise<ApiResponse<void>> => {
    const response = await api.post<ApiResponse<void>>(
      `/schedules/retry/${loanRequestId}`
    );
    return response.data;
  },

  cancelSchedule: async (
    scheduleId: number,
    data: { cancellationReason: string }
  ): Promise<ApiResponse<void>> => {
    const response = await api.post<ApiResponse<void>>(
      `/schedules/${scheduleId}/cancel`,
      data
    );
    return response.data;
  },
};

// Vehicle Services
export const vehicleService = {
  getAll: async (status?: string): Promise<ApiResponse<Vehicle[]>> => {
    const params = status ? { status } : {};
    const response = await api.get<ApiResponse<Vehicle[]>>("/vehicles", {
      params,
    });
    return response.data;
  },

  getAvailable: async (
    startDate?: string,
    endDate?: string
  ): Promise<ApiResponse<Vehicle[]>> => {
    const params: any = {};
    if (startDate) params.startDate = startDate;
    if (endDate) params.endDate = endDate;

    const response = await api.get<ApiResponse<Vehicle[]>>(
      "/vehicles/available",
      { params }
    );
    return response.data;
  },

  getById: async (id: number): Promise<ApiResponse<Vehicle>> => {
    const response = await api.get<ApiResponse<Vehicle>>(`/vehicles/${id}`);
    return response.data;
  },

  create: async (data: CreateVehicle): Promise<ApiResponse<Vehicle>> => {
    const response = await api.post<ApiResponse<Vehicle>>("/vehicles", data);
    return response.data;
  },

  update: async (
    id: number,
    data: CreateVehicle
  ): Promise<ApiResponse<Vehicle>> => {
    const response = await api.put<ApiResponse<Vehicle>>(
      `/vehicles/${id}`,
      data
    );
    return response.data;
  },

  updateStatus: async (
    id: number,
    status: string
  ): Promise<ApiResponse<void>> => {
    const response = await api.patch<ApiResponse<void>>(
      `/vehicles/${id}/status`,
      { status }
    );
    return response.data;
  },

  delete: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/vehicles/${id}`);
    return response.data;
  },
};

// Driver Services
export const driverService = {
  getAll: async (status?: string): Promise<ApiResponse<Driver[]>> => {
    const params = status ? { status } : {};
    const response = await api.get<ApiResponse<Driver[]>>("/drivers", {
      params,
    });
    return response.data;
  },

  getAvailable: async (
    startDate?: string,
    endDate?: string
  ): Promise<ApiResponse<Driver[]>> => {
    const params: any = {};
    if (startDate) params.startDate = startDate;
    if (endDate) params.endDate = endDate;

    const response = await api.get<ApiResponse<Driver[]>>(
      "/drivers/available",
      { params }
    );
    return response.data;
  },

  getById: async (id: number): Promise<ApiResponse<Driver>> => {
    const response = await api.get<ApiResponse<Driver>>(`/drivers/${id}`);
    return response.data;
  },

  create: async (data: CreateDriver): Promise<ApiResponse<Driver>> => {
    const response = await api.post<ApiResponse<Driver>>("/drivers", data);
    return response.data;
  },

  update: async (
    id: number,
    data: CreateDriver
  ): Promise<ApiResponse<Driver>> => {
    const response = await api.put<ApiResponse<Driver>>(`/drivers/${id}`, data);
    return response.data;
  },

  updateStatus: async (
    id: number,
    status: string
  ): Promise<ApiResponse<void>> => {
    const response = await api.patch<ApiResponse<void>>(
      `/drivers/${id}/status`,
      { status }
    );
    return response.data;
  },

  delete: async (id: number): Promise<ApiResponse<void>> => {
    const response = await api.delete<ApiResponse<void>>(`/drivers/${id}`);
    return response.data;
  },
};

// Dashboard Services
export const dashboardService = {
  getDashboard: async (): Promise<ApiResponse<Dashboard>> => {
    const response = await api.get<ApiResponse<Dashboard>>("/dashboard");
    return response.data;
  },

  getStats: async (): Promise<ApiResponse<DashboardStats>> => {
    const response = await api.get<ApiResponse<DashboardStats>>(
      "/dashboard/stats"
    );
    return response.data;
  },
};

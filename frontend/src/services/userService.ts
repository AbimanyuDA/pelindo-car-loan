import api from "./api";

export interface User {
  id: number;
  fullName: string;
  email: string;
  role: string;
  division?: string;
  unitKerja?: string;
  phoneNumber?: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateUserRequest {
  fullName: string;
  email: string;
  password: string;
  role: string;
  division?: string;
  unitKerja?: string;
  phoneNumber?: string;
}

export interface UpdateUserRequest {
  fullName: string;
  email: string;
  role: string;
  division?: string;
  unitKerja?: string;
  phoneNumber?: string;
  isActive: boolean;
}

export interface BulkImportResult {
  totalRows: number;
  successCount: number;
  failedCount: number;
  errors: Array<{
    rowNumber: number;
    email: string;
    errorMessage: string;
  }>;
}

export const userService = {
  async getAll() {
    return api.get<User[]>("/users");
  },

  async getById(id: number) {
    return api.get<User>(`/users/${id}`);
  },

  async create(data: CreateUserRequest) {
    return api.post<number>("/users", data);
  },

  async update(id: number, data: UpdateUserRequest) {
    return api.put(`/users/${id}`, data);
  },

  async delete(id: number) {
    return api.delete(`/users/${id}`);
  },

  async importFromExcel(file: File) {
    const formData = new FormData();
    formData.append("file", file);
    return api.post<BulkImportResult>("/users/import", formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    });
  },

  async downloadTemplate() {
    const response = await api.get("/users/template", {
      responseType: "blob",
    });

    // Create download link
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", "template_import_users.xlsx");
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);

    return response;
  },
};

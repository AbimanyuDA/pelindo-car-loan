import axios from "axios";
import { useAuthStore } from "@/store/authStore";
import toast from "react-hot-toast";

const API_BASE_URL = (import.meta as any).env?.VITE_API_URL || "/api";

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    // Don't set Content-Type globally - let axios/browser auto-detect
    // When FormData is sent, browser sets multipart/form-data
    // When JSON is sent, we set it explicitly in the request
  },
});

// Request interceptor to add auth token and Content-Type
api.interceptors.request.use(
  (config) => {
    const token = useAuthStore.getState().token;
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    
    // Set Content-Type for non-FormData requests
    if (config.data && !(config.data instanceof FormData)) {
      config.headers["Content-Type"] = "application/json";
    }
    
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // const message =
    //   error.response?.data?.message || error.message || "An error occurred";

    if (error.response?.status === 401) {
      useAuthStore.getState().logout();
      toast.error("Session expired. Please login again.");
      window.location.href = "/login";
    } else if (error.response?.status === 403) {
      toast.error("You do not have permission to perform this action");
    } else if (error.response?.status >= 500) {
      toast.error("Server error. Please try again later.");
    }

    return Promise.reject(error);
  }
);

export default api;

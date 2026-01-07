import { Routes, Route, Navigate } from "react-router-dom";

// Layouts
import MainLayout from "@/layouts/MainLayout";
import AuthLayout from "@/layouts/AuthLayout";

// Pages
import {
  LoginPage,
  DashboardPage,
  LoanRequestsPage,
  LoanRequestFormPage,
  LoanRequestDetailPage,
  ApprovalL1Page,
  ApprovalL2Page,
  DriverSchedulePage,
  AdminSchedulePage,
  VehiclesPage,
  DriversPage,
} from "@/pages";

// Components
import ProtectedRoute from "@/components/ProtectedRoute";

function App() {
  return (
    <Routes>
      {/* Auth Routes */}
      <Route element={<AuthLayout />}>
        <Route path="/login" element={<LoginPage />} />
      </Route>

      {/* Protected Routes */}
      <Route
        element={
          <ProtectedRoute>
            <MainLayout />
          </ProtectedRoute>
        }
      >
        <Route path="/dashboard" element={<DashboardPage />} />

        {/* Pemohon Routes */}
        <Route path="/loan-requests" element={<LoanRequestsPage />} />
        <Route path="/loan-requests/new" element={<LoanRequestFormPage />} />
        <Route path="/loan-requests/:id" element={<LoanRequestDetailPage />} />

        {/* Approval Routes */}
        <Route path="/approvals/l1" element={<ApprovalL1Page />} />
        <Route path="/approvals/l2" element={<ApprovalL2Page />} />

        {/* Driver Routes */}
        <Route path="/driver/schedules" element={<DriverSchedulePage />} />

        {/* Admin Routes */}
        <Route path="/admin/schedules" element={<AdminSchedulePage />} />
        <Route path="/admin/vehicles" element={<VehiclesPage />} />
        <Route path="/admin/drivers" element={<DriversPage />} />
      </Route>

      {/* Default redirect */}
      <Route path="/" element={<Navigate to="/dashboard" replace />} />
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}

export default App;

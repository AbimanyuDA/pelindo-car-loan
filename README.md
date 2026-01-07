# Pelindo Car Loan System

Sistem Peminjaman Kendaraan Operasional untuk PT Pelabuhan Indonesia (Persero).

## 📋 Overview

Aplikasi web enterprise untuk mengelola peminjaman kendaraan operasional dengan fitur:

- Pengajuan peminjaman kendaraan oleh karyawan
- Proses persetujuan dua level (L1 & L2)
- Penjadwalan otomatis kendaraan dan driver
- Dashboard role-based untuk setiap pengguna
- Manajemen kendaraan dan driver (Admin)

## 🏗️ Architecture

```
pelindo-car-loan/
├── backend/                    # ASP.NET Core Web API
│   └── PelindoCarLoan.API/
│       ├── Controllers/        # API Endpoints
│       ├── Services/           # Business Logic
│       ├── Repositories/       # Data Access Layer
│       ├── Models/             # Entity Models
│       ├── DTOs/               # Data Transfer Objects
│       ├── Extensions/         # Service Extensions
│       ├── Middleware/         # Custom Middleware
│       └── Validators/         # Request Validators
├── frontend/                   # React + TypeScript + Vite
│   └── src/
│       ├── components/         # Reusable UI Components
│       ├── layouts/            # Page Layouts
│       ├── pages/              # Page Components
│       ├── services/           # API Service Layer
│       ├── store/              # Zustand State Management
│       ├── types/              # TypeScript Interfaces
│       └── lib/                # Utility Functions
└── database/                   # Oracle DDL Scripts
```

## 👥 User Roles

| Role              | Description      | Capabilities                                |
| ----------------- | ---------------- | ------------------------------------------- |
| `PEMOHON`         | Requester        | Submit loan requests, track status          |
| `PIC_APPROVAL_L1` | Level 1 Approver | Review & approve/reject L1                  |
| `PIC_APPROVAL_L2` | Level 2 Approver | Final approval, triggers scheduling         |
| `DRIVER`          | Driver           | View assigned schedules, update trip status |
| `ADMIN`           | Administrator    | Manage vehicles, drivers, manual scheduling |

## 🔄 Business Flow

```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│  PEMOHON │────▶│ PENDING  │────▶│PENDING_L1│────▶│PENDING_L2│
│  Submit  │     │          │     │          │     │          │
└──────────┘     └──────────┘     └──────────┘     └──────────┘
                                        │                │
                                   ┌────▼────┐     ┌────▼────┐
                                   │REJECTED │     │APPROVED │
                                   └─────────┘     └────┬────┘
                                                        │
                                              ┌─────────▼─────────┐
                                              │ Auto-Scheduling   │
                                              │ (Vehicle + Driver)│
                                              └─────────┬─────────┘
                                                        │
                                        ┌───────────────┼───────────────┐
                                        ▼               ▼               ▼
                                   SCHEDULED    WAITING_RESOURCE   IN_PROGRESS
                                        │                               │
                                        └──────────────┬────────────────┘
                                                       ▼
                                                  COMPLETED
```

## 🛠️ Tech Stack

### Backend

- **Framework**: ASP.NET Core 8.0
- **ORM**: Dapper (Micro-ORM)
- **Database**: Oracle Database
- **Authentication**: JWT Bearer Token
- **Validation**: FluentValidation
- **Logging**: Serilog
- **Documentation**: Swagger/OpenAPI

### Frontend

- **Framework**: React 18 + TypeScript
- **Build Tool**: Vite
- **Styling**: Tailwind CSS (Maritime Theme)
- **State Management**: Zustand
- **Data Fetching**: TanStack React Query
- **Forms**: React Hook Form + Zod
- **Icons**: Lucide React
- **HTTP Client**: Axios

## 🎨 Design Theme

Maritime/Port theme with colors:

- **Primary (Navy Blue)**: `#1e3a8a` - Main navigation, headers
- **Secondary (Teal)**: `#14b8a6` - Actions, highlights
- **Accent (Light Blue)**: `#38bdf8` - Links, interactive elements

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK
- Node.js 18+ & npm/pnpm
- Oracle Database (or Oracle XE for development)

### Database Setup

1. Connect to Oracle Database
2. Run DDL script:

```bash
sqlplus username/password@database @database/01_create_tables.sql
```

3. Insert seed data:

```bash
sqlplus username/password@database @database/02_seed_data.sql
```

### Backend Setup

```bash
cd backend/PelindoCarLoan.API

# Update connection string in appsettings.json
# Run the application
dotnet run
```

API will be available at: `https://localhost:7001`
Swagger UI: `https://localhost:7001/swagger`

### Frontend Setup

```bash
cd frontend

# Install dependencies
npm install

# Start development server
npm run dev
```

Frontend will be available at: `http://localhost:5173`

## 📡 API Endpoints

### Authentication

| Method | Endpoint             | Description        |
| ------ | -------------------- | ------------------ |
| POST   | `/api/auth/login`    | User login         |
| GET    | `/api/auth/me`       | Get current user   |
| GET    | `/api/auth/validate` | Validate JWT token |

### Loan Requests

| Method | Endpoint                        | Description        |
| ------ | ------------------------------- | ------------------ |
| GET    | `/api/loanrequests`             | Get all requests   |
| GET    | `/api/loanrequests/my-requests` | Get my requests    |
| GET    | `/api/loanrequests/{id}`        | Get request by ID  |
| POST   | `/api/loanrequests`             | Create new request |
| PUT    | `/api/loanrequests/{id}`        | Update request     |
| DELETE | `/api/loanrequests/{id}`        | Cancel request     |

### Approvals

| Method | Endpoint                                 | Description              |
| ------ | ---------------------------------------- | ------------------------ |
| GET    | `/api/approvals/pending/l1`              | Get pending L1 approvals |
| GET    | `/api/approvals/pending/l2`              | Get pending L2 approvals |
| POST   | `/api/approvals/process/l1`              | Process L1 approval      |
| POST   | `/api/approvals/process/l2`              | Process L2 approval      |
| GET    | `/api/approvals/history/{loanRequestId}` | Get approval history     |

### Schedules

| Method | Endpoint                      | Description              |
| ------ | ----------------------------- | ------------------------ |
| GET    | `/api/schedules`              | Get all schedules        |
| GET    | `/api/schedules/my-schedules` | Get driver's schedules   |
| GET    | `/api/schedules/upcoming`     | Get upcoming schedules   |
| POST   | `/api/schedules/assign`       | Manually assign schedule |
| PATCH  | `/api/schedules/{id}/status`  | Update schedule status   |

### Vehicles & Drivers

| Method | Endpoint                  | Description            |
| ------ | ------------------------- | ---------------------- |
| GET    | `/api/vehicles`           | Get all vehicles       |
| GET    | `/api/vehicles/available` | Get available vehicles |
| POST   | `/api/vehicles`           | Create vehicle         |
| PUT    | `/api/vehicles/{id}`      | Update vehicle         |
| DELETE | `/api/vehicles/{id}`      | Delete vehicle         |
| GET    | `/api/drivers`            | Get all drivers        |
| GET    | `/api/drivers/available`  | Get available drivers  |
| POST   | `/api/drivers`            | Create driver          |
| PUT    | `/api/drivers/{id}`       | Update driver          |
| DELETE | `/api/drivers/{id}`       | Delete driver          |

### Dashboard

| Method | Endpoint               | Description        |
| ------ | ---------------------- | ------------------ |
| GET    | `/api/dashboard`       | Get dashboard data |
| GET    | `/api/dashboard/stats` | Get statistics     |

## 🔐 Demo Credentials

| Role    | Username | Password    |
| ------- | -------- | ----------- |
| Pemohon | pemohon1 | password123 |
| PIC L1  | pic_l1   | password123 |
| PIC L2  | pic_l2   | password123 |
| Driver  | driver1  | password123 |
| Admin   | admin    | password123 |

## 📂 Project Structure Details

### Backend Controllers

- `AuthController` - Authentication endpoints
- `LoanRequestsController` - CRUD for loan requests
- `ApprovalsController` - Approval workflow
- `SchedulesController` - Schedule management
- `ResourcesController` - Vehicles & Drivers management
- `DashboardController` - Dashboard statistics

### Frontend Pages

- `LoginPage` - User authentication
- `DashboardPage` - Role-based dashboard
- `LoanRequestsPage` - List of requests (Pemohon)
- `LoanRequestFormPage` - Create/edit request form
- `LoanRequestDetailPage` - Request details view
- `ApprovalPage` (L1/L2) - Approval workflow
- `DriverSchedulePage` - Driver's schedule view
- `AdminSchedulePage` - Admin schedule management
- `VehiclesPage` - Vehicle CRUD
- `DriversPage` - Driver CRUD

## 🧪 Development Notes

### Auto-Scheduling Logic

When a request is approved at L2:

1. System checks for available vehicles matching capacity
2. System checks for available drivers
3. If both available: Schedule created with `SCHEDULED` status
4. If either unavailable: Schedule created with `WAITING_RESOURCE` status
5. Admin can manually assign or retry scheduling

### Status Transitions

**Loan Request**:
`PENDING` → `PENDING_L1` → `PENDING_L2` → `APPROVED` / `REJECTED`

**Schedule**:
`SCHEDULED` → `IN_PROGRESS` → `COMPLETED`
`WAITING_RESOURCE` → `SCHEDULED` (after manual assignment)

## 📄 License

Proprietary - PT Pelabuhan Indonesia (Persero)

## 👨‍💻 Author

Enterprise Application Development Team

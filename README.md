# Pelindo Car Loan System

Sistem manajemen peminjaman kendaraan untuk PT. Pelindo dengan approval workflow 2-level, scheduling, dan notifikasi email otomatis.

## Daftar Isi

- [Gambaran Umum](#gambaran-umum)
- [Teknologi yang Digunakan](#teknologi-yang-digunakan)
- [Persyaratan Sistem](#persyaratan-sistem)
- [Instalasi](#instalasi)
- [Konfigurasi](#konfigurasi)
- [Menjalankan Aplikasi](#menjalankan-aplikasi)
- [Struktur Project](#struktur-project)
- [Fitur Utama](#fitur-utama)
- [Akun Default](#akun-default)
- [Troubleshooting](#troubleshooting)

---

## Gambaran Umum

Aplikasi web untuk mengelola permohonan peminjaman kendaraan dengan fitur:

- **Permohonan Peminjaman**: User dapat mengajukan permohonan dengan detail lengkap
- **Approval Workflow**: 2-level approval (L1 Manager, L2 Director) dengan vehicle/driver assignment
- **Scheduling**: Automatic vehicle dan driver assignment berdasarkan ketersediaan
- **Email Notifications**: Notifikasi otomatis ke semua stakeholder dengan WhatsApp integration
- **Dashboard**: Real-time status monitoring untuk approvers dan requesters
- **Report**: History dan tracking semua permohonan

---

## Teknologi yang Digunakan

### Backend

- **Framework**: ASP.NET Core 8.0
- **Database**: Oracle Database 21c (XE version compatible)
- **ORM**: Dapper (Micro ORM)
- **Authentication**: JWT (JSON Web Tokens)
- **Validation**: FluentValidation + DataAnnotations
- **Logging**: Serilog
- **Email**: System.Net.Mail (SMTP)

### Frontend

- **Framework**: React 18.2 with TypeScript
- **Build Tool**: Vite 5.0
- **State Management**: Zustand
- **HTTP Client**: Axios + TanStack Query (React Query) v5
- **Form Management**: React Hook Form + Zod
- **Styling**: Tailwind CSS 3.4
- **UI Components**: Custom + Lucide Icons
- **Notifications**: React Hot Toast

---

## Persyaratan Sistem

### Minimum Requirements

- **OS**: Windows 10/11 atau macOS/Linux
- **RAM**: 4 GB (recommended 8 GB)
- **Storage**: 5 GB free space

### Software yang Harus Diinstall

1. **Node.js** v18.0 atau lebih tinggi

   - Download: https://nodejs.org
   - Verify: `node --version` dan `npm --version`

2. **.NET 8 SDK**

   - Download: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify: `dotnet --version`

3. **Oracle Database 19c** (atau XE Edition)

   - Download: https://www.oracle.com/database/technologies/xe-downloads.html
   - Atau gunakan Oracle instance yang sudah ada
   - Default: localhost:1521, SID: XE

4. **IDE Recommendations**:
   - **Frontend**: Visual Studio Code
   - **Backend**: Visual Studio 2022 Community atau Rider
   - **Database**: SQL Developer atau DBeaver

---

## Instalasi

### Step 1: Clone Repository

```bash
git clone <repository-url>
cd pelindo-car-loan
```

### Step 2: Setup Database

#### 2.1 Pastikan Oracle Database Berjalan

```bash
# Windows - Check service
sc query OracleServiceXE

# Linux/Mac - Check process
ps aux | grep oracle
```

#### 2.2 Jalankan Database Script

**Option A: Menggunakan SQLPlus (GUI)**

```bash
# Buka Oracle SQL Developer
# 1. Create new connection dengan:
#    - Name: Pelindo Development
#    - Username: system
#    - Password: (password)
#    - Hostname: localhost
#    - Port: 1521
#    - SID: XE
#
# 2. Open file: database/CreateDatabase.sql
# 3. Run script (Ctrl+Enter atau klik Run)
```

**Option B: Menggunakan Command Line**

```bash
# Windows
sqlplus system/password@XE @database\CreateDatabase.sql

# Linux/Mac
sqlplus system/password@XE @database/CreateDatabase.sql
```

#### 2.3 Verify Database Setup

```bash
# Connect to database
sqlplus system/password@XE

# Check tables created
SQL> SELECT table_name FROM user_tables;
```

Expected tables: USERS, DRIVERS, VEHICLES, LOAN_REQUESTS, APPROVALS, SCHEDULES, etc.

### Step 3: Setup Backend

```bash
# Navigate to backend
cd backend/PelindoCarLoan.API

# Restore NuGet packages
dotnet restore

# Build project
dotnet build

# Verify build success (should show "Build succeeded")
```

### Step 4: Setup Frontend

```bash
# From root directory, navigate to frontend
cd frontend

# Install npm dependencies
npm install

# Verify installation
npm list (should show all dependencies)
```

---

## ⚙️ Konfigurasi

### Backend Configuration

Edit file: `backend/PelindoCarLoan.API/appsettings.json`

```json
{
  "ConnectionStrings": {
    "OracleConnection": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)));User Id=system;Password=password"
  },
  "JwtSettings": {
    "SecretKey": "PelindoCarLoanSystemSecretKey2026VerySecure!@#$%",
    "Issuer": "PelindoCarLoan.API",
    "Audience": "PelindoCarLoan.Client",
    "ExpirationMinutes": 480
  },
  "CorsSettings": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"]
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "Pelindo Car Loan System"
  }
}
```

#### Penting untuk Email Configuration:

1. **Gunakan Gmail** dengan App Password (bukan password biasa)
2. **Setup Gmail App Password**:
   - Enable 2-Factor Authentication di Gmail
   - Go to https://myaccount.google.com/apppasswords
   - Create app password untuk "Mail" dan "Windows Computer"
   - Copy generated password ke `SmtpPassword`

### Database Configuration

Jika menggunakan database instance yang berbeda:

1. Update `ConnectionStrings.OracleConnection` di appsettings.json
2. Format: `Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=<HOST>)(PORT=<PORT>))(CONNECT_DATA=(SERVICE_NAME=<SID>)));User Id=<USERNAME>;Password=<PASSWORD>`

---

## Menjalankan Aplikasi

### Development Mode (Recommended)

#### Terminal 1: Backend

```bash
cd backend/PelindoCarLoan.API
dotnet run

# Output:
# [HH:MM:SS INF] Pelindo Car Loan API started successfully
# [HH:MM:SS INF] Now listening on: http://localhost:5000
# [HH:MM:SS INF] Application started. Press Ctrl+C to shut down.
```

✅ Backend berjalan di: **http://localhost:5000**

#### Terminal 2: Frontend

```bash
cd frontend
npm run dev

# Output:
# VITE v5.0.8  ready in XXX ms
#
# ➜  Local:   http://localhost:3000/
# ➜  press h to show help
```

✅ Frontend berjalan di: **http://localhost:3000**

#### Terminal 3: Optional - Watch Database Logs

```bash
cd backend/PelindoCarLoan.API
tail -f logs/pelindo-car-loan-*.log  # Linux/Mac
Get-Content logs/pelindo-car-loan-*.log -Wait  # PowerShell Windows
```

### Production Mode

#### Build Backend

```bash
cd backend/PelindoCarLoan.API
dotnet build -c Release
dotnet publish -c Release -o ./publish
cd publish
dotnet PelindoCarLoan.API.dll
```

#### Build Frontend

```bash
cd frontend
npm run build

# Output created in: frontend/dist/
# Deploy dist/ folder to your web server
```

---

## Fitur Utama

### 1. Authentication & Authorization

- Login dengan Email & Password
- JWT Token-based authentication
- Role-based access control (Requester, L1 Approver, L2 Approver, Admin)
- Automatic token refresh
- Logout dengan token invalidation

### 2. Loan Request Management

- Create loan request dengan detail lengkap:
  - Destination & Purpose
  - Guest list
  - Hotel accommodation
  - Service letter basis & file upload
  - Start & end datetime
- View request history
- Track status real-time
- Download uploaded documents

### 3. Approval Workflow (2-Level)

- **Level 1 (Manager)**:
  - Review loan requests
  - Assign vehicle & driver
  - Approve/Reject dengan notes
  - Automatic notification ke L2 approver
- **Level 2 (Director)**:
  - Final approval/rejection
  - Automatic scheduling creation
  - Email sent to requester & driver

### 4. Vehicle & Driver Management

- Add/Edit/Delete vehicles
- Add/Edit/Delete drivers
- Track vehicle availability
- License expiry monitoring
- Driver rating system

### 5. Scheduling

- Automatic schedule creation saat L2 approval
- Conflict detection (vehicle/driver busy)
- Status tracking: SCHEDULED, IN_PROGRESS, COMPLETED, CANCELLED

### 6. Email Notifications

- Loan request submitted notification to L1 approver
- L2 approval notification with vehicle/driver details
- Requester approval notification with WhatsApp contact
- Driver assignment notification with trip details
- Request rejection notifications
- WhatsApp integration buttons untuk direct communication

### 7. Dashboard

- Statistics: Total requests, pending approvals, completed trips
- Recent requests list
- Approval queue
- Vehicle utilization
- Driver assignments

---

## Akun Default

Database seeded dengan default users. Login di: `http://localhost:3000/login`

| Email                   | Password    | Role        | Division       |
| ----------------------- | ----------- | ----------- | -------------- |
| requester1@pelindo.com  | password123 | Requester   | Operations     |
| approver.l1@pelindo.com | password123 | L1 Approver | Management     |
| approver.l2@pelindo.com | password123 | L2 Approver | Director       |
| admin@pelindo.com       | password123 | Admin       | IT             |
| driver1@pelindo.com     | password123 | Driver      | Transportation |

---

## API Endpoints

### Authentication

```
POST   /api/auth/login              # Login
POST   /api/auth/logout             # Logout
POST   /api/auth/refresh-token      # Refresh JWT token
GET    /api/auth/profile            # Get current user
```

### Loan Requests

```
GET    /api/loan-requests           # Get all requests
GET    /api/loan-requests/{id}      # Get request detail
POST   /api/loan-requests           # Create new request
PUT    /api/loan-requests/{id}      # Update request
GET    /api/loan-requests/{id}/history  # Get request history
```

### Approvals

```
GET    /api/approvals/pending       # Get pending approvals
POST   /api/approvals/{id}/approve  # Approve request (L1/L2)
POST   /api/approvals/{id}/reject   # Reject request
GET    /api/approvals/{id}          # Get approval detail
```

### Dashboard

```
GET    /api/dashboard/statistics    # Dashboard stats
GET    /api/dashboard/pending       # Pending items
GET    /api/dashboard/recent        # Recent activities
```

### Resources

```
GET    /api/resources/vehicles      # Get all vehicles
GET    /api/resources/drivers       # Get all drivers
GET    /api/resources/available-vehicles  # Available vehicles
GET    /api/resources/available-drivers   # Available drivers
```

Full Swagger/OpenAPI docs tersedia di: `http://localhost:5000/swagger`

---

## Troubleshooting

### Issue 1: Database Connection Error

```
Error: ORA-12514: TNS:listener does not currently know of service requested in connect descriptor
```

**Solution**:

- Pastikan Oracle service berjalan: `sc query OracleServiceXE`
- Check connection string di appsettings.json
- Verify username/password: `sqlplus system/password@XE`

### Issue 2: Port Already in Use

```
System.Net.Sockets.SocketException: Only one usage of each socket address (protocol/IP port) is normally permitted
```

**Solution**:

```bash
# Windows - Kill process using port 5000
Get-Process -Id (Get-NetTCPConnection -LocalPort 5000).OwningProcess | Stop-Process

# Linux/Mac - Kill process using port 5000
lsof -ti:5000 | xargs kill -9

# Or change port in Program.cs or launchSettings.json
```

### Issue 3: Frontend Cannot Connect to Backend

```
Error: Network Error / Failed to fetch API
```

**Solution**:

- Ensure backend running on http://localhost:5000
- Check CORS settings in appsettings.json
- Clear browser cache (Ctrl+Shift+Delete)
- Check browser console (F12) for detailed error

### Issue 4: Email Not Sending

```
Error: SmtpException: The SMTP server requires a secure connection
```

**Solution**:

- Verify Gmail App Password (not regular password)
- Enable 2FA di Google Account
- Check firewall not blocking SMTP port 587
- Test SMTP credentials separately

### Issue 5: Build Failed with NuGet Error

```
Error: Unable to restore packages
```

**Solution**:

```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# Force rebuild
dotnet clean && dotnet build
```

### Issue 6: Node_modules Size Too Large

```
npm install takes too long / Storage full
```

**Solution**:

```bash
# Use npm ci instead (cleaner install)
npm ci

# Or shallow clone dependencies
npm install --legacy-peer-deps

# Check disk space
df -h  # Linux/Mac
Get-Volume  # PowerShell Windows
```

---

## Logging

### Backend Logs

- Location: `backend/PelindoCarLoan.API/logs/`
- Format: `pelindo-car-loan-YYYY-MM-DD.log`
- Retention: Daily rolling logs

View logs:

```bash
# Linux/Mac
tail -f logs/pelindo-car-loan-*.log

# PowerShell Windows
Get-Content logs/pelindo-car-loan-*.log -Wait -Tail 50
```

### Frontend Logs

Browser console (F12) melihat React warnings dan errors

---

## Security Notes

- **JWT Secret**: Change `JwtSettings.SecretKey` di production
- **Database Password**: Change default password `password` di production
- **Email Password**: Gunakan Google App Password, jangan raw password
- **CORS**: Update `AllowedOrigins` sesuai deployment domain
- **HTTPS**: Enable HTTPS certificate di production

---

## Additional Resources

- **ASP.NET Core Docs**: https://docs.microsoft.com/aspnet/core
- **React Docs**: https://react.dev
- **Oracle Database**: https://docs.oracle.com/en/database
- **Dapper Documentation**: https://github.com/DapperLib/Dapper
- **JWT.io**: https://jwt.io
- **Tailwind CSS**: https://tailwindcss.com/docs

---

## Support

Untuk bantuan dan pertanyaan:

- Email: abimanyudans@gmail.com
- Internal Wiki: https://abimanyudans.vercel.app/
- Chat: Slack #car-loan-system

---

## 📄 License

Internal Use Only - PT. Pelindo Regional III

---

## 👥 Contributors

- Abimanyu Danendra A (Frontend & Backend)

---

## 🎉 Getting Started Checklist

```bash
# 1. Clone repo
git clone <url>
cd pelindo-car-loan

# 2. Setup database (5 minutes)
# - Run CreateDatabase.sql via SQL Developer

# 3. Backend setup (2 minutes)
cd backend/PelindoCarLoan.API
dotnet restore
dotnet build

# 4. Frontend setup (3 minutes)
cd ../../frontend
npm install

# 5. Run backend
cd ../backend/PelindoCarLoan.API
dotnet run
# Backend ready at http://localhost:5000

# 6. Run frontend (new terminal)
cd frontend
npm run dev
# Frontend ready at http://localhost:3000

# 7. Login
# Use default credentials or create new account
```

**Total Setup Time: ~15-20 minutes**

---

_Last Updated: January 13, 2026_
_Version: 1.0.0_

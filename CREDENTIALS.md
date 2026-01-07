# Kredensial dan Informasi Akses Sistem

## 🌐 URL Akses

- **Frontend (React)**: http://localhost:3000
- **Backend API**: http://localhost:5000
- **Swagger Documentation**: http://localhost:5000/swagger

## 👥 User Credentials

Semua user menggunakan password yang sama: **`Password123!`**

### Admin
- **Email**: admin@pelindo.co.id
- **Role**: ADMIN
- **Nama**: Administrator
- **Divisi**: IT

### Pemohon (Requesters)
1. **Email**: pemohon1@pelindo.co.id
   - **Nama**: Budi Santoso
   - **Divisi**: Finance

2. **Email**: pemohon2@pelindo.co.id
   - **Nama**: Siti Rahayu
   - **Divisi**: Operations

### PIC Approval L1 (First Level Approvers)
1. **Email**: approver.l1.1@pelindo.co.id
   - **Nama**: Agus Wijaya
   - **Divisi**: Finance

2. **Email**: approver.l1.2@pelindo.co.id
   - **Nama**: Dewi Kusuma
   - **Divisi**: Operations

### PIC Approval L2 (Second Level Approver)
- **Email**: approver.l2@pelindo.co.id
- **Nama**: Ahmad Rahman
- **Divisi**: Management

### Driver
1. **Email**: driver1@pelindo.co.id
   - **Nama**: Joko Susilo
   - **SIM**: SIM-A-12345678

2. **Email**: driver2@pelindo.co.id
   - **Nama**: Andi Pratama
   - **SIM**: SIM-A-87654321

3. **Email**: driver3@pelindo.co.id
   - **Nama**: Bambang Surya
   - **SIM**: SIM-A-11223344

## 🗄️ Database (Oracle)

- **Host**: localhost
- **Port**: 1521
- **Service Name**: XE
- **Username**: system
- **Password**: bima2005
- **Connection String**: 
  ```
  Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)));User Id=system;Password=bima2005;
  ```

### Koneksi via SQL*Plus
```bash
sqlplus system/bima2005@localhost:1521/XE
```

## 🚀 Cara Menjalankan Aplikasi

### Backend (ASP.NET Core)
```powershell
cd backend\PelindoCarLoan.API
dotnet run
```

### Frontend (React + Vite)
```powershell
cd frontend
npm run dev
```

## 📊 Data yang Tersedia

### Vehicles (6 kendaraan)
- B 1234 ABC - Toyota Camry (Sedan)
- B 5678 DEF - Honda CR-V (SUV)
- B 9012 GHI - Toyota Avanza (MPV)
- B 3456 JKL - Isuzu Elf (Minibus)
- B 7890 MNO - Honda Accord (Sedan) - IN_USE
- B 2345 PQR - Mitsubishi Pajero Sport (SUV) - MAINTENANCE

### Drivers (3 driver)
- Joko Susilo (Available)
- Andi Pratama (Available)
- Bambang Surya (On Duty)

### Sample Loan Requests (3 request)
1. Meeting with port authority - SUBMITTED
2. Attending maritime conference - SUBMITTED
3. Business trip pickup - APPROVED_L1 (dengan schedule)

## 🔐 JWT Settings

- **Secret Key**: PelindoCarLoanSystemSecretKey2026VerySecure!@#$%
- **Issuer**: PelindoCarLoan.API
- **Audience**: PelindoCarLoan.Client
- **Expiration**: 480 minutes (8 hours)

## 🎨 Theme Colors

- **Primary**: Navy Blue (#1d4ed8)
- **Secondary**: Teal (#14b8a6)
- **Accent**: Orange (#f97316)
- **Success**: Green (#10b981)
- **Warning**: Yellow (#f59e0b)
- **Error**: Red (#ef4444)

## 📝 Catatan Penting

1. **Password Hash**: Semua password di-hash menggunakan BCrypt dengan cost factor 10
2. **Default Password**: Password123! untuk semua user
3. **Database Schema**: Menggunakan sequences untuk auto-increment ID
4. **API Authentication**: Menggunakan JWT Bearer Token
5. **CORS**: Enabled untuk localhost:3000 dan localhost:5173

## 🔄 Reset Password Database (jika diperlukan)

```sql
UPDATE users SET password_hash = '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy';
COMMIT;
```

Hash di atas adalah BCrypt hash untuk password "Password123!"

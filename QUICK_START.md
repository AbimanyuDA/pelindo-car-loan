# 🚀 Quick Start Guide - Panduan Cepat

Untuk yang terburu-buru, ikuti langkah ini untuk menjalankan aplikasi dalam 10 menit!

## ⚡ Super Quick Setup (10 Minutes)

### Prerequisites Check
```bash
# Check Node.js (should be v18+)
node --version
npm --version

# Check .NET (should be v8.0+)
dotnet --version

# Check Oracle is running
sqlplus system/bima2005@XE
# Type: EXIT (jika berhasil connect)
```

### Step 1: Database Setup (2 minutes)

Buka Oracle SQL Developer atau SQL*Plus:

```sql
-- Connect sebagai system
sqlplus system/bima2005@XE

-- Run database creation script
@database/CreateDatabase.sql

-- Verify tables created
SELECT COUNT(*) FROM user_tables;
-- Should return: 8 (atau lebih)
```

### Step 2: Backend (3 minutes)

```bash
# Terminal 1
cd backend/PelindoCarLoan.API
dotnet run

# Tunggu sampai lihat:
# "Now listening on: http://localhost:5000"
```

### Step 3: Frontend (3 minutes)

```bash
# Terminal 2
cd frontend
npm install  # Skip kalau sudah pernah install
npm run dev

# Tunggu sampai lihat:
# "Local: http://localhost:3000"
```

### Step 4: Login (2 minutes)

Buka browser: **http://localhost:3000**

Login dengan akun:
- **Email**: requester1@pelindo.com
- **Password**: password123

✅ **Selesai!** Aplikasi siap digunakan.

---

## 🎯 Common Quick Tasks

### Buat Loan Request Baru
1. Login as Requester
2. Click "New Loan Request"
3. Fill form dengan detail perjalanan
4. Upload service letter (optional)
5. Click "Submit"
6. ✅ Approval L1 akan menerima notification

### Approve L1 (Assign Vehicle & Driver)
1. Login as L1 Approver (approver.l1@pelindo.com)
2. Go to "Approvals" → "Pending"
3. Click request to review
4. Select vehicle & driver
5. Click "Approve with Assignment"
6. ✅ L2 Approver akan dapat notification

### Approve L2 (Final)
1. Login as L2 Approver (approver.l2@pelindo.com)
2. Go to "Approvals" → "Pending"
3. Review details
4. Click "Approve"
5. ✅ Requester & Driver dapat email confirmation

---

## 🆘 Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| "Connection refused" backend | Pastikan backend running di terminal 1, port 5000 |
| "Cannot GET /" frontend | Pastikan frontend running di terminal 2, port 3000 |
| Database error | Verify Oracle service: `sc query OracleServiceXE` |
| "Port already in use" | Kill process: `Get-Process -Id (Get-NetTCPConnection -LocalPort 5000).OwningProcess \| Stop-Process` |
| Email not sending | Check Gmail app password config di appsettings.json |
| "Cannot find module" npm | Run `npm install` again di frontend folder |

---

## 📞 Ports Reference

| Service | Port | URL |
|---------|------|-----|
| Frontend | 3000 | http://localhost:3000 |
| Backend API | 5000 | http://localhost:5000 |
| API Docs | 5000 | http://localhost:5000/swagger |
| Oracle DB | 1521 | localhost:1521 |

---

## 🔑 Default Accounts

```
Requester:   requester1@pelindo.com / password123
L1 Approver: approver.l1@pelindo.com / password123
L2 Approver: approver.l2@pelindo.com / password123
Admin:       admin@pelindo.com / password123
Driver:      driver1@pelindo.com / password123
```

---

## 📚 File Penting

| File | Purpose |
|------|---------|
| `README.md` | Full documentation (Anda baca ini) |
| `QUICK_START.md` | Quick start guide (file ini) |
| `backend/PelindoCarLoan.API/appsettings.json` | Backend configuration |
| `database/CreateDatabase.sql` | Database schema |
| `frontend/vite.config.ts` | Frontend config |

---

## 🎓 Learning Path

1. **Understand the workflow**
   - Read "Fitur Utama" section di README.md
   - Understand 2-level approval process

2. **Explore the code**
   - Backend: Controllers → Services → Repositories
   - Frontend: Pages → Components → Services

3. **Test the features**
   - Create loan request
   - Test approval workflow
   - Check email notifications

4. **Customize as needed**
   - Update company info
   - Customize email templates
   - Add more workflows

---

## 🚨 Emergency Reset

Jika semua error dan perlu fresh start:

```bash
# 1. Stop all terminals (Ctrl+C)

# 2. Reset database
sqlplus system/bima2005@XE
SQL> DROP TABLE approvals;
SQL> DROP TABLE schedules;
SQL> DROP TABLE loan_requests;
SQL> DROP TABLE vehicles;
SQL> DROP TABLE drivers;
SQL> DROP TABLE users;
SQL> EXIT

# 3. Recreate from scratch
@database/CreateDatabase.sql

# 4. Clean build
cd backend/PelindoCarLoan.API
dotnet clean
dotnet restore
dotnet build

# 5. Reinstall frontend
cd frontend
rm -r node_modules package-lock.json  # Windows: rmdir /s /q
npm install

# 6. Start fresh
# Terminal 1: dotnet run
# Terminal 2: npm run dev
```

---

## ✅ Verification Checklist

Sebelum claim "Running":

- [ ] Backend running di http://localhost:5000
- [ ] Frontend running di http://localhost:3000
- [ ] Bisa login dengan default account
- [ ] Database terhubung (check logs)
- [ ] Tidak ada error di browser console (F12)
- [ ] Email service configured (jika diperlukan)

---

*Need more help? Check full README.md atau hubungi support@pelindo.com*

**Version: 1.0.0 | Last Updated: January 13, 2026**

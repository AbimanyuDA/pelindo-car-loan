# 🔌 API Documentation

Complete API reference untuk Pelindo Car Loan System

## Base URL
```
Development:  http://localhost:5000
Production:   https://api.pelindo.com  (example)
```

## Authentication

Semua endpoint (kecuali login) memerlukan JWT token di header:

```
Authorization: Bearer <JWT_TOKEN>
```

### Token Format
JWT token terdiri dari 3 bagian: `Header.Payload.Signature`

**Lifetime**: 480 minutes (8 hours) - bisa disesuaikan di appsettings.json

---

## 🔐 Auth Endpoints

### 1. Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@pelindo.com",
  "password": "password123"
}
```

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "id": 1,
    "email": "user@pelindo.com",
    "fullName": "John Doe",
    "role": "Requester",
    "division": "Operations",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 28800
  }
}
```

**Response (401 Unauthorized)**:
```json
{
  "success": false,
  "message": "Invalid email or password"
}
```

---

### 2. Get Current User Profile
```http
GET /api/auth/profile
Authorization: Bearer <JWT_TOKEN>
```

**Response (200 OK)**:
```json
{
  "success": true,
  "data": {
    "id": 1,
    "email": "user@pelindo.com",
    "fullName": "John Doe",
    "role": "Requester",
    "division": "Operations",
    "unitKerja": "Admin",
    "phoneNumber": "+62812XXXXXX"
  }
}
```

---

### 3. Logout
```http
POST /api/auth/logout
Authorization: Bearer <JWT_TOKEN>
```

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Logged out successfully"
}
```

---

### 4. Refresh Token
```http
POST /api/auth/refresh-token
Authorization: Bearer <EXPIRED_JWT_TOKEN>
```

**Response (200 OK)**:
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresIn": 28800
  }
}
```

---

## 📋 Loan Request Endpoints

### 1. Get All Loan Requests
```http
GET /api/loan-requests
Authorization: Bearer <JWT_TOKEN>
```

**Query Parameters**:
- `status`: string (SUBMITTED, APPROVED_L1, APPROVED_L2, REJECTED, COMPLETED)
- `userId`: number (filter by requester)
- `pageNumber`: number (default: 1)
- `pageSize`: number (default: 10)

**Response (200 OK)**:
```json
{
  "success": true,
  "data": [
    {
      "loanRequestId": 101,
      "requestNumber": "REQ-2026-001",
      "requesterName": "John Doe",
      "requesterEmail": "john@pelindo.com",
      "destination": "Jakarta",
      "purpose": "Business Meeting",
      "startDatetime": "2026-01-15T08:00:00",
      "endDatetime": "2026-01-15T17:00:00",
      "status": "APPROVED_L2",
      "vehicleId": 5,
      "driverId": 3,
      "createdAt": "2026-01-13T10:30:00"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalItems": 25,
    "totalPages": 3
  }
}
```

---

### 2. Get Loan Request Detail
```http
GET /api/loan-requests/{id}
Authorization: Bearer <JWT_TOKEN>
```

**URL Parameters**:
- `id`: number - Loan Request ID

**Response (200 OK)**:
```json
{
  "success": true,
  "data": {
    "loanRequestId": 101,
    "requestNumber": "REQ-2026-001",
    "requesterName": "John Doe",
    "requesterEmail": "john@pelindo.com",
    "requesterPhone": "+62812XXXXXX",
    "requesterDivision": "Operations",
    "requesterUnitKerja": "Admin",
    "destination": "Jakarta",
    "purpose": "Business Meeting",
    "guestList": "Jane Smith, Bob Wilson",
    "hotelAccommodation": "Hotel Grand Indonesia",
    "startDatetime": "2026-01-15T08:00:00",
    "endDatetime": "2026-01-15T17:00:00",
    "serviceLetterBasis": "Director Decision Letter #2026/001",
    "serviceLetterFilePath": "uploads/service-letters/REQ-2026-001.pdf",
    "status": "APPROVED_L2",
    "vehicleId": 5,
    "vehiclePlateNumber": "B 1234 XX",
    "vehicleType": "Toyota Innova",
    "driverId": 3,
    "driverName": "Ahmad Suryanto",
    "driverPhone": "+62812YYYYYY",
    "notes": "No special requirements",
    "createdAt": "2026-01-13T10:30:00",
    "updatedAt": "2026-01-13T14:00:00"
  }
}
```

---

### 3. Create New Loan Request
```http
POST /api/loan-requests
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json

{
  "destination": "Surabaya",
  "purpose": "Factory Inspection",
  "guestList": "Alice Brown, Charlie Davis",
  "hotelAccommodation": "Hotel Majapahit",
  "startDatetime": "2026-02-01T06:00:00",
  "endDatetime": "2026-02-02T18:00:00",
  "serviceLetterBasis": "Director Decision #2026/002",
  "serviceLetterFilePath": "uploads/service-letters/new-letter.pdf"
}
```

**Response (201 Created)**:
```json
{
  "success": true,
  "message": "Loan request created successfully",
  "data": {
    "loanRequestId": 102,
    "requestNumber": "REQ-2026-002",
    "status": "SUBMITTED",
    "createdAt": "2026-01-13T15:00:00"
  }
}
```

**Validation Errors (400 Bad Request)**:
```json
{
  "success": false,
  "errors": {
    "destination": ["Destination is required"],
    "purpose": ["Purpose must be at least 10 characters"],
    "startDatetime": ["Start date cannot be in the past"]
  }
}
```

---

### 4. Update Loan Request
```http
PUT /api/loan-requests/{id}
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json

{
  "destination": "Surabaya (Updated)",
  "hotelAccommodation": "Hotel Surabaya Premier"
}
```

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Loan request updated successfully",
  "data": {
    "loanRequestId": 102,
    "destination": "Surabaya (Updated)"
  }
}
```

---

### 5. Get Request History
```http
GET /api/loan-requests/{id}/history
Authorization: Bearer <JWT_TOKEN>
```

**Response (200 OK)**:
```json
{
  "success": true,
  "data": [
    {
      "timestamp": "2026-01-13T10:30:00",
      "action": "CREATED",
      "changedBy": "John Doe",
      "changes": "Request submitted"
    },
    {
      "timestamp": "2026-01-13T11:00:00",
      "action": "APPROVED_L1",
      "changedBy": "Manager Name",
      "changes": "Approved with Vehicle B 1234 XX, Driver Ahmad"
    },
    {
      "timestamp": "2026-01-13T14:00:00",
      "action": "APPROVED_L2",
      "changedBy": "Director Name",
      "changes": "Final approval"
    }
  ]
}
```

---

## ✅ Approval Endpoints

### 1. Get Pending Approvals
```http
GET /api/approvals/pending
Authorization: Bearer <JWT_TOKEN>
```

**Query Parameters**:
- `approvalLevel`: number (1 or 2)
- `pageNumber`: number
- `pageSize`: number

**Response (200 OK)**:
```json
{
  "success": true,
  "data": [
    {
      "approvalId": 201,
      "loanRequestId": 101,
      "requestNumber": "REQ-2026-001",
      "requesterName": "John Doe",
      "approvalLevel": 1,
      "status": "PENDING",
      "submittedAt": "2026-01-13T10:30:00",
      "daysWaiting": 2
    }
  ],
  "statistics": {
    "totalPending": 5,
    "level1Pending": 3,
    "level2Pending": 2
  }
}
```

---

### 2. Get Approval Detail
```http
GET /api/approvals/{id}
Authorization: Bearer <JWT_TOKEN>
```

**Response (200 OK)**:
```json
{
  "success": true,
  "data": {
    "approvalId": 201,
    "loanRequestId": 101,
    "requestNumber": "REQ-2026-001",
    "requesterName": "John Doe",
    "approvalLevel": 1,
    "status": "PENDING",
    "loanRequestDetails": {
      "destination": "Jakarta",
      "purpose": "Business Meeting",
      "startDatetime": "2026-01-15T08:00:00",
      "endDatetime": "2026-01-15T17:00:00",
      "guestList": "Jane Smith, Bob Wilson"
    },
    "submittedAt": "2026-01-13T10:30:00"
  }
}
```

---

### 3. Approve L1 (Assign Vehicle & Driver)
```http
POST /api/approvals/{id}/approve-l1
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json

{
  "vehicleId": 5,
  "driverId": 3,
  "notes": "Approved. Vehicle available and driver confirmed."
}
```

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "L1 approval successful. L2 approver notified.",
  "data": {
    "approvalId": 201,
    "status": "APPROVED_L1",
    "vehicleAssigned": "B 1234 XX",
    "driverAssigned": "Ahmad Suryanto"
  }
}
```

---

### 4. Approve L2 (Final)
```http
POST /api/approvals/{id}/approve-l2
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json

{
  "notes": "Approved. All requirements met."
}
```

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "L2 approval successful. Schedule created. Requester and driver notified.",
  "data": {
    "approvalId": 201,
    "status": "APPROVED_L2",
    "scheduleId": 501,
    "notificationsSent": ["requester@pelindo.com", "driver@pelindo.com"]
  }
}
```

---

### 5. Reject Request (L1/L2)
```http
POST /api/approvals/{id}/reject
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json

{
  "approvalLevel": 1,
  "notes": "Driver not available for requested dates"
}
```

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Request rejected. Requester notified.",
  "data": {
    "approvalId": 201,
    "status": "REJECTED",
    "rejectionReason": "Driver not available for requested dates"
  }
}
```

---

## 🏎️ Resources Endpoints

### 1. Get All Vehicles
```http
GET /api/resources/vehicles
Authorization: Bearer <JWT_TOKEN>
```

**Query Parameters**:
- `status`: string (AVAILABLE, IN_USE, MAINTENANCE, RETIRED)
- `type`: string (filter by vehicle type)

**Response (200 OK)**:
```json
{
  "success": true,
  "data": [
    {
      "vehicleId": 5,
      "plateNumber": "B 1234 XX",
      "type": "Toyota Innova",
      "capacity": 8,
      "fuelType": "Diesel",
      "year": 2022,
      "status": "AVAILABLE",
      "lastServiceDate": "2026-01-01",
      "nextServiceDate": "2026-02-01"
    }
  ]
}
```

---

### 2. Get Available Vehicles
```http
GET /api/resources/available-vehicles
Authorization: Bearer <JWT_TOKEN>
```

**Query Parameters**:
- `startDate`: datetime (ISO 8601 format)
- `endDate`: datetime

**Response (200 OK)**:
```json
{
  "success": true,
  "data": [
    {
      "vehicleId": 5,
      "plateNumber": "B 1234 XX",
      "type": "Toyota Innova",
      "capacity": 8,
      "status": "AVAILABLE"
    }
  ],
  "statistics": {
    "totalVehicles": 10,
    "availableCount": 7
  }
}
```

---

### 3. Get All Drivers
```http
GET /api/resources/drivers
Authorization: Bearer <JWT_TOKEN>
```

**Query Parameters**:
- `status`: string (AVAILABLE, ON_DUTY, ON_LEAVE, INACTIVE)

**Response (200 OK)**:
```json
{
  "success": true,
  "data": [
    {
      "driverId": 3,
      "fullName": "Ahmad Suryanto",
      "phoneNumber": "+62812XXXXXX",
      "licenseNumber": "DK123456789",
      "licenseExpiry": "2027-12-31",
      "experienceYears": 5,
      "rating": 4.8,
      "status": "AVAILABLE"
    }
  ]
}
```

---

### 4. Get Available Drivers
```http
GET /api/resources/available-drivers
Authorization: Bearer <JWT_TOKEN>
```

**Query Parameters**:
- `startDate`: datetime (ISO 8601)
- `endDate`: datetime

**Response (200 OK)**:
```json
{
  "success": true,
  "data": [
    {
      "driverId": 3,
      "fullName": "Ahmad Suryanto",
      "phoneNumber": "+62812XXXXXX",
      "licenseExpiry": "2027-12-31",
      "rating": 4.8,
      "status": "AVAILABLE"
    }
  ],
  "statistics": {
    "totalDrivers": 15,
    "availableCount": 12
  }
}
```

---

## 📊 Dashboard Endpoints

### 1. Get Dashboard Statistics
```http
GET /api/dashboard/statistics
Authorization: Bearer <JWT_TOKEN>
```

**Response (200 OK)**:
```json
{
  "success": true,
  "data": {
    "totalRequests": 150,
    "pendingApprovals": 5,
    "approvedRequests": 120,
    "rejectedRequests": 10,
    "inProgressTrips": 8,
    "completedTrips": 95,
    "availableVehicles": 7,
    "availableDrivers": 12
  }
}
```

---

### 2. Get Dashboard Summary
```http
GET /api/dashboard/summary
Authorization: Bearer <JWT_TOKEN>
```

**Response (200 OK)**:
```json
{
  "success": true,
  "data": {
    "month": "January 2026",
    "requestsThisMonth": 45,
    "averageApprovalTime": "2.5 days",
    "vehicleUtilization": "70%",
    "driverUtilization": "65%"
  }
}
```

---

## 📅 Schedule Endpoints

### 1. Get Schedules
```http
GET /api/schedules
Authorization: Bearer <JWT_TOKEN>
```

**Query Parameters**:
- `status`: string (SCHEDULED, IN_PROGRESS, COMPLETED, CANCELLED)
- `vehicleId`: number
- `driverId`: number
- `fromDate`: datetime
- `toDate`: datetime

**Response (200 OK)**:
```json
{
  "success": true,
  "data": [
    {
      "scheduleId": 501,
      "loanRequestId": 101,
      "requestNumber": "REQ-2026-001",
      "vehicleId": 5,
      "vehiclePlateNumber": "B 1234 XX",
      "driverId": 3,
      "driverName": "Ahmad Suryanto",
      "startDatetime": "2026-01-15T08:00:00",
      "endDatetime": "2026-01-15T17:00:00",
      "destination": "Jakarta",
      "status": "SCHEDULED",
      "createdAt": "2026-01-13T14:00:00"
    }
  ]
}
```

---

### 2. Update Schedule Status
```http
PUT /api/schedules/{id}/status
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json

{
  "status": "IN_PROGRESS",
  "notes": "Trip started"
}
```

**Response (200 OK)**:
```json
{
  "success": true,
  "message": "Schedule status updated",
  "data": {
    "scheduleId": 501,
    "status": "IN_PROGRESS"
  }
}
```

---

## Error Responses

### 400 Bad Request
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": {
    "fieldName": ["Error message"]
  }
}
```

### 401 Unauthorized
```json
{
  "success": false,
  "message": "Unauthorized. Please login."
}
```

### 403 Forbidden
```json
{
  "success": false,
  "message": "You don't have permission to access this resource"
}
```

### 404 Not Found
```json
{
  "success": false,
  "message": "Resource not found"
}
```

### 500 Internal Server Error
```json
{
  "success": false,
  "message": "An error occurred. Please try again later."
}
```

---

## 🔗 Interactive API Docs

Swagger/OpenAPI documentation tersedia di:
```
http://localhost:5000/swagger
```

Gunakan Swagger UI untuk:
- Test semua endpoint
- Lihat request/response examples
- Generate client code

---

## Rate Limiting

Tidak ada rate limiting di development. Di production:
- 100 requests per minute per user
- 1000 requests per minute per API key

---

## Pagination

Endpoint yang mengembalikan list menggunakan pagination:

**Query Parameters**:
- `pageNumber`: 1-based (default: 1)
- `pageSize`: 1-100 (default: 10)

**Response includes**:
```json
{
  "pagination": {
    "currentPage": 1,
    "pageSize": 10,
    "totalItems": 150,
    "totalPages": 15
  }
}
```

---

## Timestamps

Semua timestamps dalam format ISO 8601 UTC:
```
2026-01-13T15:30:45.123Z
```

---

## Version

API Version: **1.0.0**

Last Updated: January 13, 2026

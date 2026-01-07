-- ============================================================
-- PELINDO VEHICLE LOAN SYSTEM - SEED DATA
-- ============================================================
-- Created: 2026-01-07
-- Description: Initial seed data for testing and development
-- ============================================================

-- ============================================================
-- INSERT USERS
-- ============================================================
-- Password for all users: "password123" (hashed with BCrypt)
-- In production, use proper password hashing

INSERT INTO users (id, name, email, password_hash, role, division) VALUES 
(seq_users.NEXTVAL, 'Ahmad Pemohon', 'ahmad.pemohon@pelindo.co.id', '$2a$11$Kx7bTqWYo8VeN3rC6AeOMunD9n7Xtm5GXrZ5H1VmY9kXz3Wa7PvEi', 'PEMOHON', 'Operations');

INSERT INTO users (id, name, email, password_hash, role, division) VALUES 
(seq_users.NEXTVAL, 'Budi Pemohon', 'budi.pemohon@pelindo.co.id', '$2a$11$Kx7bTqWYo8VeN3rC6AeOMunD9n7Xtm5GXrZ5H1VmY9kXz3Wa7PvEi', 'PEMOHON', 'Finance');

INSERT INTO users (id, name, email, password_hash, role, division) VALUES 
(seq_users.NEXTVAL, 'Citra Approver L1', 'citra.approver@pelindo.co.id', '$2a$11$Kx7bTqWYo8VeN3rC6AeOMunD9n7Xtm5GXrZ5H1VmY9kXz3Wa7PvEi', 'PIC_APPROVAL_L1', 'HR');

INSERT INTO users (id, name, email, password_hash, role, division) VALUES 
(seq_users.NEXTVAL, 'Dewi Manager', 'dewi.manager@pelindo.co.id', '$2a$11$Kx7bTqWYo8VeN3rC6AeOMunD9n7Xtm5GXrZ5H1VmY9kXz3Wa7PvEi', 'PIC_APPROVAL_L2', 'Management');

INSERT INTO users (id, name, email, password_hash, role, division) VALUES 
(seq_users.NEXTVAL, 'Eko Driver', 'eko.driver@pelindo.co.id', '$2a$11$Kx7bTqWYo8VeN3rC6AeOMunD9n7Xtm5GXrZ5H1VmY9kXz3Wa7PvEi', 'DRIVER', 'Transport');

INSERT INTO users (id, name, email, password_hash, role, division) VALUES 
(seq_users.NEXTVAL, 'Fajar Driver', 'fajar.driver@pelindo.co.id', '$2a$11$Kx7bTqWYo8VeN3rC6AeOMunD9n7Xtm5GXrZ5H1VmY9kXz3Wa7PvEi', 'DRIVER', 'Transport');

INSERT INTO users (id, name, email, password_hash, role, division) VALUES 
(seq_users.NEXTVAL, 'Gita Driver', 'gita.driver@pelindo.co.id', '$2a$11$Kx7bTqWYo8VeN3rC6AeOMunD9n7Xtm5GXrZ5H1VmY9kXz3Wa7PvEi', 'DRIVER', 'Transport');

INSERT INTO users (id, name, email, password_hash, role, division) VALUES 
(seq_users.NEXTVAL, 'Hendra Admin', 'hendra.admin@pelindo.co.id', '$2a$11$Kx7bTqWYo8VeN3rC6AeOMunD9n7Xtm5GXrZ5H1VmY9kXz3Wa7PvEi', 'ADMIN', 'IT');

-- ============================================================
-- INSERT VEHICLES
-- ============================================================
INSERT INTO vehicles (id, plate_number, brand, type, capacity, status, notes) VALUES 
(seq_vehicles.NEXTVAL, 'B 1234 PLD', 'Toyota', 'Innova', 7, 'AVAILABLE', 'Executive vehicle for management');

INSERT INTO vehicles (id, plate_number, brand, type, capacity, status, notes) VALUES 
(seq_vehicles.NEXTVAL, 'B 5678 PLD', 'Honda', 'CR-V', 5, 'AVAILABLE', 'SUV for field visits');

INSERT INTO vehicles (id, plate_number, brand, type, capacity, status, notes) VALUES 
(seq_vehicles.NEXTVAL, 'B 9012 PLD', 'Toyota', 'Avanza', 7, 'AVAILABLE', 'Multi-purpose vehicle');

INSERT INTO vehicles (id, plate_number, brand, type, capacity, status, notes) VALUES 
(seq_vehicles.NEXTVAL, 'B 3456 PLD', 'Mitsubishi', 'Pajero', 7, 'AVAILABLE', 'Heavy duty for port area');

INSERT INTO vehicles (id, plate_number, brand, type, capacity, status, notes) VALUES 
(seq_vehicles.NEXTVAL, 'B 7890 PLD', 'Toyota', 'Fortuner', 7, 'MAINTENANCE', 'Currently under maintenance');

INSERT INTO vehicles (id, plate_number, brand, type, capacity, status, notes) VALUES 
(seq_vehicles.NEXTVAL, 'B 2345 PLD', 'Daihatsu', 'Xenia', 7, 'AVAILABLE', 'Economy class vehicle');

-- ============================================================
-- INSERT DRIVERS
-- ============================================================
INSERT INTO drivers (id, user_id, license_number, license_expiry, phone_number, status) VALUES 
(seq_drivers.NEXTVAL, 5, 'SIM-A-12345678', TO_DATE('2028-12-31', 'YYYY-MM-DD'), '081234567890', 'AVAILABLE');

INSERT INTO drivers (id, user_id, license_number, license_expiry, phone_number, status) VALUES 
(seq_drivers.NEXTVAL, 6, 'SIM-A-23456789', TO_DATE('2027-06-30', 'YYYY-MM-DD'), '081234567891', 'AVAILABLE');

INSERT INTO drivers (id, user_id, license_number, license_expiry, phone_number, status) VALUES 
(seq_drivers.NEXTVAL, 7, 'SIM-A-34567890', TO_DATE('2028-03-15', 'YYYY-MM-DD'), '081234567892', 'AVAILABLE');

-- ============================================================
-- INSERT SAMPLE LOAN REQUESTS
-- ============================================================
INSERT INTO loan_requests (id, user_id, request_number, purpose, destination, passenger_count, start_datetime, end_datetime, status, notes) VALUES 
(seq_loan_requests.NEXTVAL, 1, 'LR-20260107-000001', 'Meeting with client at Terminal 3', 'Terminal 3 Tanjung Priok', 3, 
    TO_TIMESTAMP('2026-01-10 09:00:00', 'YYYY-MM-DD HH24:MI:SS'), 
    TO_TIMESTAMP('2026-01-10 17:00:00', 'YYYY-MM-DD HH24:MI:SS'), 
    'SUBMITTED', 'Need vehicle with AC');

INSERT INTO loan_requests (id, user_id, request_number, purpose, destination, passenger_count, start_datetime, end_datetime, status, notes) VALUES 
(seq_loan_requests.NEXTVAL, 2, 'LR-20260107-000002', 'Site inspection at Koja Terminal', 'Koja Terminal', 2, 
    TO_TIMESTAMP('2026-01-12 08:00:00', 'YYYY-MM-DD HH24:MI:SS'), 
    TO_TIMESTAMP('2026-01-12 12:00:00', 'YYYY-MM-DD HH24:MI:SS'), 
    'APPROVED_L1', 'Urgent inspection required');

INSERT INTO loan_requests (id, user_id, request_number, purpose, destination, passenger_count, start_datetime, end_datetime, status, notes) VALUES 
(seq_loan_requests.NEXTVAL, 1, 'LR-20260107-000003', 'Document delivery to Ministry of Transport', 'Ministry of Transport Jakarta', 1, 
    TO_TIMESTAMP('2026-01-15 10:00:00', 'YYYY-MM-DD HH24:MI:SS'), 
    TO_TIMESTAMP('2026-01-15 14:00:00', 'YYYY-MM-DD HH24:MI:SS'), 
    'APPROVED_L2', 'Important documents');

-- ============================================================
-- INSERT SAMPLE APPROVALS
-- ============================================================
INSERT INTO approvals (id, loan_request_id, approver_id, approval_level, status, notes) VALUES 
(seq_approvals.NEXTVAL, 2, 3, 1, 'APPROVED', 'Approved for site inspection');

INSERT INTO approvals (id, loan_request_id, approver_id, approval_level, status, notes) VALUES 
(seq_approvals.NEXTVAL, 3, 3, 1, 'APPROVED', 'Approved - high priority');

INSERT INTO approvals (id, loan_request_id, approver_id, approval_level, status, notes) VALUES 
(seq_approvals.NEXTVAL, 3, 4, 2, 'APPROVED', 'Approved by management');

-- ============================================================
-- INSERT SAMPLE SCHEDULE (for APPROVED_L2 request)
-- ============================================================
INSERT INTO schedules (id, loan_request_id, driver_id, vehicle_id, assigned_by, status, notes) VALUES 
(seq_schedules.NEXTVAL, 3, 1, 1, NULL, 'ASSIGNED', 'Auto-assigned by system');

-- Update the loan request status to SCHEDULED
UPDATE loan_requests SET status = 'SCHEDULED' WHERE id = 3;

COMMIT;

-- ============================================================
-- VERIFICATION QUERIES
-- ============================================================
SELECT 'Users' AS table_name, COUNT(*) AS record_count FROM users
UNION ALL
SELECT 'Vehicles', COUNT(*) FROM vehicles
UNION ALL
SELECT 'Drivers', COUNT(*) FROM drivers
UNION ALL
SELECT 'Loan Requests', COUNT(*) FROM loan_requests
UNION ALL
SELECT 'Approvals', COUNT(*) FROM approvals
UNION ALL
SELECT 'Schedules', COUNT(*) FROM schedules;

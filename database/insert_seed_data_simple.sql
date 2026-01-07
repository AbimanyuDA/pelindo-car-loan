-- Insert seed data for Pelindo Car Loan System
-- Password for all users: Password123!
-- Hashed with BCrypt ($2a$11$...)

-- Insert users (Password: Password123!)
INSERT INTO users (user_id, username, email, password_hash, full_name, role, division, phone_number, is_active)
VALUES (seq_users.NEXTVAL, 'admin', 'admin@pelindo.co.id', '$2a$11$LjGhT3d7YCIVqF8Pn5OOgO9T7YiVGx6b3Q4g5f6h7j8k9l0m1n2o3', 'Administrator', 'ADMIN', 'IT', '081234567890', 1);

INSERT INTO users (user_id, username, email, password_hash, full_name, role, division, phone_number, is_active)
VALUES (seq_users.NEXTVAL, 'pemohon1', 'pemohon1@pelindo.co.id', '$2a$11$LjGhT3d7YCIVqF8Pn5OOgO9T7YiVGx6b3Q4g5f6h7j8k9l0m1n2o3', 'Budi Santoso', 'PEMOHON', 'Finance', '081234567891', 1);

INSERT INTO users (user_id, username, email, password_hash, full_name, role, division, phone_number, is_active)
VALUES (seq_users.NEXTVAL, 'pemohon2', 'pemohon2@pelindo.co.id', '$2a$11$LjGhT3d7YCIVqF8Pn5OOgO9T7YiVGx6b3Q4g5f6h7j8k9l0m1n2o3', 'Siti Rahayu', 'PEMOHON', 'Operations', '081234567892', 1);

INSERT INTO users (user_id, username, email, password_hash, full_name, role, division, phone_number, is_active)
VALUES (seq_users.NEXTVAL, 'approver_l1_1', 'approver.l1.1@pelindo.co.id', '$2a$11$LjGhT3d7YCIVqF8Pn5OOgO9T7YiVGx6b3Q4g5f6h7j8k9l0m1n2o3', 'Agus Wijaya', 'PIC_APPROVAL_L1', 'Finance', '081234567893', 1);

INSERT INTO users (user_id, username, email, password_hash, full_name, role, division, phone_number, is_active)
VALUES (seq_users.NEXTVAL, 'approver_l1_2', 'approver.l1.2@pelindo.co.id', '$2a$11$LjGhT3d7YCIVqF8Pn5OOgO9T7YiVGx6b3Q4g5f6h7j8k9l0m1n2o3', 'Dewi Kusuma', 'PIC_APPROVAL_L1', 'Operations', '081234567894', 1);

INSERT INTO users (user_id, username, email, password_hash, full_name, role, division, phone_number, is_active)
VALUES (seq_users.NEXTVAL, 'approver_l2', 'approver.l2@pelindo.co.id', '$2a$11$LjGhT3d7YCIVqF8Pn5OOgO9T7YiVGx6b3Q4g5f6h7j8k9l0m1n2o3', 'Ahmad Rahman', 'PIC_APPROVAL_L2', 'Management', '081234567895', 1);

INSERT INTO users (user_id, username, email, password_hash, full_name, role, division, phone_number, is_active)
VALUES (seq_users.NEXTVAL, 'driver1', 'driver1@pelindo.co.id', '$2a$11$LjGhT3d7YCIVqF8Pn5OOgO9T7YiVGx6b3Q4g5f6h7j8k9l0m1n2o3', 'Joko Susilo', 'DRIVER', 'Transportation', '081234567896', 1);

INSERT INTO users (user_id, username, email, password_hash, full_name, role, division, phone_number, is_active)
VALUES (seq_users.NEXTVAL, 'driver2', 'driver2@pelindo.co.id', '$2a$11$LjGhT3d7YCIVqF8Pn5OOgO9T7YiVGx6b3Q4g5f6h7j8k9l0m1n2o3', 'Andi Pratama', 'DRIVER', 'Transportation', '081234567897', 1);

INSERT INTO users (user_id, username, email, password_hash, full_name, role, division, phone_number, is_active)
VALUES (seq_users.NEXTVAL, 'driver3', 'driver3@pelindo.co.id', '$2a$11$LjGhT3d7YCIVqF8Pn5OOgO9T7YiVGx6b3Q4g5f6h7j8k9l0m1n2o3', 'Bambang Surya', 'DRIVER', 'Transportation', '081234567898', 1);

-- Insert vehicles
INSERT INTO vehicles (vehicle_id, license_plate, type, brand, model, year, capacity, status, last_maintenance, next_maintenance)
VALUES (seq_vehicles.NEXTVAL, 'B 1234 ABC', 'Sedan', 'Toyota', 'Camry', 2022, 4, 'AVAILABLE', TO_DATE('2025-12-01', 'YYYY-MM-DD'), TO_DATE('2026-06-01', 'YYYY-MM-DD'));

INSERT INTO vehicles (vehicle_id, license_plate, type, brand, model, year, capacity, status, last_maintenance, next_maintenance)
VALUES (seq_vehicles.NEXTVAL, 'B 5678 DEF', 'SUV', 'Honda', 'CR-V', 2023, 6, 'AVAILABLE', TO_DATE('2025-12-15', 'YYYY-MM-DD'), TO_DATE('2026-06-15', 'YYYY-MM-DD'));

INSERT INTO vehicles (vehicle_id, license_plate, type, brand, model, year, capacity, status, last_maintenance, next_maintenance)
VALUES (seq_vehicles.NEXTVAL, 'B 9012 GHI', 'MPV', 'Toyota', 'Avanza', 2021, 7, 'AVAILABLE', TO_DATE('2025-11-20', 'YYYY-MM-DD'), TO_DATE('2026-05-20', 'YYYY-MM-DD'));

INSERT INTO vehicles (vehicle_id, license_plate, type, brand, model, year, capacity, status, last_maintenance, next_maintenance)
VALUES (seq_vehicles.NEXTVAL, 'B 3456 JKL', 'Minibus', 'Isuzu', 'Elf', 2020, 15, 'AVAILABLE', TO_DATE('2025-12-10', 'YYYY-MM-DD'), TO_DATE('2026-06-10', 'YYYY-MM-DD'));

INSERT INTO vehicles (vehicle_id, license_plate, type, brand, model, year, capacity, status, last_maintenance, next_maintenance)
VALUES (seq_vehicles.NEXTVAL, 'B 7890 MNO', 'Sedan', 'Honda', 'Accord', 2022, 4, 'IN_USE', TO_DATE('2025-11-25', 'YYYY-MM-DD'), TO_DATE('2026-05-25', 'YYYY-MM-DD'));

INSERT INTO vehicles (vehicle_id, license_plate, type, brand, model, year, capacity, status, last_maintenance, next_maintenance)
VALUES (seq_vehicles.NEXTVAL, 'B 2345 PQR', 'SUV', 'Mitsubishi', 'Pajero Sport', 2023, 6, 'MAINTENANCE', TO_DATE('2025-12-01', 'YYYY-MM-DD'), TO_DATE('2026-06-01', 'YYYY-MM-DD'));

-- Insert drivers (linked to driver user accounts)
INSERT INTO drivers (driver_id, user_id, license_number, license_expiry, status, experience_years, rating)
VALUES (seq_drivers.NEXTVAL, 7, 'SIM-A-12345678', TO_DATE('2027-12-31', 'YYYY-MM-DD'), 'AVAILABLE', 10, 4.80);

INSERT INTO drivers (driver_id, user_id, license_number, license_expiry, status, experience_years, rating)
VALUES (seq_drivers.NEXTVAL, 8, 'SIM-A-87654321', TO_DATE('2028-06-30', 'YYYY-MM-DD'), 'AVAILABLE', 8, 4.90);

INSERT INTO drivers (driver_id, user_id, license_number, license_expiry, status, experience_years, rating)
VALUES (seq_drivers.NEXTVAL, 9, 'SIM-A-11223344', TO_DATE('2027-09-15', 'YYYY-MM-DD'), 'ON_DUTY', 12, 4.95);

-- Insert sample loan requests
INSERT INTO loan_requests (loan_request_id, user_id, destination, purpose, passenger_count, start_datetime, end_datetime, status)
VALUES (seq_loan_requests.NEXTVAL, 2, 'Tanjung Priok Port', 'Meeting with port authority', 3, TO_TIMESTAMP('2026-01-15 08:00:00', 'YYYY-MM-DD HH24:MI:SS'), TO_TIMESTAMP('2026-01-15 12:00:00', 'YYYY-MM-DD HH24:MI:SS'), 'SUBMITTED');

INSERT INTO loan_requests (loan_request_id, user_id, destination, purpose, passenger_count, start_datetime, end_datetime, status)
VALUES (seq_loan_requests.NEXTVAL, 3, 'Jakarta Convention Center', 'Attending maritime conference', 5, TO_TIMESTAMP('2026-01-20 07:00:00', 'YYYY-MM-DD HH24:MI:SS'), TO_TIMESTAMP('2026-01-20 18:00:00', 'YYYY-MM-DD HH24:MI:SS'), 'SUBMITTED');

INSERT INTO loan_requests (loan_request_id, user_id, destination, purpose, passenger_count, start_datetime, end_datetime, status)
VALUES (seq_loan_requests.NEXTVAL, 2, 'Soekarno-Hatta Airport', 'Business trip pickup', 2, TO_TIMESTAMP('2026-01-25 14:00:00', 'YYYY-MM-DD HH24:MI:SS'), TO_TIMESTAMP('2026-01-25 16:00:00', 'YYYY-MM-DD HH24:MI:SS'), 'APPROVED_L1');

-- Insert approvals for the third loan request
INSERT INTO approvals (approval_id, loan_request_id, approver_id, approval_level, status, notes, approved_at)
VALUES (seq_approvals.NEXTVAL, 3, 4, 1, 'APPROVED', 'Approved for business purposes', SYSTIMESTAMP);

-- Insert a completed schedule
INSERT INTO schedules (schedule_id, loan_request_id, driver_id, vehicle_id, status, notes)
VALUES (seq_schedules.NEXTVAL, 3, 3, 5, 'CONFIRMED', 'Driver and vehicle assigned');

COMMIT;

-- Show data counts
SELECT 'USERS' AS TABLE_NAME, COUNT(*) AS COUNT FROM users
UNION ALL
SELECT 'VEHICLES', COUNT(*) FROM vehicles
UNION ALL
SELECT 'DRIVERS', COUNT(*) FROM drivers
UNION ALL
SELECT 'LOAN_REQUESTS', COUNT(*) FROM loan_requests
UNION ALL
SELECT 'APPROVALS', COUNT(*) FROM approvals
UNION ALL
SELECT 'SCHEDULES', COUNT(*) FROM schedules
ORDER BY TABLE_NAME;

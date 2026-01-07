-- Create tables for Pelindo Car Loan System
-- Simple format for SQL*Plus execution

-- Drop existing sequences
DROP SEQUENCE seq_users;
DROP SEQUENCE seq_vehicles;
DROP SEQUENCE seq_drivers;
DROP SEQUENCE seq_loan_requests;
DROP SEQUENCE seq_approvals;
DROP SEQUENCE seq_schedules;

-- Drop existing tables
DROP TABLE schedules CASCADE CONSTRAINTS;
DROP TABLE approvals CASCADE CONSTRAINTS;
DROP TABLE loan_requests CASCADE CONSTRAINTS;
DROP TABLE drivers CASCADE CONSTRAINTS;
DROP TABLE vehicles CASCADE CONSTRAINTS;
DROP TABLE users CASCADE CONSTRAINTS;

-- Create sequences
CREATE SEQUENCE seq_users START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE seq_vehicles START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE seq_drivers START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE seq_loan_requests START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE seq_approvals START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE seq_schedules START WITH 1 INCREMENT BY 1;

-- Create users table
CREATE TABLE users (
    user_id NUMBER(10) PRIMARY KEY,
    username VARCHAR2(50) NOT NULL UNIQUE,
    email VARCHAR2(100) NOT NULL UNIQUE,
    password_hash VARCHAR2(255) NOT NULL,
    full_name VARCHAR2(100) NOT NULL,
    role VARCHAR2(20) NOT NULL CHECK (role IN ('PEMOHON', 'PIC_APPROVAL_L1', 'PIC_APPROVAL_L2', 'DRIVER', 'ADMIN')),
    division VARCHAR2(100),
    phone_number VARCHAR2(20),
    is_active NUMBER(1) DEFAULT 1,
    created_at TIMESTAMP DEFAULT SYSTIMESTAMP,
    updated_at TIMESTAMP DEFAULT SYSTIMESTAMP
);

-- Create vehicles table
CREATE TABLE vehicles (
    vehicle_id NUMBER(10) PRIMARY KEY,
    license_plate VARCHAR2(20) NOT NULL UNIQUE,
    type VARCHAR2(50) NOT NULL,
    brand VARCHAR2(50) NOT NULL,
    model VARCHAR2(50) NOT NULL,
    year NUMBER(4) NOT NULL,
    capacity NUMBER(2) NOT NULL,
    status VARCHAR2(20) DEFAULT 'AVAILABLE' CHECK (status IN ('AVAILABLE', 'IN_USE', 'MAINTENANCE', 'RETIRED')),
    last_maintenance DATE,
    next_maintenance DATE,
    created_at TIMESTAMP DEFAULT SYSTIMESTAMP,
    updated_at TIMESTAMP DEFAULT SYSTIMESTAMP
);

-- Create drivers table
CREATE TABLE drivers (
    driver_id NUMBER(10) PRIMARY KEY,
    user_id NUMBER(10) NOT NULL REFERENCES users(user_id),
    license_number VARCHAR2(50) NOT NULL UNIQUE,
    license_expiry DATE NOT NULL,
    status VARCHAR2(20) DEFAULT 'AVAILABLE' CHECK (status IN ('AVAILABLE', 'ON_DUTY', 'OFF_DUTY', 'LEAVE')),
    experience_years NUMBER(2),
    rating NUMBER(3,2) DEFAULT 5.00,
    created_at TIMESTAMP DEFAULT SYSTIMESTAMP,
    updated_at TIMESTAMP DEFAULT SYSTIMESTAMP
);

-- Create loan_requests table
CREATE TABLE loan_requests (
    loan_request_id NUMBER(10) PRIMARY KEY,
    user_id NUMBER(10) NOT NULL REFERENCES users(user_id),
    destination VARCHAR2(255) NOT NULL,
    purpose CLOB NOT NULL,
    passenger_count NUMBER(2) NOT NULL,
    start_datetime TIMESTAMP NOT NULL,
    end_datetime TIMESTAMP NOT NULL,
    status VARCHAR2(20) DEFAULT 'SUBMITTED' CHECK (status IN ('SUBMITTED', 'APPROVED_L1', 'REJECTED_L1', 'APPROVED_L2', 'REJECTED_L2', 'SCHEDULED', 'WAITING_RESOURCE', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED')),
    created_at TIMESTAMP DEFAULT SYSTIMESTAMP,
    updated_at TIMESTAMP DEFAULT SYSTIMESTAMP
);

-- Create approvals table
CREATE TABLE approvals (
    approval_id NUMBER(10) PRIMARY KEY,
    loan_request_id NUMBER(10) NOT NULL REFERENCES loan_requests(loan_request_id),
    approver_id NUMBER(10) NOT NULL REFERENCES users(user_id),
    approval_level NUMBER(1) NOT NULL CHECK (approval_level IN (1, 2)),
    status VARCHAR2(20) NOT NULL CHECK (status IN ('PENDING', 'APPROVED', 'REJECTED')),
    notes CLOB,
    approved_at TIMESTAMP
);

-- Create schedules table
CREATE TABLE schedules (
    schedule_id NUMBER(10) PRIMARY KEY,
    loan_request_id NUMBER(10) NOT NULL REFERENCES loan_requests(loan_request_id),
    driver_id NUMBER(10) REFERENCES drivers(driver_id),
    vehicle_id NUMBER(10) REFERENCES vehicles(vehicle_id),
    status VARCHAR2(20) DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'CONFIRMED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED')),
    assigned_at TIMESTAMP DEFAULT SYSTIMESTAMP,
    notes CLOB
);

-- Create indexes
CREATE INDEX idx_users_role ON users(role);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_division ON users(division);
CREATE INDEX idx_vehicles_status ON vehicles(status);
CREATE INDEX idx_vehicles_type ON vehicles(type);
CREATE INDEX idx_drivers_status ON drivers(status);
CREATE INDEX idx_drivers_user_id ON drivers(user_id);
CREATE INDEX idx_loan_requests_user_id ON loan_requests(user_id);
CREATE INDEX idx_loan_requests_status ON loan_requests(status);
CREATE INDEX idx_loan_requests_dates ON loan_requests(start_datetime, end_datetime);
CREATE INDEX idx_approvals_loan_request ON approvals(loan_request_id);
CREATE INDEX idx_approvals_approver ON approvals(approver_id);
CREATE INDEX idx_approvals_level_status ON approvals(approval_level, status);
CREATE INDEX idx_schedules_driver ON schedules(driver_id);
CREATE INDEX idx_schedules_vehicle ON schedules(vehicle_id);
CREATE INDEX idx_schedules_status ON schedules(status);

COMMIT;

-- Show created tables
SELECT table_name FROM user_tables ORDER BY table_name;

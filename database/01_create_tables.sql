-- ============================================================
-- PELINDO VEHICLE LOAN SYSTEM - ORACLE DATABASE DDL
-- ============================================================
-- Created: 2026-01-07
-- Description: Database schema for vehicle loan management system
-- ============================================================

-- ============================================================
-- DROP EXISTING OBJECTS (for clean installation)
-- ============================================================
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE schedules CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE approvals CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE loan_requests CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE drivers CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE vehicles CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE users CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP SEQUENCE seq_users';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP SEQUENCE seq_loan_requests';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP SEQUENCE seq_approvals';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP SEQUENCE seq_schedules';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP SEQUENCE seq_drivers';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

BEGIN
    EXECUTE IMMEDIATE 'DROP SEQUENCE seq_vehicles';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

-- ============================================================
-- CREATE SEQUENCES
-- ============================================================
CREATE SEQUENCE seq_users START WITH 1 INCREMENT BY 1 NOCACHE NOCYCLE;
CREATE SEQUENCE seq_loan_requests START WITH 1 INCREMENT BY 1 NOCACHE NOCYCLE;
CREATE SEQUENCE seq_approvals START WITH 1 INCREMENT BY 1 NOCACHE NOCYCLE;
CREATE SEQUENCE seq_schedules START WITH 1 INCREMENT BY 1 NOCACHE NOCYCLE;
CREATE SEQUENCE seq_drivers START WITH 1 INCREMENT BY 1 NOCACHE NOCYCLE;
CREATE SEQUENCE seq_vehicles START WITH 1 INCREMENT BY 1 NOCACHE NOCYCLE;

-- ============================================================
-- TABLE: USERS
-- Description: Stores all system users with their roles
-- ============================================================
CREATE TABLE users (
    id              NUMBER(10) PRIMARY KEY,
    name            VARCHAR2(100) NOT NULL,
    email           VARCHAR2(150) NOT NULL,
    password_hash   VARCHAR2(255) NOT NULL,
    role            VARCHAR2(50) NOT NULL,
    division        VARCHAR2(100),
    is_active       NUMBER(1) DEFAULT 1 NOT NULL,
    created_at      TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    updated_at      TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    
    CONSTRAINT uk_users_email UNIQUE (email),
    CONSTRAINT chk_users_role CHECK (role IN ('PEMOHON', 'PIC_APPROVAL_L1', 'PIC_APPROVAL_L2', 'DRIVER', 'ADMIN'))
);

COMMENT ON TABLE users IS 'Stores all system users including requesters, approvers, drivers, and admins';
COMMENT ON COLUMN users.role IS 'User role: PEMOHON, PIC_APPROVAL_L1, PIC_APPROVAL_L2, DRIVER, ADMIN';

-- ============================================================
-- TABLE: VEHICLES
-- Description: Stores vehicle inventory
-- ============================================================
CREATE TABLE vehicles (
    id              NUMBER(10) PRIMARY KEY,
    plate_number    VARCHAR2(20) NOT NULL,
    brand           VARCHAR2(50) NOT NULL,
    type            VARCHAR2(50) NOT NULL,
    capacity        NUMBER(3) DEFAULT 4,
    status          VARCHAR2(30) DEFAULT 'AVAILABLE' NOT NULL,
    notes           VARCHAR2(500),
    is_active       NUMBER(1) DEFAULT 1 NOT NULL,
    created_at      TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    updated_at      TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    
    CONSTRAINT uk_vehicles_plate UNIQUE (plate_number),
    CONSTRAINT chk_vehicles_status CHECK (status IN ('AVAILABLE', 'IN_USE', 'MAINTENANCE', 'RETIRED'))
);

COMMENT ON TABLE vehicles IS 'Stores vehicle inventory with status tracking';
COMMENT ON COLUMN vehicles.status IS 'Vehicle status: AVAILABLE, IN_USE, MAINTENANCE, RETIRED';

-- ============================================================
-- TABLE: DRIVERS
-- Description: Stores driver information
-- ============================================================
CREATE TABLE drivers (
    id              NUMBER(10) PRIMARY KEY,
    user_id         NUMBER(10),
    license_number  VARCHAR2(50) NOT NULL,
    license_expiry  DATE NOT NULL,
    phone_number    VARCHAR2(20),
    status          VARCHAR2(30) DEFAULT 'AVAILABLE' NOT NULL,
    is_active       NUMBER(1) DEFAULT 1 NOT NULL,
    created_at      TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    updated_at      TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    
    CONSTRAINT uk_drivers_license UNIQUE (license_number),
    CONSTRAINT fk_drivers_user FOREIGN KEY (user_id) REFERENCES users(id),
    CONSTRAINT chk_drivers_status CHECK (status IN ('AVAILABLE', 'ON_DUTY', 'OFF_DUTY', 'LEAVE'))
);

COMMENT ON TABLE drivers IS 'Stores driver information linked to user accounts';
COMMENT ON COLUMN drivers.status IS 'Driver status: AVAILABLE, ON_DUTY, OFF_DUTY, LEAVE';

-- ============================================================
-- TABLE: LOAN_REQUESTS
-- Description: Stores vehicle loan requests from users
-- ============================================================
CREATE TABLE loan_requests (
    id              NUMBER(10) PRIMARY KEY,
    user_id         NUMBER(10) NOT NULL,
    request_number  VARCHAR2(30) NOT NULL,
    service_letter_basis VARCHAR2(200) DEFAULT '' NOT NULL,
    purpose         VARCHAR2(500) NOT NULL,
    destination     VARCHAR2(255) NOT NULL,
    passenger_count NUMBER(3) DEFAULT 1,
    start_datetime  TIMESTAMP NOT NULL,
    end_datetime    TIMESTAMP NOT NULL,
    status          VARCHAR2(30) DEFAULT 'SUBMITTED' NOT NULL,
    notes           VARCHAR2(1000),
    created_at      TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    updated_at      TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    
    CONSTRAINT uk_loan_requests_number UNIQUE (request_number),
    CONSTRAINT fk_loan_requests_user FOREIGN KEY (user_id) REFERENCES users(id),
    CONSTRAINT chk_loan_requests_status CHECK (status IN (
        'SUBMITTED', 
        'APPROVED_L1', 
        'REJECTED_L1',
        'APPROVED_L2', 
        'REJECTED_L2',
        'SCHEDULED',
        'WAITING_RESOURCE',
        'IN_PROGRESS',
        'COMPLETED',
        'CANCELLED'
    )),
    CONSTRAINT chk_loan_requests_dates CHECK (end_datetime > start_datetime)
);

COMMENT ON TABLE loan_requests IS 'Stores vehicle loan requests from PEMOHON users';
COMMENT ON COLUMN loan_requests.service_letter_basis IS 'Service letter basis / SPPD number for the loan request';
COMMENT ON COLUMN loan_requests.status IS 'Request status: SUBMITTED, APPROVED_L1, REJECTED_L1, APPROVED_L2, REJECTED_L2, SCHEDULED, WAITING_RESOURCE, IN_PROGRESS, COMPLETED, CANCELLED';

-- ============================================================
-- TABLE: APPROVALS
-- Description: Stores approval history for loan requests
-- ============================================================
CREATE TABLE approvals (
    id                  NUMBER(10) PRIMARY KEY,
    loan_request_id     NUMBER(10) NOT NULL,
    approver_id         NUMBER(10) NOT NULL,
    approval_level      NUMBER(1) NOT NULL,
    status              VARCHAR2(20) NOT NULL,
    notes               VARCHAR2(500),
    approved_at         TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    
    CONSTRAINT fk_approvals_loan_request FOREIGN KEY (loan_request_id) REFERENCES loan_requests(id),
    CONSTRAINT fk_approvals_approver FOREIGN KEY (approver_id) REFERENCES users(id),
    CONSTRAINT chk_approvals_level CHECK (approval_level IN (1, 2)),
    CONSTRAINT chk_approvals_status CHECK (status IN ('APPROVED', 'REJECTED'))
);

COMMENT ON TABLE approvals IS 'Stores approval history with two-level approval workflow';
COMMENT ON COLUMN approvals.approval_level IS 'Approval level: 1 for L1, 2 for L2';

-- ============================================================
-- TABLE: SCHEDULES
-- Description: Stores assigned schedules for drivers and vehicles
-- ============================================================
CREATE TABLE schedules (
    id                  NUMBER(10) PRIMARY KEY,
    loan_request_id     NUMBER(10) NOT NULL,
    driver_id           NUMBER(10) NOT NULL,
    vehicle_id          NUMBER(10) NOT NULL,
    assigned_by         NUMBER(10),
    assigned_at         TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL,
    actual_start_time   TIMESTAMP,
    actual_end_time     TIMESTAMP,
    status              VARCHAR2(30) DEFAULT 'ASSIGNED' NOT NULL,
    notes               VARCHAR2(500),
    
    CONSTRAINT uk_schedules_loan_request UNIQUE (loan_request_id),
    CONSTRAINT fk_schedules_loan_request FOREIGN KEY (loan_request_id) REFERENCES loan_requests(id),
    CONSTRAINT fk_schedules_driver FOREIGN KEY (driver_id) REFERENCES drivers(id),
    CONSTRAINT fk_schedules_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(id),
    CONSTRAINT fk_schedules_assigned_by FOREIGN KEY (assigned_by) REFERENCES users(id),
    CONSTRAINT chk_schedules_status CHECK (status IN ('ASSIGNED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED'))
);

COMMENT ON TABLE schedules IS 'Stores assigned schedules linking loan requests with drivers and vehicles';

-- ============================================================
-- CREATE INDEXES FOR PERFORMANCE
-- ============================================================

-- Users indexes
CREATE INDEX idx_users_role ON users(role);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_division ON users(division);

-- Vehicles indexes
CREATE INDEX idx_vehicles_status ON vehicles(status);
CREATE INDEX idx_vehicles_type ON vehicles(type);

-- Drivers indexes
CREATE INDEX idx_drivers_status ON drivers(status);
CREATE INDEX idx_drivers_user_id ON drivers(user_id);

-- Loan requests indexes
CREATE INDEX idx_loan_requests_user_id ON loan_requests(user_id);
CREATE INDEX idx_loan_requests_status ON loan_requests(status);
CREATE INDEX idx_loan_requests_dates ON loan_requests(start_datetime, end_datetime);
CREATE INDEX idx_loan_requests_created ON loan_requests(created_at DESC);

-- Approvals indexes
CREATE INDEX idx_approvals_loan_request ON approvals(loan_request_id);
CREATE INDEX idx_approvals_approver ON approvals(approver_id);
CREATE INDEX idx_approvals_level_status ON approvals(approval_level, status);

-- Schedules indexes
CREATE INDEX idx_schedules_driver ON schedules(driver_id);
CREATE INDEX idx_schedules_vehicle ON schedules(vehicle_id);
CREATE INDEX idx_schedules_status ON schedules(status);
CREATE INDEX idx_schedules_assigned_at ON schedules(assigned_at DESC);

-- ============================================================
-- CREATE TRIGGERS FOR AUTO-INCREMENT AND AUDIT
-- ============================================================

-- Trigger for users auto-increment
CREATE OR REPLACE TRIGGER trg_users_bi
BEFORE INSERT ON users
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := seq_users.NEXTVAL;
    END IF;
    :NEW.created_at := SYSTIMESTAMP;
    :NEW.updated_at := SYSTIMESTAMP;
END;
/

-- Trigger for users update timestamp
CREATE OR REPLACE TRIGGER trg_users_bu
BEFORE UPDATE ON users
FOR EACH ROW
BEGIN
    :NEW.updated_at := SYSTIMESTAMP;
END;
/

-- Trigger for vehicles auto-increment
CREATE OR REPLACE TRIGGER trg_vehicles_bi
BEFORE INSERT ON vehicles
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := seq_vehicles.NEXTVAL;
    END IF;
    :NEW.created_at := SYSTIMESTAMP;
    :NEW.updated_at := SYSTIMESTAMP;
END;
/

-- Trigger for vehicles update timestamp
CREATE OR REPLACE TRIGGER trg_vehicles_bu
BEFORE UPDATE ON vehicles
FOR EACH ROW
BEGIN
    :NEW.updated_at := SYSTIMESTAMP;
END;
/

-- Trigger for drivers auto-increment
CREATE OR REPLACE TRIGGER trg_drivers_bi
BEFORE INSERT ON drivers
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := seq_drivers.NEXTVAL;
    END IF;
    :NEW.created_at := SYSTIMESTAMP;
    :NEW.updated_at := SYSTIMESTAMP;
END;
/

-- Trigger for drivers update timestamp
CREATE OR REPLACE TRIGGER trg_drivers_bu
BEFORE UPDATE ON drivers
FOR EACH ROW
BEGIN
    :NEW.updated_at := SYSTIMESTAMP;
END;
/

-- Trigger for loan_requests auto-increment and request number generation
CREATE OR REPLACE TRIGGER trg_loan_requests_bi
BEFORE INSERT ON loan_requests
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := seq_loan_requests.NEXTVAL;
    END IF;
    IF :NEW.request_number IS NULL THEN
        :NEW.request_number := 'LR-' || TO_CHAR(SYSDATE, 'YYYYMMDD') || '-' || LPAD(seq_loan_requests.CURRVAL, 6, '0');
    END IF;
    :NEW.created_at := SYSTIMESTAMP;
    :NEW.updated_at := SYSTIMESTAMP;
END;
/

-- Trigger for loan_requests update timestamp
CREATE OR REPLACE TRIGGER trg_loan_requests_bu
BEFORE UPDATE ON loan_requests
FOR EACH ROW
BEGIN
    :NEW.updated_at := SYSTIMESTAMP;
END;
/

-- Trigger for approvals auto-increment
CREATE OR REPLACE TRIGGER trg_approvals_bi
BEFORE INSERT ON approvals
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := seq_approvals.NEXTVAL;
    END IF;
    :NEW.approved_at := SYSTIMESTAMP;
END;
/

-- Trigger for schedules auto-increment
CREATE OR REPLACE TRIGGER trg_schedules_bi
BEFORE INSERT ON schedules
FOR EACH ROW
BEGIN
    IF :NEW.id IS NULL THEN
        :NEW.id := seq_schedules.NEXTVAL;
    END IF;
    :NEW.assigned_at := SYSTIMESTAMP;
END;
/

COMMIT;

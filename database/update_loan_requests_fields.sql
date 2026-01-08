-- ============================================================
-- UPDATE LOAN_REQUESTS TABLE - Replace PassengerCount with HotelAccommodation and GuestList
-- ============================================================
-- Created: 2026-01-07
-- Updated: 2026-01-07
-- Description: Add guest_list, hotel_accommodation, vehicle_id (required), driver_id (required)
-- ============================================================

-- Drop passenger_count column
ALTER TABLE loan_requests DROP COLUMN passenger_count;

-- Add guest_list column (required)
ALTER TABLE loan_requests 
ADD guest_list VARCHAR2(500) DEFAULT 'N/A' NOT NULL;

-- Add hotel_accommodation column (optional)
ALTER TABLE loan_requests 
ADD hotel_accommodation VARCHAR2(200);

-- Add vehicle_id column (required)
ALTER TABLE loan_requests 
ADD vehicle_id NUMBER(10) NOT NULL;

-- Add driver_id column (required)
ALTER TABLE loan_requests 
ADD driver_id NUMBER(10) NOT NULL;

-- Add foreign key constraints
ALTER TABLE loan_requests 
ADD CONSTRAINT fk_loan_requests_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id);

ALTER TABLE loan_requests 
ADD CONSTRAINT fk_loan_requests_driver FOREIGN KEY (driver_id) REFERENCES drivers(driver_id);

-- Add comments
COMMENT ON COLUMN loan_requests.guest_list IS 'List of guests being served (required)';
COMMENT ON COLUMN loan_requests.hotel_accommodation IS 'Hotel accommodation details (optional)';
COMMENT ON COLUMN loan_requests.vehicle_id IS 'Requested vehicle ID (required)';
COMMENT ON COLUMN loan_requests.driver_id IS 'Requested driver ID (required)';

-- Commit changes
COMMIT;

-- Verify the changes
SELECT column_name, data_type, nullable 
FROM user_tab_columns 
WHERE table_name = 'LOAN_REQUESTS' 
  AND column_name IN ('GUEST_LIST', 'HOTEL_ACCOMMODATION', 'VEHICLE_ID', 'DRIVER_ID')
ORDER BY column_name;

SELECT 'SUCCESS: Schema updated!' AS status FROM DUAL;

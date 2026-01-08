-- Allow NULL for vehicle_id and driver_id in loan_requests table
-- This enables users to submit without selecting resources (will be assigned by approver)

ALTER TABLE loan_requests MODIFY vehicle_id NUMBER(10) NULL;
ALTER TABLE loan_requests MODIFY driver_id NUMBER(10) NULL;

COMMIT;

-- Verify the changes
SELECT column_name, nullable 
FROM user_tab_columns 
WHERE table_name = 'LOAN_REQUESTS' 
AND column_name IN ('VEHICLE_ID', 'DRIVER_ID');

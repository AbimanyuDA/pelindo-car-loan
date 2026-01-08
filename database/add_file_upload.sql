-- Add service_letter_file_path column to store uploaded document
-- Also update vehicle_id and driver_id to allow assignment during approval

ALTER TABLE loan_requests ADD service_letter_file_path VARCHAR2(500);

COMMIT;

-- Verify changes
SELECT column_name, data_type, nullable, data_length
FROM user_tab_columns 
WHERE table_name = 'LOAN_REQUESTS' 
AND column_name IN ('VEHICLE_ID', 'DRIVER_ID', 'SERVICE_LETTER_FILE_PATH');

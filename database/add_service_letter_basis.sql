-- ============================================================
-- ADD SERVICE LETTER BASIS COLUMN TO LOAN_REQUESTS TABLE
-- ============================================================
-- Created: 2026-01-07
-- Description: Adds service_letter_basis column for SPPD number
-- ============================================================

-- Add the new column
ALTER TABLE loan_requests 
ADD service_letter_basis VARCHAR2(200) DEFAULT ' ' NOT NULL;

-- Add comment to the column
COMMENT ON COLUMN loan_requests.service_letter_basis IS 'Service letter basis / SPPD number for the loan request';

-- Commit changes
COMMIT;

-- Verify the change
SELECT column_name, data_type, data_length, nullable, data_default
FROM user_tab_columns
WHERE table_name = 'LOAN_REQUESTS'
ORDER BY column_id;

-- Success message
SELECT 'Column service_letter_basis added successfully!' AS status FROM DUAL;

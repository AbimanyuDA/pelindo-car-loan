-- Add notes column to loan_requests table
ALTER TABLE loan_requests ADD notes VARCHAR2(1000);

COMMENT ON COLUMN loan_requests.notes IS 'Notes from the requester (pemohon)';

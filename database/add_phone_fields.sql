-- Add phone field to users table
ALTER TABLE users ADD phone_number VARCHAR2(20);

-- Update some sample phone numbers for testing (optional)
UPDATE users SET phone_number = '081234567890' WHERE role = 'PEMOHON';
UPDATE users SET phone_number = '081234567891' WHERE role = 'DRIVER';
UPDATE users SET phone_number = '081234567892' WHERE role = 'PIC_APPROVAL_L1';
UPDATE users SET phone_number = '081234567893' WHERE role = 'PIC_APPROVAL_L2';

COMMIT;

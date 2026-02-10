-- Update SCHEDULES constraint to include WAITING_DRIVER and EMERGENCY status

-- First, find and drop the existing CHECK constraint
BEGIN
  FOR c IN (SELECT constraint_name 
            FROM user_constraints 
            WHERE table_name = 'SCHEDULES' 
            AND constraint_type = 'C' 
            AND search_condition LIKE '%PENDING%CONFIRMED%')
  LOOP
    EXECUTE IMMEDIATE 'ALTER TABLE SCHEDULES DROP CONSTRAINT ' || c.constraint_name;
  END LOOP;
END;
/

-- Add the new CHECK constraint with updated statuses
ALTER TABLE SCHEDULES ADD CHECK (status IN ('PENDING', 'CONFIRMED', 'WAITING_DRIVER', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED', 'EMERGENCY')) ENABLE;

COMMIT;

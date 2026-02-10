-- Fix SCHEDULES STATUS constraint
-- This script updates the CHECK constraint to include new status values

-- Drop existing constraint (the name SYS_C008432 might be different in your DB)
DECLARE
  v_constraint_name VARCHAR2(30);
BEGIN
  SELECT constraint_name INTO v_constraint_name
  FROM user_constraints
  WHERE table_name = 'SCHEDULES'
    AND constraint_type = 'C'
    AND search_condition LIKE '%PENDING%CONFIRMED%IN_PROGRESS%'
    AND ROWNUM = 1;
  
  EXECUTE IMMEDIATE 'ALTER TABLE SCHEDULES DROP CONSTRAINT ' || v_constraint_name;
  DBMS_OUTPUT.PUT_LINE('Dropped constraint: ' || v_constraint_name);
EXCEPTION
  WHEN NO_DATA_FOUND THEN
    DBMS_OUTPUT.PUT_LINE('Constraint not found, will add new one');
  WHEN OTHERS THEN
    DBMS_OUTPUT.PUT_LINE('Error: ' || SQLERRM);
END;
/

-- Add new constraint with all required statuses
ALTER TABLE SCHEDULES ADD CHECK (status IN ('PENDING', 'CONFIRMED', 'WAITING_DRIVER', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED', 'EMERGENCY')) ENABLE;

COMMIT;

SELECT constraint_name, search_condition 
FROM user_constraints 
WHERE table_name = 'SCHEDULES' 
  AND constraint_type = 'C';

-- ============================================================
-- CHECK LOAN_REQUESTS SCHEMA
-- ============================================================
-- Script untuk mengecek apakah migration sudah dijalankan
-- ============================================================

SET SERVEROUTPUT ON;

PROMPT ============================================================
PROMPT CHECKING LOAN_REQUESTS TABLE SCHEMA
PROMPT ============================================================

-- Check if table exists
SELECT 'Table LOAN_REQUESTS exists: YES' AS status FROM dual
WHERE EXISTS (SELECT 1 FROM user_tables WHERE table_name = 'LOAN_REQUESTS');

PROMPT 
PROMPT Checking for required columns:
PROMPT 

-- Check all columns
SELECT 
    column_name, 
    data_type || 
    CASE 
        WHEN data_type IN ('VARCHAR2', 'CHAR') THEN '(' || data_length || ')'
        WHEN data_type = 'NUMBER' THEN 
            CASE WHEN data_precision IS NOT NULL 
            THEN '(' || data_precision || ')'
            ELSE '' END
        ELSE ''
    END AS data_type_full,
    CASE WHEN nullable = 'Y' THEN 'NULL' ELSE 'NOT NULL' END AS nullable_status
FROM user_tab_columns 
WHERE table_name = 'LOAN_REQUESTS'
ORDER BY column_id;

PROMPT 
PROMPT ============================================================
PROMPT Checking specific NEW columns:
PROMPT ============================================================

-- Check for GUEST_LIST column
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM user_tab_columns
    WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'GUEST_LIST';
    
    IF v_count > 0 THEN
        DBMS_OUTPUT.PUT_LINE('✓ Column GUEST_LIST: EXISTS');
    ELSE
        DBMS_OUTPUT.PUT_LINE('✗ Column GUEST_LIST: MISSING - Migration needed!');
    END IF;
END;
/

-- Check for HOTEL_ACCOMMODATION column
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM user_tab_columns
    WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'HOTEL_ACCOMMODATION';
    
    IF v_count > 0 THEN
        DBMS_OUTPUT.PUT_LINE('✓ Column HOTEL_ACCOMMODATION: EXISTS');
    ELSE
        DBMS_OUTPUT.PUT_LINE('✗ Column HOTEL_ACCOMMODATION: MISSING - Migration needed!');
    END IF;
END;
/

-- Check for VEHICLE_ID column
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM user_tab_columns
    WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'VEHICLE_ID';
    
    IF v_count > 0 THEN
        DBMS_OUTPUT.PUT_LINE('✓ Column VEHICLE_ID: EXISTS');
    ELSE
        DBMS_OUTPUT.PUT_LINE('✗ Column VEHICLE_ID: MISSING - Migration needed!');
    END IF;
END;
/

-- Check for DRIVER_ID column
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM user_tab_columns
    WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'DRIVER_ID';
    
    IF v_count > 0 THEN
        DBMS_OUTPUT.PUT_LINE('✓ Column DRIVER_ID: EXISTS');
    ELSE
        DBMS_OUTPUT.PUT_LINE('✗ Column DRIVER_ID: MISSING - Migration needed!');
    END IF;
END;
/

-- Check for OLD column (should NOT exist)
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM user_tab_columns
    WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'PASSENGER_COUNT';
    
    IF v_count = 0 THEN
        DBMS_OUTPUT.PUT_LINE('✓ Column PASSENGER_COUNT: REMOVED (correct)');
    ELSE
        DBMS_OUTPUT.PUT_LINE('✗ Column PASSENGER_COUNT: STILL EXISTS - Migration needed!');
    END IF;
END;
/

PROMPT 
PROMPT ============================================================
PROMPT Checking Foreign Key Constraints:
PROMPT ============================================================

SELECT 
    constraint_name,
    constraint_type,
    r_constraint_name,
    delete_rule,
    status
FROM user_constraints
WHERE table_name = 'LOAN_REQUESTS'
  AND constraint_type = 'R'
ORDER BY constraint_name;

PROMPT 
PROMPT ============================================================
PROMPT SUMMARY
PROMPT ============================================================

-- Final check
DECLARE
    v_guest_list NUMBER;
    v_hotel NUMBER;
    v_vehicle NUMBER;
    v_driver NUMBER;
    v_passenger NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_guest_list FROM user_tab_columns WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'GUEST_LIST';
    SELECT COUNT(*) INTO v_hotel FROM user_tab_columns WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'HOTEL_ACCOMMODATION';
    SELECT COUNT(*) INTO v_vehicle FROM user_tab_columns WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'VEHICLE_ID';
    SELECT COUNT(*) INTO v_driver FROM user_tab_columns WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'DRIVER_ID';
    SELECT COUNT(*) INTO v_passenger FROM user_tab_columns WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'PASSENGER_COUNT';
    
    IF v_guest_list = 1 AND v_hotel = 1 AND v_vehicle = 1 AND v_driver = 1 AND v_passenger = 0 THEN
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('★★★ MIGRATION STATUS: COMPLETED ★★★');
        DBMS_OUTPUT.PUT_LINE('All required columns exist. Database is ready!');
    ELSE
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('✗✗✗ MIGRATION STATUS: INCOMPLETE ✗✗✗');
        DBMS_OUTPUT.PUT_LINE('Please run: database/update_loan_requests_fields.sql');
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('Missing columns:');
        IF v_guest_list = 0 THEN DBMS_OUTPUT.PUT_LINE('  - GUEST_LIST'); END IF;
        IF v_hotel = 0 THEN DBMS_OUTPUT.PUT_LINE('  - HOTEL_ACCOMMODATION'); END IF;
        IF v_vehicle = 0 THEN DBMS_OUTPUT.PUT_LINE('  - VEHICLE_ID'); END IF;
        IF v_driver = 0 THEN DBMS_OUTPUT.PUT_LINE('  - DRIVER_ID'); END IF;
        IF v_passenger = 1 THEN DBMS_OUTPUT.PUT_LINE('  - PASSENGER_COUNT (should be removed)'); END IF;
    END IF;
END;
/

PROMPT ============================================================

-- Simple script to check if migration has been executed
-- Run this in SQLPlus or SQL Developer to verify

SET SERVEROUTPUT ON;
DECLARE
    v_count NUMBER;
    v_guest_list_exists NUMBER := 0;
    v_passenger_count_exists NUMBER := 0;
    v_vehicle_id_exists NUMBER := 0;
    v_driver_id_exists NUMBER := 0;
    v_hotel_exists NUMBER := 0;
BEGIN
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('CHECKING LOAN_REQUESTS TABLE SCHEMA');
    DBMS_OUTPUT.PUT_LINE('========================================');
    DBMS_OUTPUT.PUT_LINE('');
    
    -- Check for GUEST_LIST column (should exist)
    SELECT COUNT(*) INTO v_guest_list_exists
    FROM user_tab_columns
    WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'GUEST_LIST';
    
    IF v_guest_list_exists > 0 THEN
        DBMS_OUTPUT.PUT_LINE('✅ GUEST_LIST column exists');
    ELSE
        DBMS_OUTPUT.PUT_LINE('❌ GUEST_LIST column MISSING');
    END IF;
    
    -- Check for HOTEL_ACCOMMODATION column (should exist)
    SELECT COUNT(*) INTO v_hotel_exists
    FROM user_tab_columns
    WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'HOTEL_ACCOMMODATION';
    
    IF v_hotel_exists > 0 THEN
        DBMS_OUTPUT.PUT_LINE('✅ HOTEL_ACCOMMODATION column exists');
    ELSE
        DBMS_OUTPUT.PUT_LINE('❌ HOTEL_ACCOMMODATION column MISSING');
    END IF;
    
    -- Check for VEHICLE_ID column (should exist)
    SELECT COUNT(*) INTO v_vehicle_id_exists
    FROM user_tab_columns
    WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'VEHICLE_ID';
    
    IF v_vehicle_id_exists > 0 THEN
        DBMS_OUTPUT.PUT_LINE('✅ VEHICLE_ID column exists');
    ELSE
        DBMS_OUTPUT.PUT_LINE('❌ VEHICLE_ID column MISSING');
    END IF;
    
    -- Check for DRIVER_ID column (should exist)
    SELECT COUNT(*) INTO v_driver_id_exists
    FROM user_tab_columns
    WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'DRIVER_ID';
    
    IF v_driver_id_exists > 0 THEN
        DBMS_OUTPUT.PUT_LINE('✅ DRIVER_ID column exists');
    ELSE
        DBMS_OUTPUT.PUT_LINE('❌ DRIVER_ID column MISSING');
    END IF;
    
    -- Check for PASSENGER_COUNT column (should NOT exist)
    SELECT COUNT(*) INTO v_passenger_count_exists
    FROM user_tab_columns
    WHERE table_name = 'LOAN_REQUESTS' AND column_name = 'PASSENGER_COUNT';
    
    IF v_passenger_count_exists = 0 THEN
        DBMS_OUTPUT.PUT_LINE('✅ PASSENGER_COUNT column removed (correct)');
    ELSE
        DBMS_OUTPUT.PUT_LINE('❌ PASSENGER_COUNT column still exists (should be deleted)');
    END IF;
    
    DBMS_OUTPUT.PUT_LINE('');
    DBMS_OUTPUT.PUT_LINE('========================================');
    
    -- Summary
    IF v_guest_list_exists > 0 AND v_hotel_exists > 0 AND 
       v_vehicle_id_exists > 0 AND v_driver_id_exists > 0 AND 
       v_passenger_count_exists = 0 THEN
        DBMS_OUTPUT.PUT_LINE('✅ MIGRATION COMPLETED SUCCESSFULLY!');
        DBMS_OUTPUT.PUT_LINE('All new columns exist and old column removed.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('❌ MIGRATION NOT COMPLETE!');
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('You MUST run this migration script:');
        DBMS_OUTPUT.PUT_LINE('database/update_loan_requests_fields.sql');
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('Using SQL Developer:');
        DBMS_OUTPUT.PUT_LINE('1. Open file: database/update_loan_requests_fields.sql');
        DBMS_OUTPUT.PUT_LINE('2. Click "Run Script" (F5)');
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('Or using SQLPlus:');
        DBMS_OUTPUT.PUT_LINE('sqlplus system/bima2005@localhost:1521/XE @database\update_loan_requests_fields.sql');
    END IF;
    
    DBMS_OUTPUT.PUT_LINE('========================================');
END;
/

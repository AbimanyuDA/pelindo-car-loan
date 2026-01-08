-- ============================================================
-- PELINDO CAR LOAN - Database Cleanup Script
-- ============================================================
SET SERVEROUTPUT ON SIZE 1000000
SET LINESIZE 200
SET PAGESIZE 1000

PROMPT ============================================================
PROMPT Checking current database objects...
PROMPT ============================================================
PROMPT

PROMPT Current Tables:
PROMPT ----------------
SELECT 
    table_name,
    CASE 
        WHEN table_name IN ('USERS', 'DRIVERS', 'VEHICLES', 'LOAN_REQUESTS', 'APPROVALS', 'SCHEDULES')
        THEN '[KEEP]'
        ELSE '[DELETE]'
    END AS status
FROM user_tables 
ORDER BY table_name;

PROMPT
PROMPT Current Sequences:
PROMPT ------------------
SELECT 
    sequence_name,
    CASE 
        WHEN sequence_name IN ('SEQ_USERS', 'SEQ_DRIVERS', 'SEQ_VEHICLES', 'SEQ_LOAN_REQUESTS', 'SEQ_APPROVALS', 'SEQ_SCHEDULES')
        THEN '[KEEP]'
        ELSE '[DELETE]'
    END AS status
FROM user_sequences 
ORDER BY sequence_name;

PROMPT
PROMPT ============================================================
PROMPT Starting cleanup...
PROMPT ============================================================

-- Drop unused tables
DECLARE
    v_count NUMBER := 0;
BEGIN
    FOR rec IN (
        SELECT table_name 
        FROM user_tables 
        WHERE table_name NOT IN ('USERS', 'DRIVERS', 'VEHICLES', 'LOAN_REQUESTS', 'APPROVALS', 'SCHEDULES')
    ) LOOP
        BEGIN
            EXECUTE IMMEDIATE 'DROP TABLE ' || rec.table_name || ' CASCADE CONSTRAINTS';
            DBMS_OUTPUT.PUT_LINE('✓ Dropped table: ' || rec.table_name);
            v_count := v_count + 1;
        EXCEPTION
            WHEN OTHERS THEN
                DBMS_OUTPUT.PUT_LINE('✗ Failed to drop table ' || rec.table_name || ': ' || SQLERRM);
        END;
    END LOOP;
    
    IF v_count = 0 THEN
        DBMS_OUTPUT.PUT_LINE('No unused tables found.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('Total tables dropped: ' || v_count);
    END IF;
END;
/

PROMPT

-- Drop unused sequences
DECLARE
    v_count NUMBER := 0;
BEGIN
    FOR rec IN (
        SELECT sequence_name 
        FROM user_sequences 
        WHERE sequence_name NOT IN ('SEQ_USERS', 'SEQ_DRIVERS', 'SEQ_VEHICLES', 'SEQ_LOAN_REQUESTS', 'SEQ_APPROVALS', 'SEQ_SCHEDULES')
    ) LOOP
        BEGIN
            EXECUTE IMMEDIATE 'DROP SEQUENCE ' || rec.sequence_name;
            DBMS_OUTPUT.PUT_LINE('✓ Dropped sequence: ' || rec.sequence_name);
            v_count := v_count + 1;
        EXCEPTION
            WHEN OTHERS THEN
                DBMS_OUTPUT.PUT_LINE('✗ Failed to drop sequence ' || rec.sequence_name || ': ' || SQLERRM);
        END;
    END LOOP;
    
    IF v_count = 0 THEN
        DBMS_OUTPUT.PUT_LINE('No unused sequences found.');
    ELSE
        DBMS_OUTPUT.PUT_LINE('');
        DBMS_OUTPUT.PUT_LINE('Total sequences dropped: ' || v_count);
    END IF;
END;
/

PROMPT
PROMPT ============================================================
PROMPT Cleanup completed!
PROMPT ============================================================
PROMPT
PROMPT Remaining Tables:
PROMPT -----------------
SELECT table_name FROM user_tables ORDER BY table_name;

PROMPT
PROMPT Remaining Sequences:
PROMPT --------------------
SELECT sequence_name FROM user_sequences ORDER BY sequence_name;

COMMIT;
EXIT;

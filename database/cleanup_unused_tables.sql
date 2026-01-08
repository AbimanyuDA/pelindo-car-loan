-- ============================================================
-- CLEANUP UNUSED TABLES - PELINDO CAR LOAN SYSTEM
-- ============================================================
-- Script untuk menghapus tabel yang tidak digunakan dalam project
-- ============================================================

-- List semua tabel yang ada
SELECT 'Current tables in database:' AS info FROM dual;
SELECT table_name FROM user_tables ORDER BY table_name;

-- List semua sequences yang ada
SELECT 'Current sequences in database:' AS info FROM dual;
SELECT sequence_name FROM user_sequences ORDER BY sequence_name;

-- ============================================================
-- Tabel yang HARUS ADA (jangan dihapus):
-- 1. USERS
-- 2. DRIVERS
-- 3. VEHICLES
-- 4. LOAN_REQUESTS
-- 5. APPROVALS
-- 6. SCHEDULES
--
-- Sequences yang HARUS ADA:
-- 1. SEQ_USERS
-- 2. SEQ_DRIVERS
-- 3. SEQ_VEHICLES
-- 4. SEQ_LOAN_REQUESTS
-- 5. SEQ_APPROVALS
-- 6. SEQ_SCHEDULES
-- ============================================================

-- Uncomment baris berikut untuk menghapus tabel yang tidak digunakan
-- Contoh: DROP TABLE nama_tabel CASCADE CONSTRAINTS;

-- Untuk menjalankan cleanup otomatis, jalankan query berikut:
/*
DECLARE
    v_table_name VARCHAR2(100);
    CURSOR c_unused_tables IS
        SELECT table_name 
        FROM user_tables 
        WHERE table_name NOT IN (
            'USERS', 
            'DRIVERS', 
            'VEHICLES', 
            'LOAN_REQUESTS', 
            'APPROVALS', 
            'SCHEDULES'
        )
        ORDER BY table_name;
BEGIN
    OPEN c_unused_tables;
    LOOP
        FETCH c_unused_tables INTO v_table_name;
        EXIT WHEN c_unused_tables%NOTFOUND;
        
        DBMS_OUTPUT.PUT_LINE('Dropping table: ' || v_table_name);
        EXECUTE IMMEDIATE 'DROP TABLE ' || v_table_name || ' CASCADE CONSTRAINTS';
    END LOOP;
    CLOSE c_unused_tables;
    
    DBMS_OUTPUT.PUT_LINE('Cleanup completed!');
END;
/
*/

-- Untuk menghapus sequences yang tidak digunakan:
/*
DECLARE
    v_seq_name VARCHAR2(100);
    CURSOR c_unused_seqs IS
        SELECT sequence_name 
        FROM user_sequences 
        WHERE sequence_name NOT IN (
            'SEQ_USERS', 
            'SEQ_DRIVERS', 
            'SEQ_VEHICLES', 
            'SEQ_LOAN_REQUESTS', 
            'SEQ_APPROVALS', 
            'SEQ_SCHEDULES'
        )
        ORDER BY sequence_name;
BEGIN
    OPEN c_unused_seqs;
    LOOP
        FETCH c_unused_seqs INTO v_seq_name;
        EXIT WHEN c_unused_seqs%NOTFOUND;
        
        DBMS_OUTPUT.PUT_LINE('Dropping sequence: ' || v_seq_name);
        EXECUTE IMMEDIATE 'DROP SEQUENCE ' || v_seq_name;
    END LOOP;
    CLOSE c_unused_seqs;
    
    DBMS_OUTPUT.PUT_LINE('Sequence cleanup completed!');
END;
/
*/

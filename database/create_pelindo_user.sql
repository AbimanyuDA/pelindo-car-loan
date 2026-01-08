-- ============================================================
-- CREATE PELINDO USER - Dedicated Schema for Pelindo Car Loan
-- ============================================================
-- Membuat user khusus untuk aplikasi agar database lebih bersih
-- tanpa tabel-tabel sistem Oracle
-- ============================================================

-- Drop user jika sudah ada (akan menghapus semua objek milik user)
BEGIN
    EXECUTE IMMEDIATE 'DROP USER pelindo CASCADE';
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -1918 THEN
            RAISE;
        END IF;
END;
/

-- Create user baru
CREATE USER pelindo IDENTIFIED BY pelindo2005
    DEFAULT TABLESPACE USERS
    TEMPORARY TABLESPACE TEMP
    QUOTA UNLIMITED ON USERS;

-- Grant privileges
GRANT CONNECT, RESOURCE TO pelindo;
GRANT CREATE SESSION TO pelindo;
GRANT CREATE TABLE TO pelindo;
GRANT CREATE VIEW TO pelindo;
GRANT CREATE SEQUENCE TO pelindo;
GRANT CREATE TRIGGER TO pelindo;
GRANT CREATE PROCEDURE TO pelindo;

-- Additional privileges untuk development
GRANT SELECT ANY TABLE TO pelindo;
GRANT INSERT ANY TABLE TO pelindo;
GRANT UPDATE ANY TABLE TO pelindo;
GRANT DELETE ANY TABLE TO pelindo;

COMMIT;

PROMPT ============================================================
PROMPT User 'pelindo' created successfully!
PROMPT Password: pelindo2005
PROMPT ============================================================
PROMPT
PROMPT Next steps:
PROMPT 1. Connect as pelindo user: sqlplus pelindo/pelindo2005@localhost:1521/XE
PROMPT 2. Run: @01_create_tables.sql
PROMPT 3. Run: @02_seed_data.sql
PROMPT 4. Update connection string in appsettings.json to use pelindo user
PROMPT ============================================================

EXIT;

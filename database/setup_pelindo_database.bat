@echo off
REM ============================================================
REM PELINDO CAR LOAN - Setup New Database Schema
REM ============================================================
echo.
echo ============================================================
echo PELINDO CAR LOAN - Database Schema Setup
echo ============================================================
echo.
echo This script will:
echo 1. Create dedicated 'pelindo' user/schema
echo 2. Create all tables and sequences
echo 3. Insert seed data
echo.
echo Current connection: SYSTEM/bima2005
echo New user will be: pelindo/pelindo2005
echo.
pause

echo.
echo Step 1: Creating pelindo user...
echo ============================================================
sqlplus -S system/bima2005@localhost:1521/XE @create_pelindo_user.sql

echo.
echo Step 2: Creating tables and sequences...
echo ============================================================
sqlplus -S pelindo/pelindo2005@localhost:1521/XE @01_create_tables.sql

echo.
echo Step 3: Inserting seed data...
echo ============================================================
sqlplus -S pelindo/pelindo2005@localhost:1521/XE @02_seed_data.sql

echo.
echo ============================================================
echo Setup completed successfully!
echo ============================================================
echo.
echo Database connection info:
echo   Host: localhost
echo   Port: 1521
echo   Service: XE
echo   Username: pelindo
echo   Password: pelindo2005
echo.
echo Connection string for appsettings.json:
echo Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)));User Id=pelindo;Password=pelindo2005;
echo.
echo Don't forget to update your appsettings.json!
echo.
pause

@echo off
REM ============================================================
REM PELINDO CAR LOAN - Database Cleanup Script
REM ============================================================

echo ============================================================
echo PELINDO CAR LOAN - DATABASE CLEANUP UTILITY
echo ============================================================
echo.
echo This script will:
echo 1. List all tables and sequences in the database
echo 2. Identify unused tables/sequences
echo 3. Drop unused objects (with confirmation)
echo.
echo Required tables: USERS, DRIVERS, VEHICLES, LOAN_REQUESTS, APPROVALS, SCHEDULES
echo Required sequences: SEQ_USERS, SEQ_DRIVERS, SEQ_VEHICLES, SEQ_LOAN_REQUESTS, SEQ_APPROVALS, SEQ_SCHEDULES
echo.
pause

sqlplus -S system/bima2005@localhost:1521/XE @cleanup_db_script.sql

echo.
echo Cleanup process completed!
pause

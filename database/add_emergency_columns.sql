-- Add emergency_reason and emergency_type columns to SCHEDULES table
ALTER TABLE SCHEDULES ADD emergency_reason VARCHAR2(1000);
ALTER TABLE SCHEDULES ADD emergency_type VARCHAR2(20);

-- Commit changes
COMMIT;

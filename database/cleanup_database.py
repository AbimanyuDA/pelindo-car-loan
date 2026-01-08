"""
Script untuk membersihkan tabel dan sequence yang tidak digunakan
di database Oracle Pelindo Car Loan System
"""
import cx_Oracle

# Database connection details
DSN = cx_Oracle.makedsn('localhost', 1521, service_name='XE')
USERNAME = 'system'
PASSWORD = 'bima2005'

# Tabel yang HARUS DIPERTAHANKAN
REQUIRED_TABLES = {
    'USERS',
    'DRIVERS', 
    'VEHICLES',
    'LOAN_REQUESTS',
    'APPROVALS',
    'SCHEDULES'
}

# Sequence yang HARUS DIPERTAHANKAN  
REQUIRED_SEQUENCES = {
    'SEQ_USERS',
    'SEQ_DRIVERS',
    'SEQ_VEHICLES', 
    'SEQ_LOAN_REQUESTS',
    'SEQ_APPROVALS',
    'SEQ_SCHEDULES'
}

def cleanup_database():
    try:
        # Connect to database
        connection = cx_Oracle.connect(USERNAME, PASSWORD, DSN)
        cursor = connection.cursor()
        
        print("=" * 60)
        print("PELINDO CAR LOAN - DATABASE CLEANUP UTILITY")
        print("=" * 60)
        print()
        
        # Get all existing tables
        cursor.execute("SELECT table_name FROM user_tables ORDER BY table_name")
        all_tables = [row[0] for row in cursor.fetchall()]
        
        print(f"📊 Total tables found: {len(all_tables)}")
        print("Tables in database:")
        for table in all_tables:
            status = "✅ KEEP" if table in REQUIRED_TABLES else "❌ DELETE"
            print(f"  - {table} [{status}]")
        print()
        
        # Get all existing sequences
        cursor.execute("SELECT sequence_name FROM user_sequences ORDER BY sequence_name")
        all_sequences = [row[0] for row in cursor.fetchall()]
        
        print(f"🔢 Total sequences found: {len(all_sequences)}")
        print("Sequences in database:")
        for seq in all_sequences:
            status = "✅ KEEP" if seq in REQUIRED_SEQUENCES else "❌ DELETE"
            print(f"  - {seq} [{status}]")
        print()
        
        # Find tables to delete
        tables_to_delete = [t for t in all_tables if t not in REQUIRED_TABLES]
        sequences_to_delete = [s for s in all_sequences if s not in REQUIRED_SEQUENCES]
        
        if not tables_to_delete and not sequences_to_delete:
            print("✨ Database is clean! No unused tables or sequences found.")
            return
        
        # Confirm deletion
        print("⚠️  WARNING: The following objects will be DELETED:")
        if tables_to_delete:
            print(f"\nTables to delete ({len(tables_to_delete)}):")
            for table in tables_to_delete:
                print(f"  - {table}")
        
        if sequences_to_delete:
            print(f"\nSequences to delete ({len(sequences_to_delete)}):")
            for seq in sequences_to_delete:
                print(f"  - {seq}")
        
        print()
        confirm = input("Do you want to proceed with deletion? (yes/no): ").strip().lower()
        
        if confirm != 'yes':
            print("❌ Cleanup cancelled by user.")
            return
        
        print()
        print("🗑️  Starting cleanup...")
        print()
        
        # Delete unused tables
        deleted_tables = 0
        for table in tables_to_delete:
            try:
                cursor.execute(f"DROP TABLE {table} CASCADE CONSTRAINTS")
                print(f"✅ Dropped table: {table}")
                deleted_tables += 1
            except Exception as e:
                print(f"❌ Failed to drop table {table}: {e}")
        
        # Delete unused sequences
        deleted_sequences = 0
        for seq in sequences_to_delete:
            try:
                cursor.execute(f"DROP SEQUENCE {seq}")
                print(f"✅ Dropped sequence: {seq}")
                deleted_sequences += 1
            except Exception as e:
                print(f"❌ Failed to drop sequence {seq}: {e}")
        
        # Commit changes
        connection.commit()
        
        print()
        print("=" * 60)
        print("✨ CLEANUP COMPLETED!")
        print(f"   Tables deleted: {deleted_tables}")
        print(f"   Sequences deleted: {deleted_sequences}")
        print("=" * 60)
        
    except cx_Oracle.DatabaseError as e:
        error, = e.args
        print(f"❌ Database Error: {error.message}")
    except Exception as e:
        print(f"❌ Error: {e}")
    finally:
        if cursor:
            cursor.close()
        if connection:
            connection.close()

if __name__ == "__main__":
    try:
        cleanup_database()
    except KeyboardInterrupt:
        print("\n❌ Cleanup interrupted by user.")

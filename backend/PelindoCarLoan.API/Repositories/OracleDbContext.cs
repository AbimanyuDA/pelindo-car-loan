using Oracle.ManagedDataAccess.Client;
using System.Data;
using Dapper;

namespace PelindoCarLoan.API.Repositories
{
    /// <summary>
    /// Database context for Oracle connection management
    /// </summary>
    public interface IDbContext
    {
        IDbConnection CreateConnection();
        Task InitializeDatabaseAsync();
    }

    public class OracleDbContext : IDbContext
    {
        private readonly string _connectionString;

        public OracleDbContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleConnection") 
                ?? throw new ArgumentNullException("OracleConnection connection string is not configured");
        }

        public IDbConnection CreateConnection()
        {
            return new OracleConnection(_connectionString);
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                using var connection = (OracleConnection)CreateConnection();
                await connection.OpenAsync();
                
                // Get all check constraints on SCHEDULES table to find the old one
                var checkConstraints = new List<string>();
                var findConstraintSql = @"
                    SELECT constraint_name FROM user_constraints 
                    WHERE table_name = 'SCHEDULES' AND constraint_type = 'C'
                ";
                
                using (var cmd = new OracleCommand(findConstraintSql, connection))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            checkConstraints.Add(reader.GetString(0));
                        }
                    }
                }
                
                Console.WriteLine($"[INFO] Found {checkConstraints.Count} check constraints on SCHEDULES table");
                
                // Drop all old check constraints
                foreach (var constraintName in checkConstraints)
                {
                    try
                    {
                        var dropSql = $"ALTER TABLE SCHEDULES DROP CONSTRAINT {constraintName}";
                        using (var cmd = new OracleCommand(dropSql, connection))
                        {
                            await cmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"[INFO] Dropped constraint: {constraintName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WARN] Could not drop {constraintName}: {ex.Message}");
                        throw; // Re-throw to prevent adding new constraint with old one still present
                    }
                }
                
                // Add the new constraint with all required status values
                // Using CHK_SCHEDULE_STATUS as constraint name to match database naming convention
                var createConstraintSql = @"
                    ALTER TABLE SCHEDULES ADD CONSTRAINT CHK_SCHEDULE_STATUS 
                    CHECK (status IN ('PENDING', 'CONFIRMED', 'WAITING_DRIVER', 'DRIVER_CONFIRMED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED', 'EMERGENCY', 'WAITING', 'WAITING_L2'))
                ";
                
                using (var cmd = new OracleCommand(createConstraintSql, connection))
                {
                    try
                    {
                        await cmd.ExecuteNonQueryAsync();
                        Console.WriteLine("[INFO] Created new constraint: CHK_SCHEDULE_STATUS with all required statuses");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to create CHK_SCHEDULE_STATUS constraint: {ex.Message}");
                        throw;
                    }
                }
                
                // Add new columns for final fuel condition and refuel info if they don't exist
                var addColumnsSql = new[]
                {
                    @"BEGIN 
                        EXECUTE IMMEDIATE 'ALTER TABLE SCHEDULES ADD final_fuel_condition VARCHAR2(50)';
                      EXCEPTION 
                        WHEN OTHERS THEN 
                          IF SQLCODE != -1430 THEN RAISE; END IF;
                      END;",
                    @"BEGIN 
                        EXECUTE IMMEDIATE 'ALTER TABLE SCHEDULES ADD is_refueled NUMBER(1) DEFAULT 0';
                      EXCEPTION 
                        WHEN OTHERS THEN 
                          IF SQLCODE != -1430 THEN RAISE; END IF;
                      END;",
                    @"BEGIN 
                        EXECUTE IMMEDIATE 'ALTER TABLE SCHEDULES ADD refuel_amount NUMBER(10,2)';
                      EXCEPTION 
                        WHEN OTHERS THEN 
                          IF SQLCODE != -1430 THEN RAISE; END IF;
                      END;",
                    @"BEGIN 
                        EXECUTE IMMEDIATE 'ALTER TABLE SCHEDULES ADD refuel_receipt_path VARCHAR2(255)';
                      EXCEPTION 
                        WHEN OTHERS THEN 
                          IF SQLCODE != -1430 THEN RAISE; END IF;
                      END;",
                    @"BEGIN 
                        EXECUTE IMMEDIATE 'ALTER TABLE SCHEDULES ADD emergency_reason VARCHAR2(1000)';
                      EXCEPTION 
                        WHEN OTHERS THEN 
                          IF SQLCODE != -1430 THEN RAISE; END IF;
                      END;",
                    @"BEGIN 
                        EXECUTE IMMEDIATE 'ALTER TABLE SCHEDULES ADD emergency_type VARCHAR2(20)';
                      EXCEPTION 
                        WHEN OTHERS THEN 
                          IF SQLCODE != -1430 THEN RAISE; END IF;
                      END;",
                    @"BEGIN 
                        EXECUTE IMMEDIATE 'ALTER TABLE SCHEDULES ADD driver_message VARCHAR2(1000)';
                      EXCEPTION 
                        WHEN OTHERS THEN 
                          IF SQLCODE != -1430 THEN RAISE; END IF;
                      END;"
                };
                
                foreach (var sql in addColumnsSql)
                {
                    try
                    {
                        using (var cmd = new OracleCommand(sql, connection))
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WARN] Column might already exist: {ex.Message}");
                    }
                }
                
                Console.WriteLine("[INFO] Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Failed to initialize database: {ex.Message}");
                // Don't throw - allow app to continue
            }
        }
    }
}

using Dapper;
using PelindoCarLoan.API.Models;

namespace PelindoCarLoan.API.Repositories
{
    /// <summary>
    /// Repository interface for Driver operations
    /// </summary>
    public interface IDriverRepository
    {
        Task<Driver?> GetByIdAsync(int id);
        Task<Driver?> GetByUserIdAsync(int userId);
        Task<IEnumerable<Driver>> GetAllAsync(string? status = null);
        Task<IEnumerable<Driver>> GetAvailableAsync();
        Task<IEnumerable<Driver>> GetAvailableForPeriodAsync(DateTime start, DateTime end);
        Task<int> CreateAsync(Driver driver);
        Task<bool> UpdateAsync(Driver driver);
        Task<bool> UpdateStatusAsync(int id, string status);
        Task<bool> DeleteAsync(int id);
        Task<int> GetAvailableCountAsync();
    }

    public class DriverRepository : IDriverRepository
    {
        private readonly IDbContext _dbContext;

        public DriverRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Driver?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT d.id, d.user_id AS UserId, d.license_number AS LicenseNumber, 
                       d.license_expiry AS LicenseExpiry, d.phone_number AS PhoneNumber, d.status,
                       d.is_active AS IsActive, d.created_at AS CreatedAt, d.updated_at AS UpdatedAt,
                       u.id, u.name, u.email, u.role, u.division
                FROM drivers d
                LEFT JOIN users u ON d.user_id = u.id
                WHERE d.id = :Id";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Driver, User, Driver>(
                sql,
                (d, u) =>
                {
                    d.User = u;
                    return d;
                },
                new { Id = id },
                splitOn: "id"
            );

            return result.FirstOrDefault();
        }

        public async Task<Driver?> GetByUserIdAsync(int userId)
        {
            const string sql = @"
                SELECT id, user_id AS UserId, license_number AS LicenseNumber, 
                       license_expiry AS LicenseExpiry, phone_number AS PhoneNumber, status,
                       is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM drivers
                WHERE user_id = :UserId AND is_active = 1";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<Driver>(sql, new { UserId = userId });
        }

        public async Task<IEnumerable<Driver>> GetAllAsync(string? status = null)
        {
            var sql = @"
                SELECT d.id, d.user_id AS UserId, d.license_number AS LicenseNumber, 
                       d.license_expiry AS LicenseExpiry, d.phone_number AS PhoneNumber, d.status,
                       d.is_active AS IsActive, d.created_at AS CreatedAt, d.updated_at AS UpdatedAt,
                       u.id, u.name, u.email, u.role, u.division
                FROM drivers d
                LEFT JOIN users u ON d.user_id = u.id
                WHERE d.is_active = 1";

            if (!string.IsNullOrEmpty(status))
                sql += " AND d.status = :Status";

            sql += " ORDER BY u.name";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Driver, User, Driver>(
                sql,
                (d, u) =>
                {
                    d.User = u;
                    return d;
                },
                new { Status = status },
                splitOn: "id"
            );

            return result;
        }

        public async Task<IEnumerable<Driver>> GetAvailableAsync()
        {
            return await GetAllAsync(DriverStatus.Available);
        }

        public async Task<IEnumerable<Driver>> GetAvailableForPeriodAsync(DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT d.id, d.user_id AS UserId, d.license_number AS LicenseNumber, 
                       d.license_expiry AS LicenseExpiry, d.phone_number AS PhoneNumber, d.status,
                       d.is_active AS IsActive, d.created_at AS CreatedAt, d.updated_at AS UpdatedAt,
                       u.id, u.name, u.email, u.role, u.division
                FROM drivers d
                LEFT JOIN users u ON d.user_id = u.id
                WHERE d.is_active = 1 
                  AND d.status = 'AVAILABLE'
                  AND d.license_expiry > :EndTime
                  AND NOT EXISTS (
                      SELECT 1 FROM schedules s
                      INNER JOIN loan_requests lr ON s.loan_request_id = lr.id
                      WHERE s.driver_id = d.id
                        AND s.status NOT IN ('COMPLETED', 'CANCELLED')
                        AND lr.start_datetime < :EndTime
                        AND lr.end_datetime > :StartTime
                  )
                ORDER BY u.name";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryAsync<Driver, User, Driver>(
                sql,
                (d, u) =>
                {
                    d.User = u;
                    return d;
                },
                new { StartTime = start, EndTime = end },
                splitOn: "id"
            );

            return result;
        }

        public async Task<int> CreateAsync(Driver driver)
        {
            const string sql = @"
                INSERT INTO drivers (user_id, license_number, license_expiry, phone_number, status, is_active)
                VALUES (:UserId, :LicenseNumber, :LicenseExpiry, :PhoneNumber, :Status, 1)
                RETURNING id INTO :Id";

            using var connection = _dbContext.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", driver.UserId);
            parameters.Add("LicenseNumber", driver.LicenseNumber);
            parameters.Add("LicenseExpiry", driver.LicenseExpiry);
            parameters.Add("PhoneNumber", driver.PhoneNumber);
            parameters.Add("Status", driver.Status);
            parameters.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

            await connection.ExecuteAsync(sql, parameters);
            return parameters.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(Driver driver)
        {
            const string sql = @"
                UPDATE drivers
                SET license_number = :LicenseNumber, license_expiry = :LicenseExpiry,
                    phone_number = :PhoneNumber, status = :Status
                WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                driver.Id,
                driver.LicenseNumber,
                driver.LicenseExpiry,
                driver.PhoneNumber,
                driver.Status
            });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            const string sql = "UPDATE drivers SET status = :Status WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, Status = status });
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE drivers SET is_active = 0 WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<int> GetAvailableCountAsync()
        {
            const string sql = @"
                SELECT COUNT(1) FROM drivers 
                WHERE status = 'AVAILABLE' AND is_active = 1 AND license_expiry > SYSDATE";

            using var connection = _dbContext.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql);
        }
    }
}

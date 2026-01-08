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
                SELECT d.driver_id, d.user_id, d.license_number, d.license_expiry, 
                       d.status, d.experience_years, d.rating, d.created_at, d.updated_at,
                       u.user_id as u_user_id, u.full_name, u.email, u.role, u.division
                FROM drivers d
                LEFT JOIN users u ON d.user_id = u.user_id
                WHERE d.driver_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
            if (result == null) return null;
            
            return MapToDriver(result);
        }

        public async Task<Driver?> GetByUserIdAsync(int userId)
        {
            const string sql = @"
                SELECT driver_id, user_id, license_number, license_expiry, 
                       status, experience_years, rating, created_at, updated_at
                FROM drivers
                WHERE user_id = :UserId";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { UserId = userId });
            if (result == null) return null;
            
            return MapToDriverSimple(result);
        }

        public async Task<IEnumerable<Driver>> GetAllAsync(string? status = null)
        {
            var sql = @"
                SELECT d.driver_id, d.user_id, d.license_number, d.license_expiry, 
                       d.status, d.experience_years, d.rating, d.created_at, d.updated_at,
                       u.user_id as u_user_id, u.full_name, u.email, u.phone_number, u.role, u.division
                FROM drivers d
                LEFT JOIN users u ON d.user_id = u.user_id
                WHERE 1=1";

            if (!string.IsNullOrEmpty(status))
                sql += " AND d.status = :Status";

            sql += " ORDER BY u.full_name";

            using var connection = _dbContext.CreateConnection();
            var results = await connection.QueryAsync<dynamic>(sql, new { Status = status });
            var drivers = new List<Driver>();
            foreach (var r in results)
            {
                drivers.Add(MapToDriver(r));
            }
            return drivers;
        }

        public async Task<IEnumerable<Driver>> GetAvailableAsync()
        {
            return await GetAllAsync(DriverStatus.Available);
        }

        public async Task<IEnumerable<Driver>> GetAvailableForPeriodAsync(DateTime start, DateTime end)
        {
            const string sql = @"
                SELECT d.driver_id, d.user_id, d.license_number, d.license_expiry, 
                       d.status, d.experience_years, d.rating, d.created_at, d.updated_at,
                       u.user_id as u_user_id, u.full_name, u.email, u.role, u.division
                FROM drivers d
                LEFT JOIN users u ON d.user_id = u.user_id
                WHERE d.status = 'AVAILABLE'
                  AND d.license_expiry > :EndTime
                  AND NOT EXISTS (
                      SELECT 1 FROM schedules s
                      INNER JOIN loan_requests lr ON s.loan_request_id = lr.loan_request_id
                      WHERE s.driver_id = d.driver_id
                        AND s.status NOT IN ('COMPLETED', 'CANCELLED')
                        AND lr.start_datetime < :EndTime
                        AND lr.end_datetime > :StartTime
                  )
                ORDER BY u.full_name";

            using var connection = _dbContext.CreateConnection();
            var results = await connection.QueryAsync<dynamic>(sql, new { StartTime = start, EndTime = end });
            var drivers = new List<Driver>();
            foreach (var r in results)
            {
                drivers.Add(MapToDriver(r));
            }
            return drivers;
        }

        public async Task<int> CreateAsync(Driver driver)
        {
            const string sql = @"
                INSERT INTO drivers (user_id, license_number, license_expiry, status, experience_years, rating)
                VALUES (:UserId, :LicenseNumber, :LicenseExpiry, :Status, :ExperienceYears, :Rating)
                RETURNING driver_id INTO :Id";

            using var connection = _dbContext.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("UserId", driver.UserId);
            parameters.Add("LicenseNumber", driver.LicenseNumber);
            parameters.Add("LicenseExpiry", driver.LicenseExpiry);
            parameters.Add("Status", driver.Status);
            parameters.Add("ExperienceYears", driver.ExperienceYears);
            parameters.Add("Rating", driver.Rating);
            parameters.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

            await connection.ExecuteAsync(sql, parameters);
            return parameters.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(Driver driver)
        {
            const string sql = @"
                UPDATE drivers
                SET license_number = :LicenseNumber, license_expiry = :LicenseExpiry,
                    status = :Status, experience_years = :ExperienceYears, rating = :Rating,
                    updated_at = SYSDATE
                WHERE driver_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                driver.Id,
                driver.LicenseNumber,
                driver.LicenseExpiry,
                driver.Status,
                driver.ExperienceYears,
                driver.Rating
            });
            return rowsAffected > 0;
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            const string sql = "UPDATE drivers SET status = :Status, updated_at = SYSDATE WHERE driver_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, Status = status });
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM drivers WHERE driver_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<int> GetAvailableCountAsync()
        {
            const string sql = @"
                SELECT COUNT(1) FROM drivers 
                WHERE status = 'AVAILABLE' AND license_expiry > SYSDATE";

            using var connection = _dbContext.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql);
        }

        private Driver MapToDriver(dynamic result)
        {
            var driver = new Driver
            {
                Id = (int)result.DRIVER_ID,
                UserId = result.USER_ID != null ? (int?)result.USER_ID : null,
                LicenseNumber = result.LICENSE_NUMBER ?? string.Empty,
                LicenseExpiry = result.LICENSE_EXPIRY ?? DateTime.Now,
                Status = result.STATUS ?? DriverStatus.Available,
                ExperienceYears = result.EXPERIENCE_YEARS != null ? (int?)result.EXPERIENCE_YEARS : null,
                Rating = result.RATING != null ? (decimal?)result.RATING : null,
                CreatedAt = result.CREATED_AT ?? DateTime.Now,
                UpdatedAt = result.UPDATED_AT ?? DateTime.Now
            };

            // Map User if exists
            if (result.U_USER_ID != null)
            {
                driver.User = new User
                {
                    Id = (int)result.U_USER_ID,
                    Name = result.FULL_NAME ?? string.Empty,
                    Email = result.EMAIL ?? string.Empty,
                    PhoneNumber = result.PHONE_NUMBER,
                    Role = result.ROLE ?? string.Empty,
                    Division = result.DIVISION
                };
            }

            return driver;
        }

        private Driver MapToDriverSimple(dynamic result)
        {
            return new Driver
            {
                Id = (int)result.DRIVER_ID,
                UserId = result.USER_ID != null ? (int?)result.USER_ID : null,
                LicenseNumber = result.LICENSE_NUMBER ?? string.Empty,
                LicenseExpiry = result.LICENSE_EXPIRY ?? DateTime.Now,
                Status = result.STATUS ?? DriverStatus.Available,
                ExperienceYears = result.EXPERIENCE_YEARS != null ? (int?)result.EXPERIENCE_YEARS : null,
                Rating = result.RATING != null ? (decimal?)result.RATING : null,
                CreatedAt = result.CREATED_AT ?? DateTime.Now,
                UpdatedAt = result.UPDATED_AT ?? DateTime.Now
            };
        }
    }
}

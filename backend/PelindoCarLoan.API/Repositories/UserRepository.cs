using Dapper;
using PelindoCarLoan.API.Models;

namespace PelindoCarLoan.API.Repositories
{
    /// <summary>
    /// Repository interface for User operations
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task<IEnumerable<User>> GetByRoleAsync(string role);
        Task<int> CreateAsync(User user);
        Task<bool> UpdateAsync(User user);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> EmailExistsAsync(string email);
    }

    public class UserRepository : IUserRepository
    {
        private readonly IDbContext _dbContext;

        public UserRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT user_id, full_name, email, phone_number, password_hash, role, division, 
                       unit_kerja, is_active, created_at, updated_at
                FROM users
                WHERE user_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
            if (result == null) return null;
            
            return new User
            {
                Id = (int)result.USER_ID,
                Name = result.FULL_NAME ?? string.Empty,
                Email = result.EMAIL ?? string.Empty,
                PhoneNumber = result.PHONE_NUMBER,
                PasswordHash = result.PASSWORD_HASH ?? string.Empty,
                Role = result.ROLE ?? string.Empty,
                Division = result.DIVISION,
                UnitKerja = result.UNIT_KERJA,
                IsActive = result.IS_ACTIVE == 1,
                CreatedAt = result.CREATED_AT ?? DateTime.Now,
                UpdatedAt = result.UPDATED_AT ?? DateTime.Now
            };
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            const string sql = @"
                SELECT user_id, full_name, email, phone_number, password_hash, role, division,
                       unit_kerja, is_active, created_at, updated_at
                FROM users
                WHERE LOWER(email) = LOWER(:Email)";

            using var connection = _dbContext.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { Email = email });
            if (result == null) return null;
            
            return new User
            {
                Id = (int)result.USER_ID,
                Name = result.FULL_NAME ?? string.Empty,
                Email = result.EMAIL ?? string.Empty,
                PhoneNumber = result.PHONE_NUMBER,
                PasswordHash = result.PASSWORD_HASH ?? string.Empty,
                Role = result.ROLE ?? string.Empty,
                Division = result.DIVISION,
                UnitKerja = result.UNIT_KERJA,
                IsActive = result.IS_ACTIVE == 1,
                CreatedAt = result.CREATED_AT ?? DateTime.Now,
                UpdatedAt = result.UPDATED_AT ?? DateTime.Now
            };
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            const string sql = @"
                SELECT user_id AS Id, full_name AS Name, email, role, division, unit_kerja AS UnitKerja,
                       phone_number AS PhoneNumber, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM users
                WHERE is_active = 1
                ORDER BY full_name";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryAsync<User>(sql);
        }

        public async Task<IEnumerable<User>> GetByRoleAsync(string role)
        {
            const string sql = @"
                SELECT user_id AS Id, full_name AS Name, email, role, division, unit_kerja AS UnitKerja,
                       phone_number AS PhoneNumber, is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM users
                WHERE role = :Role AND is_active = 1
                ORDER BY full_name";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryAsync<User>(sql, new { Role = role });
        }

        public async Task<int> CreateAsync(User user)
        {
            const string sql = @"
                INSERT INTO users (user_id, username, email, password_hash, full_name, role, division, unit_kerja, phone_number, is_active)
                VALUES (seq_users.NEXTVAL, :Username, :Email, :PasswordHash, :Name, :Role, :Division, :UnitKerja, :PhoneNumber, 1)
                RETURNING user_id INTO :Id";

            using var connection = _dbContext.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("Username", user.Email.Split('@')[0]); // Generate username from email
            parameters.Add("Name", user.Name);
            parameters.Add("Email", user.Email);
            parameters.Add("PasswordHash", user.PasswordHash);
            parameters.Add("Role", user.Role);
            parameters.Add("Division", user.Division);
            parameters.Add("UnitKerja", user.UnitKerja);
            parameters.Add("PhoneNumber", user.PhoneNumber);
            parameters.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

            await connection.ExecuteAsync(sql, parameters);
            return parameters.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(User user)
        {
            const string sql = @"
                UPDATE users
                SET full_name = :Name, email = :Email, role = :Role, division = :Division, 
                    unit_kerja = :UnitKerja, phone_number = :PhoneNumber, is_active = :IsActive, updated_at = SYSTIMESTAMP
                WHERE user_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                user.Division,
                user.UnitKerja,
                user.PhoneNumber,
                IsActive = user.IsActive ? 1 : 0
            });
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE users SET is_active = 0 WHERE user_id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            const string sql = "SELECT COUNT(1) FROM users WHERE user_id = :Id AND is_active = 1";

            using var connection = _dbContext.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { Id = id }) > 0;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            const string sql = "SELECT COUNT(1) FROM users WHERE LOWER(email) = LOWER(:Email)";

            using var connection = _dbContext.CreateConnection();
            return await connection.ExecuteScalarAsync<int>(sql, new { Email = email }) > 0;
        }
    }
}

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
                SELECT id, name, email, password_hash AS PasswordHash, role, division, 
                       is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM users
                WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            const string sql = @"
                SELECT id, name, email, password_hash AS PasswordHash, role, division,
                       is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM users
                WHERE LOWER(email) = LOWER(:Email) AND is_active = 1";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            const string sql = @"
                SELECT id, name, email, role, division,
                       is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM users
                WHERE is_active = 1
                ORDER BY name";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryAsync<User>(sql);
        }

        public async Task<IEnumerable<User>> GetByRoleAsync(string role)
        {
            const string sql = @"
                SELECT id, name, email, role, division,
                       is_active AS IsActive, created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM users
                WHERE role = :Role AND is_active = 1
                ORDER BY name";

            using var connection = _dbContext.CreateConnection();
            return await connection.QueryAsync<User>(sql, new { Role = role });
        }

        public async Task<int> CreateAsync(User user)
        {
            const string sql = @"
                INSERT INTO users (name, email, password_hash, role, division, is_active)
                VALUES (:Name, :Email, :PasswordHash, :Role, :Division, 1)
                RETURNING id INTO :Id";

            using var connection = _dbContext.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("Name", user.Name);
            parameters.Add("Email", user.Email);
            parameters.Add("PasswordHash", user.PasswordHash);
            parameters.Add("Role", user.Role);
            parameters.Add("Division", user.Division);
            parameters.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

            await connection.ExecuteAsync(sql, parameters);
            return parameters.Get<int>("Id");
        }

        public async Task<bool> UpdateAsync(User user)
        {
            const string sql = @"
                UPDATE users
                SET name = :Name, email = :Email, role = :Role, division = :Division, is_active = :IsActive
                WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                user.Id,
                user.Name,
                user.Email,
                user.Role,
                user.Division,
                IsActive = user.IsActive ? 1 : 0
            });
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "UPDATE users SET is_active = 0 WHERE id = :Id";

            using var connection = _dbContext.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            const string sql = "SELECT COUNT(1) FROM users WHERE id = :Id AND is_active = 1";

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

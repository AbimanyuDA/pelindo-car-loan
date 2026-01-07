using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace PelindoCarLoan.API.Repositories
{
    /// <summary>
    /// Database context for Oracle connection management
    /// </summary>
    public interface IDbContext
    {
        IDbConnection CreateConnection();
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
    }
}

using Dapper;
using System.Data;
using System.Threading.Tasks;

namespace Coherent.Infrastructure.Repositories
{
    public class CustomerLoginInfoRepository
    {
        private readonly IDbConnection _connection;

        public CustomerLoginInfoRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<string?> GetTokenByRegistrationCodeAsync(string registrationCode, IDbTransaction? transaction = null)
        {
            const string sql = "SELECT TOP 1 Token FROM CustomerLoginInfo WHERE RegistrationCode = @RegistrationCode";
            return await _connection.QueryFirstOrDefaultAsync<string?>(sql, new { RegistrationCode = registrationCode }, transaction);
        }
    }
}

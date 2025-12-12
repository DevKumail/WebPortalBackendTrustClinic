using Coherent.Domain.Entities;
using Dapper;
using System.Data;
using System.Threading.Tasks;

namespace Coherent.Infrastructure.Repositories
{
    public class HREmployeeRepository
    {
        private readonly IDbConnection _connection;

        public HREmployeeRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<HREmployee?> GetByUsernameAsync(string username, IDbTransaction? transaction = null)
        {
            var sql = "SELECT TOP 1 * FROM HREmployee WHERE UserName = @Username AND Active = 1";
            return await _connection.QueryFirstOrDefaultAsync<HREmployee>(sql, new { Username = username }, transaction);
        }
    }
}

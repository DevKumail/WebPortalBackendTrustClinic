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

        public async Task<HREmployee?> GetByEmpIdAsync(long empId, IDbTransaction? transaction = null)
        {
            var sql = "SELECT TOP 1 * FROM HREmployee WHERE EmpId = @EmpId AND Active = 1";
            return await _connection.QueryFirstOrDefaultAsync<HREmployee>(sql, new { EmpId = empId }, transaction);
        }

        public async Task<IEnumerable<HREmployee>> GetCRMUsersAsync(int? empType = null, bool? isCRM = null, int limit = 100)
        {
            var sql = @"SELECT TOP (@Limit) EmpId, FName, MName, LName, Prefix, ProvNPI, EmpType, Active, 
                        Email, Phone, Speciality, UserName, RoleId, VIPPatientAccess, DepartmentID, 
                        ISNULL(IsCRM, 0) AS IsCRM
                        FROM HREmployee 
                        WHERE Active = 1
                        AND (@EmpType IS NULL OR EmpType = @EmpType)
                        AND (@IsCRM IS NULL OR ISNULL(IsCRM, 0) = @IsCRM)
                        ORDER BY FName, LName";
            return await _connection.QueryAsync<HREmployee>(sql, new { Limit = limit, EmpType = empType, IsCRM = isCRM });
        }

        public async Task<bool> UpdateIsCRMAsync(long empId, bool isCRM)
        {
            var sql = "UPDATE HREmployee SET IsCRM = @IsCRM WHERE EmpId = @EmpId";
            var affected = await _connection.ExecuteAsync(sql, new { EmpId = empId, IsCRM = isCRM });
            return affected > 0;
        }

        public async Task<int> BulkUpdateIsCRMAsync(IEnumerable<long> empIds, bool isCRM)
        {
            var sql = "UPDATE HREmployee SET IsCRM = @IsCRM WHERE EmpId IN @EmpIds";
            return await _connection.ExecuteAsync(sql, new { EmpIds = empIds, IsCRM = isCRM });
        }
    }
}

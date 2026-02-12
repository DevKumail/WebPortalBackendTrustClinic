using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Coherent.Infrastructure.Repositories;

public class CRMUserRepository : ICRMUserRepository
{
    private readonly DatabaseConnectionFactory _connectionFactory;
    private readonly ILogger<CRMUserRepository> _logger;

    public CRMUserRepository(DatabaseConnectionFactory connectionFactory, ILogger<CRMUserRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<CRMUserListResponse> GetCRMUsersAsync(int? empType, bool? isCRM, int limit)
    {
        try
        {
            using var connection = _connectionFactory.CreatePrimaryConnection();

            var sql = @"SELECT TOP (@Limit) e.EmpId, e.FName, e.MName, e.LName, e.Prefix, e.ProvNPI, e.EmpType, e.Active, 
                        e.Email, e.Phone, e.UserName, e.RoleId, e.VIPPatientAccess, e.DepartmentID, 
                        ISNULL(e.IsCRM, 0) AS IsCRM,
                        r.RoleName
                        FROM HREmployee e
                        LEFT JOIN SecRole r ON r.RoleId = e.RoleId AND r.Active = 1
                        WHERE e.Active = 1
                        AND (@EmpType IS NULL OR e.EmpType = @EmpType)
                        AND (@IsCRM IS NULL OR ISNULL(e.IsCRM, 0) = @IsCRM)
                        ORDER BY e.FName, e.LName";

            var employees = await connection.QueryAsync<dynamic>(sql, new { Limit = limit, EmpType = empType, IsCRM = isCRM });

            var users = employees.Select(e => new CRMUserDto
            {
                EmpId = (long)e.EmpId,
                FullName = $"{e.FName} {e.LName}".Trim(),
                FName = e.FName,
                LName = e.LName,
                Email = e.Email,
                Phone = e.Phone,
                UserName = e.UserName,
                EmpType = (int)e.EmpType,
                EmpTypeName = GetEmpTypeName((int)e.EmpType),
                DepartmentID = (int?)e.DepartmentID,
                IsCRM = (bool)e.IsCRM,
                Active = (bool)e.Active,
                RoleId = (int?)e.RoleId,
                RoleName = (string?)e.RoleName
            }).ToList();

            return new CRMUserListResponse
            {
                Users = users,
                TotalCount = users.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetCRMUsersAsync. EmpType={EmpType}, IsCRM={IsCRM}, Limit={Limit}", empType, isCRM, limit);
            throw;
        }
    }

    public async Task<UpdateIsCRMResponse> UpdateIsCRMAsync(long empId, bool isCRM)
    {
        try
        {
            using var connection = _connectionFactory.CreatePrimaryConnection();

            var sql = "UPDATE HREmployee SET IsCRM = @IsCRM WHERE EmpId = @EmpId";
            var affected = await connection.ExecuteAsync(sql, new { EmpId = empId, IsCRM = isCRM });

            return new UpdateIsCRMResponse
            {
                Success = affected > 0,
                Message = affected > 0 ? "IsCRM updated successfully" : "Employee not found",
                AffectedCount = affected
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateIsCRMAsync. EmpId={EmpId}, IsCRM={IsCRM}", empId, isCRM);
            throw;
        }
    }

    public async Task<UpdateIsCRMResponse> BulkUpdateIsCRMAsync(List<long> empIds, bool isCRM)
    {
        try
        {
            if (empIds == null || empIds.Count == 0)
            {
                return new UpdateIsCRMResponse
                {
                    Success = false,
                    Message = "No employee IDs provided",
                    AffectedCount = 0
                };
            }

            using var connection = _connectionFactory.CreatePrimaryConnection();

            var sql = "UPDATE HREmployee SET IsCRM = @IsCRM WHERE EmpId IN @EmpIds";
            var affected = await connection.ExecuteAsync(sql, new { EmpIds = empIds, IsCRM = isCRM });

            return new UpdateIsCRMResponse
            {
                Success = affected > 0,
                Message = $"Updated {affected} employees",
                AffectedCount = affected
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in BulkUpdateIsCRMAsync. Count={Count}, IsCRM={IsCRM}", empIds?.Count ?? 0, isCRM);
            throw;
        }
    }

    public async Task<UpdateRoleIdResponse> UpdateRoleIdAsync(long empId, int? roleId)
    {
        try
        {
            using var connection = _connectionFactory.CreatePrimaryConnection();

            var sql = "UPDATE HREmployee SET RoleId = @RoleId WHERE EmpId = @EmpId";
            var affected = await connection.ExecuteAsync(sql, new { EmpId = empId, RoleId = roleId });

            return new UpdateRoleIdResponse
            {
                Success = affected > 0,
                Message = affected > 0 ? "Role updated successfully" : "Employee not found"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateRoleIdAsync. EmpId={EmpId}, RoleId={RoleId}", empId, roleId);
            throw;
        }
    }

    private static string GetEmpTypeName(int empType) => empType switch
    {
        1 => "Doctor/Provider",
        2 => "Nurse",
        3 => "Receptionist",
        4 => "IVFLab",
        5 => "Admin",
        _ => "Unknown"
    };
}

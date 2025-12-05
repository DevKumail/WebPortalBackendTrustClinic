using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Dapper;
using System.Data;
using System.Text;

namespace Coherent.Infrastructure.Repositories;

/// <summary>
/// Repository for RegPatient table operations
/// </summary>
public class PatientRepository : IPatientRepository
{
    private readonly IDbConnection _connection;

    public PatientRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<(IEnumerable<RegPatient> Patients, int TotalCount)> SearchPatientsAsync(
        string? mrNo,
        string? name,
        string? emiratesIDN,
        string? cellPhone,
        int pageNumber,
        int pageSize)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        // Build WHERE clause based on search criteria
        if (!string.IsNullOrWhiteSpace(mrNo))
        {
            whereClauses.Add("MRNo LIKE @MRNo");
            parameters.Add("MRNo", $"%{mrNo}%");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            whereClauses.Add("(PersonFirstName LIKE @Name OR PersonMiddleName LIKE @Name OR PersonLastName LIKE @Name)");
            parameters.Add("Name", $"%{name}%");
        }

        if (!string.IsNullOrWhiteSpace(emiratesIDN))
        {
            whereClauses.Add("EmiratesIDN LIKE @EmiratesIDN");
            parameters.Add("EmiratesIDN", $"%{emiratesIDN}%");
        }

        if (!string.IsNullOrWhiteSpace(cellPhone))
        {
            whereClauses.Add("PersonCellPhone LIKE @CellPhone");
            parameters.Add("CellPhone", $"%{cellPhone}%");
        }

        var whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        // Get total count
        var countQuery = $@"
            SELECT COUNT(*) 
            FROM RegPatient 
            {whereClause}";

        var totalCount = await _connection.ExecuteScalarAsync<int>(countQuery, parameters);

        // Get paginated results
        var offset = (pageNumber - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var dataQuery = $@"
            SELECT 
                MRNo,
                PersonFirstName,
                PersonMiddleName,
                PersonLastName,
                PersonSex,
                PatientBirthDate,
                PersonCellPhone,
                PersonEmail,
                PersonAddress1,
                Nationality,
                EmiratesIDN,
                PatientFirstVisitDate,
                CreatedDate,
                VIPPatient,
                Inactive,
                FacilityName,
                PersonHomePhone1,
                PersonWorkPhone1,
                PersonCountryId,
                PatientBloodGroupId
            FROM RegPatient 
            {whereClause}
            ORDER BY CreatedDate DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

        var patients = await _connection.QueryAsync<RegPatient>(dataQuery, parameters);

        return (patients, totalCount);
    }

    public async Task<RegPatient?> GetPatientByMRNoAsync(string mrNo)
    {
        var query = @"
            SELECT * 
            FROM RegPatient 
            WHERE MRNo = @MRNo";

        return await _connection.QueryFirstOrDefaultAsync<RegPatient>(query, new { MRNo = mrNo });
    }
}

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
        string? personSocialSecurityNo,
        string? cellPhone,
        DateTime? visitDateFrom,
        DateTime? visitDateTo,
        bool? onboardedOnMobileApp,
        int pageNumber,
        int pageSize)
    {
        try
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

        if (!string.IsNullOrWhiteSpace(personSocialSecurityNo))
        {
            whereClauses.Add("PersonSocialSecurityNo LIKE @PersonSocialSecurityNo");
            parameters.Add("PersonSocialSecurityNo", $"%{personSocialSecurityNo}%");
        }

        if (!string.IsNullOrWhiteSpace(cellPhone))
        {
            whereClauses.Add("PersonCellPhone LIKE @CellPhone");
            parameters.Add("CellPhone", $"%{cellPhone}%");
        }

        // Visit date range filter (uses BLPatientVisit via SchAppointment for actual visit dates)
        bool hasVisitDateFilter = visitDateFrom.HasValue || visitDateTo.HasValue;
        if (visitDateFrom.HasValue)
        {
            var fromDateStr = visitDateFrom.Value.ToString("yyyyMMdd");
            parameters.Add("VisitDateFrom", fromDateStr);
        }

        if (visitDateTo.HasValue)
        {
            var toDateStr = visitDateTo.Value.ToString("yyyyMMdd");
            parameters.Add("VisitDateTo", toDateStr);
        }

        // Mobile app onboarding filter
        if (onboardedOnMobileApp.HasValue)
        {
            whereClauses.Add("IsMobileUser = @IsMobileUser");
            parameters.Add("IsMobileUser", onboardedOnMobileApp.Value);
        }

        var whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        // Build visit date join clause if filtering by visit date
        var visitDateJoin = "";
        var visitDateWhereClause = "";
        if (hasVisitDateFilter)
        {
            visitDateJoin = @"
                INNER JOIN SchAppointment SA ON RP.MRNo = SA.MRNo";
            
            var visitDateConditions = new List<string>();
            if (visitDateFrom.HasValue)
            {
                visitDateConditions.Add("SA.AppDate >= @VisitDateFrom");
            }
            if (visitDateTo.HasValue)
            {
                visitDateConditions.Add("SA.AppDate <= @VisitDateTo");
            }
            visitDateWhereClause = visitDateConditions.Any() 
                ? (whereClauses.Any() ? " AND " : " WHERE ") + string.Join(" AND ", visitDateConditions)
                : "";
        }

        // Get total count
        var countQuery = hasVisitDateFilter
            ? $@"
                SELECT COUNT(DISTINCT RP.MRNo) 
                FROM RegPatient RP
                {visitDateJoin}
                {whereClause.Replace("WHERE ", "WHERE RP.").Replace(" AND ", " AND RP.")}{visitDateWhereClause}"
            : $@"
                SELECT COUNT(*) 
                FROM RegPatient 
                {whereClause}";

        var totalCount = await _connection.ExecuteScalarAsync<int>(countQuery, parameters);

        // Get paginated results
        var offset = (pageNumber - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var dataQuery = hasVisitDateFilter
            ? $@"
                SELECT DISTINCT
                    RP.MRNo,
                    RP.PersonFirstName,
                    RP.PersonMiddleName,
                    RP.PersonLastName,
                    RP.PersonSex,
                    RP.PatientBirthDate,
                    RP.PersonCellPhone,
                    RP.PersonEmail,
                    RP.PersonAddress1,
                    N.NationalityName AS Nationality,
                    RP.PersonSocialSecurityNo,
                    RP.PatientFirstVisitDate,
                    RP.CreatedDate,
                    RP.VIPPatient,
                    RP.FacilityName,
                    RP.PersonHomePhone1,
                    RP.PersonWorkPhone1,
                    RP.PersonCountryId,
                    RP.PatientBloodGroupId,
                    RP.IsMobileUser
                FROM RegPatient RP
                LEFT JOIN Nationality N ON TRY_CAST(RP.Nationality AS INT) = N.NationalityId
                {visitDateJoin}
                {whereClause.Replace("WHERE ", "WHERE RP.").Replace(" AND ", " AND RP.")}{visitDateWhereClause}
                ORDER BY RP.CreatedDate DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY"
            : $@"
                SELECT 
                    RP.MRNo,
                    RP.PersonFirstName,
                    RP.PersonMiddleName,
                    RP.PersonLastName,
                    RP.PersonSex,
                    RP.PatientBirthDate,
                    RP.PersonCellPhone,
                    RP.PersonEmail,
                    RP.PersonAddress1,
                    N.NationalityName AS Nationality,
                    RP.PersonSocialSecurityNo,
                    RP.PatientFirstVisitDate,
                    RP.CreatedDate,
                    RP.VIPPatient,
                    RP.FacilityName,
                    RP.PersonHomePhone1,
                    RP.PersonWorkPhone1,
                    RP.PersonCountryId,
                    RP.PatientBloodGroupId,
                    RP.IsMobileUser
                FROM RegPatient RP
                LEFT JOIN Nationality N ON TRY_CAST(RP.Nationality AS INT) = N.NationalityId
                {whereClause.Replace("WHERE ", "WHERE RP.").Replace(" AND ", " AND RP.")}
                ORDER BY RP.CreatedDate DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            var patients = await _connection.QueryAsync<RegPatient>(dataQuery, parameters);

            return (patients, totalCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] SearchPatientsAsync failed: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
            throw;
        }
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

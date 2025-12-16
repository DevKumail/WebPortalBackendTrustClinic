using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Domain.Entities;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class CrmFacilityRepository : ICrmFacilityRepository
{
    private readonly IDbConnection _connection;

    public CrmFacilityRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<MFacility>> GetAllAsync(bool includeInactive)
    {
        var sql = "SELECT * FROM MFacility ORDER BY FName";
        var rows = await _connection.QueryAsync<MFacility>(sql);
        return rows.ToList();
    }

    public async Task<MFacility?> GetByIdAsync(int facilityId)
    {
        var sql = "SELECT * FROM MFacility WHERE FId = @FacilityId";
        return await _connection.QueryFirstOrDefaultAsync<MFacility>(sql, new { FacilityId = facilityId });
    }

    public async Task<int> UpsertAsync(CrmFacilityUpsertRequest request)
    {
        if (request.FId.HasValue)
        {
            var updateSql = @"
UPDATE MFacility
SET FName = @FName,
    LicenceNo = @LicenceNo,
    AddressLine1 = @AddressLine1,
    AddressLine2 = @AddressLine2,
    City = @City,
    State = @State,
    Country = @Country,
    Phone1 = @Phone1,
    Phone2 = @Phone2,
    EmailAddress = @EmailAddress,
    WebsiteUrl = @WebsiteUrl,
    FbUrl = @FbUrl,
    LinkedInUrl = @LinkedInUrl,
    YoutubeUrl = @YoutubeUrl,
    TwitterUrl = @TwitterUrl,
    TiktokUrl = @TiktokUrl,
    Instagram = @Instagram,
    WhatsappNo = @WhatsappNo,
    GoogleMapUrl = @GoogleMapUrl,
    About = @About,
    AboutShort = @AboutShort,
    FacilityImages = @FacilityImages,
    ArAbout = @ArAbout,
    ArAboutShort = @ArAboutShort
WHERE FId = @FId";

            await _connection.ExecuteAsync(updateSql, request);
            return request.FId.Value;
        }

        var insertSql = @"
INSERT INTO MFacility
(
    FName, LicenceNo, AddressLine1, AddressLine2, City, State, Country,
    Phone1, Phone2, EmailAddress, WebsiteUrl, FbUrl, LinkedInUrl, YoutubeUrl,
    TwitterUrl, TiktokUrl, Instagram, WhatsappNo, GoogleMapUrl,
    About, AboutShort, FacilityImages, ArAbout, ArAboutShort
)
VALUES
(
    @FName, @LicenceNo, @AddressLine1, @AddressLine2, @City, @State, @Country,
    @Phone1, @Phone2, @EmailAddress, @WebsiteUrl, @FbUrl, @LinkedInUrl, @YoutubeUrl,
    @TwitterUrl, @TiktokUrl, @Instagram, @WhatsappNo, @GoogleMapUrl,
    @About, @AboutShort, @FacilityImages, @ArAbout, @ArAboutShort
);
SELECT CAST(SCOPE_IDENTITY() as int);";

        var newId = await _connection.QuerySingleAsync<int>(insertSql, request);
        return newId;
    }
}

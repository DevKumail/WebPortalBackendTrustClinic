using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Helpers;
using Dapper;
using System.Data;
using System.Globalization;

namespace Coherent.Infrastructure.Repositories;

/// <summary>
/// Repository for patient health data from UEMedical_For_R&D database
/// </summary>
public class PatientHealthRepository : IPatientHealthRepository
{
    private readonly IDbConnection _connection;

    public PatientHealthRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<VitalSignsDto?> GetVitalSignsByMRNOAsync(string mrNo)
    {
        // Get from RegPatient table - height and weight
        var patientQuery = @"
            SELECT 
                MRNo AS MRNO,
                PersonHeight AS Height,
                PersonWeight AS Weight
            FROM RegPatient
            WHERE MRNo = @MRNo";

        var patient = await _connection.QueryFirstOrDefaultAsync<VitalSignsDto>(patientQuery, new { MRNo = mrNo });

        if (patient != null)
        {
            // Calculate BMI if height and weight exist
            if (patient.Height.HasValue && patient.Weight.HasValue && patient.Height.Value > 0)
            {
                // BMI = weight(kg) / (height(m))^2
                // Convert height from cm to m
                decimal heightInMeters = patient.Height.Value / 100;
                patient.BMI = Math.Round(patient.Weight.Value / (heightInMeters * heightInMeters), 2);
            }

            patient.RecordedDate = DateTime.Now;

            // Try to get additional vital signs from ClinicVisits or similar table if exists
            // This is a placeholder - adjust based on actual table structure
            var vitalsQuery = @"
                SELECT TOP 1
                    Temperature,
                    BloodPressure,
                    HeartRate,
                    VisitDate AS RecordedDate
                FROM PatientVitalSigns
                WHERE MRNo = @MRNo
                ORDER BY VisitDate DESC";

            try
            {
                var vitals = await _connection.QueryFirstOrDefaultAsync<dynamic>(vitalsQuery, new { MRNo = mrNo });
                if (vitals != null)
                {
                    patient.Temperature = vitals.Temperature;
                    patient.BloodPressure = vitals.BloodPressure;
                    patient.HeartRate = vitals.HeartRate;
                    if (vitals.RecordedDate != null)
                        patient.RecordedDate = DateStringConversion.StringToDate(vitals.RecordedDate.ToString());
                }
            }
            catch
            {
                // Table might not exist, ignore
            }
        }

        return patient;
    }

    public async Task<List<MedicationDto>> GetMedicationsByMRNOAsync(string mrNo)
    {
        // This query assumes a Medications table exists in UEMedical_For_R&D
        // Adjust table and column names based on actual schema
        var query = @"
            SELECT 
                MedicationId,
                MRNo AS MRNO,
                MedicationName,
                Dosage,
                Frequency,
                Route,
                PrescribedBy,
                PrescribedDate,
                StartDate,
                EndDate,
                Instructions,
                IsActive
            FROM PatientMedications
            WHERE MRNo = @MRNo
            AND IsActive = 1
            ORDER BY PrescribedDate DESC";

        try
        {
            var medications = await _connection.QueryAsync<MedicationDto>(query, new { MRNo = mrNo });
            
            // Convert date strings if necessary
            foreach (var med in medications)
            {
                if (med.PrescribedDate.HasValue)
                    med.PrescribedDate = DateStringConversion.StringToDate(med.PrescribedDate.ToString() ?? "");
                if (med.StartDate.HasValue)
                    med.StartDate = DateStringConversion.StringToDate(med.StartDate.ToString() ?? "");
                if (med.EndDate.HasValue)
                    med.EndDate = DateStringConversion.StringToDate(med.EndDate.ToString() ?? "");
            }
            
            return medications.ToList();
        }
        catch
        {
            // If table doesn't exist, return empty list
            return new List<MedicationDto>();
        }
    }

    public async Task<List<MedicationV2Dto>> GetMedicationsV2ByMRNOAsync(string mrNo)
    {
        var query = @"
            SELECT
                p.MedicationId,
                p.MRNo,
                p.VisitAccountNo,
                p.Rx,
                p.Dose,
                p.Frequency,
                p.Duration,
                p.Quantity,
                p.Instructions,
                p.Status,
                p.PrescriptionDate,
                p.StartDate,
                p.StopDate,
                er.Name AS RouteName,
                (ISNULL(hr.Prefix, '') + CASE WHEN hr.Prefix IS NULL OR LTRIM(RTRIM(hr.Prefix)) = '' THEN '' ELSE ' ' END +
                 ISNULL(hr.FName, '') + CASE WHEN hr.FName IS NULL OR LTRIM(RTRIM(hr.FName)) = '' THEN '' ELSE ' ' END +
                 ISNULL(hr.MName, '') + CASE WHEN hr.MName IS NULL OR LTRIM(RTRIM(hr.MName)) = '' THEN '' ELSE ' ' END +
                 ISNULL(hr.LName, '')) AS ProviderFullName
            FROM Prescription p
            LEFT JOIN EMRRoute er ON er.RouteId = p.Route
            LEFT JOIN HREmployee hr ON hr.EmpId = p.ProviderId
            WHERE p.MRNo = @MRNo
            ORDER BY p.PrescriptionDate DESC";

        // local projection model for raw DB values
        var rows = await _connection.QueryAsync<dynamic>(query, new { MRNo = mrNo });

        var result = new List<MedicationV2Dto>();

        foreach (var row in rows)
        {
            string? prescriptionDateRaw = row.PrescriptionDate?.ToString();
            string? startDateRaw = row.StartDate?.ToString();
            string? stopDateRaw = row.StopDate?.ToString();

            DateTime? prescriptionDate = ParseLegacyDateTime(prescriptionDateRaw);
            DateTime? startDate = ParseLegacyDateTime(startDateRaw);
            DateTime? stopDate = ParseLegacyDateTime(stopDateRaw);

            // daysLeft: based on stopDate - today (floor at 0)
            string? daysLeft = null;
            if (stopDate.HasValue && stopDate.Value != DateTime.MinValue)
            {
                var diff = (stopDate.Value.Date - DateTime.Today).TotalDays;
                daysLeft = Math.Max(0, (int)Math.Ceiling(diff)).ToString(CultureInfo.InvariantCulture);
            }

            result.Add(new MedicationV2Dto
            {
                MedicationId = (long)(row.MedicationId ?? 0L),
                Mrno = row.MRNo?.ToString(),
                VisitAccountNo = TryToLong(row.VisitAccountNo),
                Rx = row.Rx?.ToString(),
                Dose = row.Dose?.ToString(),
                ProviderName = NormalizeSpaces(row.ProviderFullName?.ToString()),
                Route = row.RouteName?.ToString() ?? row.Route?.ToString(),
                Frequency = row.Frequency?.ToString(),
                Duration = row.Duration?.ToString(),
                Quantity = row.Quantity?.ToString() ?? "0",
                PrescriptionDate = FormatMobileDateTime(prescriptionDate),
                StartDate = FormatMobileDateTime(startDate),
                StopDate = FormatMobileDateTime(stopDate),
                DaysLeft = daysLeft,
                ProviderImage = "https://purepng.com/public/uploads/large/purepng.com-doctorsdoctorsdoctors-and-nursesa-qualified-practitioner-of-medicine-aclinicianmedical-practitionermale-doctornotepad-1421526856962ngglq.png",
                Instructions = row.Instructions?.ToString() ?? " ",
                Status = row.Status?.ToString() ?? ""
            });
        }

        return result;
    }

    private static DateTime? ParseLegacyDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // If already a standard datetime, DateStringConversion will return MinValue; try normal parse first.
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            return parsed;

        var legacy = DateStringConversion.StringToDate(value);
        if (legacy == DateTime.MinValue)
            return null;

        return legacy;
    }

    private static string? FormatMobileDateTime(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        return value.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private static long? TryToLong(object? value)
    {
        if (value == null)
            return null;

        if (value is long l) return l;
        if (value is int i) return i;

        if (long.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return null;
    }

    private static string? NormalizeSpaces(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public async Task<List<AllergyDto>> GetAllergiesByMRNOAsync(string mrNo)
    {
        var query = @"
            SELECT
                PA.AllergyId,
                PA.VisitAccountNo,
                PA.TypeId,
                PA.Reaction,
                PA.StartDate,
                PA.EndDate,
                PA.Status,
                PA.Active,
                PA.ProviderId,
                PA.MRNo,
                PA.CreatedDate,
                PA.SeverityCode,
                PA.Allergen,
                T.AlergyName AS ViewAllergyTypeName
            FROM PatientAllergy PA
            LEFT OUTER JOIN AlergyTypes T ON PA.TypeId = T.AlergyTypeId
            WHERE
                PA.Active = 1
                AND PA.MRNo = @MRNo
            ORDER BY PA.CreatedDate DESC";

        try
        {
            var rows = await _connection.QueryAsync<dynamic>(query, new { MRNo = mrNo });

            var result = new List<AllergyDto>();

            foreach (var row in rows)
            {
                result.Add(new AllergyDto
                {
                    AllergyId = TryToLong(row.AllergyId) ?? 0,
                    VisitAccountNo = TryToLong(row.VisitAccountNo),
                    TypeId = row.TypeId == null ? null : (int?)Convert.ToInt32(row.TypeId),
                    MRNO = row.MRNo?.ToString() ?? mrNo,
                    Reaction = row.Reaction?.ToString(),
                    StartDate = row.StartDate?.ToString(),
                    EndDate = row.EndDate?.ToString(),
                    Status = row.Status == null ? null : (int?)Convert.ToInt32(row.Status),
                    ProviderId = TryToLong(row.ProviderId),
                    CreatedDate = row.CreatedDate?.ToString(),
                    SeverityCode = row.SeverityCode?.ToString(),
                    Severity = row.SeverityCode?.ToString(),
                    Allergen = row.Allergen?.ToString(),
                    ViewAllergyTypeName = row.ViewAllergyTypeName?.ToString(),
                    AllergyType = row.ViewAllergyTypeName?.ToString(),
                    IsActive = row.Active == null ? null : (bool?)Convert.ToBoolean(row.Active)
                });
            }

            return result;
        }
        catch
        {
            return new List<AllergyDto>();
        }
    }

    public async Task<List<DiagnosisDto>> GetDiagnosisByMRNOAsync(string mrNo)
    {
        var query = @"
            SELECT
                BSD.DiagnosisId AS Id,
                BSD.VisitAccountNo,
                BSD.ICD9Code,
                BSD.Confidential,
                BSD.LastUpdatedBy,
                BSD.LastUpdatedDate,
                SA.MrNo,
                BMICD.DescriptionFull AS ICD9Description,
                SA.ProviderId,
                SA.AppDateTime AS VisitDate,
                LTRIM(RTRIM(
                    COALESCE(NULLIF(emp.Prefix, ''), '')
                    + CASE WHEN emp.Prefix IS NULL OR LTRIM(RTRIM(emp.Prefix)) = '' THEN '' ELSE ' ' END
                    + COALESCE(NULLIF(emp.FName, ''), '')
                    + CASE WHEN emp.FName IS NULL OR LTRIM(RTRIM(emp.FName)) = '' THEN '' ELSE ' ' END
                    + COALESCE(NULLIF(emp.MName, ''), '')
                    + CASE WHEN emp.MName IS NULL OR LTRIM(RTRIM(emp.MName)) = '' THEN '' ELSE ' ' END
                    + COALESCE(NULLIF(emp.LName, ''), '')
                )) AS DoctorName
                
            FROM BLSuperBillDiagnosis AS BSD
            INNER JOIN BLPatientVisit AS BPV ON BSD.VisitAccountNo = BPV.VisitAccountNo
            INNER JOIN SchAppointment AS SA ON SA.AppId = BPV.AppointmentId
            INNER JOIN BLMasterICD9CM AS BMICD ON BSD.ICD9Code = BMICD.ICD9Code
            INNER JOIN HREmployee AS emp ON SA.ProviderId = emp.EmpId
            WHERE SA.MrNo = @MRNo
            ORDER BY SA.AppDateTime DESC, BSD.DiagnosisId DESC";

        try
        {
            var rows = await _connection.QueryAsync<dynamic>(query, new { MRNo = mrNo });
            var result = new List<DiagnosisDto>();

            foreach (var row in rows)
            {
                result.Add(new DiagnosisDto
                {
                    Id = TryToLong(row.Id) ?? 0,
                    VisitAccountNo = TryToLong(row.VisitAccountNo),
                    ICD9Code = row.ICD9Code?.ToString(),
                    Confidential = row.Confidential == null ? null : (bool?)Convert.ToBoolean(row.Confidential),
                    LastUpdatedBy = row.LastUpdatedBy?.ToString(),
                    LastUpdatedDate = row.LastUpdatedDate?.ToString(),
                    MRNO = row.MrNo?.ToString() ?? mrNo,
                    ICD9Description = row.ICD9Description?.ToString(),
                    ProviderId = TryToLong(row.ProviderId),
                    VisitDate = row.VisitDate?.ToString(),
                    DoctorName = NormalizeSpaces(row.DoctorName?.ToString()),
                    //Speciality = NormalizeSpaces(row.Speciality?.ToString())
                });
            }

            return result;
        }
        catch(Exception ex )
        {
            throw ex;
        }
    }
}

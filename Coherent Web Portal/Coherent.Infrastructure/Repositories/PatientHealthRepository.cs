using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Coherent.Infrastructure.Helpers;
using Dapper;
using System.Data;

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

    public async Task<List<AllergyDto>> GetAllergiesByMRNOAsync(string mrNo)
    {
        // This query assumes an Allergies table exists in UEMedical_For_R&D
        // Adjust table and column names based on actual schema
        var query = @"
            SELECT 
                AllergyId,
                MRNo AS MRNO,
                AllergyType,
                Allergen,
                Reaction,
                Severity,
                OnsetDate,
                Notes,
                IsActive
            FROM PatientAllergies
            WHERE MRNo = @MRNo
            AND IsActive = 1
            ORDER BY OnsetDate DESC";

        try
        {
            var allergies = await _connection.QueryAsync<AllergyDto>(query, new { MRNo = mrNo });
            
            // Convert date strings if necessary
            foreach (var allergy in allergies)
            {
                if (allergy.OnsetDate.HasValue)
                    allergy.OnsetDate = DateStringConversion.StringToDate(allergy.OnsetDate.ToString() ?? "");
            }
            
            return allergies.ToList();
        }
        catch
        {
            // If table doesn't exist, return empty list
            return new List<AllergyDto>();
        }
    }
}

using Coherent.Core.DTOs;
using Coherent.Core.Interfaces;
using Dapper;
using System.Data;

namespace Coherent.Infrastructure.Repositories;

public class AppointmentSchedulingRepository : IAppointmentSchedulingRepository
{
    private readonly IDbConnection _connection;

    public AppointmentSchedulingRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<List<SchAppointmentDto>> LoadAppointmentsForAvailabilityAsync(long providerId, int locationTypeId, string appDate, bool isUMC)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@AppId", 0L, DbType.Int64);
        parameters.Add("@ProviderId", providerId, DbType.Int64);
        parameters.Add("@MRNo", "", DbType.String);
        parameters.Add("@FromDate", appDate, DbType.String);
        parameters.Add("@ToDate", appDate, DbType.String);
        parameters.Add("@FromTime", "", DbType.String);
        parameters.Add("@ToTime", "", DbType.String);
        parameters.Add("@SiteId", locationTypeId, DbType.Int32);
        parameters.Add("@LocationId", 0, DbType.Int32);
        parameters.Add("@AppTypeId", 0, DbType.Int32);
        parameters.Add("@AppCriteriaId", 0, DbType.Int32);
        parameters.Add("@AppStatusId", 0, DbType.Int32);
        parameters.Add("@PatientStatusId", 0, DbType.Int32);
        parameters.Add("@ReferredProviderIds", "", DbType.String);
        parameters.Add("@EnteredBy", "", DbType.String);
        parameters.Add("@FromEntryDate", "", DbType.String);
        parameters.Add("@ToEntryDate", "", DbType.String);
        parameters.Add("@SpecialtyId", 0, DbType.Int32);
        parameters.Add("@PurposeOfVisit", "", DbType.String);
        parameters.Add("@FacilityID", 0, DbType.Int32);

        var spName = isUMC ? "SchAppointmentGetUMC" : "SchAppointmentGet";

        var rows = await _connection.QueryAsync<SchAppointmentDto>(
            spName,
            parameters,
            commandType: CommandType.StoredProcedure);

        return rows.ToList();
    }

    public async Task<long> InsertAppointmentAsync(SchAppointmentDto appointment)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@AppId", dbType: DbType.Int64, direction: ParameterDirection.Output);
        parameters.Add("@ProviderId", appointment.ProviderId, DbType.Int64);
        parameters.Add("@MRNo", appointment.MRNo, DbType.String);
        parameters.Add("@AppDateTime", appointment.AppDateTime, DbType.String);
        parameters.Add("@Duration", appointment.Duration, DbType.Int32);
        parameters.Add("@AppNote", appointment.AppNote, DbType.String);
        parameters.Add("@SiteId", appointment.SiteId, DbType.Int32);
        parameters.Add("@LocationId", appointment.LocationId, DbType.Int32);
        parameters.Add("@AppTypeId", appointment.AppTypeId, DbType.Int32);
        parameters.Add("@AppCriteriaId", appointment.AppCriteriaId, DbType.Int32);
        parameters.Add("@AppStatusId", appointment.AppStatusId, DbType.Int32);
        parameters.Add("@PatientStatusId", appointment.PatientStatusId, DbType.Int32);
        parameters.Add("@ReferredProviderId", appointment.ReferredProviderId ?? 0L, DbType.Int64);
        parameters.Add("@IsPatientNotified", appointment.IsPatientNotified, DbType.Boolean);
        parameters.Add("@IsActive", appointment.IsActive, DbType.Boolean);
        parameters.Add("@EnteredBy", appointment.EnteredBy, DbType.String);
        parameters.Add("@EntryDateTime", appointment.EntryDateTime, DbType.String);

        parameters.Add("@DateTimeNotYetArrived", appointment.DateTimeNotYetArrived ?? "", DbType.String);
        parameters.Add("@DateTimeCheckIn", appointment.DateTimeCheckIn ?? "", DbType.String);
        parameters.Add("@DateTimeReady", appointment.DateTimeReady ?? "", DbType.String);
        parameters.Add("@DateTimeSeen", appointment.DateTimeSeen ?? "", DbType.String);
        parameters.Add("@DateTimeBilled", appointment.DateTimeBilled ?? "", DbType.String);
        parameters.Add("@DateTimeCheckOut", appointment.DateTimeCheckOut ?? "", DbType.String);

        parameters.Add("@UserNotYetArrived", appointment.UserNotYetArrived ?? "", DbType.String);
        parameters.Add("@UserCheckIn", appointment.UserCheckIn ?? "", DbType.String);
        parameters.Add("@UserReady", appointment.UserReady ?? "", DbType.String);
        parameters.Add("@UserSeen", appointment.UserSeen ?? "", DbType.String);
        parameters.Add("@UserBilled", appointment.UserBilled ?? "", DbType.String);
        parameters.Add("@UserCheckOut", appointment.UserCheckOut ?? "", DbType.String);

        parameters.Add("@PurposeOfVisit", appointment.PurposeOfVisit ?? "", DbType.String);
        parameters.Add("@PatientNotifiedID", appointment.PatientNotifiedID, DbType.Int32);
        parameters.Add("@RescheduledID", appointment.RescheduleID, DbType.Int32);
        parameters.Add("@ByProvider", appointment.ByProvider, DbType.Boolean);
        parameters.Add("@SpecialtyId", appointment.SpecialtyId, DbType.Int32);
        parameters.Add("@UpdateServerTime", appointment.UpdateServerTime, DbType.Boolean);
        parameters.Add("@VisitStatusEnabled", appointment.VisitStatusEnabled, DbType.Boolean);
        parameters.Add("@Anesthesiologist", appointment.Anesthesiologist, DbType.Int64);
        parameters.Add("@CPTGroupId", appointment.CPTGroupId, DbType.Int64);
        parameters.Add("@AppointmentClassification", appointment.AppointmentClassification, DbType.Int32);
        parameters.Add("@OrderReferralId", appointment.OrderReferralId, DbType.Int64);
        parameters.Add("@TelemedicineURL", appointment.TelemedicineURL ?? "", DbType.String);

        await _connection.ExecuteAsync(
            "SchAppointmentInsert",
            parameters,
            commandType: CommandType.StoredProcedure);

        return parameters.Get<long>("@AppId");
    }

    public async Task<bool> UpdateAppointmentAsync(SchAppointmentDto appointment)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@AppId", appointment.AppId, DbType.Int64);
        parameters.Add("@ProviderId", appointment.ProviderId, DbType.Int64);
        parameters.Add("@MRNo", appointment.MRNo, DbType.String);
        parameters.Add("@AppDateTime", appointment.AppDateTime, DbType.String);
        parameters.Add("@Duration", appointment.Duration, DbType.Int32);
        parameters.Add("@AppNote", appointment.AppNote, DbType.String);
        parameters.Add("@SiteId", appointment.SiteId, DbType.Int32);
        parameters.Add("@LocationId", appointment.LocationId, DbType.Int32);
        parameters.Add("@AppTypeId", appointment.AppTypeId, DbType.Int32);
        parameters.Add("@AppCriteriaId", appointment.AppCriteriaId, DbType.Int32);
        parameters.Add("@AppStatusId", appointment.AppStatusId, DbType.Int32);
        parameters.Add("@PatientStatusId", appointment.PatientStatusId, DbType.Int32);
        parameters.Add("@ReferredProviderId", appointment.ReferredProviderId ?? 0L, DbType.Int64);
        parameters.Add("@IsPatientNotified", appointment.IsPatientNotified, DbType.Boolean);
        parameters.Add("@IsActive", appointment.IsActive, DbType.Boolean);
        parameters.Add("@EnteredBy", appointment.EnteredBy, DbType.String);
        parameters.Add("@EntryDateTime", appointment.EntryDateTime, DbType.String);

        parameters.Add("@DateTimeNotYetArrived", appointment.DateTimeNotYetArrived ?? "", DbType.String);
        parameters.Add("@DateTimeCheckIn", appointment.DateTimeCheckIn ?? "", DbType.String);
        parameters.Add("@DateTimeReady", appointment.DateTimeReady ?? "", DbType.String);
        parameters.Add("@DateTimeSeen", appointment.DateTimeSeen ?? "", DbType.String);
        parameters.Add("@DateTimeBilled", appointment.DateTimeBilled ?? "", DbType.String);
        parameters.Add("@DateTimeCheckOut", appointment.DateTimeCheckOut ?? "", DbType.String);

        parameters.Add("@UserNotYetArrived", appointment.UserNotYetArrived ?? "", DbType.String);
        parameters.Add("@UserCheckIn", appointment.UserCheckIn ?? "", DbType.String);
        parameters.Add("@UserReady", appointment.UserReady ?? "", DbType.String);
        parameters.Add("@UserSeen", appointment.UserSeen ?? "", DbType.String);
        parameters.Add("@UserBilled", appointment.UserBilled ?? "", DbType.String);
        parameters.Add("@UserCheckOut", appointment.UserCheckOut ?? "", DbType.String);

        parameters.Add("@PurposeOfVisit", appointment.PurposeOfVisit ?? "", DbType.String);
        parameters.Add("@PatientNotifiedID", appointment.PatientNotifiedID, DbType.Int32);
        parameters.Add("@RescheduledID", appointment.RescheduleID, DbType.Int32);
        parameters.Add("@ByProvider", appointment.ByProvider, DbType.Boolean);
        parameters.Add("@SpecialtyId", appointment.SpecialtyId, DbType.Int32);
        parameters.Add("@UpdateServerTime", appointment.UpdateServerTime, DbType.Boolean);
        parameters.Add("@CPTGroupId", appointment.CPTGroupId, DbType.Int64);
        parameters.Add("@TelemedicineURL", appointment.TelemedicineURL ?? "", DbType.String);

        var rows = await _connection.ExecuteAsync(
            "SchAppointmentUpdate",
            parameters,
            commandType: CommandType.StoredProcedure);

        return rows > 0;
    }

    public async Task<bool> UpdatePatientNotifyAsync(long appId, bool isPatientNotified, int patientNotifiedId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@AppId", appId, DbType.Int64);
        parameters.Add("@IsPatientNotified", isPatientNotified, DbType.Boolean);
        parameters.Add("@PatientNotifiedID", patientNotifiedId, DbType.Int32);

        var rows = await _connection.ExecuteAsync(
            "SchAppointmentUpdatePN",
            parameters,
            commandType: CommandType.StoredProcedure);

        return rows > 0;
    }

    public async Task<bool> UpdateAppointmentStatusAsync(long appId, int appStatusId, bool byProvider, int rescheduledId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@AppId", appId, DbType.Int64);
        parameters.Add("@AppStatusId", appStatusId, DbType.Int32);
        parameters.Add("@ByProvider", byProvider, DbType.Boolean);
        parameters.Add("@RescheduledID", rescheduledId, DbType.Int32);

        var rows = await _connection.ExecuteAsync(
            "SchAppointmentUpdateAppStatus",
            parameters,
            commandType: CommandType.StoredProcedure);

        return rows > 0;
    }

    public async Task<long> InsertAppointmentProcedureAsync(SchAppointmentProcedureDto procedure)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@AppProcedureId", dbType: DbType.Int64, direction: ParameterDirection.Output);
        parameters.Add("@AppId", procedure.AppId, DbType.Int64);
        parameters.Add("@ProcedureCode", procedure.ProcedureCode, DbType.String);
        parameters.Add("@ProcedureName", procedure.ProcedureName, DbType.String);
        parameters.Add("@LocationID", procedure.LocationID, DbType.Int32);
        parameters.Add("@StartTime", procedure.StartTime, DbType.String);
        parameters.Add("@Duration", procedure.Duration, DbType.Int32);
        parameters.Add("@Active", procedure.Active, DbType.Boolean);
        parameters.Add("@OrderDetailId", procedure.OrderDetailId, DbType.Int64);

        await _connection.ExecuteAsync(
            "InsertSchAppointmentProcedures",
            parameters,
            commandType: CommandType.StoredProcedure);

        return parameters.Get<long>("@AppProcedureId");
    }

    public async Task<bool> UpdateAppointmentProcedureAsync(SchAppointmentProcedureDto procedure)
    {
        var parameters = new DynamicParameters();

        parameters.Add("@AppProcedureId", procedure.AppProcedureId, DbType.Int64);
        parameters.Add("@AppId", procedure.AppId, DbType.Int64);
        parameters.Add("@ProcedureCode", procedure.ProcedureCode, DbType.String);
        parameters.Add("@ProcedureName", procedure.ProcedureName, DbType.String);
        parameters.Add("@LocationID", procedure.LocationID, DbType.Int32);
        parameters.Add("@StartTime", procedure.StartTime, DbType.String);
        parameters.Add("@Duration", procedure.Duration, DbType.Int32);
        parameters.Add("@Active", procedure.Active, DbType.Boolean);
        parameters.Add("@OrderDetailId", procedure.OrderDetailId, DbType.Int64);

        var rows = await _connection.ExecuteAsync(
            "UpdateSchAppointmentProcedures",
            parameters,
            commandType: CommandType.StoredProcedure);

        return rows > 0;
    }

    public async Task<bool> DeleteAppointmentProcedureByDetailIdAsync(long orderDetailId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@OrderDetailId", orderDetailId, DbType.Int64);

        var rows = await _connection.ExecuteAsync(
            "DeleteSchAppointmentProcedures_byDetailId",
            parameters,
            commandType: CommandType.StoredProcedure);

        return rows > 0;
    }
}

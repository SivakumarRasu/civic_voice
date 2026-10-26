using CivicVoice.Application.DTOs;
using CivicVoice.Application.Interfaces;
using CivicVoice.Domain.Entities;
using CivicVoice.Domain.Enums;

namespace CivicVoice.Application.Interfaces;

/// <summary>
/// Service interface for grievance business operations.
/// </summary>
public interface IGrievanceService
{
    Task<ServiceResult<Grievance>> CreateGrievanceAsync(CreateGrievanceDto dto, string citizenId, CancellationToken ct = default);
    Task<Grievance?> GetGrievanceByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Grievance>> SearchGrievancesAsync(GrievanceSearchDto search, CancellationToken ct = default);
    Task<ServiceResult<bool>> UpdateStatusAsync(int grievanceId, GrievanceStatus newStatus, string changedBy, CancellationToken ct = default);
    Task<DashboardStats> GetDashboardStatsAsync(CancellationToken ct = default);
    Task<IEnumerable<Grievance>> GetGrievancesByDepartmentAsync(int departmentId, CancellationToken ct = default);
    Task<IEnumerable<Grievance>> GetGrievancesByCitizenAsync(string citizenId, CancellationToken ct = default);
    Task<IEnumerable<Grievance>> GetOverdueGrievancesAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when a grievance is resolved. Used for feedback workflow.
    /// </summary>
    event Domain.Events.GrievanceResolvedHandler? GrievanceResolved;
}

public interface IFeedbackService
{
    Task<ServiceResult<Feedback>> SubmitFeedbackAsync(int grievanceId, string citizenId, int rating, string? comments, CancellationToken ct = default);
    Task<Feedback?> GetFeedbackAsync(int grievanceId, string citizenId, CancellationToken ct = default);
    Task<IEnumerable<Feedback>> GetFeedbacksForGrievanceAsync(int grievanceId, CancellationToken ct = default);
}

public interface IDepartmentService
{
    Task<IEnumerable<Department>> GetAllDepartmentsAsync(CancellationToken ct = default);
    Task<IEnumerable<Department>> GetAllWithCategoriesAsync(CancellationToken ct = default);
    Task<Department?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<DepartmentReportDto> GetDepartmentReportAsync(CancellationToken ct = default);
}

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllWithDepartmentsAsync(CancellationToken ct = default);
    Task<Category?> GetByIdWithDepartmentAsync(int id, CancellationToken ct = default);
}

public interface IWardService
{
    Task<IEnumerable<Ward>> GetAllWardsAsync(CancellationToken ct = default);
    Task<bool> WardExistsAsync(int wardId, CancellationToken ct = default);
}

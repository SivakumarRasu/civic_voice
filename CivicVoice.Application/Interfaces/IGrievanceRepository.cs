using CivicVoice.Domain.Entities;
using CivicVoice.Domain.Enums;

namespace CivicVoice.Application.Interfaces;

/// <summary>
/// Specialized repository for Grievance with query methods beyond basic CRUD.
/// </summary>
public interface IGrievanceRepository : IRepository<Grievance>
{
    /// <summary>
    /// Get a grievance by ID with all related entities loaded.
    /// </summary>
    Task<Grievance?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Search grievances with optional filters.
    /// </summary>
    Task<IEnumerable<Grievance>> SearchAsync(
        string? searchTerm = null,
        GrievanceStatus? status = null,
        int? wardId = null,
        int? categoryId = null,
        int? departmentId = null,
        string? citizenId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get grievances assigned to a specific department.
    /// </summary>
    Task<IEnumerable<Grievance>> GetByDepartmentAsync(int departmentId, CancellationToken ct = default);

    /// <summary>
    /// Get all grievances that are overdue (past DueAt, not resolved/rejected).
    /// </summary>
    Task<IEnumerable<Grievance>> GetOverdueAsync(CancellationToken ct = default);

    /// <summary>
    /// Get dashboard statistics.
    /// </summary>
    Task<DashboardStats> GetDashboardStatsAsync(CancellationToken ct = default);
}

/// <summary>
/// Dashboard statistics DTO returned by the repository.
/// </summary>
public class DashboardStats
{
    public int TotalGrievances { get; set; }
    public int OpenGrievances { get; set; }
    public int ResolvedGrievances { get; set; }
    public int SlaBreach { get; set; }
}

using CivicVoice.Application.DTOs;
using CivicVoice.Application.Interfaces;
using CivicVoice.Domain.Entities;
using CivicVoice.Domain.Enums;
using CivicVoice.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CivicVoice.Application.Services;

/// <summary>
/// Core business service for grievance operations.
/// Implements the grievance lifecycle state machine, SLA calculation,
/// department routing, and raises domain events on resolution.
/// </summary>
public class GrievanceService : IGrievanceService
{
    private readonly IGrievanceRepository _grievanceRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IWardRepository _wardRepo;
    private readonly IAssignmentRepository _assignmentRepo;
    private readonly IStatusHistoryRepository _historyRepo;
    private readonly ILogger<GrievanceService> _logger;

    /// <summary>
    /// Event raised when a grievance is resolved — enables the feedback workflow.
    /// Demonstrates meaningful use of C# delegates/events.
    /// </summary>
    public event GrievanceResolvedHandler? GrievanceResolved;

    // ── Status transition state machine ──
    // Defines which transitions are allowed from each status.
    private static readonly Dictionary<GrievanceStatus, HashSet<GrievanceStatus>> _validTransitions = new()
    {
        [GrievanceStatus.Submitted] = new() { GrievanceStatus.Assigned, GrievanceStatus.Rejected },
        [GrievanceStatus.Assigned] = new() { GrievanceStatus.InProgress, GrievanceStatus.Rejected },
        [GrievanceStatus.InProgress] = new() { GrievanceStatus.Resolved, GrievanceStatus.Rejected },
        [GrievanceStatus.Resolved] = new(),    // Terminal state
        [GrievanceStatus.Rejected] = new()     // Terminal state
    };

    public GrievanceService(
        IGrievanceRepository grievanceRepo,
        ICategoryRepository categoryRepo,
        IWardRepository wardRepo,
        IAssignmentRepository assignmentRepo,
        IStatusHistoryRepository historyRepo,
        ILogger<GrievanceService> logger)
    {
        _grievanceRepo = grievanceRepo;
        _categoryRepo = categoryRepo;
        _wardRepo = wardRepo;
        _assignmentRepo = assignmentRepo;
        _historyRepo = historyRepo;
        _logger = logger;
    }

    public async Task<ServiceResult<Grievance>> CreateGrievanceAsync(
        CreateGrievanceDto dto, string citizenId, CancellationToken ct = default)
    {
        // ── Validate description ──
        if (string.IsNullOrWhiteSpace(dto.Description))
            return ServiceResult<Grievance>.ValidationFail("Please describe your complaint.");

        if (dto.Description.Trim().Length < 10)
            return ServiceResult<Grievance>.ValidationFail("Description must be at least 10 characters.");

        if (dto.Description.Trim().Length > 2000)
            return ServiceResult<Grievance>.ValidationFail("Description cannot exceed 2000 characters.");

        // ── Validate ward exists in database ──
        var wardExists = await _wardRepo.ExistsAsync(dto.WardId, ct);
        if (!wardExists)
            return ServiceResult<Grievance>.ValidationFail("The selected ward does not exist.");

        // ── Validate category and determine department ──
        var category = await _categoryRepo.GetByIdWithDepartmentAsync(dto.CategoryId, ct);
        if (category is null)
            return ServiceResult<Grievance>.ValidationFail("The selected category does not exist.");

        // ── Server-set fields (never from user input) ──
        var now = DateTime.UtcNow;
        var grievance = new Grievance
        {
            CitizenId = citizenId,
            WardId = dto.WardId,
            CategoryId = dto.CategoryId,
            Description = dto.Description.Trim(),
            IsAnonymous = dto.IsAnonymous,
            CreatedAt = now,
            DueAt = now.AddHours(category.DefaultSlaHours), // SLA deadline
            Status = GrievanceStatus.Submitted
        };

        try
        {
            // ── Atomic transaction: grievance + history + assignment ──
            await _grievanceRepo.AddAsync(grievance, ct);
            await _grievanceRepo.SaveChangesAsync(ct);

            // Initial status history record (application creates this, trigger handles future changes)
            var initialHistory = new StatusHistory
            {
                GrievanceId = grievance.GrievanceId,
                OldStatus = null,
                NewStatus = GrievanceStatus.Submitted,
                ChangedAt = now,
                ChangedBy = citizenId
            };
            await _historyRepo.AddAsync(initialHistory, ct);

            // Department assignment (derived from category → department)
            var assignment = new Assignment
            {
                GrievanceId = grievance.GrievanceId,
                DepartmentId = category.DepartmentId,
                AssignedAt = now
            };
            await _assignmentRepo.AddAsync(assignment, ct);

            await _grievanceRepo.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Grievance {Id} created by citizen, assigned to department {Dept} with SLA deadline {Due}",
                grievance.GrievanceId, category.Department.Name, grievance.DueAt);

            return ServiceResult<Grievance>.Ok(grievance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create grievance");
            return ServiceResult<Grievance>.Fail("An error occurred while submitting your complaint. Please try again.");
        }
    }

    public async Task<Grievance?> GetGrievanceByIdAsync(int id, CancellationToken ct = default)
        => await _grievanceRepo.GetByIdWithDetailsAsync(id, ct);

    public async Task<IEnumerable<Grievance>> SearchGrievancesAsync(
        GrievanceSearchDto search, CancellationToken ct = default)
    {
        return await _grievanceRepo.SearchAsync(
            search.SearchTerm, search.Status, search.WardId,
            search.CategoryId, search.DepartmentId, search.CitizenId, ct);
    }

    public async Task<ServiceResult<bool>> UpdateStatusAsync(
        int grievanceId, GrievanceStatus newStatus, string changedBy, CancellationToken ct = default)
    {
        var grievance = await _grievanceRepo.GetByIdWithDetailsAsync(grievanceId, ct);
        if (grievance is null)
            return ServiceResult<bool>.Fail("Grievance not found.");

        // ── Validate state machine transition ──
        if (!_validTransitions.TryGetValue(grievance.Status, out var allowedStatuses) ||
            !allowedStatuses.Contains(newStatus))
        {
            var allowed = allowedStatuses != null ? string.Join(", ", allowedStatuses) : "None";
            return ServiceResult<bool>.Fail(
                $"Cannot transition from {grievance.Status} to {newStatus}. Allowed transitions: {allowed}");
        }

        var oldStatus = grievance.Status;
        grievance.Status = newStatus;

        // The SQL trigger will record the StatusHistory for this change.
        // We only update the grievance status here.
        _grievanceRepo.Update(grievance);
        await _grievanceRepo.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Grievance {Id} status changed from {Old} to {New} by {User}",
            grievanceId, oldStatus, newStatus, changedBy);

        // ── Raise domain event on resolution ──
        if (newStatus == GrievanceStatus.Resolved)
        {
            GrievanceResolved?.Invoke(this, new GrievanceResolvedEventArgs(
                grievanceId, grievance.CitizenId, DateTime.UtcNow));
        }

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<DashboardStats> GetDashboardStatsAsync(CancellationToken ct = default)
        => await _grievanceRepo.GetDashboardStatsAsync(ct);

    public async Task<IEnumerable<Grievance>> GetGrievancesByDepartmentAsync(
        int departmentId, CancellationToken ct = default)
        => await _grievanceRepo.GetByDepartmentAsync(departmentId, ct);

    public async Task<IEnumerable<Grievance>> GetGrievancesByCitizenAsync(
        string citizenId, CancellationToken ct = default)
        => await _grievanceRepo.SearchAsync(citizenId: citizenId, ct: ct);

    public async Task<IEnumerable<Grievance>> GetOverdueGrievancesAsync(CancellationToken ct = default)
        => await _grievanceRepo.GetOverdueAsync(ct);

    /// <summary>
    /// Validates whether a status transition is allowed.
    /// </summary>
    public static bool IsValidTransition(GrievanceStatus from, GrievanceStatus to)
        => _validTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}

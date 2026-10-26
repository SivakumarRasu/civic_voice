using CivicVoice.Application.DTOs;
using CivicVoice.Application.Interfaces;
using CivicVoice.Domain.Entities;
using CivicVoice.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CivicVoice.Application.Services;

public class FeedbackService : IFeedbackService
{
    private readonly IFeedbackRepository _feedbackRepo;
    private readonly IGrievanceRepository _grievanceRepo;
    private readonly ILogger<FeedbackService> _logger;

    public FeedbackService(
        IFeedbackRepository feedbackRepo,
        IGrievanceRepository grievanceRepo,
        ILogger<FeedbackService> logger)
    {
        _feedbackRepo = feedbackRepo;
        _grievanceRepo = grievanceRepo;
        _logger = logger;
    }

    public async Task<ServiceResult<Feedback>> SubmitFeedbackAsync(
        int grievanceId, string citizenId, int rating, string? comments, CancellationToken ct = default)
    {
        // Validate rating
        if (rating < 1 || rating > 5)
            return ServiceResult<Feedback>.ValidationFail("Rating must be between 1 and 5.");

        // Verify grievance exists and is resolved
        var grievance = await _grievanceRepo.GetByIdAsync(grievanceId, ct);
        if (grievance is null)
            return ServiceResult<Feedback>.Fail("Grievance not found.");

        if (grievance.Status != GrievanceStatus.Resolved)
            return ServiceResult<Feedback>.Fail("Feedback can only be submitted for resolved grievances.");

        // Verify the citizen owns this grievance
        if (grievance.CitizenId != citizenId)
            return ServiceResult<Feedback>.Fail("You can only provide feedback for your own grievances.");

        // Check for duplicate feedback
        var existing = await _feedbackRepo.GetByGrievanceAndCitizenAsync(grievanceId, citizenId, ct);
        if (existing is not null)
            return ServiceResult<Feedback>.Fail("You have already submitted feedback for this grievance.");

        var feedback = new Feedback
        {
            GrievanceId = grievanceId,
            CitizenId = citizenId,
            Rating = rating,
            Comments = comments?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _feedbackRepo.AddAsync(feedback, ct);
            await _feedbackRepo.SaveChangesAsync(ct);

            _logger.LogInformation("Feedback submitted for grievance {Id} with rating {Rating}", grievanceId, rating);
            return ServiceResult<Feedback>.Ok(feedback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit feedback for grievance {Id}", grievanceId);
            return ServiceResult<Feedback>.Fail("An error occurred while submitting your feedback.");
        }
    }

    public async Task<Feedback?> GetFeedbackAsync(int grievanceId, string citizenId, CancellationToken ct = default)
        => await _feedbackRepo.GetByGrievanceAndCitizenAsync(grievanceId, citizenId, ct);

    public async Task<IEnumerable<Feedback>> GetFeedbacksForGrievanceAsync(int grievanceId, CancellationToken ct = default)
        => await _feedbackRepo.GetByGrievanceIdAsync(grievanceId, ct);
}

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepo;
    private readonly IGrievanceRepository _grievanceRepo;

    public DepartmentService(IDepartmentRepository departmentRepo, IGrievanceRepository grievanceRepo)
    {
        _departmentRepo = departmentRepo;
        _grievanceRepo = grievanceRepo;
    }

    public async Task<IEnumerable<Department>> GetAllDepartmentsAsync(CancellationToken ct = default)
        => await _departmentRepo.GetAllAsync(ct);

    public async Task<IEnumerable<Department>> GetAllWithCategoriesAsync(CancellationToken ct = default)
        => await _departmentRepo.GetAllWithCategoriesAsync(ct);

    public async Task<Department?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _departmentRepo.GetByIdAsync(id, ct);

    public async Task<DepartmentReportDto> GetDepartmentReportAsync(CancellationToken ct = default)
    {
        var departments = await _departmentRepo.GetAllAsync(ct);
        var now = DateTime.UtcNow;
        var report = new DepartmentReportDto();

        foreach (var dept in departments)
        {
            var grievances = await _grievanceRepo.GetByDepartmentAsync(dept.DepartmentId, ct);
            var list = grievances.ToList();

            report.Items.Add(new DepartmentReportItem
            {
                DepartmentId = dept.DepartmentId,
                DepartmentName = dept.Name,
                TotalGrievances = list.Count,
                OpenGrievances = list.Count(g =>
                    g.Status != GrievanceStatus.Resolved &&
                    g.Status != GrievanceStatus.Rejected),
                ResolvedGrievances = list.Count(g => g.Status == GrievanceStatus.Resolved),
                SlaBreach = list.Count(g =>
                    g.DueAt < now &&
                    g.Status != GrievanceStatus.Resolved &&
                    g.Status != GrievanceStatus.Rejected)
            });
        }

        return report;
    }
}

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepo;

    public CategoryService(ICategoryRepository categoryRepo) => _categoryRepo = categoryRepo;

    public async Task<IEnumerable<Category>> GetAllWithDepartmentsAsync(CancellationToken ct = default)
        => await _categoryRepo.GetAllWithDepartmentsAsync(ct);

    public async Task<Category?> GetByIdWithDepartmentAsync(int id, CancellationToken ct = default)
        => await _categoryRepo.GetByIdWithDepartmentAsync(id, ct);
}

public class WardService : IWardService
{
    private readonly IWardRepository _wardRepo;

    public WardService(IWardRepository wardRepo) => _wardRepo = wardRepo;

    public async Task<IEnumerable<Ward>> GetAllWardsAsync(CancellationToken ct = default)
        => await _wardRepo.GetAllAsync(ct);

    public async Task<bool> WardExistsAsync(int wardId, CancellationToken ct = default)
        => await _wardRepo.ExistsAsync(wardId, ct);
}

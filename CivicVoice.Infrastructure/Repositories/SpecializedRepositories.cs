using CivicVoice.Application.Interfaces;
using CivicVoice.Domain.Entities;
using CivicVoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicVoice.Infrastructure.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(CivicVoiceDbContext context) : base(context) { }

    public async Task<Category?> GetByIdWithDepartmentAsync(int id, CancellationToken ct = default)
        => await _dbSet.Include(c => c.Department).FirstOrDefaultAsync(c => c.CategoryId == id, ct);

    public async Task<IEnumerable<Category>> GetAllWithDepartmentsAsync(CancellationToken ct = default)
        => await _dbSet.Include(c => c.Department).OrderBy(c => c.Name).ToListAsync(ct);
}

public class WardRepository : Repository<Ward>, IWardRepository
{
    public WardRepository(CivicVoiceDbContext context) : base(context) { }

    public async Task<bool> ExistsAsync(int wardId, CancellationToken ct = default)
        => await _dbSet.AnyAsync(w => w.WardId == wardId, ct);
}

public class DepartmentRepository : Repository<Department>, IDepartmentRepository
{
    public DepartmentRepository(CivicVoiceDbContext context) : base(context) { }

    public async Task<IEnumerable<Department>> GetAllWithCategoriesAsync(CancellationToken ct = default)
        => await _dbSet.Include(d => d.Categories).OrderBy(d => d.Name).ToListAsync(ct);
}

public class AssignmentRepository : Repository<Assignment>, IAssignmentRepository
{
    public AssignmentRepository(CivicVoiceDbContext context) : base(context) { }

    public async Task<IEnumerable<Assignment>> GetByGrievanceIdAsync(int grievanceId, CancellationToken ct = default)
        => await _dbSet.Include(a => a.Department)
            .Where(a => a.GrievanceId == grievanceId)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(ct);
}

public class StatusHistoryRepository : Repository<StatusHistory>, IStatusHistoryRepository
{
    public StatusHistoryRepository(CivicVoiceDbContext context) : base(context) { }

    public async Task<IEnumerable<StatusHistory>> GetByGrievanceIdAsync(int grievanceId, CancellationToken ct = default)
        => await _dbSet
            .Where(s => s.GrievanceId == grievanceId)
            .OrderBy(s => s.ChangedAt)
            .ToListAsync(ct);
}

public class FeedbackRepository : Repository<Feedback>, IFeedbackRepository
{
    public FeedbackRepository(CivicVoiceDbContext context) : base(context) { }

    public async Task<Feedback?> GetByGrievanceAndCitizenAsync(int grievanceId, string citizenId, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(f => f.GrievanceId == grievanceId && f.CitizenId == citizenId, ct);

    public async Task<IEnumerable<Feedback>> GetByGrievanceIdAsync(int grievanceId, CancellationToken ct = default)
        => await _dbSet.Where(f => f.GrievanceId == grievanceId).OrderByDescending(f => f.CreatedAt).ToListAsync(ct);
}

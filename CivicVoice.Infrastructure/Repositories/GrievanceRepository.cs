using CivicVoice.Application.Interfaces;
using CivicVoice.Domain.Entities;
using CivicVoice.Domain.Enums;
using CivicVoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicVoice.Infrastructure.Repositories;

public class GrievanceRepository : Repository<Grievance>, IGrievanceRepository
{
    public GrievanceRepository(CivicVoiceDbContext context) : base(context) { }

    public async Task<Grievance?> GetByIdWithDetailsAsync(int id, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(g => g.Ward)
            .Include(g => g.Category)
                .ThenInclude(c => c.Department)
            .Include(g => g.Assignments)
                .ThenInclude(a => a.Department)
            .Include(g => g.StatusHistories)
            .Include(g => g.Feedbacks)
            .FirstOrDefaultAsync(g => g.GrievanceId == id, ct);
    }

    public async Task<IEnumerable<Grievance>> SearchAsync(
        string? searchTerm = null,
        GrievanceStatus? status = null,
        int? wardId = null,
        int? categoryId = null,
        int? departmentId = null,
        string? citizenId = null,
        CancellationToken ct = default)
    {
        // LINQ filtering — builds query incrementally
        IQueryable<Grievance> query = _dbSet
            .Include(g => g.Ward)
            .Include(g => g.Category)
                .ThenInclude(c => c.Department)
            .Include(g => g.Assignments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(g =>
                g.Description.ToLower().Contains(term) ||
                g.Ward.Name.ToLower().Contains(term) ||
                g.Category.Name.ToLower().Contains(term));
        }

        if (status.HasValue)
            query = query.Where(g => g.Status == status.Value);

        if (wardId.HasValue)
            query = query.Where(g => g.WardId == wardId.Value);

        if (categoryId.HasValue)
            query = query.Where(g => g.CategoryId == categoryId.Value);

        if (departmentId.HasValue)
            query = query.Where(g => g.Assignments.Any(a => a.DepartmentId == departmentId.Value));

        if (!string.IsNullOrWhiteSpace(citizenId))
            query = query.Where(g => g.CitizenId == citizenId);

        return await query
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Grievance>> GetByDepartmentAsync(int departmentId, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(g => g.Ward)
            .Include(g => g.Category)
            .Include(g => g.Assignments)
            .Where(g => g.Assignments.Any(a => a.DepartmentId == departmentId))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Grievance>> GetOverdueAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(g => g.Ward)
            .Include(g => g.Category)
            .Where(g => g.DueAt < now &&
                        g.Status != GrievanceStatus.Resolved &&
                        g.Status != GrievanceStatus.Rejected)
            .OrderBy(g => g.DueAt)
            .ToListAsync(ct);
    }

    public async Task<DashboardStats> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // LINQ aggregation — single database round trip with projections
        var stats = await _dbSet
            .GroupBy(_ => 1)
            .Select(g => new DashboardStats
            {
                TotalGrievances = g.Count(),
                OpenGrievances = g.Count(x =>
                    x.Status != GrievanceStatus.Resolved &&
                    x.Status != GrievanceStatus.Rejected),
                ResolvedGrievances = g.Count(x => x.Status == GrievanceStatus.Resolved),
                SlaBreach = g.Count(x =>
                    x.DueAt < now &&
                    x.Status != GrievanceStatus.Resolved &&
                    x.Status != GrievanceStatus.Rejected)
            })
            .FirstOrDefaultAsync(ct);

        return stats ?? new DashboardStats();
    }
}

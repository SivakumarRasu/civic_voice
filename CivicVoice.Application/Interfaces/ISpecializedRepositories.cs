using CivicVoice.Domain.Entities;

namespace CivicVoice.Application.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetByIdWithDepartmentAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<Category>> GetAllWithDepartmentsAsync(CancellationToken ct = default);
}

public interface IWardRepository : IRepository<Ward>
{
    Task<bool> ExistsAsync(int wardId, CancellationToken ct = default);
}

public interface IDepartmentRepository : IRepository<Department>
{
    Task<IEnumerable<Department>> GetAllWithCategoriesAsync(CancellationToken ct = default);
}

public interface IAssignmentRepository : IRepository<Assignment>
{
    Task<IEnumerable<Assignment>> GetByGrievanceIdAsync(int grievanceId, CancellationToken ct = default);
}

public interface IStatusHistoryRepository : IRepository<StatusHistory>
{
    Task<IEnumerable<StatusHistory>> GetByGrievanceIdAsync(int grievanceId, CancellationToken ct = default);
}

public interface IFeedbackRepository : IRepository<Feedback>
{
    Task<Feedback?> GetByGrievanceAndCitizenAsync(int grievanceId, string citizenId, CancellationToken ct = default);
    Task<IEnumerable<Feedback>> GetByGrievanceIdAsync(int grievanceId, CancellationToken ct = default);
}

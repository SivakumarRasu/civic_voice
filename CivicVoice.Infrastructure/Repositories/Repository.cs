using System.Linq.Expressions;
using CivicVoice.Application.Interfaces;
using CivicVoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CivicVoice.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation using EF Core.
/// Demonstrates generics — one class handles CRUD for any entity type.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly CivicVoiceDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(CivicVoiceDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _dbSet.FindAsync(new object[] { id }, ct);

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.Where(predicate).ToListAsync(ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await _dbSet.AddAsync(entity, ct);

    public virtual void Update(T entity)
        => _dbSet.Update(entity);

    public virtual void Remove(T entity)
        => _dbSet.Remove(entity);

    public virtual async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}

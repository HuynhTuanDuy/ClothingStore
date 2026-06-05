using ClothingStore.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ClothingStore.Repositories;

public class Repository<TEntity>(StoreDbContext dbContext) : IRepository<TEntity> where TEntity : class
{
    public IQueryable<TEntity> Query() => dbContext.Set<TEntity>();

    public async Task<TEntity?> FindAsync(params object[] keyValues)
    {
        return await dbContext.Set<TEntity>().FindAsync(keyValues);
    }

    public async Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        var query = dbContext.Set<TEntity>().AsQueryable();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync();
    }

    public async Task AddAsync(TEntity entity)
    {
        await dbContext.Set<TEntity>().AddAsync(entity);
    }

    public void Update(TEntity entity)
    {
        dbContext.Set<TEntity>().Update(entity);
    }

    public void Remove(TEntity entity)
    {
        dbContext.Set<TEntity>().Remove(entity);
    }

    public Task<int> SaveChangesAsync() => dbContext.SaveChangesAsync();
}

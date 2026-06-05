using ClothingStore.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClothingStore.Repositories;

public class UnitOfWork(StoreDbContext dbContext) : IUnitOfWork
{
    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return dbContext.Database.BeginTransactionAsync();
    }

    public Task<int> SaveChangesAsync()
    {
        return dbContext.SaveChangesAsync();
    }
}

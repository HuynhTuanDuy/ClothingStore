using Microsoft.EntityFrameworkCore.Storage;

namespace ClothingStore.Repositories;

public interface IUnitOfWork
{
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task<int> SaveChangesAsync();
}

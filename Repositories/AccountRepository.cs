using ClothingStore.Data;
using ClothingStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByUsernameAsync(string username);
    Task<Account?> GetByEmailAsync(string email);
    Task<Account?> GetByIdAsync(int userId);
    Task<Account?> GetWithRolesAsync(int userId);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task AddAccountAsync(Account account);
    Task<List<string>> GetRolesAsync(int userId);
}

public class AccountRepository(StoreDbContext dbContext) : IAccountRepository
{
    public Task<Account?> GetByUsernameAsync(string username)
    {
        return dbContext.Accounts
            .Include(x => x.AccountRoles).ThenInclude(x => x.Role)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.UserName == username && x.Status == AccountStatus.Active);
    }

    public Task<Account?> GetByEmailAsync(string email)
    {
        return dbContext.Accounts
            .Include(x => x.AccountRoles).ThenInclude(x => x.Role)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Email == email && x.Status == AccountStatus.Active);
    }

    public Task<Account?> GetByIdAsync(int userId)
    {
        return dbContext.Accounts
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public Task<Account?> GetWithRolesAsync(int userId)
    {
        return dbContext.Accounts
            .Include(x => x.AccountRoles).ThenInclude(x => x.Role)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public Task<bool> UsernameExistsAsync(string username)
    {
        return dbContext.Accounts.AnyAsync(x => x.UserName == username);
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        return dbContext.Accounts.AnyAsync(x => x.Email == email);
    }

    public async Task AddAccountAsync(Account account)
    {
        await dbContext.Accounts.AddAsync(account);
    }

    public async Task<List<string>> GetRolesAsync(int userId)
    {
        return await dbContext.AccountRoles
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => x.Role.Name)
            .ToListAsync();
    }
}

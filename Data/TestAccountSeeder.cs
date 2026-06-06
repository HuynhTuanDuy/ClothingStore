using ClothingStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Data;

public static class TestAccountSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
        
        // Ensure database is created/migrated
        // await context.Database.MigrateAsync(); // Un-comment if you use migrations

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");

        var testAccounts = new List<(string Username, string Role, string Email)>
        {
            ("admin", "Admin", "admin@wearwhatever.local"),
            ("manager", "Manager", "manager@wearwhatever.local"),
            ("staff", "Staff", "staff@wearwhatever.local")
        };

        foreach (var acc in testAccounts)
        {
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == acc.Role);
            if (role == null)
            {
                role = new Role { Name = acc.Role, NormalizedName = acc.Role.ToUpper() };
                context.Roles.Add(role);
                await context.SaveChangesAsync();
            }

            if (!await context.Accounts.AnyAsync(a => a.UserName == acc.Username))
            {
                var newAccount = new Account
                {
                    UserName = acc.Username,
                    Email = acc.Email,
                    PasswordHash = passwordHash,
                    Status = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AccountRoles = new List<AccountRole>
                    {
                        new AccountRole { Role = role }
                    }
                };
                context.Accounts.Add(newAccount);
            }
        }

        await context.SaveChangesAsync();
    }
}

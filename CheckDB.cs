using System;
using System.Threading.Tasks;
using System.Text.Json;
using ClothingStore.Data;
using ClothingStore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CheckDB
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddDbContext<StoreDbContext>(options =>
                options.UseSqlServer("Server=.;Database=WEB_CUAHANGQUANAO;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"));
            var provider = services.BuildServiceProvider();
            var db = provider.GetRequiredService<StoreDbContext>();
            var repo = new OrderRepository(db);
            
            var rev = await repo.GetMonthlyRevenueAsync(2026);
            Console.WriteLine(JsonSerializer.Serialize(rev));
        }
    }
}

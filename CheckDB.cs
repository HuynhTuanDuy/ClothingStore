using System;
using System.Linq;
using System.Threading.Tasks;
using ClothingStore.Data;
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
            
            Console.WriteLine("--- Báo cáo Dữ liệu ---");

            var negativePrices = await db.ProductVariants.Where(v => v.SellingPrice < 0).Select(v => v.SKU).ToListAsync();
            Console.WriteLine($"Variants với Giá âm: {negativePrices.Count}");

            var negativeStocks = await db.ProductVariants.Where(v => v.StockQuantity < 0).Select(v => v.SKU).ToListAsync();
            Console.WriteLine($"Variants với Tồn kho âm: {negativeStocks.Count}");

            var skus = await db.ProductVariants.Select(v => v.SKU).ToListAsync();
            var duplicateSkus = skus.GroupBy(x => x).Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key)).Select(g => g.Key).ToList();
            Console.WriteLine($"SKU trùng lặp: {duplicateSkus.Count}");
            if (duplicateSkus.Any()) Console.WriteLine(" - " + string.Join(", ", duplicateSkus));

            var slugs = await db.Products.Select(p => p.Slug).ToListAsync();
            var duplicateSlugs = slugs.GroupBy(x => x).Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key)).Select(g => g.Key).ToList();
            Console.WriteLine($"Slug trùng lặp: {duplicateSlugs.Count}");
            if (duplicateSlugs.Any()) Console.WriteLine(" - " + string.Join(", ", duplicateSlugs));

            var orphanVariants = await db.ProductVariants.Where(v => !db.Products.Any(p => p.ProductID == v.ProductID)).Select(v => v.SKU).ToListAsync();
            Console.WriteLine($"Biến thể mồ côi: {orphanVariants.Count}");

            Console.WriteLine("--- Hoàn tất ---");
        }
    }
}

using ClothingStore.Data;
using ClothingStore.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ClothingStore.Services
{
    public class AddressService(StoreDbContext dbContext, IMemoryCache cache) : IAddressService
    {
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);

        public async Task<IEnumerable<Province>> GetProvincesAsync()
        {
            return await cache.GetOrCreateAsync("provinces_active", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                return await dbContext.Provinces
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }) ?? new List<Province>();
        }

        public async Task<IEnumerable<District>> GetDistrictsAsync(int provinceId)
        {
            return await cache.GetOrCreateAsync($"districts_active_{provinceId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                return await dbContext.Districts
                    .AsNoTracking()
                    .Where(d => d.ProvinceId == provinceId && d.IsActive)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
            }) ?? new List<District>();
        }

        public async Task<IEnumerable<Ward>> GetWardsAsync(int districtId)
        {
            return await cache.GetOrCreateAsync($"wards_active_{districtId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                return await dbContext.Wards
                    .AsNoTracking()
                    .Where(w => w.DistrictId == districtId && w.IsActive)
                    .OrderBy(w => w.Name)
                    .ToListAsync();
            }) ?? new List<Ward>();
        }

        public async Task<Province?> GetProvinceByIdAsync(int provinceId)
        {
            var provinces = await GetProvincesAsync();
            return provinces.FirstOrDefault(p => p.ProvinceId == provinceId);
        }

        public async Task<District?> GetDistrictByIdAsync(int districtId)
        {
            // Fetch from DB if we need just one to validate (or we can use cache if we want)
            // Caching specific district might not be needed since we cache the list, but for validation
            // we can just use the DB directly or fetch from cached list. Let's use DB to be safe or cache for speed.
            return await dbContext.Districts.AsNoTracking().FirstOrDefaultAsync(d => d.DistrictId == districtId && d.IsActive);
        }

        public async Task<Ward?> GetWardByIdAsync(int wardId)
        {
            return await dbContext.Wards.AsNoTracking().FirstOrDefaultAsync(w => w.WardId == wardId && w.IsActive);
        }
    }
}

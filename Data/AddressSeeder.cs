using System.Text.Json;
using System.Text.Json.Serialization;
using ClothingStore.Models.Entities;

namespace ClothingStore.Data
{
    public class AddressSeeder
    {
        public static async Task SeedAsync(StoreDbContext context, IWebHostEnvironment env)
        {
            if (context.Provinces.Any())
            {
                return; // Already seeded
            }

            var filePath = Path.Combine(env.WebRootPath, "data", "vietnam-administrative.json");
            if (!File.Exists(filePath))
            {
                return;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var provincesData = JsonSerializer.Deserialize<List<ProvinceJsonModel>>(json);

            if (provincesData == null || provincesData.Count == 0) return;

            var provinces = new List<Province>();
            var districts = new List<District>();
            var wards = new List<Ward>();

            foreach (var p in provincesData)
            {
                if (!int.TryParse(p.Id, out int pId)) continue;

                provinces.Add(new Province
                {
                    ProvinceId = pId,
                    Code = p.Id,
                    Name = p.Name,
                    IsActive = true
                });

                if (p.Districts != null)
                {
                    foreach (var d in p.Districts)
                    {
                        if (!int.TryParse(d.Id, out int dId)) continue;

                        districts.Add(new District
                        {
                            DistrictId = dId,
                            ProvinceId = pId,
                            Code = d.Id,
                            Name = d.Name,
                            IsActive = true
                        });

                        if (d.Wards != null)
                        {
                            foreach (var w in d.Wards)
                            {
                                if (!int.TryParse(w.Id, out int wId)) continue;

                                wards.Add(new Ward
                                {
                                    WardId = wId,
                                    DistrictId = dId,
                                    Code = w.Id,
                                    Name = w.Name,
                                    IsActive = true
                                });
                            }
                        }
                    }
                }
            }

            // Insert in batches or entirely if it's not too huge. EF Core can handle this amount.
            await context.Provinces.AddRangeAsync(provinces);
            await context.Districts.AddRangeAsync(districts);
            await context.Wards.AddRangeAsync(wards);
            
            await context.SaveChangesAsync();
        }

        private class ProvinceJsonModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public List<DistrictJsonModel>? Districts { get; set; }
        }

        private class DistrictJsonModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public List<WardJsonModel>? Wards { get; set; }
        }

        private class WardJsonModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Level { get; set; } = string.Empty;
        }
    }
}

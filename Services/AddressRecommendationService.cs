using ClothingStore.Data;
using ClothingStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Services;

public class AddressRecommendationService(StoreDbContext dbContext) : IAddressRecommendationService
{
    public async Task<int?> GetSuggestedAddressAsync(int customerId)
    {
        // Lấy 10 đơn gần nhất có ShippingAddressId và đã giao thành công
        var recentOrders = await dbContext.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId 
                     && o.ShippingAddressId != null
                     && (o.OrderStatus == OrderStatus.Delivered || o.OrderStatus == OrderStatus.DeliveredPendingCOD))
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .Select(o => o.ShippingAddressId!.Value)
            .ToListAsync();

        int totalCount = recentOrders.Count;
        
        // Cần ít nhất 5 đơn để đưa ra gợi ý
        if (totalCount < 5) return null;

        var addressGroups = recentOrders
            .GroupBy(id => id)
            .Select(g => new { AddressId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        if (addressGroups.Count == 0) return null;

        var topAddress = addressGroups.First();

        // Nếu tần suất sử dụng >= 50%
        if ((double)topAddress.Count / totalCount >= 0.5)
        {
            // Tránh thiên vị khi hòa (ví dụ 5/10 đơn cho A, 5/10 đơn cho B -> hòa)
            if (addressGroups.Count > 1 && addressGroups[1].Count == topAddress.Count)
            {
                return null;
            }

            // Kiểm tra địa chỉ có còn tồn tại và thuộc về user không
            var isAddressActive = await dbContext.ShippingAddresses
                .AnyAsync(a => a.AddressID == topAddress.AddressId && a.CustomerId == customerId);

            if (isAddressActive)
            {
                return topAddress.AddressId;
            }
        }

        return null;
    }
}

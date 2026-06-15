using ClothingStore.Data;
using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;
using ClothingStore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Services;

public interface ICustomerAccountService
{
    Task<CustomerDashboardViewModel?> GetDashboardAsync(int customerId);
    Task<CustomerProfileViewModel?> GetProfileAsync(int customerId);
    Task<(bool Success, string? Error)> UpdateProfileAsync(int customerId, CustomerProfileViewModel model);
    Task<(bool Success, string? Error)> ChangePasswordAsync(int accountId, CustomerChangePasswordViewModel model);
    Task<CustomerOrdersViewModel> GetCustomerOrdersAsync(int customerId, int page = 1, int pageSize = 10);
    Task<Order?> GetCustomerOrderDetailsAsync(int customerId, string orderCode);
    Task<List<ShippingAddress>> GetAddressesAsync(int customerId);
    Task<ShippingAddress?> GetAddressAsync(int customerId, int addressId);
    Task<(bool Success, string? Error)> CreateAddressAsync(int customerId, AddressFormViewModel model);
    Task<(bool Success, string? Error)> UpdateAddressAsync(int customerId, AddressFormViewModel model);
    Task<(bool Success, string? Error)> DeleteAddressAsync(int customerId, int addressId);
    Task<(bool Success, string? Error)> SetDefaultAddressAsync(int customerId, int addressId);
}

public class CustomerAccountService(
    StoreDbContext dbContext,
    IOrderRepository orderRepository,
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    ILogger<CustomerAccountService> logger,
    IAddressService addressService) : ICustomerAccountService
{
    public async Task<CustomerDashboardViewModel?> GetDashboardAsync(int customerId)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (customer == null) return null;

        var orderStats = await dbContext.Orders
            .Where(o => o.CustomerId == customerId)
            .GroupBy(o => o.OrderStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var pendingOrders = orderStats.Where(s => s.Status == OrderStatus.Pending || s.Status == OrderStatus.Confirmed).Sum(s => s.Count);
        var shippingOrders = orderStats.Where(s => s.Status == OrderStatus.Shipping).Sum(s => s.Count);
        var completedOrders = orderStats.Where(s => s.Status == OrderStatus.Delivered).Sum(s => s.Count);

        return new CustomerDashboardViewModel
        {
            CustomerName = customer.FullName,
            TotalOrders = orderStats.Sum(s => s.Count),
            PendingOrders = pendingOrders,
            ShippingOrders = shippingOrders,
            CompletedOrders = completedOrders,
            RewardPoints = customer.RewardPoints
        };
    }

    public async Task<CustomerProfileViewModel?> GetProfileAsync(int customerId)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (customer == null) return null;

        return new CustomerProfileViewModel
        {
            FullName = customer.FullName,
            Phone = customer.Phone,
            Email = customer.Account?.Email ?? customer.Email ?? string.Empty
        };
    }

    public async Task<(bool Success, string? Error)> UpdateProfileAsync(int customerId, CustomerProfileViewModel model)
    {
        var customer = await dbContext.Customers.FindAsync(customerId);
        if (customer == null) return (false, "Không tìm thấy khách hàng.");

        customer.FullName = model.FullName;
        customer.Phone = model.Phone;
        customer.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync();
        logger.LogInformation("Customer {CustomerId} profile updated successfully.", customerId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(int accountId, CustomerChangePasswordViewModel model)
    {
        var account = await accountRepository.GetByIdAsync(accountId);
        if (account == null) return (false, "Không tìm thấy tài khoản.");

        if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, account.PasswordHash))
        {
            logger.LogWarning("Account {AccountId} attempted to change password with invalid current password.", accountId);
            return (false, "Mật khẩu hiện tại không đúng.");
        }

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        account.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync();
        logger.LogInformation("Account {AccountId} changed password successfully.", accountId);

        return (true, null);
    }

    public async Task<CustomerOrdersViewModel> GetCustomerOrdersAsync(int customerId, int page = 1, int pageSize = 10)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate);

        var totalRecords = await query.CountAsync();
        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new CustomerOrdersViewModel
        {
            Orders = orders,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
        };
    }

    public async Task<Order?> GetCustomerOrderDetailsAsync(int customerId, string orderCode)
    {
        var order = await orderRepository.GetOrderByCodeAsync(orderCode);
        
        // Ownership Validation handled at Controller level too, but adding here as well
        if (order != null && order.CustomerId == customerId)
        {
            return order;
        }
        
        return null;
    }

    public async Task<List<ShippingAddress>> GetAddressesAsync(int customerId)
    {
        return await dbContext.ShippingAddresses
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.AddressID)
            .ToListAsync();
    }

    public async Task<ShippingAddress?> GetAddressAsync(int customerId, int addressId)
    {
        return await dbContext.ShippingAddresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.CustomerId == customerId && a.AddressID == addressId);
    }

    public async Task<(bool Success, string? Error)> CreateAddressAsync(int customerId, AddressFormViewModel model)
    {
        var addressesCount = await dbContext.ShippingAddresses.CountAsync(a => a.CustomerId == customerId);
        bool isDefault = addressesCount == 0 || model.IsDefault;

        if (model.ProvinceId == null || model.DistrictId == null || model.WardId == null)
            return (false, "Vui lòng chọn đầy đủ Tỉnh, Quận, Phường.");
            
        var province = await addressService.GetProvinceByIdAsync(model.ProvinceId.Value);
        var district = await addressService.GetDistrictByIdAsync(model.DistrictId.Value);
        var ward = await addressService.GetWardByIdAsync(model.WardId.Value);
        
        if (province == null || district == null || ward == null)
            return (false, "Dữ liệu địa chỉ không hợp lệ.");
            
        if (district.ProvinceId != province.ProvinceId || ward.DistrictId != district.DistrictId)
            return (false, "Dữ liệu phường/quận/tỉnh không khớp nhau.");

        await using var transaction = await unitOfWork.BeginTransactionAsync();
        try
        {
            if (isDefault && addressesCount > 0)
            {
                await dbContext.ShippingAddresses
                    .Where(a => a.CustomerId == customerId && a.IsDefault)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
            }

            var newAddress = new ShippingAddress
            {
                CustomerId = customerId,
                AddressName = string.IsNullOrWhiteSpace(model.AddressName) ? null : model.AddressName.Trim(),
                RecipientName = model.RecipientName,
                ReceiverPhone = model.ReceiverPhone,
                AddressLine = model.AddressLine,
                WardId = model.WardId,
                DistrictId = model.DistrictId,
                ProvinceId = model.ProvinceId,
                Note = model.Note,
                Ward = ward.Name,
                District = district.Name,
                Province = province.Name,
                IsDefault = isDefault
            };

            await dbContext.ShippingAddresses.AddAsync(newAddress);
            await unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation("Customer {CustomerId} added a new address.", customerId);
            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error creating address for customer {CustomerId}", customerId);
            return (false, "Có lỗi xảy ra khi thêm địa chỉ. Vui lòng thử lại.");
        }
    }

    public async Task<(bool Success, string? Error)> UpdateAddressAsync(int customerId, AddressFormViewModel model)
    {
        var address = await dbContext.ShippingAddresses.FirstOrDefaultAsync(a => a.CustomerId == customerId && a.AddressID == model.AddressID);
        if (address == null)
        {
            logger.LogWarning("Customer {CustomerId} attempted to update a non-existent address {AddressId}.", customerId, model.AddressID);
            return (false, "Không tìm thấy địa chỉ.");
        }

        if (model.ProvinceId == null || model.DistrictId == null || model.WardId == null)
            return (false, "Vui lòng chọn đầy đủ Tỉnh, Quận, Phường.");
            
        var province = await addressService.GetProvinceByIdAsync(model.ProvinceId.Value);
        var district = await addressService.GetDistrictByIdAsync(model.DistrictId.Value);
        var ward = await addressService.GetWardByIdAsync(model.WardId.Value);
        
        if (province == null || district == null || ward == null)
            return (false, "Dữ liệu địa chỉ không hợp lệ.");
            
        if (district.ProvinceId != province.ProvinceId || ward.DistrictId != district.DistrictId)
            return (false, "Dữ liệu phường/quận/tỉnh không khớp nhau.");

        await using var transaction = await unitOfWork.BeginTransactionAsync();
        try
        {
            if (model.IsDefault && !address.IsDefault)
            {
                await dbContext.ShippingAddresses
                    .Where(a => a.CustomerId == customerId && a.IsDefault)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
            }

            address.AddressName = string.IsNullOrWhiteSpace(model.AddressName) ? null : model.AddressName.Trim();
            address.RecipientName = model.RecipientName;
            address.ReceiverPhone = model.ReceiverPhone;
            address.AddressLine = model.AddressLine;
            address.WardId = model.WardId;
            address.DistrictId = model.DistrictId;
            address.ProvinceId = model.ProvinceId;
            address.Note = model.Note;
            address.Ward = ward.Name;
            address.District = district.Name;
            address.Province = province.Name;
            
            if (model.IsDefault)
            {
                address.IsDefault = true;
            }

            await unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error updating address {AddressId} for customer {CustomerId}", model.AddressID, customerId);
            return (false, "Có lỗi xảy ra khi cập nhật địa chỉ. Vui lòng thử lại.");
        }
    }

    public async Task<(bool Success, string? Error)> DeleteAddressAsync(int customerId, int addressId)
    {
        var address = await dbContext.ShippingAddresses.FirstOrDefaultAsync(a => a.CustomerId == customerId && a.AddressID == addressId);
        if (address == null)
        {
            logger.LogWarning("Customer {CustomerId} attempted to delete a non-existent address {AddressId}.", customerId, addressId);
            return (false, "Không tìm thấy địa chỉ.");
        }

        dbContext.ShippingAddresses.Remove(address);
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation("Customer {CustomerId} deleted address {AddressId}.", customerId, addressId);

        // If we deleted the default, set another one as default
        if (address.IsDefault)
        {
            var firstAddress = await dbContext.ShippingAddresses.FirstOrDefaultAsync(a => a.CustomerId == customerId);
            if (firstAddress != null)
            {
                firstAddress.IsDefault = true;
                await unitOfWork.SaveChangesAsync();
            }
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> SetDefaultAddressAsync(int customerId, int addressId)
    {
        var addressExists = await dbContext.ShippingAddresses.AnyAsync(a => a.CustomerId == customerId && a.AddressID == addressId);
        if (!addressExists) return (false, "Không tìm thấy địa chỉ.");

        await using var transaction = await unitOfWork.BeginTransactionAsync();
        try
        {
            await dbContext.ShippingAddresses
                .Where(a => a.CustomerId == customerId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));

            await dbContext.ShippingAddresses
                .Where(a => a.CustomerId == customerId && a.AddressID == addressId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, true));

            await transaction.CommitAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error setting default address {AddressId} for customer {CustomerId}", addressId, customerId);
            return (false, "Có lỗi xảy ra, vui lòng thử lại.");
        }
    }
}

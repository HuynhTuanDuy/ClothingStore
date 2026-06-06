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
    ILogger<CustomerAccountService> logger) : ICustomerAccountService
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

        if (isDefault && addressesCount > 0)
        {
            var defaults = await dbContext.ShippingAddresses.Where(a => a.CustomerId == customerId && a.IsDefault).ToListAsync();
            foreach (var d in defaults)
            {
                d.IsDefault = false;
            }
        }

        var newAddress = new ShippingAddress
        {
            CustomerId = customerId,
            RecipientName = model.RecipientName,
            ReceiverPhone = model.ReceiverPhone,
            AddressLine = model.AddressLine,
            Ward = model.Ward,
            District = model.District,
            Province = model.Province,
            IsDefault = isDefault
        };

        await dbContext.ShippingAddresses.AddAsync(newAddress);
        await unitOfWork.SaveChangesAsync();
        logger.LogInformation("Customer {CustomerId} added a new address.", customerId);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAddressAsync(int customerId, AddressFormViewModel model)
    {
        var address = await dbContext.ShippingAddresses.FirstOrDefaultAsync(a => a.CustomerId == customerId && a.AddressID == model.AddressID);
        if (address == null)
        {
            logger.LogWarning("Customer {CustomerId} attempted to update a non-existent address {AddressId}.", customerId, model.AddressID);
            return (false, "Không tìm thấy địa chỉ.");
        }

        if (model.IsDefault && !address.IsDefault)
        {
            var defaults = await dbContext.ShippingAddresses.Where(a => a.CustomerId == customerId && a.IsDefault).ToListAsync();
            foreach (var d in defaults)
            {
                d.IsDefault = false;
            }
        }

        address.RecipientName = model.RecipientName;
        address.ReceiverPhone = model.ReceiverPhone;
        address.AddressLine = model.AddressLine;
        address.Ward = model.Ward;
        address.District = model.District;
        address.Province = model.Province;
        
        if (model.IsDefault)
        {
            address.IsDefault = true;
        }

        await unitOfWork.SaveChangesAsync();
        return (true, null);
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
        var addresses = await dbContext.ShippingAddresses.Where(a => a.CustomerId == customerId).ToListAsync();
        if (!addresses.Any(a => a.AddressID == addressId)) return (false, "Không tìm thấy địa chỉ.");

        foreach (var a in addresses)
        {
            a.IsDefault = a.AddressID == addressId;
        }

        await unitOfWork.SaveChangesAsync();
        return (true, null);
    }
}

using ClothingStore.Models.Entities;

namespace ClothingStore.Services
{
    public interface IAddressService
    {
        Task<IEnumerable<Province>> GetProvincesAsync();
        Task<IEnumerable<District>> GetDistrictsAsync(int provinceId);
        Task<IEnumerable<Ward>> GetWardsAsync(int districtId);
        Task<Province?> GetProvinceByIdAsync(int provinceId);
        Task<District?> GetDistrictByIdAsync(int districtId);
        Task<Ward?> GetWardByIdAsync(int wardId);
    }
}

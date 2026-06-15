using ClothingStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClothingStore.Controllers
{
    [Route("[controller]")]
    public class AddressController(IAddressService addressService) : Controller
    {
        [HttpGet("Districts")]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            var districts = await addressService.GetDistrictsAsync(provinceId);
            return Json(districts.Select(d => new { d.DistrictId, d.Name }));
        }

        [HttpGet("Wards")]
        public async Task<IActionResult> GetWards(int districtId)
        {
            var wards = await addressService.GetWardsAsync(districtId);
            return Json(wards.Select(w => new { w.WardId, w.Name }));
        }
    }
}

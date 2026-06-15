using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;

namespace ClothingStore.Services;

public interface IDiscountProgramService
{
    Task<(IEnumerable<DiscountProgram> Programs, DiscountProgramDashboardStats Stats)> GetProgramsFilteredAsync(DiscountProgramFilter filter);
    Task<DiscountProgramEditViewModel?> GetProgramByIdAsync(int id);
    Task<bool> SaveProgramAsync(DiscountProgram program, int userId);
    Task ToggleProgramAsync(int id, int userId);
}

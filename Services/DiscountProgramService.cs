using System.Text.Json;
using ClothingStore.Data;
using ClothingStore.Models.Entities;
using ClothingStore.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ClothingStore.Services;

public class DiscountProgramService(StoreDbContext dbContext) : IDiscountProgramService
{
    public async Task<(IEnumerable<DiscountProgram> Programs, DiscountProgramDashboardStats Stats)> GetProgramsFilteredAsync(DiscountProgramFilter filter)
    {
        var query = dbContext.DiscountPrograms.AsNoTracking().AsQueryable();

        var today = DateTime.Today;
        var allPrograms = await query.ToListAsync();
        
        var stats = new DiscountProgramDashboardStats
        {
            TotalPrograms = allPrograms.Count,
            ActivePrograms = allPrograms.Count(p => p.IsActive && p.StartDate.Date <= today && p.EndDate.Date >= today),
            UpcomingPrograms = allPrograms.Count(p => p.IsActive && p.StartDate.Date > today),
            ExpiredPrograms = allPrograms.Count(p => p.EndDate.Date < today)
        };

        if (!string.IsNullOrWhiteSpace(filter.SearchKeyword))
        {
            var kw = filter.SearchKeyword.Trim().ToLower();
            query = query.Where(p => p.ProgramName.ToLower().Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = filter.Status switch
            {
                "Active" => query.Where(p => p.IsActive && p.StartDate.Date <= today && p.EndDate.Date >= today),
                "Inactive" => query.Where(p => !p.IsActive),
                "Upcoming" => query.Where(p => p.IsActive && p.StartDate.Date > today),
                "Expired" => query.Where(p => p.EndDate.Date < today),
                _ => query
            };
        }

        query = query.OrderByDescending(p => p.ProgramID);

        var programs = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (programs, stats);
    }

    public async Task<DiscountProgramEditViewModel?> GetProgramByIdAsync(int id)
    {
        var program = await dbContext.DiscountPrograms.FindAsync(id);
        if (program == null) return null;

        return new DiscountProgramEditViewModel
        {
            ProgramID = program.ProgramID,
            ProgramName = program.ProgramName,
            DiscountPercent = program.DiscountPercent,
            StartDate = program.StartDate,
            EndDate = program.EndDate,
            IsActive = program.IsActive,
            RowVersion = program.RowVersion
        };
    }

    public async Task<bool> SaveProgramAsync(DiscountProgram program, int userId)
    {
        if (userId <= 0) throw new UnauthorizedAccessException("Người thực hiện không hợp lệ.");
        if (program.DiscountPercent <= 0 || program.DiscountPercent > 100) return false;
        if (program.EndDate <= program.StartDate) return false;

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var audit = new DiscountProgramAudit
            {
                ChangedByUserId = userId,
                ChangedAt = DateTime.UtcNow
            };

            var newValues = new
            {
                program.ProgramName,
                program.DiscountPercent,
                program.StartDate,
                program.EndDate,
                program.IsActive
            };

            if (program.ProgramID == 0)
            {
                program.CreatedAt = DateTime.UtcNow;
                dbContext.DiscountPrograms.Add(program);
                await dbContext.SaveChangesAsync(); // Get ProgramID

                audit.ProgramID = program.ProgramID;
                audit.ActionType = "CreateProgram";
                audit.NewValues = JsonSerializer.Serialize(newValues);
            }
            else
            {
                var existing = await dbContext.DiscountPrograms.FindAsync(program.ProgramID);
                if (existing == null) return false;

                // Concurrency checking handled by EF Core RowVersion
                dbContext.Entry(existing).OriginalValues["RowVersion"] = program.RowVersion;

                audit.ProgramID = existing.ProgramID;
                audit.ActionType = "UpdateProgram";
                audit.OldValues = JsonSerializer.Serialize(new
                {
                    existing.ProgramName,
                    existing.DiscountPercent,
                    existing.StartDate,
                    existing.EndDate,
                    existing.IsActive
                });
                audit.NewValues = JsonSerializer.Serialize(newValues);

                existing.ProgramName = program.ProgramName;
                existing.DiscountPercent = program.DiscountPercent;
                existing.StartDate = program.StartDate;
                existing.EndDate = program.EndDate;
                existing.IsActive = program.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync();
            }

            dbContext.DiscountProgramAudits.Add(audit);
            await dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            throw; // Let controller handle it
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task ToggleProgramAsync(int id, int userId)
    {
        if (userId <= 0) throw new UnauthorizedAccessException("Người thực hiện không hợp lệ.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            var program = await dbContext.DiscountPrograms.FindAsync(id);
            if (program != null)
            {
                var oldIsActive = program.IsActive;
                program.IsActive = !program.IsActive;
                program.UpdatedAt = DateTime.UtcNow;

                var audit = new DiscountProgramAudit
                {
                    ProgramID = program.ProgramID,
                    ActionType = program.IsActive ? "ActivateProgram" : "DeactivateProgram",
                    ChangedByUserId = userId,
                    ChangedAt = DateTime.UtcNow,
                    OldValues = JsonSerializer.Serialize(new { IsActive = oldIsActive }),
                    NewValues = JsonSerializer.Serialize(new { IsActive = program.IsActive })
                };

                dbContext.DiscountProgramAudits.Add(audit);
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

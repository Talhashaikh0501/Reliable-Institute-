using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;
using ReliableAcademy.ViewModels;

namespace ReliableAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Teacher")]
public sealed class BatchesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(bool includeInactive = false)
    {
        var query = db.TeachingBatches.AsNoTracking().AsQueryable();
        if (!includeInactive) query = query.Where(x => x.IsActive);

        var model = new BatchListViewModel
        {
            IncludeInactive = includeInactive,
            Batches = await query.OrderBy(x => x.Standard).ThenBy(x => x.Name)
                .Select(x => new BatchListItemViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Standard = x.Standard,
                    AcademicYear = x.AcademicYear,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    IsActive = x.IsActive,
                    ActiveStudents = x.Students.Count(s => s.IsActive)
                }).ToListAsync()
        };
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create() => View(new BatchFormViewModel { AcademicYear = GetCurrentAcademicYear() });

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BatchFormViewModel model)
    {
        ValidateTimesAndStandard(model);
        var name = model.Name?.Trim() ?? "";
        var year = model.AcademicYear?.Trim() ?? "";
        if (name.Length > 0 && year.Length > 0 &&
            await db.TeachingBatches.AnyAsync(x => x.Name == name && x.Standard == model.Standard && x.AcademicYear == year))
            ModelState.AddModelError(nameof(model.Name), "A batch with this name already exists for this standard and academic year.");

        if (!ModelState.IsValid) return View(model);

        var batch = new TeachingBatch
        {
            Name = name,
            Standard = model.Standard,
            AcademicYear = year,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            IsActive = true
        };
        db.TeachingBatches.Add(batch);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Batch {batch.Name} was created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var batch = await db.TeachingBatches.FindAsync(id);
        if (batch is null) return NotFound();

        return View(new BatchFormViewModel
        {
            Name = batch.Name,
            Standard = batch.Standard,
            AcademicYear = batch.AcademicYear,
            StartTime = batch.StartTime,
            EndTime = batch.EndTime
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BatchFormViewModel model)
    {
        var batch = await db.TeachingBatches.FindAsync(id);
        if (batch is null) return NotFound();

        ValidateTimesAndStandard(model);
        var name = model.Name?.Trim() ?? "";
        var year = model.AcademicYear?.Trim() ?? "";
        if (name.Length > 0 && year.Length > 0 &&
            await db.TeachingBatches.AnyAsync(x => x.Id != id && x.Name == name && x.Standard == model.Standard && x.AcademicYear == year))
            ModelState.AddModelError(nameof(model.Name), "A batch with this name already exists for this standard and academic year.");

        if (batch.Standard != model.Standard && await db.Students.AnyAsync(x => x.BatchId == id))
            ModelState.AddModelError(nameof(model.Standard), "Move students to another batch before changing this batch's standard.");

        if (!ModelState.IsValid) return View(model);

        batch.Name = name;
        batch.Standard = model.Standard;
        batch.AcademicYear = year;
        batch.StartTime = model.StartTime;
        batch.EndTime = model.EndTime;
        await db.SaveChangesAsync();
        TempData["Success"] = $"Batch {batch.Name} was updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var batch = await db.TeachingBatches.FindAsync(id);
        if (batch is null) return NotFound();

        batch.IsActive = !batch.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = batch.IsActive ? $"Batch {batch.Name} reactivated." : $"Batch {batch.Name} archived.";
        return RedirectToAction(nameof(Index), new { includeInactive = !batch.IsActive });
    }

    private void ValidateTimesAndStandard(BatchFormViewModel model)
    {
        if (!Enum.IsDefined(model.Standard))
            ModelState.AddModelError(nameof(model.Standard), "Select a valid standard.");
        if (model.StartTime.HasValue && model.EndTime.HasValue && model.EndTime.Value <= model.StartTime.Value)
            ModelState.AddModelError(nameof(model.EndTime), "End time must be later than start time.");
    }

    private static string GetCurrentAcademicYear()
    {
        var today = DateTime.Today;
        var start = today.Month >= 6 ? today.Year : today.Year - 1;
        return $"{start}-{start + 1}";
    }
}

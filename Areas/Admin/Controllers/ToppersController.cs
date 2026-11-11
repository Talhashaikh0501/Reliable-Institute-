using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;
using ReliableAcademy.ViewModels;

namespace ReliableAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class ToppersController(ApplicationDbContext db, IWebHostEnvironment environment) : Controller
{
    private const long MaxPhotoBytes = 5 * 1024 * 1024;

    public async Task<IActionResult> Index()
    {
        var toppers = await db.Toppers.AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.StudentName)
            .ToListAsync();
        return View(toppers);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new TopperFormViewModel
        {
            AcademicYear = GetAcademicYear(),
            ExamName = "Board Examination",
            MaximumMarks = 100,
            IsPublished = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TopperFormViewModel model)
    {
        ValidateResult(model);
        if (model.Photo is null || model.Photo.Length == 0)
            ModelState.AddModelError(nameof(model.Photo), "Please select a student photograph.");

        if (!ModelState.IsValid)
            return View(model);

        var photoPath = await SavePhotoAsync(model.Photo!);
        if (photoPath is null)
        {
            ModelState.AddModelError(nameof(model.Photo), "Choose a valid JPG, PNG or WebP image under 5 MB.");
            return View(model);
        }

        var topper = new Topper
        {
            StudentName = model.StudentName.Trim(),
            Standard = model.Standard,
            AcademicYear = model.AcademicYear.Trim(),
            ExamName = model.ExamName.Trim(),
            MarksObtained = model.MarksObtained,
            MaximumMarks = model.MaximumMarks,
            PhotoPath = photoPath,
            DisplayOrder = model.DisplayOrder,
            IsPublished = model.IsPublished,
            HasPublicationConsent = model.HasPublicationConsent
        };

        db.Toppers.Add(topper);
        await db.SaveChangesAsync();
        TempData["Success"] = $"{topper.StudentName} was added to the topper list.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var topper = await db.Toppers.FindAsync(id);
        if (topper is null) return NotFound();

        return View(new TopperFormViewModel
        {
            Id = topper.Id,
            StudentName = topper.StudentName,
            Standard = topper.Standard,
            AcademicYear = topper.AcademicYear,
            ExamName = topper.ExamName,
            MarksObtained = topper.MarksObtained,
            MaximumMarks = topper.MaximumMarks,
            DisplayOrder = topper.DisplayOrder,
            IsPublished = topper.IsPublished,
            HasPublicationConsent = topper.HasPublicationConsent,
            ExistingPhotoPath = topper.PhotoPath
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TopperFormViewModel model)
    {
        var topper = await db.Toppers.FirstOrDefaultAsync(x => x.Id == id);
        if (topper is null) return NotFound();

        ValidateResult(model);
        if (!ModelState.IsValid)
        {
            model.Id = id;
            model.ExistingPhotoPath = topper.PhotoPath;
            return View(model);
        }

        string? newPhotoPath = null;
        if (model.Photo is { Length: > 0 })
        {
            newPhotoPath = await SavePhotoAsync(model.Photo);
            if (newPhotoPath is null)
            {
                ModelState.AddModelError(nameof(model.Photo), "Choose a valid JPG, PNG or WebP image under 5 MB.");
                model.Id = id;
                model.ExistingPhotoPath = topper.PhotoPath;
                return View(model);
            }
        }

        var oldPhotoPath = topper.PhotoPath;
        topper.StudentName = model.StudentName.Trim();
        topper.Standard = model.Standard;
        topper.AcademicYear = model.AcademicYear.Trim();
        topper.ExamName = model.ExamName.Trim();
        topper.MarksObtained = model.MarksObtained;
        topper.MaximumMarks = model.MaximumMarks;
        topper.DisplayOrder = model.DisplayOrder;
        topper.IsPublished = model.IsPublished;
        topper.HasPublicationConsent = model.HasPublicationConsent;
        if (newPhotoPath is not null) topper.PhotoPath = newPhotoPath;

        await db.SaveChangesAsync();
        if (newPhotoPath is not null) DeletePhoto(oldPhotoPath);

        TempData["Success"] = $"The topper record for {topper.StudentName} was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var topper = await db.Toppers.FirstOrDefaultAsync(x => x.Id == id);
        if (topper is null) return NotFound();

        var photoPath = topper.PhotoPath;
        db.Toppers.Remove(topper);
        await db.SaveChangesAsync();
        DeletePhoto(photoPath);

        TempData["Success"] = $"{topper.StudentName} was removed from the topper list.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateResult(TopperFormViewModel model)
    {
        if (model.MaximumMarks <= 0 || model.MarksObtained > model.MaximumMarks)
            ModelState.AddModelError(nameof(model.MarksObtained), "Marks obtained must be between zero and the maximum marks.");

        if (model.IsPublished && !model.HasPublicationConsent)
            ModelState.AddModelError(nameof(model.HasPublicationConsent), "Permission is required before publishing a student's name, photograph or result.");
    }

    private async Task<string?> SavePhotoAsync(IFormFile file)
    {
        if (file.Length <= 0 || file.Length > MaxPhotoBytes) return null;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp")) return null;

        var header = new byte[12];
        await using (var input = file.OpenReadStream())
        {
            var read = await input.ReadAsync(header);
            if (!IsSupportedImage(header, read, extension)) return null;
        }

        var folder = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "toppers");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, fileName);

        await using (var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await file.CopyToAsync(output);

        return $"uploads/toppers/{fileName}";
    }

    private static bool IsSupportedImage(byte[] bytes, int length, string extension)
    {
        if (extension is ".jpg" or ".jpeg")
            return length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        if (extension == ".png")
            return length >= 8 && bytes.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        return extension == ".webp" && length >= 12 &&
            System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" &&
            System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP";
    }

    private void DeletePhoto(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith("uploads/toppers/", StringComparison.OrdinalIgnoreCase))
            return;

        var fileName = Path.GetFileName(relativePath);
        if (string.IsNullOrWhiteSpace(fileName)) return;
        var folder = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "toppers");
        var fullPath = Path.Combine(folder, fileName);
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
    }

    private static string GetAcademicYear()
    {
        var today = DateTime.Today;
        var startYear = today.Month >= 6 ? today.Year : today.Year - 1;
        return $"{startYear}-{(startYear + 1) % 100:00}";
    }
}

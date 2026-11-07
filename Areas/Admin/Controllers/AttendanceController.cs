using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;
using ReliableAcademy.ViewModels;

namespace ReliableAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Teacher")]
public sealed class AttendanceController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? date, int? batchId)
    {
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        ViewBag.Batches = await GetActiveBatchesAsync(batchId);

        if (!batchId.HasValue || !await db.TeachingBatches.AnyAsync(x => x.Id == batchId && x.IsActive))
            return View(new AttendanceViewModel { AttendanceDate = selectedDate });

        var batch = await db.TeachingBatches.AsNoTracking().FirstAsync(x => x.Id == batchId);
        var students = await db.Students.AsNoTracking()
            .Where(x => x.IsActive && x.BatchId == batchId)
            .OrderBy(x => x.FullName)
            .Select(x => new { x.Id, x.AdmissionNumber, x.FullName })
            .ToListAsync();

        var studentIds = students.Select(x => x.Id).ToList();
        var records = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.AttendanceDate == selectedDate && studentIds.Contains(x.StudentId))
            .ToDictionaryAsync(x => x.StudentId);

        var rows = students.Select(student =>
        {
            records.TryGetValue(student.Id, out var record);
            return new AttendanceStudentRowViewModel
            {
                StudentId = student.Id,
                AdmissionNumber = student.AdmissionNumber,
                FullName = student.FullName,
                Status = record?.Status,
                Note = record?.Note
            };
        }).ToList();

        return View(new AttendanceViewModel
        {
            AttendanceDate = selectedDate,
            BatchId = batch.Id,
            BatchName = batch.Name,
            Students = rows
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AttendanceViewModel model)
    {
        if (model.AttendanceDate > DateOnly.FromDateTime(DateTime.Today))
            ModelState.AddModelError(nameof(model.AttendanceDate), "Attendance cannot be recorded for a future date.");

        var batch = await db.TeachingBatches.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.BatchId && x.IsActive);
        if (batch is null)
            ModelState.AddModelError(nameof(model.BatchId), "Select an active batch.");

        if (!ModelState.IsValid)
        {
            model.BatchName = batch?.Name ?? "";
            ViewBag.Batches = await GetActiveBatchesAsync(model.BatchId);
            return View("Index", model);
        }

        var validIds = (await db.Students
            .Where(x => x.IsActive && x.BatchId == model.BatchId)
            .Select(x => x.Id)
            .ToListAsync()).ToHashSet();

        if (model.Students.Any(x => !validIds.Contains(x.StudentId)))
            return BadRequest("The attendance list contains a student outside the selected batch.");

        if (model.Students.Any(x => x.Status.HasValue && !Enum.IsDefined(x.Status.Value)))
        {
            ModelState.AddModelError(string.Empty, "One or more attendance statuses are invalid.");
            model.BatchName = batch!.Name;
            ViewBag.Batches = await GetActiveBatchesAsync(model.BatchId);
            return View("Index", model);
        }

        var records = await db.AttendanceRecords
            .Where(x => x.AttendanceDate == model.AttendanceDate && validIds.Contains(x.StudentId))
            .ToDictionaryAsync(x => x.StudentId);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var savedCount = 0;

        foreach (var row in model.Students)
        {
            if (!row.Status.HasValue) continue;

            if (records.TryGetValue(row.StudentId, out var existing))
            {
                existing.Status = row.Status.Value;
                existing.Note = Clean(row.Note);
                existing.MarkedByUserId = userId;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                db.AttendanceRecords.Add(new AttendanceRecord
                {
                    StudentId = row.StudentId,
                    AttendanceDate = model.AttendanceDate,
                    Status = row.Status.Value,
                    Note = Clean(row.Note),
                    MarkedByUserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            savedCount++;
        }

        await db.SaveChangesAsync();
        TempData["Success"] = savedCount == 0
            ? "No attendance changes were submitted."
            : $"Attendance saved for {savedCount} student(s).";
        return RedirectToAction(nameof(Index), new { date = model.AttendanceDate, batchId = model.BatchId });
    }

    private async Task<SelectList> GetActiveBatchesAsync(int? selectedId)
    {
        var batches = await db.TeachingBatches.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Standard).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Standard, x.AcademicYear })
            .ToListAsync();

        return new SelectList(batches.Select(x => new
        {
            x.Id,
            Name = $"{(int)x.Standard}th · {x.Name} · {x.AcademicYear}"
        }), "Id", "Name", selectedId);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

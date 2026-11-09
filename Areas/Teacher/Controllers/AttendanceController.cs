using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;
using ReliableAcademy.Services;
using ReliableAcademy.ViewModels;

namespace ReliableAcademy.Areas.Teacher.Controllers;

[Area("Teacher")]
[Authorize(Roles = "Teacher")]
public sealed class AttendanceController(ApplicationDbContext db, UserManager<IdentityUser> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? date, int? batchId)
    {
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var assignedIds = await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User);
        var batches = await db.TeachingBatches.AsNoTracking()
            .Where(x => x.IsActive && assignedIds.Contains(x.Id))
            .OrderBy(x => x.Standard).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Standard, x.AcademicYear })
            .ToListAsync();

        var selectedBatchId = batchId ?? batches.FirstOrDefault()?.Id;
        ViewBag.Batches = new SelectList(batches.Select(x => new
        {
            x.Id, Label = $"{(int)x.Standard}th · {x.Name} · {x.AcademicYear}"
        }), "Id", "Label", selectedBatchId);

        if (!selectedBatchId.HasValue)
            return View(new AttendanceViewModel { AttendanceDate = selectedDate });

        if (!batches.Any(x => x.Id == selectedBatchId.Value))
            return Forbid();

        var batch = batches.First(x => x.Id == selectedBatchId.Value);
        var students = await db.Students.AsNoTracking()
            .Where(x => x.IsActive && x.BatchId == selectedBatchId.Value)
            .OrderBy(x => x.FullName)
            .Select(x => new { x.Id, x.AdmissionNumber, x.FullName })
            .ToListAsync();

        var studentIds = students.Select(x => x.Id).ToList();
        var records = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.AttendanceDate == selectedDate && studentIds.Contains(x.StudentId))
            .ToDictionaryAsync(x => x.StudentId);

        return View(new AttendanceViewModel
        {
            AttendanceDate = selectedDate,
            BatchId = batch.Id,
            BatchName = batch.Name,
            Students = students.Select(student =>
            {
                records.TryGetValue(student.Id, out var record);
                return new AttendanceStudentRowViewModel
                {
                    StudentId = student.Id, AdmissionNumber = student.AdmissionNumber,
                    FullName = student.FullName, Status = record?.Status, Note = record?.Note
                };
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AttendanceViewModel model)
    {
        var assignedIds = await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User);
        var batch = await db.TeachingBatches.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == model.BatchId && x.IsActive && assignedIds.Contains(x.Id));
        if (batch is null) return Forbid();

        if (model.AttendanceDate > DateOnly.FromDateTime(DateTime.Today))
            ModelState.AddModelError(nameof(model.AttendanceDate), "Attendance cannot be recorded for a future date.");

        var validIds = (await db.Students.AsNoTracking()
            .Where(x => x.IsActive && x.BatchId == model.BatchId)
            .Select(x => x.Id).ToListAsync()).ToHashSet();

        if (model.Students.Any(x => !validIds.Contains(x.StudentId)))
            return BadRequest("The attendance list contains a student outside your assigned batch.");

        if (model.Students.Any(x => x.Status.HasValue && !Enum.IsDefined(x.Status.Value)))
            ModelState.AddModelError(string.Empty, "One or more attendance statuses are invalid.");

        if (!ModelState.IsValid)
        {
            model.BatchName = batch.Name;
            await PopulateBatchesAsync(assignedIds, model.BatchId);
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
                    StudentId = row.StudentId, AttendanceDate = model.AttendanceDate,
                    Status = row.Status.Value, Note = Clean(row.Note),
                    MarkedByUserId = userId, CreatedAtUtc = DateTime.UtcNow
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

    private async Task PopulateBatchesAsync(HashSet<int> assignedIds, int? selectedId)
    {
        var batches = await db.TeachingBatches.AsNoTracking()
            .Where(x => x.IsActive && assignedIds.Contains(x.Id))
            .OrderBy(x => x.Standard).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Standard, x.AcademicYear }).ToListAsync();
        ViewBag.Batches = new SelectList(batches.Select(x => new
        {
            x.Id, Label = $"{(int)x.Standard}th · {x.Name} · {x.AcademicYear}"
        }), "Id", "Label", selectedId);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

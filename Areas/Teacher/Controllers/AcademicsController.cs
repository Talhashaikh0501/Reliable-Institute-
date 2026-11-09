using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;
using ReliableAcademy.Services;
using ReliableAcademy.ViewModels;

namespace ReliableAcademy.Areas.Teacher.Controllers;

[Area("Teacher")]
[Authorize(Roles = "Teacher")]
public sealed class AcademicsController(ApplicationDbContext db, UserManager<IdentityUser> users) : Controller
{
    public async Task<IActionResult> Exams(int? batchId)
    {
        var assignedBatchIds = await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User);
        var assignedSubjectIds = await TeacherSubjectAccess.GetAssignedSubjectIdsAsync(users, User);
        var batches = await GetAssignedBatchesAsync(assignedBatchIds);
        if (batchId.HasValue && !batches.Any(x => x.Id == batchId.Value)) return Forbid();
        var selected = batchId ?? batches.FirstOrDefault()?.Id;
        var model = new TeacherExamListViewModel { Batches = batches };
        if (!selected.HasValue || assignedSubjectIds.Count == 0) return View(model);

        var batch = batches.First(x => x.Id == selected.Value);
        model.BatchId = batch.Id;
        model.BatchName = batch.Name;
        var exams = await db.Exams.AsNoTracking().Include(x => x.Subject).Include(x => x.Batch)
            .Include(x => x.Results).ThenInclude(x => x.Student)
            .Where(x => assignedSubjectIds.Contains(x.SubjectId) &&
                x.Subject.Standard == batch.Standard &&
                (x.BatchId == batch.Id || !x.BatchId.HasValue))
            .OrderByDescending(x => x.ExamDate).Take(300).ToListAsync();

        model.Exams = exams.Select(x => new TeacherExamSummary
        {
            Id = x.Id, BatchId = batch.Id, Name = x.Name, SubjectName = x.Subject.Name,
            BatchName = batch.Name, ExamDate = x.ExamDate, MaximumMarks = x.MaximumMarks,
            ResultsEntered = x.Results.Count(r => r.Student.IsActive && r.Student.BatchId == batch.Id)
        }).ToList();
        return View(model);
    }

    public async Task<IActionResult> Results(int id, int batchId)
    {
        var assignedBatchIds = await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User);
        var assignedSubjectIds = await TeacherSubjectAccess.GetAssignedSubjectIdsAsync(users, User);
        var batch = await db.TeachingBatches.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == batchId && x.IsActive && assignedBatchIds.Contains(x.Id));
        if (batch is null) return Forbid();

        var exam = await db.Exams.AsNoTracking().Include(x => x.Subject)
            .FirstOrDefaultAsync(x => x.Id == id && assignedSubjectIds.Contains(x.SubjectId) &&
                x.Subject.Standard == batch.Standard &&
                (x.BatchId == batch.Id || !x.BatchId.HasValue));
        if (exam is null) return NotFound();

        var students = await db.Students.AsNoTracking()
            .Where(x => x.IsActive && x.BatchId == batch.Id)
            .OrderBy(x => x.FullName).ToListAsync();
        var results = await db.ExamResults.AsNoTracking()
            .Where(x => x.ExamId == exam.Id && students.Select(s => s.Id).Contains(x.StudentId))
            .ToDictionaryAsync(x => x.StudentId);

        return View(new TeacherExamResultViewModel
        {
            Exam = exam, BatchId = batch.Id, BatchName = batch.Name,
            Students = students.Select(student =>
            {
                results.TryGetValue(student.Id, out var result);
                return new TeacherExamResultRowViewModel
                {
                    StudentId = student.Id, AdmissionNumber = student.AdmissionNumber,
                    FullName = student.FullName, MarksObtained = result?.MarksObtained,
                    Remarks = result?.Remarks, IsPassing = result is not null && result.MarksObtained >= exam.PassingMarks
                };
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordResult(int examId, int batchId, int studentId, decimal marksObtained, string? remarks)
    {
        var assignedBatchIds = await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User);
        var assignedSubjectIds = await TeacherSubjectAccess.GetAssignedSubjectIdsAsync(users, User);
        var batch = await db.TeachingBatches.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == batchId && x.IsActive && assignedBatchIds.Contains(x.Id));
        if (batch is null) return Forbid();

        var exam = await db.Exams.Include(x => x.Subject)
            .FirstOrDefaultAsync(x => x.Id == examId && assignedSubjectIds.Contains(x.SubjectId) &&
                x.Subject.Standard == batch.Standard && (x.BatchId == batch.Id || !x.BatchId.HasValue));
        var student = await db.Students.FirstOrDefaultAsync(x =>
            x.Id == studentId && x.IsActive && x.BatchId == batch.Id);
        if (exam is null || student is null || marksObtained < 0 ||
            marksObtained > (exam?.MaximumMarks ?? 0) || (remarks?.Length ?? 0) > 500)
        {
            TempData["Error"] = "The exam, student or marks are invalid for your assigned subjects and batch.";
            return RedirectToAction(nameof(Exams), new { batchId });
        }

        var result = await db.ExamResults.FirstOrDefaultAsync(x => x.ExamId == examId && x.StudentId == studentId);
        if (result is null)
            db.ExamResults.Add(new ExamResult
            {
                ExamId = examId, StudentId = studentId, MarksObtained = marksObtained, Remarks = Clean(remarks)
            });
        else
        {
            result.MarksObtained = marksObtained;
            result.Remarks = Clean(remarks);
        }
        await db.SaveChangesAsync();
        TempData["Success"] = $"Result saved for {student.FullName}.";
        return RedirectToAction(nameof(Results), new { id = examId, batchId });
    }

    public async Task<IActionResult> Subjects()
    {
        var assignedBatchIds = await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User);
        var assignedSubjectIds = await TeacherSubjectAccess.GetAssignedSubjectIdsAsync(users, User);
        var standards = await db.TeachingBatches.AsNoTracking()
            .Where(x => x.IsActive && assignedBatchIds.Contains(x.Id))
            .Select(x => x.Standard).Distinct().ToListAsync();
        var subjects = await db.Subjects.AsNoTracking()
            .Where(x => x.IsActive && assignedSubjectIds.Contains(x.Id) && standards.Contains(x.Standard))
            .OrderBy(x => x.Standard).ThenBy(x => x.Name).ToListAsync();
        return View(subjects);
    }

    public async Task<IActionResult> Announcements()
    {
        var userId = users.GetUserId(User);
        var items = await db.Announcements.AsNoTracking().Include(x => x.Batch)
            .Where(x => (x.IsPublished && x.BatchId == null &&
                    (x.Audience == AnnouncementAudience.Everyone || x.Audience == AnnouncementAudience.Teachers) &&
                    (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTime.UtcNow)) ||
                x.CreatedByUserId == userId)
            .OrderByDescending(x => x.PublishedAtUtc).Take(200).ToListAsync();
        ViewBag.Batches = await GetAssignedBatchesAsync(
            await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User));
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAnnouncement(int batchId, string title, string message)
    {
        var assignedIds = await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User);
        var batch = await db.TeachingBatches.FirstOrDefaultAsync(x =>
            x.Id == batchId && x.IsActive && assignedIds.Contains(x.Id));
        title = title?.Trim() ?? "";
        message = message?.Trim() ?? "";
        if (batch is null) return Forbid();
        if (title.Length is < 3 or > 160 || message.Length is < 3 or > 3000)
        {
            TempData["Error"] = "Enter a title (3–160 characters) and message (3–3000 characters).";
            return RedirectToAction(nameof(Announcements));
        }

        db.Announcements.Add(new Announcement
        {
            Title = title, Message = message, Audience = AnnouncementAudience.Students,
            BatchId = batch.Id, CreatedByUserId = users.GetUserId(User),
            PublishedAtUtc = DateTime.UtcNow, IsPublished = true
        });
        await db.SaveChangesAsync();
        TempData["Success"] = $"Announcement published for {batch.Name}.";
        return RedirectToAction(nameof(Announcements));
    }

    [HttpGet]
    public async Task<IActionResult> EditAnnouncement(int id)
    {
        var item = await db.Announcements.Include(x => x.Batch)
            .FirstOrDefaultAsync(x => x.Id == id && x.CreatedByUserId == users.GetUserId(User));
        if (item is null || !item.BatchId.HasValue) return NotFound();
        var assignedIds = await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User);
        if (!assignedIds.Contains(item.BatchId.Value)) return Forbid();
        ViewBag.Batches = await GetAssignedBatchesAsync(assignedIds);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAnnouncement(int id, int batchId, string title, string message)
    {
        var assignedIds = await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User);
        var item = await db.Announcements.FirstOrDefaultAsync(x =>
            x.Id == id && x.CreatedByUserId == users.GetUserId(User));
        var batch = await db.TeachingBatches.FirstOrDefaultAsync(x =>
            x.Id == batchId && x.IsActive && assignedIds.Contains(x.Id));
        title = title?.Trim() ?? "";
        message = message?.Trim() ?? "";
        if (item is null) return NotFound();
        if (batch is null) return Forbid();
        if (title.Length is < 3 or > 160 || message.Length is < 3 or > 3000)
        {
            TempData["Error"] = "Check the title and message length, then try again.";
            return RedirectToAction(nameof(EditAnnouncement), new { id });
        }

        item.Title = title;
        item.Message = message;
        item.BatchId = batch.Id;
        item.Audience = AnnouncementAudience.Students;
        await db.SaveChangesAsync();
        TempData["Success"] = "Your announcement was updated.";
        return RedirectToAction(nameof(Announcements));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAnnouncement(int id)
    {
        var assignedIds = await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User);
        var item = await db.Announcements.FirstOrDefaultAsync(x =>
            x.Id == id && x.CreatedByUserId == users.GetUserId(User));
        if (item is null) return NotFound();
        if (!item.BatchId.HasValue || !assignedIds.Contains(item.BatchId.Value)) return Forbid();
        item.IsPublished = !item.IsPublished;
        await db.SaveChangesAsync();
        TempData["Success"] = item.IsPublished ? "Announcement published." : "Announcement unpublished.";
        return RedirectToAction(nameof(Announcements));
    }

    private async Task<List<TeacherBatchOptionViewModel>> GetAssignedBatchesAsync(HashSet<int> assignedIds) =>
        await db.TeachingBatches.AsNoTracking()
            .Where(x => x.IsActive && assignedIds.Contains(x.Id))
            .OrderBy(x => x.Standard).ThenBy(x => x.Name)
            .Select(x => new TeacherBatchOptionViewModel
            {
                Id = x.Id, Name = x.Name, Standard = x.Standard, AcademicYear = x.AcademicYear,
                StartTime = x.StartTime, EndTime = x.EndTime, StudentCount = x.Students.Count(s => s.IsActive)
            }).ToListAsync();

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;

namespace ReliableAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Teacher")]
public sealed class AcademicsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Subjects(string? search, StudentStandard? standard, bool includeInactive = false)
    {
        var query = db.Subjects.AsNoTracking().AsQueryable();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (standard.HasValue) query = query.Where(x => x.Standard == standard.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Name.Contains(term));
        }
        ViewBag.Search = search;
        ViewBag.Standard = standard;
        ViewBag.IncludeInactive = includeInactive;
        return View(await query.OrderBy(x => x.Standard).ThenBy(x => x.Name).ToListAsync());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubject(string name, StudentStandard standard)
    {
        name = name?.Trim() ?? "";
        if (name.Length is < 2 or > 100 || !Enum.IsDefined(standard))
            TempData["Error"] = "Enter a valid subject name and standard.";
        else if (await db.Subjects.AnyAsync(x => x.Name == name && x.Standard == standard))
            TempData["Error"] = "That subject already exists for the selected standard.";
        else
        {
            db.Subjects.Add(new Subject { Name = name, Standard = standard, IsActive = true });
            await db.SaveChangesAsync();
            TempData["Success"] = "Subject created successfully.";
        }
        return RedirectToAction(nameof(Subjects));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSubject(int id)
    {
        var subject = await db.Subjects.FindAsync(id);
        if (subject is null) return NotFound();
        subject.IsActive = !subject.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = subject.IsActive ? "Subject reactivated." : "Subject archived.";
        return RedirectToAction(nameof(Subjects), new { includeInactive = !subject.IsActive });
    }

    public async Task<IActionResult> Exams()
    {
        var exams = await db.Exams.AsNoTracking().Include(x => x.Subject).Include(x => x.Batch)
            .Include(x => x.Results).OrderByDescending(x => x.ExamDate).ThenBy(x => x.Name)
            .Take(300).ToListAsync();
        ViewBag.Subjects = new SelectList(await db.Subjects.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Standard).ThenBy(x => x.Name)
            .Select(x => new { x.Id, Label = ((int)x.Standard) + "th · " + x.Name }).ToListAsync(), "Id", "Label");
        ViewBag.Batches = new SelectList(await db.TeachingBatches.AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.Standard).ThenBy(x => x.Name)
            .Select(x => new { x.Id, Label = ((int)x.Standard) + "th · " + x.Name }).ToListAsync(), "Id", "Label");
        return View(exams);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateExam(string name, int subjectId, int? batchId, DateOnly examDate, decimal maximumMarks, decimal passingMarks)
    {
        name = name?.Trim() ?? "";
        var subject = await db.Subjects.FirstOrDefaultAsync(x => x.Id == subjectId && x.IsActive);
        var batch = batchId.HasValue
            ? await db.TeachingBatches.FirstOrDefaultAsync(x => x.Id == batchId.Value && x.IsActive)
            : null;
        if (name.Length is < 2 or > 120 || subject is null || maximumMarks <= 0 ||
            passingMarks < 0 || passingMarks > maximumMarks || examDate == default ||
            (batchId.HasValue && (batch is null || batch.Standard != subject.Standard)))
        {
            TempData["Error"] = "Check the exam details, subject, batch and marks. The batch must match the subject's standard.";
            return RedirectToAction(nameof(Exams));
        }
        db.Exams.Add(new Exam
        {
            Name = name, SubjectId = subject.Id, BatchId = batch?.Id,
            ExamDate = examDate, MaximumMarks = maximumMarks, PassingMarks = passingMarks
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Exam scheduled successfully.";
        return RedirectToAction(nameof(Exams));
    }

    public async Task<IActionResult> Results(int id)
    {
        var exam = await db.Exams.AsNoTracking().Include(x => x.Subject).Include(x => x.Batch)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (exam is null) return NotFound();
        var studentsQuery = db.Students.AsNoTracking().Where(x => x.IsActive && x.Standard == exam.Subject.Standard);
        if (exam.BatchId.HasValue) studentsQuery = studentsQuery.Where(x => x.BatchId == exam.BatchId);
        var students = await studentsQuery.OrderBy(x => x.FullName).ToListAsync();
        var results = await db.ExamResults.AsNoTracking().Where(x => x.ExamId == id).ToDictionaryAsync(x => x.StudentId);
        ViewBag.Exam = exam;
        ViewBag.Results = results;
        return View(students);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordResult(int examId, int studentId, decimal marksObtained, string? remarks)
    {
        var exam = await db.Exams.Include(x => x.Subject).FirstOrDefaultAsync(x => x.Id == examId);
        if (exam is null) return NotFound();
        var studentQuery = db.Students.Where(x => x.Id == studentId && x.IsActive && x.Standard == exam.Subject.Standard);
        if (exam.BatchId.HasValue) studentQuery = studentQuery.Where(x => x.BatchId == exam.BatchId);
        if (!await studentQuery.AnyAsync() || marksObtained < 0 || marksObtained > exam.MaximumMarks || (remarks?.Length ?? 0) > 500)
        {
            TempData["Error"] = "The student or marks are invalid. Marks must be within the exam's maximum.";
            return RedirectToAction(nameof(Results), new { id = examId });
        }
        var result = await db.ExamResults.FirstOrDefaultAsync(x => x.ExamId == examId && x.StudentId == studentId);
        if (result is null)
            db.ExamResults.Add(new ExamResult { ExamId = examId, StudentId = studentId, MarksObtained = marksObtained, Remarks = Clean(remarks) });
        else { result.MarksObtained = marksObtained; result.Remarks = Clean(remarks); }
        await db.SaveChangesAsync();
        TempData["Success"] = "Student result saved.";
        return RedirectToAction(nameof(Results), new { id = examId });
    }

    public async Task<IActionResult> Announcements()
    {
        var query = db.Announcements.AsNoTracking().AsQueryable();
        if (!User.IsInRole("Admin"))
            query = query.Where(x => x.IsPublished &&
                (x.Audience == AnnouncementAudience.Everyone || x.Audience == AnnouncementAudience.Teachers) &&
                (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTime.UtcNow));
        return View(await query.OrderByDescending(x => x.PublishedAtUtc).Take(200).ToListAsync());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAnnouncement(string title, string message, AnnouncementAudience audience, DateTime? expiresAtUtc)
    {
        title = title?.Trim() ?? "";
        message = message?.Trim() ?? "";
        if (title.Length is < 3 or > 160 || message.Length is < 3 or > 3000 ||
            !Enum.IsDefined(audience) || (expiresAtUtc.HasValue && expiresAtUtc.Value <= DateTime.UtcNow))
        {
            TempData["Error"] = "Enter a valid title, message, audience and future expiry date.";
            return RedirectToAction(nameof(Announcements));
        }
        db.Announcements.Add(new Announcement
        {
            Title = title, Message = message, Audience = audience,
            PublishedAtUtc = DateTime.UtcNow, ExpiresAtUtc = expiresAtUtc, IsPublished = true
        });
        await db.SaveChangesAsync();
        TempData["Success"] = "Announcement published.";
        return RedirectToAction(nameof(Announcements));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAnnouncement(int id)
    {
        var announcement = await db.Announcements.FindAsync(id);
        if (announcement is null) return NotFound();
        announcement.IsPublished = !announcement.IsPublished;
        await db.SaveChangesAsync();
        TempData["Success"] = announcement.IsPublished ? "Announcement published." : "Announcement unpublished.";
        return RedirectToAction(nameof(Announcements));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

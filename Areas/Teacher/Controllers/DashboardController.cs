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
public sealed class DashboardController(ApplicationDbContext db, UserManager<IdentityUser> users) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startDate = today.AddDays(-13);
        var assignedIds = await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User);
        var activeBatchIds = await db.TeachingBatches.AsNoTracking()
            .Where(x => x.IsActive && assignedIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync();

        var model = new TeacherDashboardViewModel
        {
            Today = DateTime.Today,
            ActiveBatches = activeBatchIds.Count
        };

        if (activeBatchIds.Count == 0)
            return View(model);

        var studentIds = db.Students.AsNoTracking()
            .Where(x => x.IsActive && x.BatchId.HasValue && activeBatchIds.Contains(x.BatchId.Value))
            .Select(x => x.Id);

        model.ActiveStudents = await studentIds.CountAsync();
        model.PresentToday = await db.AttendanceRecords.CountAsync(x =>
            x.AttendanceDate == today && x.Status == AttendanceStatus.Present && studentIds.Contains(x.StudentId));
        model.AbsentToday = await db.AttendanceRecords.CountAsync(x =>
            x.AttendanceDate == today && x.Status == AttendanceStatus.Absent && studentIds.Contains(x.StudentId));
        model.AttendanceMarkedToday = await db.AttendanceRecords.CountAsync(x =>
            x.AttendanceDate == today && studentIds.Contains(x.StudentId));
        model.AttendanceCompletion = model.ActiveStudents == 0 ? 0 :
            Math.Min(100, (int)Math.Round(100d * model.AttendanceMarkedToday / model.ActiveStudents));

        model.Batches = await db.TeachingBatches.AsNoTracking()
            .Where(x => x.IsActive && activeBatchIds.Contains(x.Id))
            .OrderBy(x => x.StartTime).ThenBy(x => x.Name)
            .Select(x => new TeacherBatchOptionViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Standard = x.Standard,
                AcademicYear = x.AcademicYear,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                StudentCount = x.Students.Count(s => s.IsActive),
                PresentToday = x.Students.Count(s => s.IsActive &&
                    s.AttendanceRecords.Any(a => a.AttendanceDate == today && a.Status == AttendanceStatus.Present)),
                AttendanceMarkedToday = x.Students.Count(s => s.IsActive &&
                    s.AttendanceRecords.Any(a => a.AttendanceDate == today))
            }).ToListAsync();

        var standards = model.Batches.Select(x => x.Standard).Distinct().ToList();
        var exams = await db.Exams.AsNoTracking()
            .Include(x => x.Subject).Include(x => x.Batch)
            .Where(x => x.ExamDate >= today &&
                ((x.BatchId.HasValue && activeBatchIds.Contains(x.BatchId.Value)) ||
                 (!x.BatchId.HasValue && standards.Contains(x.Subject.Standard))))
            .OrderBy(x => x.ExamDate).Take(8).ToListAsync();

        model.UpcomingExams = exams.Select(exam =>
        {
            var targetBatch = exam.BatchId.HasValue
                ? model.Batches.FirstOrDefault(x => x.Id == exam.BatchId.Value)
                : model.Batches.FirstOrDefault(x => x.Standard == exam.Subject.Standard);
            return new TeacherExamSummary
            {
                Id = exam.Id,
                BatchId = targetBatch?.Id ?? 0,
                Name = exam.Name,
                SubjectName = exam.Subject.Name,
                BatchName = targetBatch?.Name ?? "Assigned batch",
                ExamDate = exam.ExamDate,
                MaximumMarks = exam.MaximumMarks,
                ResultsEntered = exam.Results.Count(r => targetBatch != null &&
                    r.Student.IsActive && r.Student.BatchId == targetBatch.Id)
            };
        }).Where(x => x.BatchId > 0).ToList();

        var trendRows = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.AttendanceDate >= startDate && x.AttendanceDate <= today && studentIds.Contains(x.StudentId))
            .GroupBy(x => x.AttendanceDate)
            .Select(g => new
            {
                Date = g.Key,
                Marked = g.Count(),
                Present = g.Count(x => x.Status == AttendanceStatus.Present)
            }).ToListAsync();

        var trendLookup = trendRows.ToDictionary(x => x.Date);
        model.AttendanceTrend = Enumerable.Range(0, 14).Select(offset =>
        {
            var date = startDate.AddDays(offset);
            trendLookup.TryGetValue(date, out var row);
            var marked = row?.Marked ?? 0;
            var present = row?.Present ?? 0;
            return new TeacherAttendanceTrendPoint
            {
                Date = date,
                Marked = marked,
                Present = present,
                Percentage = marked == 0 ? 0 : (int)Math.Round(100d * present / marked)
            };
        }).ToList();

        model.Announcements = await db.Announcements.AsNoTracking()
            .Where(x => x.IsPublished &&
                (x.Audience == AnnouncementAudience.Everyone || x.Audience == AnnouncementAudience.Teachers) &&
                (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTime.UtcNow))
            .OrderByDescending(x => x.PublishedAtUtc).Take(5).ToListAsync();

        return View(model);
    }
}

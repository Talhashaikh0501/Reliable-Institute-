using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;
using ReliableAcademy.Services;
using ReliableAcademy.ViewModels;

namespace ReliableAcademy.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = "Student")]
public sealed class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var studentId = StudentAccountAccess.GetStudentId(User);
        if (!studentId.HasValue) return Forbid();

        var student = await db.Students.AsNoTracking()
            .Include(x => x.Batch)
            .FirstOrDefaultAsync(x => x.Id == studentId.Value && x.IsActive &&
                (x.Standard == StudentStandard.Tenth ||
                 x.Standard == StudentStandard.Eleventh ||
                 x.Standard == StudentStandard.Twelfth));

        if (student is null) return Forbid();

        var now = DateTime.UtcNow;
        var model = new StudentDashboardViewModel
        {
            Student = student,
            RecentAttendance = await db.AttendanceRecords.AsNoTracking()
                .Where(x => x.StudentId == student.Id)
                .OrderByDescending(x => x.AttendanceDate)
                .Take(10)
                .ToListAsync(),
            RecentResults = await db.ExamResults.AsNoTracking()
                .Include(x => x.Exam).ThenInclude(x => x.Subject)
                .Where(x => x.StudentId == student.Id)
                .OrderByDescending(x => x.Exam.ExamDate)
                .Take(8)
                .ToListAsync(),
            FeePlans = await db.FeePlans.AsNoTracking()
                .Include(x => x.Payments)
                .Where(x => x.StudentId == student.Id)
                .OrderBy(x => x.DueDate)
                .Take(8)
                .ToListAsync(),
            Announcements = await db.Announcements.AsNoTracking()
                .Include(x => x.Batch)
                .Where(x => x.IsPublished &&
                    (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now) &&
                    ((x.BatchId == null &&
                      (x.Audience == AnnouncementAudience.Everyone ||
                       x.Audience == AnnouncementAudience.Students)) ||
                     (student.BatchId.HasValue &&
                      x.BatchId == student.BatchId.Value &&
                      x.Audience == AnnouncementAudience.Students)))
                .OrderByDescending(x => x.PublishedAtUtc)
                .Take(20)
                .ToListAsync()
        };

        return View(model);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;

namespace ReliableAcademy.Areas.Teacher.Controllers;

[Area("Teacher")]
[Authorize(Roles = "Teacher")]
public sealed class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var visibleAnnouncements = db.Announcements.AsNoTracking().Where(x =>
            x.IsPublished &&
            (x.Audience == AnnouncementAudience.Everyone || x.Audience == AnnouncementAudience.Teachers) &&
            (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTime.UtcNow));

        var model = new TeacherDashboardViewModel
        {
            ActiveStudents = await db.Students.CountAsync(x => x.IsActive),
            ActiveBatches = await db.TeachingBatches.CountAsync(x => x.IsActive),
            PresentToday = await db.AttendanceRecords.CountAsync(x => x.AttendanceDate == today && x.Status == AttendanceStatus.Present),
            AttendanceMarkedToday = await db.AttendanceRecords.CountAsync(x => x.AttendanceDate == today),
            UpcomingExams = await db.Exams.AsNoTracking().Include(x => x.Subject).Include(x => x.Batch)
                .Where(x => x.ExamDate >= today).OrderBy(x => x.ExamDate).Take(5).ToListAsync(),
            Announcements = await visibleAnnouncements.OrderByDescending(x => x.PublishedAtUtc).Take(4).ToListAsync(),
            CurrentBatches = await db.TeachingBatches.AsNoTracking().Where(x => x.IsActive)
                .OrderBy(x => x.StartTime).ThenBy(x => x.Name).Take(6)
                .Select(x => new TeacherBatchSummary
                {
                    Id = x.Id, Name = x.Name, Standard = x.Standard, StartTime = x.StartTime,
                    EndTime = x.EndTime, StudentCount = x.Students.Count(s => s.IsActive)
                }).ToListAsync()
        };
        return View(model);
    }
}

public sealed class TeacherDashboardViewModel
{
    public int ActiveStudents { get; set; }
    public int ActiveBatches { get; set; }
    public int PresentToday { get; set; }
    public int AttendanceMarkedToday { get; set; }
    public List<Exam> UpcomingExams { get; set; } = new();
    public List<Announcement> Announcements { get; set; } = new();
    public List<TeacherBatchSummary> CurrentBatches { get; set; } = new();
}

public sealed class TeacherBatchSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public StudentStandard Standard { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int StudentCount { get; set; }
}

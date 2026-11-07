using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;

namespace ReliableAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Teacher")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var model = new DashboardViewModel
        {
            TotalStudents = await db.Students.CountAsync(x => x.IsActive),
            ActiveBatches = await db.TeachingBatches.CountAsync(x => x.IsActive),
            PresentToday = await db.AttendanceRecords.CountAsync(x => x.AttendanceDate == today && x.Status == AttendanceStatus.Present),
            AbsentToday = await db.AttendanceRecords.CountAsync(x => x.AttendanceDate == today && x.Status == AttendanceStatus.Absent),
            FeesCollectedThisMonth = await db.FeePayments.Where(x => x.PaidAtUtc >= monthStart).SumAsync(x => (decimal?)x.Amount) ?? 0,
            RecentAnnouncements = await db.Announcements.Where(x => x.IsPublished && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTime.UtcNow)).OrderByDescending(x => x.PublishedAtUtc).Take(5).ToListAsync()
        };
        return View(model);
    }
}

public class DashboardViewModel
{
    public int TotalStudents { get; set; }
    public int ActiveBatches { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public decimal FeesCollectedThisMonth { get; set; }
    public List<Announcement> RecentAnnouncements { get; set; } = new();
}

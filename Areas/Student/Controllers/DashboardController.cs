using System.Globalization;
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
    public async Task<IActionResult> Index(string? month)
    {
        if (User.HasClaim(StudentAccountAccess.MustChangePasswordClaim, "true"))
            return RedirectToAction("ChangePassword", "Account");

        var student = await GetCurrentStudentAsync();
        if (student is null) return Forbid();

        DateOnly? monthStart = null;
        if (!string.IsNullOrWhiteSpace(month))
        {
            if (!DateOnly.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsed))
                return BadRequest("Select a valid attendance month.");
            monthStart = new DateOnly(parsed.Year, parsed.Month, 1);
        }

        var attendanceQuery = db.AttendanceRecords.AsNoTracking()
            .Where(x => x.StudentId == student.Id);
        var allAttendanceCount = await attendanceQuery.CountAsync();
        var allPresentCount = await attendanceQuery.CountAsync(x =>
            x.Status == AttendanceStatus.Present || x.Status == AttendanceStatus.Late);
        if (monthStart.HasValue)
        {
            var start = monthStart.Value;
            if (start.Year == 9999 && start.Month == 12)
                return BadRequest("Select an earlier month.");
            var next = start.AddMonths(1);
            attendanceQuery = attendanceQuery.Where(x =>
                x.AttendanceDate >= start && x.AttendanceDate < next);
        }

        var feePlans = await db.FeePlans.AsNoTracking()
            .Include(x => x.Payments)
            .Where(x => x.StudentId == student.Id)
            .OrderByDescending(x => x.DueDate)
            .ToListAsync();

        var results = await db.ExamResults.AsNoTracking()
            .Include(x => x.Exam).ThenInclude(x => x.Subject)
            .Where(x => x.StudentId == student.Id)
            .OrderByDescending(x => x.Exam.ExamDate)
            .ThenBy(x => x.Exam.Name)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var model = new StudentDashboardViewModel
        {
            Student = student,
            AttendanceMonth = monthStart?.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            AllAttendanceCount = allAttendanceCount,
            AllPresentCount = allPresentCount,
            AllExamCount = results.Count,
            RecentAttendance = await attendanceQuery
                .OrderByDescending(x => x.AttendanceDate).ToListAsync(),
            RecentResults = results,
            FeePlans = feePlans,
            PaymentHistory = feePlans.SelectMany(x => x.Payments)
                .OrderByDescending(x => x.PaidAtUtc).ToList(),
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
                .Take(20).ToListAsync()
        };
        return View(model);
    }

    public Task<IActionResult> Profile() => RenderPageAsync("Profile", null);
    public Task<IActionResult> Announcements() => RenderPageAsync("Announcements", null);
    public Task<IActionResult> Attendance(string? month) => RenderPageAsync("Attendance", month);
    public Task<IActionResult> Results() => RenderPageAsync("Results", null);
    public Task<IActionResult> Fees() => RenderPageAsync("Fees", null);
    public Task<IActionResult> Payments() => RenderPageAsync("Payments", null);

    private async Task<IActionResult> RenderPageAsync(string viewName, string? month)
    {
        var result = await Index(month);
        if (result is ViewResult view)
            return View(viewName, view.Model);
        return result;
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(int id)
    {
        if (User.HasClaim(StudentAccountAccess.MustChangePasswordClaim, "true"))
            return RedirectToAction("ChangePassword", "Account");

        var student = await GetCurrentStudentAsync();
        if (student is null) return Forbid();

        // Never resolve a payment by its identifier alone: student ownership must be in the query.
        var payment = await db.FeePayments.AsNoTracking()
            .Include(x => x.FeePlan).ThenInclude(x => x.Student)
            .Include(x => x.ReceivedBy)
            .FirstOrDefaultAsync(x => x.Id == id && x.FeePlan.StudentId == student.Id);
        if (payment is null) return NotFound();

        var indiaZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");
        var paidAtIndia = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(payment.PaidAtUtc, DateTimeKind.Utc), indiaZone);
        Response.Headers.CacheControl = "no-store";
        return View(new FeeReceiptViewModel { Payment = payment, PaidAtIndia = paidAtIndia });
    }

    private async Task<ReliableAcademy.Models.Student?> GetCurrentStudentAsync()
    {
        var studentId = StudentAccountAccess.GetStudentId(User);
        if (!studentId.HasValue) return null;
        return await db.Students.AsNoTracking()
            .Include(x => x.Batch)
            .FirstOrDefaultAsync(x => x.Id == studentId.Value && x.IsActive &&
                (x.Standard == StudentStandard.Tenth ||
                 x.Standard == StudentStandard.Eleventh ||
                 x.Standard == StudentStandard.Twelfth));
    }
}

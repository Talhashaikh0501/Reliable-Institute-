using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;

namespace ReliableAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class DashboardController(ApplicationDbContext db, UserManager<IdentityUser> users) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStartUtc = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var tomorrow = today.AddDays(1);
        var activeStudents = db.Students.AsNoTracking().Where(x => x.IsActive);
        var activePlans = db.FeePlans.AsNoTracking();

        var model = new AdminDashboardViewModel
        {
            ActiveStudents = await activeStudents.CountAsync(),
            NewAdmissionsThisMonth = await db.Students.CountAsync(x => x.AdmissionDate >= new DateOnly(today.Year, today.Month, 1) && x.AdmissionDate <= today),
            ActiveBatches = await db.TeachingBatches.CountAsync(x => x.IsActive),
            ActiveSubjects = await db.Subjects.CountAsync(x => x.IsActive),
            TeacherCount = (await users.GetUsersInRoleAsync("Teacher")).Count,
            PresentToday = await db.AttendanceRecords.CountAsync(x => x.AttendanceDate == today && x.Status == AttendanceStatus.Present),
            AbsentToday = await db.AttendanceRecords.CountAsync(x => x.AttendanceDate == today && x.Status == AttendanceStatus.Absent),
            LateToday = await db.AttendanceRecords.CountAsync(x => x.AttendanceDate == today && x.Status == AttendanceStatus.Late),
            LeaveToday = await db.AttendanceRecords.CountAsync(x => x.AttendanceDate == today && x.Status == AttendanceStatus.Leave),
            AttendanceMarkedToday = await db.AttendanceRecords.CountAsync(x => x.AttendanceDate == today),
            FeesCollectedThisMonth = await db.FeePayments.Where(x => x.PaidAtUtc >= monthStartUtc).SumAsync(x => (decimal?)x.Amount) ?? 0m,
            UpcomingExams = await db.Exams.AsNoTracking().Include(x => x.Subject).Include(x => x.Batch)
                .Where(x => x.ExamDate >= today).OrderBy(x => x.ExamDate).Take(5).ToListAsync(),
            RecentAnnouncements = await db.Announcements.AsNoTracking()
                .Where(x => x.IsPublished && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTime.UtcNow))
                .OrderByDescending(x => x.PublishedAtUtc).Take(4).ToListAsync(),
            RecentAdmissions = await db.Students.AsNoTracking().Where(x => x.IsActive)
                .OrderByDescending(x => x.AdmissionDate).ThenByDescending(x => x.Id).Take(6)
                .Select(x => new DashboardStudentRow { Id = x.Id, Name = x.FullName, AdmissionNumber = x.AdmissionNumber, Standard = x.Standard, AdmissionDate = x.AdmissionDate })
                .ToListAsync(),
            RecentPayments = await db.FeePayments.AsNoTracking().Include(x => x.FeePlan).ThenInclude(x => x.Student)
                .OrderByDescending(x => x.PaidAtUtc).Take(6)
                .Select(x => new DashboardPaymentRow { Id = x.Id, ReceiptNumber = x.ReceiptNumber, StudentName = x.FeePlan.Student.FullName, Amount = x.Amount, PaidAtUtc = x.PaidAtUtc, PaymentMethod = x.PaymentMethod })
                .ToListAsync(),
            StudentsWithoutBatch = await activeStudents.CountAsync(x => x.BatchId == null),
            UnpublishedAnnouncements = await db.Announcements.CountAsync(x => !x.IsPublished)
        };

        model.OutstandingFees = await activePlans
            .Where(x => x.DueDate >= today)
            .SumAsync(x => (decimal?)(x.TotalAmount - x.DiscountAmount - (x.Payments.Sum(p => (decimal?)p.Amount) ?? 0m))) ?? 0m;
        model.OverdueFees = await activePlans
            .Where(x => x.DueDate < today && x.TotalAmount - x.DiscountAmount > (x.Payments.Sum(p => (decimal?)p.Amount) ?? 0m))
            .SumAsync(x => (decimal?)(x.TotalAmount - x.DiscountAmount - (x.Payments.Sum(p => (decimal?)p.Amount) ?? 0m))) ?? 0m;
        model.OverduePlans = await activePlans
            .CountAsync(x => x.DueDate < today && x.TotalAmount - x.DiscountAmount > (x.Payments.Sum(p => (decimal?)p.Amount) ?? 0m));
        model.AttendanceExpected = await activeStudents.CountAsync();
        model.AttendanceCompletion = model.AttendanceExpected == 0 ? 0 :
            Math.Min(100, (int)Math.Round(100d * model.AttendanceMarkedToday / model.AttendanceExpected));
        model.TotalFeeOutstanding = model.OutstandingFees + model.OverdueFees;

        return View(model);
    }
}

public sealed class AdminDashboardViewModel
{
    public int ActiveStudents { get; set; }
    public int NewAdmissionsThisMonth { get; set; }
    public int ActiveBatches { get; set; }
    public int ActiveSubjects { get; set; }
    public int TeacherCount { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public int LateToday { get; set; }
    public int LeaveToday { get; set; }
    public int AttendanceMarkedToday { get; set; }
    public int AttendanceExpected { get; set; }
    public int AttendanceCompletion { get; set; }
    public decimal FeesCollectedThisMonth { get; set; }
    public decimal OutstandingFees { get; set; }
    public decimal OverdueFees { get; set; }
    public decimal TotalFeeOutstanding { get; set; }
    public int OverduePlans { get; set; }
    public int StudentsWithoutBatch { get; set; }
    public int UnpublishedAnnouncements { get; set; }
    public List<Exam> UpcomingExams { get; set; } = new();
    public List<Announcement> RecentAnnouncements { get; set; } = new();
    public List<DashboardStudentRow> RecentAdmissions { get; set; } = new();
    public List<DashboardPaymentRow> RecentPayments { get; set; } = new();
}

public sealed class DashboardStudentRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string AdmissionNumber { get; set; } = "";
    public StudentStandard Standard { get; set; }
    public DateOnly AdmissionDate { get; set; }
}

public sealed class DashboardPaymentRow
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public string StudentName { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime PaidAtUtc { get; set; }
    public string PaymentMethod { get; set; } = "";
}

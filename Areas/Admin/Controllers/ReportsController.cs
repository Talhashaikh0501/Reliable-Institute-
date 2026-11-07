using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;

namespace ReliableAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Teacher")]
public sealed class ReportsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startDate = from ?? new DateOnly(today.Year, today.Month, 1);
        var endDate = to ?? today;
        if (startDate > endDate || endDate.DayNumber - startDate.DayNumber > 366)
            return BadRequest("Choose a valid date range of no more than 367 days.");

        var start = startDate.ToDateTime(TimeOnly.MinValue);
        var endExclusive = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var attendance = db.AttendanceRecords.AsNoTracking()
            .Where(x => x.AttendanceDate >= startDate && x.AttendanceDate <= endDate);

        var model = new ReportsViewModel
        {
            From = startDate,
            To = endDate,
            Attendance = await attendance.GroupBy(x => x.Status)
                .Select(g => new AttendanceStatusRow { Status = g.Key, Count = g.Count() }).ToListAsync(),
            DailyAttendance = await attendance.GroupBy(x => x.AttendanceDate)
                .Select(g => new DailyAttendanceRow
                {
                    Date = g.Key,
                    Total = g.Count(),
                    Present = g.Count(x => x.Status == AttendanceStatus.Present),
                    Absent = g.Count(x => x.Status == AttendanceStatus.Absent),
                    Late = g.Count(x => x.Status == AttendanceStatus.Late),
                    Leave = g.Count(x => x.Status == AttendanceStatus.Leave)
                }).OrderBy(x => x.Date).ToListAsync()
        };

        if (User.IsInRole("Admin"))
        {
            var payments = await db.FeePayments.AsNoTracking()
                .Include(x => x.FeePlan).ThenInclude(x => x.Student)
                .Where(x => x.PaidAtUtc >= start && x.PaidAtUtc < endExclusive)
                .OrderByDescending(x => x.PaidAtUtc).ToListAsync();
            model.FeesCollected = payments.Sum(x => x.Amount);
            model.PaymentCount = payments.Count;
            model.PaymentMethods = payments.GroupBy(x => x.PaymentMethod)
                .Select(g => new PaymentMethodRow { Method = g.Key, Count = g.Count(), Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Amount).ToList();

            var plans = await db.FeePlans.AsNoTracking().Include(x => x.Student).Include(x => x.Payments)
                .Where(x => x.DueDate <= endDate).ToListAsync();
            model.Defaulters = plans.Select(x => new FeeDefaulterRow
                {
                    StudentName = x.Student.FullName,
                    AdmissionNumber = x.Student.AdmissionNumber,
                    DueDate = x.DueDate,
                    Balance = x.Balance
                })
                .Where(x => x.Balance > 0)
                .OrderByDescending(x => x.Balance)
                .Take(25).ToList();
        }

        return View(model);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportFees(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startDate = from ?? new DateOnly(today.Year, today.Month, 1);
        var endDate = to ?? today;
        if (startDate > endDate || endDate.DayNumber - startDate.DayNumber > 366)
            return BadRequest("Choose a valid date range of no more than 367 days.");

        var start = startDate.ToDateTime(TimeOnly.MinValue);
        var endExclusive = endDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var payments = await db.FeePayments.AsNoTracking()
            .Include(x => x.FeePlan).ThenInclude(x => x.Student)
            .Include(x => x.ReceivedBy)
            .Where(x => x.PaidAtUtc >= start && x.PaidAtUtc < endExclusive)
            .OrderBy(x => x.PaidAtUtc).ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Receipt Number,Student,Admission Number,Payment Date UTC,Amount,Payment Method,Transaction Reference,Received By");
        foreach (var item in payments)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                Csv(item.ReceiptNumber), Csv(item.FeePlan.Student.FullName), Csv(item.FeePlan.Student.AdmissionNumber),
                Csv(item.PaidAtUtc.ToString("yyyy-MM-dd HH:mm:ss")), Csv(item.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)),
                Csv(item.PaymentMethod), Csv(item.TransactionReference), Csv(item.ReceivedBy?.UserName)
            }));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv", $"ReliableCoaching_FeeReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv");
    }

    private static string Csv(string? value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
}

public sealed class ReportsViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public List<AttendanceStatusRow> Attendance { get; set; } = new();
    public List<DailyAttendanceRow> DailyAttendance { get; set; } = new();
    public decimal FeesCollected { get; set; }
    public int PaymentCount { get; set; }
    public List<PaymentMethodRow> PaymentMethods { get; set; } = new();
    public List<FeeDefaulterRow> Defaulters { get; set; } = new();
    public int AttendanceCount(AttendanceStatus status) => Attendance.FirstOrDefault(x => x.Status == status)?.Count ?? 0;
}
public sealed class AttendanceStatusRow { public AttendanceStatus Status { get; set; } public int Count { get; set; } }
public sealed class DailyAttendanceRow { public DateOnly Date { get; set; } public int Total { get; set; } public int Present { get; set; } public int Absent { get; set; } public int Late { get; set; } public int Leave { get; set; } }
public sealed class PaymentMethodRow { public string Method { get; set; } = ""; public int Count { get; set; } public decimal Amount { get; set; } }
public sealed class FeeDefaulterRow { public string StudentName { get; set; } = ""; public string AdmissionNumber { get; set; } = ""; public DateOnly DueDate { get; set; } public decimal Balance { get; set; } }

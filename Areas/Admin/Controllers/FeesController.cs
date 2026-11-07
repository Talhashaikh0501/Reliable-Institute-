using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;
using ReliableAcademy.ViewModels;

namespace ReliableAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class FeesController(ApplicationDbContext db) : Controller
{
    private static readonly HashSet<string> PaymentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cash", "UPI", "Bank Transfer", "Cheque", "Other"
    };

    public async Task<IActionResult> Index(string? search, bool outstandingOnly = false)
    {
        var query = db.FeePlans.AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Payments)
            .AsQueryable();

        if (outstandingOnly)
            query = query.Where(x =>
                (x.TotalAmount - x.DiscountAmount) > (x.Payments.Sum(p => (decimal?)p.Amount) ?? 0m));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Student.FullName.Contains(term) ||
                                     x.Student.AdmissionNumber.Contains(term));
        }

        return View(new FeePlanListViewModel
        {
            Plans = await query.OrderByDescending(x => x.DueDate).Take(500).ToListAsync(),
            Search = search,
            OutstandingOnly = outstandingOnly
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? studentId)
    {
        await PopulateStudentsAsync(studentId);
        var student = studentId.HasValue
            ? await db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == studentId.Value)
            : null;

        return View(new FeePlanFormViewModel
        {
            StudentId = studentId ?? 0,
            AcademicYear = student?.AcademicYear ?? GetCurrentAcademicYear(),
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FeePlanFormViewModel model)
    {
        if (model.DiscountAmount > model.TotalAmount)
            ModelState.AddModelError(nameof(model.DiscountAmount), "Discount cannot be greater than the total fee.");

        var studentExists = await db.Students.AnyAsync(x => x.Id == model.StudentId && x.IsActive);
        if (!studentExists)
            ModelState.AddModelError(nameof(model.StudentId), "Select an active student.");

        if (!ModelState.IsValid)
        {
            await PopulateStudentsAsync(model.StudentId);
            return View(model);
        }

        var plan = new FeePlan
        {
            StudentId = model.StudentId,
            TotalAmount = decimal.Round(model.TotalAmount, 2),
            DiscountAmount = decimal.Round(model.DiscountAmount, 2),
            DueDate = model.DueDate,
            AcademicYear = model.AcademicYear.Trim(),
            Notes = Clean(model.Notes)
        };

        db.FeePlans.Add(plan);
        await db.SaveChangesAsync();
        TempData["Success"] = "Fee plan created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> RecordPayment(int id)
    {
        var plan = await LoadPlanAsync(id);
        if (plan is null) return NotFound();
        if (plan.Balance <= 0)
        {
            TempData["Success"] = "This fee plan is already fully paid.";
            return RedirectToAction(nameof(Index));
        }

        return View(new RecordFeePaymentViewModel
        {
            FeePlanId = plan.Id,
            Amount = plan.Balance,
            StudentName = plan.Student.FullName,
            AdmissionNumber = plan.Student.AdmissionNumber,
            BalanceBeforePayment = plan.Balance
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(RecordFeePaymentViewModel model)
    {
        if (!PaymentMethods.Contains(model.PaymentMethod))
            ModelState.AddModelError(nameof(model.PaymentMethod), "Select a valid payment method.");
        if (model.Amount != decimal.Round(model.Amount, 2))
            ModelState.AddModelError(nameof(model.Amount), "Payment amount can have a maximum of two decimal places.");

        if (!ModelState.IsValid)
        {
            var invalidPlan = await LoadPlanAsync(model.FeePlanId);
            if (invalidPlan is null) return NotFound();
            model.StudentName = invalidPlan.Student.FullName;
            model.AdmissionNumber = invalidPlan.Student.AdmissionNumber;
            model.BalanceBeforePayment = invalidPlan.Balance;
            return View(model);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var plan = await db.FeePlans
            .Include(x => x.Student)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == model.FeePlanId);
        if (plan is null) return NotFound();

        var balance = plan.Balance;
        if (model.Amount <= 0 || model.Amount > balance)
        {
            ModelState.AddModelError(nameof(model.Amount), $"Enter an amount between ₹0.01 and ₹{balance:N2}.");
            model.StudentName = plan.Student.FullName;
            model.AdmissionNumber = plan.Student.AdmissionNumber;
            model.BalanceBeforePayment = balance;
            await transaction.RollbackAsync();
            return View(model);
        }

        var payment = new FeePayment
        {
            FeePlanId = plan.Id,
            ReceiptNumber = $"RIC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            Amount = decimal.Round(model.Amount, 2),
            PaidAtUtc = DateTime.UtcNow,
            PaymentMethod = model.PaymentMethod,
            TransactionReference = Clean(model.TransactionReference),
            ReceivedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        db.FeePayments.Add(payment);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Success"] = "Payment recorded and receipt generated.";
        return RedirectToAction(nameof(Receipt), new { id = payment.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(int id)
    {
        var payment = await db.FeePayments.AsNoTracking()
            .Include(x => x.FeePlan).ThenInclude(x => x.Student)
            .Include(x => x.ReceivedBy)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (payment is null) return NotFound();

        return View(new FeeReceiptViewModel
        {
            Payment = payment,
            PaidAtIndia = ToIndiaTime(payment.PaidAtUtc)
        });
    }

    private async Task<FeePlan?> LoadPlanAsync(int id) =>
        await db.FeePlans.AsNoTracking()
            .Include(x => x.Student)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id);

    private async Task PopulateStudentsAsync(int? selectedId)
    {
        var students = await db.Students.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.FullName)
            .Select(x => new { x.Id, Label = x.FullName + " · " + x.AdmissionNumber + " · " + (int)x.Standard + "th" })
            .ToListAsync();

        ViewBag.Students = new SelectList(students, "Id", "Label", selectedId);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string GetCurrentAcademicYear()
    {
        var today = DateTime.Today;
        var start = today.Month >= 6 ? today.Year : today.Year - 1;
        return $"{start}-{start + 1}";
    }

    private static DateTime ToIndiaTime(DateTime utc)
    {
        var zoneId = OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata";
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone);
    }
}

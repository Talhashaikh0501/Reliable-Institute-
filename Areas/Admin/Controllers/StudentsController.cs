using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;
using ReliableAcademy.Services;
using ReliableAcademy.ViewModels;

namespace ReliableAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class StudentsController(
    ApplicationDbContext db,
    UserManager<IdentityUser> users) : Controller
{
    public async Task<IActionResult> Index(string? search, StudentStandard? standard, bool includeInactive = false)
    {
        var query = db.Students.AsNoTracking().Include(x => x.Batch).AsQueryable();
        if (!includeInactive) query = query.Where(x => x.IsActive);
        if (standard.HasValue) query = query.Where(x => x.Standard == standard.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.FullName.Contains(term) ||
                x.AdmissionNumber.Contains(term) ||
                (x.PhoneNumber != null && x.PhoneNumber.Contains(term)) ||
                (x.GuardianPhone != null && x.GuardianPhone.Contains(term)));
        }

        return View(new StudentListViewModel
        {
            Students = await query.OrderBy(x => x.Standard).ThenBy(x => x.FullName).Take(300).ToListAsync(),
            Search = search,
            Standard = standard,
            IncludeInactive = includeInactive
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateBatchesAsync(StudentStandard.Eighth);
        return View(new StudentFormViewModel
        {
            AcademicYear = GetCurrentAcademicYear(),
            AdmissionDate = DateOnly.FromDateTime(DateTime.Today)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentFormViewModel model)
    {
        await ValidateBatchAsync(model);
        var admissionNumber = model.AdmissionNumber?.Trim() ?? "";
        if (admissionNumber.Length > 0 && await db.Students.AnyAsync(x => x.AdmissionNumber == admissionNumber))
            ModelState.AddModelError(nameof(model.AdmissionNumber), "This admission number is already in use.");

        if (!ModelState.IsValid)
        {
            await PopulateBatchesAsync(model.Standard, model.BatchId);
            return View(model);
        }

        var student = new ReliableAcademy.Models.Student
        {
            AdmissionNumber = admissionNumber,
            FullName = model.FullName.Trim(),
            Standard = model.Standard,
            AcademicYear = model.AcademicYear.Trim(),
            PhoneNumber = Clean(model.PhoneNumber),
            Email = Clean(model.Email),
            GuardianName = Clean(model.GuardianName),
            GuardianPhone = Clean(model.GuardianPhone),
            Address = Clean(model.Address),
            DateOfBirth = model.DateOfBirth,
            AdmissionDate = model.AdmissionDate,
            BatchId = model.BatchId,
            IsActive = true
        };

        db.Students.Add(student);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Student {student.FullName} was admitted successfully.";
        return RedirectToAction(nameof(Details), new { id = student.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return NotFound();

        await PopulateBatchesAsync(student.Standard, student.BatchId);
        return View(new StudentFormViewModel
        {
            AdmissionNumber = student.AdmissionNumber,
            FullName = student.FullName,
            Standard = student.Standard,
            AcademicYear = student.AcademicYear,
            PhoneNumber = student.PhoneNumber,
            Email = student.Email,
            GuardianName = student.GuardianName,
            GuardianPhone = student.GuardianPhone,
            Address = student.Address,
            DateOfBirth = student.DateOfBirth,
            AdmissionDate = student.AdmissionDate,
            BatchId = student.BatchId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudentFormViewModel model)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return NotFound();

        await ValidateBatchAsync(model);
        var admissionNumber = model.AdmissionNumber?.Trim() ?? "";
        if (admissionNumber.Length > 0 && await db.Students.AnyAsync(x => x.Id != id && x.AdmissionNumber == admissionNumber))
            ModelState.AddModelError(nameof(model.AdmissionNumber), "This admission number is already in use.");

        if (!ModelState.IsValid)
        {
            await PopulateBatchesAsync(model.Standard, model.BatchId);
            return View(model);
        }

        student.AdmissionNumber = admissionNumber;
        student.FullName = model.FullName.Trim();
        student.Standard = model.Standard;
        student.AcademicYear = model.AcademicYear.Trim();
        student.PhoneNumber = Clean(model.PhoneNumber);
        student.Email = Clean(model.Email);
        student.GuardianName = Clean(model.GuardianName);
        student.GuardianPhone = Clean(model.GuardianPhone);
        student.Address = Clean(model.Address);
        student.DateOfBirth = model.DateOfBirth;
        student.AdmissionDate = model.AdmissionDate;
        student.BatchId = model.BatchId;

        await db.SaveChangesAsync();
        TempData["Success"] = $"Student record for {student.FullName} was updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var query = db.Students.AsNoTracking()
            .Include(x => x.Batch)
            .Include(x => x.AttendanceRecords)
            .AsQueryable();

        if (User.IsInRole("Admin"))
            query = query.Include(x => x.FeePlans).ThenInclude(x => x.Payments);

        var student = await query.FirstOrDefaultAsync(x => x.Id == id);
        if (student is null) return NotFound();

        var linkedUsers = await users.GetUsersForClaimAsync(
            new Claim(StudentAccountAccess.StudentIdClaim, student.Id.ToString()));
        ViewBag.StudentPortalUserName = linkedUsers.FirstOrDefault()?.UserName;
        ViewBag.StudentPortalUserId = linkedUsers.FirstOrDefault()?.Id;
        return View(student);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePortalAccount(int id)
    {
        var student = await db.Students.FirstOrDefaultAsync(x => x.Id == id);
        if (student is null) return NotFound();

        if (!student.IsActive || student.Standard is not (
            StudentStandard.Tenth or StudentStandard.Eleventh or StudentStandard.Twelfth))
        {
            TempData["Error"] = "Student portal accounts are available only for active 10th, 11th and 12th standard students.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var studentClaim = new Claim(StudentAccountAccess.StudentIdClaim, student.Id.ToString());
        if ((await users.GetUsersForClaimAsync(studentClaim)).Count > 0)
        {
            TempData["Error"] = "This student record is already linked to a portal account.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var usernameSuffix = Regex.Replace(student.AdmissionNumber, "[^A-Za-z0-9]", "");
        var username = $"student.{usernameSuffix}";
        if (await users.FindByNameAsync(username) is not null)
        {
            TempData["Error"] = "The generated student username is already in use. Contact the administrator.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var temporaryPassword = $"Rci{RandomNumberGenerator.GetInt32(10_000_000, 99_999_999)}";
        var account = new IdentityUser
        {
            UserName = username,
            Email = null,
            EmailConfirmed = false,
            LockoutEnabled = true
        };

        var createResult = await users.CreateAsync(account, temporaryPassword);
        if (!createResult.Succeeded)
        {
            TempData["Error"] = string.Join(" ", createResult.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Details), new { id });
        }

        var roleResult = await users.AddToRoleAsync(account, "Student");
        if (!roleResult.Succeeded)
        {
            await users.DeleteAsync(account);
            TempData["Error"] = "The student account could not be assigned the Student role. " +
                string.Join(" ", roleResult.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Details), new { id });
        }

        var claimResult = await users.AddClaimAsync(account, studentClaim);
        var passwordClaimResult = claimResult.Succeeded
            ? await users.AddClaimAsync(account, new Claim(StudentAccountAccess.MustChangePasswordClaim, "true"))
            : IdentityResult.Failed();

        if (!claimResult.Succeeded || !passwordClaimResult.Succeeded)
        {
            if (claimResult.Succeeded)
                await users.RemoveClaimAsync(account, studentClaim);
            await users.RemoveFromRoleAsync(account, "Student");
            await users.DeleteAsync(account);
            TempData["Error"] = "The student record could not be securely linked to the new account.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["PortalUsername"] = username;
        TempData["PortalTemporaryPassword"] = temporaryPassword;
        TempData["Success"] = "Student portal account created. Copy the temporary password now; it will not be shown again.";
        return RedirectToAction(nameof(Details), new { id });
    }


    [HttpGet]
    public async Task<IActionResult> ResetPortalPassword(int id)
    {
        var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (student is null) return NotFound();

        var linkedUsers = await users.GetUsersForClaimAsync(
            new Claim(StudentAccountAccess.StudentIdClaim, student.Id.ToString()));
        var account = linkedUsers.FirstOrDefault();
        if (account is null)
        {
            TempData["Error"] = "This student does not have a linked portal account.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ViewBag.AccountLabel = $"{student.FullName} ({account.UserName})";
        ViewBag.Controller = "Students";
        ViewBag.PostAction = "SetPortalPassword";
        ViewBag.AccountId = id;
        ViewBag.CancelUrl = Url.Action(nameof(Details), new { id });
        return View("~/Areas/Admin/Views/Shared/ResetAccountPassword.cshtml", new AdminResetPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPortalPassword(int id, AdminResetPasswordViewModel model)
    {
        var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (student is null) return NotFound();

        var linkedUsers = await users.GetUsersForClaimAsync(
            new Claim(StudentAccountAccess.StudentIdClaim, student.Id.ToString()));
        var account = linkedUsers.FirstOrDefault();
        if (account is null) return NotFound();

        ViewBag.AccountLabel = $"{student.FullName} ({account.UserName})";
        ViewBag.Controller = "Students";
        ViewBag.PostAction = "SetPortalPassword";
        ViewBag.AccountId = id;
        ViewBag.CancelUrl = Url.Action(nameof(Details), new { id });

        if (!ModelState.IsValid)
            return View("~/Areas/Admin/Views/Shared/ResetAccountPassword.cshtml", model);

        var token = await users.GeneratePasswordResetTokenAsync(account);
        var result = await users.ResetPasswordAsync(account, token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View("~/Areas/Admin/Views/Shared/ResetAccountPassword.cshtml", model);
        }

        await users.UpdateSecurityStampAsync(account);
        TempData["Success"] = $"Portal password updated for {student.FullName}. Share the new password with the student securely.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return NotFound();

        student.IsActive = !student.IsActive;
        await db.SaveChangesAsync();
        TempData["Success"] = student.IsActive
            ? $"{student.FullName} has been reactivated."
            : $"{student.FullName} has been archived. Their history is preserved and portal access is blocked.";
        return RedirectToAction(nameof(Index), new { includeInactive = !student.IsActive });
    }

    private async Task PopulateBatchesAsync(StudentStandard standard, int? selectedId = null)
    {
        var batches = await db.TeachingBatches.AsNoTracking()
            .Where(x => x.IsActive && x.Standard == standard)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.AcademicYear })
            .ToListAsync();

        ViewBag.Batches = new SelectList(batches.Select(x => new
        {
            x.Id,
            Name = $"{x.Name} · {x.AcademicYear}"
        }), "Id", "Name", selectedId);
    }

    private async Task ValidateBatchAsync(StudentFormViewModel model)
    {
        if (!Enum.IsDefined(model.Standard))
            ModelState.AddModelError(nameof(model.Standard), "Select a valid standard.");

        if (!model.BatchId.HasValue) return;
        var valid = await db.TeachingBatches.AnyAsync(x =>
            x.Id == model.BatchId.Value && x.IsActive && x.Standard == model.Standard);
        if (!valid)
            ModelState.AddModelError(nameof(model.BatchId), "Choose an active batch for the selected standard.");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string GetCurrentAcademicYear()
    {
        var today = DateTime.Today;
        var start = today.Month >= 6 ? today.Year : today.Year - 1;
        return $"{start}-{start + 1}";
    }
}

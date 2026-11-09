using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Services;
using ReliableAcademy.ViewModels;

namespace ReliableAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class StaffController(
    ApplicationDbContext db,
    UserManager<IdentityUser> users) : Controller
{
    public async Task<IActionResult> Index()
    {
        var teachers = await users.GetUsersInRoleAsync("Teacher");
        var model = new List<TeacherAccountViewModel>();
        foreach (var teacher in teachers.OrderBy(x => x.Email))
        {
            var claims = await users.GetClaimsAsync(teacher);
            model.Add(new TeacherAccountViewModel
            {
                UserId = teacher.Id,
                Email = teacher.Email ?? teacher.UserName ?? "Unknown account",
                PhoneNumber = teacher.PhoneNumber,
                IsLocked = teacher.LockoutEnd.HasValue && teacher.LockoutEnd.Value > DateTimeOffset.UtcNow,
                AssignedBatchCount = claims.Count(x => x.Type == TeacherBatchAccess.ClaimType)
            });
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult Create() => View(new TeacherFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeacherFormViewModel model)
    {
        var email = model.Email?.Trim() ?? "";
        if (email.Length > 0 && await users.FindByEmailAsync(email) is not null)
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");

        if (!ModelState.IsValid) return View(model);

        var teacher = new IdentityUser
        {
            UserName = email,
            Email = email,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var createResult = await users.CreateAsync(teacher, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        var roleResult = await users.AddToRoleAsync(teacher, "Teacher");
        if (!roleResult.Succeeded)
        {
            await users.DeleteAsync(teacher);
            foreach (var error in roleResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["Success"] = $"Teacher account for {email} was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Assignments(string id)
    {
        var teacher = await users.FindByIdAsync(id);
        if (teacher is null || !await users.IsInRoleAsync(teacher, "Teacher")) return NotFound();

        var claims = await users.GetClaimsAsync(teacher);
        var assignedIds = claims.Where(x => x.Type == TeacherBatchAccess.ClaimType)
            .Select(x => int.TryParse(x.Value, out var value) ? value : 0)
            .Where(x => x > 0).ToHashSet();

        var batches = await db.TeachingBatches.AsNoTracking()
            .OrderBy(x => x.Standard).ThenBy(x => x.Name)
            .Select(x => new TeacherAssignmentBatchOptionViewModel
            {
                Id = x.Id, Name = x.Name, Standard = x.Standard,
                AcademicYear = x.AcademicYear, IsActive = x.IsActive,
                IsAssigned = assignedIds.Contains(x.Id)
            }).ToListAsync();

        return View(new TeacherAssignmentViewModel
        {
            UserId = teacher.Id,
            Email = teacher.Email ?? teacher.UserName ?? "Teacher",
            BatchIds = assignedIds.ToList(),
            Batches = batches
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAssignments(TeacherAssignmentViewModel model)
    {
        var teacher = await users.FindByIdAsync(model.UserId);
        if (teacher is null || !await users.IsInRoleAsync(teacher, "Teacher")) return NotFound();

        var selectedIds = (model.BatchIds ?? new List<int>()).Distinct().ToList();
        var validIds = await db.TeachingBatches.AsNoTracking()
            .Where(x => x.IsActive && selectedIds.Contains(x.Id))
            .Select(x => x.Id).ToListAsync();

        if (validIds.Count != selectedIds.Count)
        {
            TempData["Error"] = "One or more selected batches are no longer active. Review the assignments and try again.";
            return RedirectToAction(nameof(Assignments), new { id = teacher.Id });
        }

        var existing = (await users.GetClaimsAsync(teacher))
            .Where(x => x.Type == TeacherBatchAccess.ClaimType).ToList();

        if (existing.Count > 0)
        {
            var removeResult = await users.RemoveClaimsAsync(teacher, existing);
            if (!removeResult.Succeeded)
            {
                TempData["Error"] = string.Join("; ", removeResult.Errors.Select(x => x.Description));
                return RedirectToAction(nameof(Assignments), new { id = teacher.Id });
            }
        }

        if (validIds.Count > 0)
        {
            var addResult = await users.AddClaimsAsync(teacher, validIds.Select(x =>
                new Claim(TeacherBatchAccess.ClaimType, x.ToString())).ToList());
            if (!addResult.Succeeded)
            {
                TempData["Error"] = string.Join("; ", addResult.Errors.Select(x => x.Description));
                return RedirectToAction(nameof(Assignments), new { id = teacher.Id });
            }
        }

        TempData["Success"] = $"Batch access updated for {teacher.Email}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAccess(string id)
    {
        var teacher = await users.FindByIdAsync(id);
        if (teacher is null || !await users.IsInRoleAsync(teacher, "Teacher")) return NotFound();

        var isLocked = teacher.LockoutEnd.HasValue && teacher.LockoutEnd.Value > DateTimeOffset.UtcNow;
        var result = await users.SetLockoutEndDateAsync(
            teacher, isLocked ? null : DateTimeOffset.MaxValue);

        if (!result.Succeeded)
            TempData["Error"] = string.Join("; ", result.Errors.Select(x => x.Description));
        else
            TempData["Success"] = isLocked
                ? $"Teacher access for {teacher.Email} was restored."
                : $"Teacher access for {teacher.Email} was suspended.";

        return RedirectToAction(nameof(Index));
    }
}

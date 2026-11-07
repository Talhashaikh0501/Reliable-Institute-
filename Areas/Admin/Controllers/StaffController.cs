using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReliableAcademy.ViewModels;

namespace ReliableAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class StaffController(UserManager<IdentityUser> users) : Controller
{
    public async Task<IActionResult> Index()
    {
        var teachers = await users.GetUsersInRoleAsync("Teacher");
        var model = teachers
            .OrderBy(x => x.Email)
            .Select(x => new TeacherAccountViewModel
            {
                UserId = x.Id,
                Email = x.Email ?? x.UserName ?? "Unknown account",
                PhoneNumber = x.PhoneNumber,
                IsLocked = x.LockoutEnd.HasValue && x.LockoutEnd.Value > DateTimeOffset.UtcNow
            }).ToList();

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
            foreach (var error in createResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        var roleResult = await users.AddToRoleAsync(teacher, "Teacher");
        if (!roleResult.Succeeded)
        {
            await users.DeleteAsync(teacher);
            foreach (var error in roleResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["Success"] = $"Teacher account for {email} was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAccess(string id)
    {
        var teacher = await users.FindByIdAsync(id);
        if (teacher is null || !await users.IsInRoleAsync(teacher, "Teacher"))
            return NotFound();

        var isLocked = teacher.LockoutEnd.HasValue && teacher.LockoutEnd.Value > DateTimeOffset.UtcNow;
        var result = await users.SetLockoutEndDateAsync(
            teacher,
            isLocked ? null : DateTimeOffset.MaxValue);

        if (!result.Succeeded)
            TempData["Error"] = string.Join("; ", result.Errors.Select(x => x.Description));
        else
            TempData["Success"] = isLocked
                ? $"Teacher access for {teacher.Email} was restored."
                : $"Teacher access for {teacher.Email} was suspended.";

        return RedirectToAction(nameof(Index));
    }
}

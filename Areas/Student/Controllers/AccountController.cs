using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReliableAcademy.Services;
using ReliableAcademy.ViewModels;

namespace ReliableAcademy.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = "Student")]
public sealed class AccountController(
    UserManager<IdentityUser> users,
    SignInManager<IdentityUser> signInManager) : Controller
{
    [HttpGet]
    public IActionResult ChangePassword()
    {
        if (!User.HasClaim(StudentAccountAccess.MustChangePasswordClaim, "true"))
            return RedirectToAction("Index", "Dashboard");

        return View(new StudentPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(StudentPasswordViewModel model)
    {
        if (!User.HasClaim(StudentAccountAccess.MustChangePasswordClaim, "true"))
            return RedirectToAction("Index", "Dashboard");

        if (!ModelState.IsValid)
            return View(model);

        var user = await users.GetUserAsync(User);
        if (user is null) return Forbid();

        var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        var mustChangeClaim = (await users.GetClaimsAsync(user))
            .FirstOrDefault(x => x.Type == StudentAccountAccess.MustChangePasswordClaim);
        if (mustChangeClaim is not null)
        {
            var removeResult = await users.RemoveClaimAsync(user, mustChangeClaim);
            if (!removeResult.Succeeded)
            {
                ModelState.AddModelError(string.Empty,
                    "Your password changed, but the first-login security flag could not be cleared. Please sign out and contact the administrator.");
                await signInManager.RefreshSignInAsync(user);
                return View(new StudentPasswordViewModel());
            }
        }

        await signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Your password has been changed successfully.";
        return RedirectToAction("Index", "Dashboard");
    }
}

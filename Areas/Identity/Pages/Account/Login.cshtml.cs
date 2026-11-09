using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;
using ReliableAcademy.Services;

namespace ReliableAcademy.IdentityPages.Account;

[AllowAnonymous]
public sealed class LoginModel(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager,
    ApplicationDbContext db) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; private set; }

    public void OnGet()
    {
        ReturnUrl ??= Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl ??= Url.Content("~/");
        if (!ModelState.IsValid)
            return Page();

        var identifier = Input.LoginIdentifier.Trim();
        var user = await userManager.FindByEmailAsync(identifier)
            ?? await userManager.FindByNameAsync(identifier);

        if (user is null)
        {
            ErrorMessage = "The email address or username, or password, is incorrect. Please check your details and try again.";
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(
            user.UserName!, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var roles = await userManager.GetRolesAsync(user);
            var safeReturnUrl = Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : null;

            if (roles.Contains("Admin"))
                return LocalRedirect(safeReturnUrl is not null &&
                    safeReturnUrl.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase)
                    ? safeReturnUrl : "/Admin/Dashboard");

            if (roles.Contains("Teacher"))
                return LocalRedirect(safeReturnUrl is not null &&
                    safeReturnUrl.StartsWith("/Teacher/", StringComparison.OrdinalIgnoreCase)
                    ? safeReturnUrl : "/Teacher/Dashboard");

            if (roles.Contains("Student"))
            {
                var claims = await userManager.GetClaimsAsync(user);
                var studentIdClaim = claims.FirstOrDefault(x =>
                    x.Type == StudentAccountAccess.StudentIdClaim);
                var studentId = 0;
                var hasStudentLink = studentIdClaim is not null &&
                    int.TryParse(studentIdClaim.Value, out studentId) && studentId > 0;

                if (hasStudentLink)
                {
                    var activeStudent = await db.Students.AsNoTracking().AnyAsync(x =>
                        x.Id == studentId && x.IsActive &&
                        (x.Standard == StudentStandard.Tenth ||
                         x.Standard == StudentStandard.Eleventh ||
                         x.Standard == StudentStandard.Twelfth));
                    if (!activeStudent)
                    {
                        await signInManager.SignOutAsync();
                        ErrorMessage = "This student account is not linked to an active student record. Please contact the institute administrator.";
                        return Page();
                    }

                    var mustChangePassword = claims.Any(x =>
                        x.Type == StudentAccountAccess.MustChangePasswordClaim && x.Value == "true");
                    if (mustChangePassword)
                        return LocalRedirect("/Student/Account/ChangePassword");

                    return LocalRedirect(safeReturnUrl is not null &&
                        safeReturnUrl.StartsWith("/Student/", StringComparison.OrdinalIgnoreCase)
                        ? safeReturnUrl : "/Student/Dashboard");
                }
            }

            await signInManager.SignOutAsync();
            ErrorMessage = "This account does not have a valid active workspace link. Please contact the institute administrator.";
            return Page();
        }

        ErrorMessage = result.IsLockedOut
            ? "Too many unsuccessful attempts. Your account is temporarily locked. Please try again later."
            : "The email address or username, or password, is incorrect. Please check your details and try again.";
        return Page();
    }

    public sealed class LoginInput
    {
        [Required(ErrorMessage = "Enter your email address or student username.")]
        [StringLength(256)]
        [Display(Name = "Email or student username")]
        public string LoginIdentifier { get; set; } = "";

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ReliableAcademy.Areas.Identity.Pages.Account;

[AllowAnonymous]
public sealed class LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager) : PageModel
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

        var result = await signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await userManager.FindByEmailAsync(Input.Email);
            if (user is not null)
            {
                var roles = await userManager.GetRolesAsync(user);
                if (roles.Contains("Admin"))
                    return LocalRedirect(Url.IsLocalUrl(ReturnUrl) && ReturnUrl != "/" ? ReturnUrl! : "/Admin/Dashboard");
                if (roles.Contains("Teacher"))
                    return LocalRedirect(Url.IsLocalUrl(ReturnUrl) && ReturnUrl != "/" ? ReturnUrl! : "/Teacher/Dashboard");
            }

            await signInManager.SignOutAsync();
            ErrorMessage = "This account does not have an active workspace role. Please contact the institute administrator.";
            return Page();
        }

        ErrorMessage = result.IsLockedOut
            ? "Too many unsuccessful attempts. Your account is temporarily locked. Please try again later."
            : "The email address or password is incorrect. Please check your details and try again.";
        return Page();
    }

    public sealed class LoginInput
    {
        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        public bool RememberMe { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace ReliableAcademy.ViewModels;

public sealed class AdminResetPasswordViewModel
{
    [Required, StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = "";

    [Required, DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = "";
}

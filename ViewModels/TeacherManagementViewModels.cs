using System.ComponentModel.DataAnnotations;

namespace ReliableAcademy.ViewModels;

public sealed class TeacherFormViewModel
{
    [Required, EmailAddress, StringLength(256)]
    [Display(Name = "Teacher email")]
    public string Email { get; set; } = "";

    [Phone, StringLength(20)]
    [Display(Name = "Mobile number")]
    public string? PhoneNumber { get; set; }

    [Required, MinLength(8), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}

public sealed class TeacherAccountViewModel
{
    public string UserId { get; init; } = "";
    public string Email { get; init; } = "";
    public string? PhoneNumber { get; init; }
    public bool IsLocked { get; init; }
    public int AssignedBatchCount { get; init; }
}

using System.ComponentModel.DataAnnotations;
using ReliableAcademy.Models;

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
    public int AssignedSubjectCount { get; init; }
}

public sealed class TeacherSubjectAssignmentViewModel
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public List<int> SubjectIds { get; set; } = new();
    public List<TeacherAssignmentSubjectOptionViewModel> Subjects { get; set; } = new();
}

public sealed class TeacherAssignmentSubjectOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public StudentStandard Standard { get; set; }
    public bool IsActive { get; set; }
    public bool IsAssigned { get; set; }
}

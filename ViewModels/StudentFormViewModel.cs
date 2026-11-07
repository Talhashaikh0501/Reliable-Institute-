using System.ComponentModel.DataAnnotations;
using ReliableAcademy.Models;

namespace ReliableAcademy.ViewModels;

public sealed class StudentFormViewModel
{
    [Required, StringLength(30)]
    [Display(Name = "Admission number")]
    public string AdmissionNumber { get; set; } = "";

    [Required, StringLength(160)]
    [Display(Name = "Student name")]
    public string FullName { get; set; } = "";

    [Required]
    public StudentStandard Standard { get; set; } = StudentStandard.Eighth;

    [Required, StringLength(20)]
    [Display(Name = "Academic year")]
    public string AcademicYear { get; set; } = "";

    [StringLength(80)]
    public string? Division { get; set; }

    [Phone, StringLength(20)]
    [Display(Name = "Student phone")]
    public string? PhoneNumber { get; set; }

    [EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    [StringLength(160)]
    [Display(Name = "Parent / guardian name")]
    public string? GuardianName { get; set; }

    [Phone, StringLength(20)]
    [Display(Name = "Parent / guardian phone")]
    public string? GuardianPhone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required]
    [Display(Name = "Admission date")]
    public DateOnly AdmissionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Batch")]
    public int? BatchId { get; set; }
}

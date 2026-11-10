using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using ReliableAcademy.Models;

namespace ReliableAcademy.ViewModels;

public sealed class TopperFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(160)]
    [Display(Name = "Student name")]
    public string StudentName { get; set; } = "";

    [Required]
    public StudentStandard Standard { get; set; } = StudentStandard.Tenth;

    [Required, StringLength(20)]
    [Display(Name = "Academic year")]
    public string AcademicYear { get; set; } = "";

    [Required, StringLength(100)]
    [Display(Name = "Exam / result title")]
    public string ExamName { get; set; } = "Board Examination";

    [Range(typeof(decimal), "0", "100000")]
    [Display(Name = "Marks obtained")]
    public decimal MarksObtained { get; set; }

    [Range(typeof(decimal), "0.01", "100000")]
    [Display(Name = "Maximum marks")]
    public decimal MaximumMarks { get; set; } = 100;

    [Display(Name = "Student photograph")]
    public IFormFile? Photo { get; set; }

    [Range(0, 999)]
    [Display(Name = "Display order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Show on public homepage")]
    public bool IsPublished { get; set; }

    [Display(Name = "I confirm that the student and parent/guardian have permitted the institute to publish this student's name, photograph and result on its public website.")]
    public bool HasPublicationConsent { get; set; }

    public string? ExistingPhotoPath { get; set; }
}

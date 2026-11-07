using System.ComponentModel.DataAnnotations;
using ReliableAcademy.Models;

namespace ReliableAcademy.ViewModels;

public sealed class AttendanceViewModel
{
    [Required]
    [Display(Name = "Attendance date")]
    public DateOnly AttendanceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    public int BatchId { get; set; }

    public string BatchName { get; set; } = "";
    public List<AttendanceStudentRowViewModel> Students { get; set; } = new();
}

public sealed class AttendanceStudentRowViewModel
{
    public int StudentId { get; set; }
    public string AdmissionNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public AttendanceStatus? Status { get; set; }
    public string? Note { get; set; }
}

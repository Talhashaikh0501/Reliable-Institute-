using System.ComponentModel.DataAnnotations;
using ReliableAcademy.Models;

namespace ReliableAcademy.ViewModels;

public sealed class BatchFormViewModel
{
    [Required, StringLength(80)]
    [Display(Name = "Batch name")]
    public string Name { get; set; } = "";

    [Required]
    public StudentStandard Standard { get; set; } = StudentStandard.Eighth;

    [Required, StringLength(20)]
    [Display(Name = "Academic year")]
    public string AcademicYear { get; set; } = "";

    [Display(Name = "Start time")]
    public TimeOnly? StartTime { get; set; }

    [Display(Name = "End time")]
    public TimeOnly? EndTime { get; set; }
}

public sealed class BatchListItemViewModel
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public StudentStandard Standard { get; init; }
    public string AcademicYear { get; init; } = "";
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public int ActiveStudents { get; init; }
    public bool IsActive { get; init; }
}

public sealed class BatchListViewModel
{
    public IReadOnlyList<BatchListItemViewModel> Batches { get; init; } = Array.Empty<BatchListItemViewModel>();
    public bool IncludeInactive { get; init; }
}

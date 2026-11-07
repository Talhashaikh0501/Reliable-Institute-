using ReliableAcademy.Models;

namespace ReliableAcademy.ViewModels;

public sealed class StudentListViewModel
{
    public IReadOnlyList<Student> Students { get; init; } = Array.Empty<Student>();
    public string? Search { get; init; }
    public StudentStandard? Standard { get; init; }
    public bool IncludeInactive { get; init; }
}

using ReliableAcademy.Models;

namespace ReliableAcademy.ViewModels;

public sealed class TeacherBatchOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public StudentStandard Standard { get; set; }
    public string AcademicYear { get; set; } = "";
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int StudentCount { get; set; }
    public int PresentToday { get; set; }
    public int AttendanceMarkedToday { get; set; }
}

public sealed class TeacherDashboardViewModel
{
    public DateTime Today { get; set; } = DateTime.Today;
    public int ActiveStudents { get; set; }
    public int ActiveBatches { get; set; }
    public int PresentToday { get; set; }
    public int AbsentToday { get; set; }
    public int AttendanceMarkedToday { get; set; }
    public int AttendanceCompletion { get; set; }
    public List<TeacherBatchOptionViewModel> Batches { get; set; } = new();
    public List<TeacherAttendanceTrendPoint> AttendanceTrend { get; set; } = new();
    public List<TeacherExamSummary> UpcomingExams { get; set; } = new();
    public List<Announcement> Announcements { get; set; } = new();
}

public sealed class TeacherAttendanceTrendPoint
{
    public DateOnly Date { get; set; }
    public int Marked { get; set; }
    public int Present { get; set; }
    public int Percentage { get; set; }
}

public sealed class TeacherExamSummary
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public string Name { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public string BatchName { get; set; } = "";
    public DateOnly ExamDate { get; set; }
    public decimal MaximumMarks { get; set; }
    public int ResultsEntered { get; set; }
}

public sealed class TeacherStudentsViewModel
{
    public int? BatchId { get; set; }
    public string? Search { get; set; }
    public List<TeacherBatchOptionViewModel> Batches { get; set; } = new();
    public List<TeacherStudentRowViewModel> Students { get; set; } = new();
}

public sealed class TeacherStudentRowViewModel
{
    public int Id { get; set; }
    public string AdmissionNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public StudentStandard Standard { get; set; }
    public string BatchName { get; set; } = "";
}

public sealed class TeacherStudentDetailsViewModel
{
    public Student Student { get; set; } = null!;
    public List<AttendanceRecord> RecentAttendance { get; set; } = new();
}

public sealed class TeacherExamListViewModel
{
    public int BatchId { get; set; }
    public string BatchName { get; set; } = "";
    public List<TeacherBatchOptionViewModel> Batches { get; set; } = new();
    public List<TeacherExamSummary> Exams { get; set; } = new();
}

public sealed class TeacherExamResultViewModel
{
    public Exam Exam { get; set; } = null!;
    public int BatchId { get; set; }
    public string BatchName { get; set; } = "";
    public List<TeacherExamResultRowViewModel> Students { get; set; } = new();
}

public sealed class TeacherExamResultRowViewModel
{
    public int StudentId { get; set; }
    public string AdmissionNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public decimal? MarksObtained { get; set; }
    public string? Remarks { get; set; }
    public bool IsPassing { get; set; }
}

public sealed class TeacherAssignmentViewModel
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public List<int> BatchIds { get; set; } = new();
    public List<TeacherAssignmentBatchOptionViewModel> Batches { get; set; } = new();
}

public sealed class TeacherAssignmentBatchOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public StudentStandard Standard { get; set; }
    public string AcademicYear { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsAssigned { get; set; }
}

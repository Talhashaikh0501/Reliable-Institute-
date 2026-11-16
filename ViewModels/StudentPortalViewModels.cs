using ReliableAcademy.Models;

namespace ReliableAcademy.ViewModels;

public sealed class StudentDashboardViewModel
{
    public Student Student { get; set; } = null!;
    public List<AttendanceRecord> RecentAttendance { get; set; } = new();
    public List<ExamResult> RecentResults { get; set; } = new();
    public List<FeePlan> FeePlans { get; set; } = new();
    public List<FeePayment> PaymentHistory { get; set; } = new();
    public List<Announcement> Announcements { get; set; } = new();
    public string? AttendanceMonth { get; set; }
    public int AllAttendanceCount { get; set; }
    public int AllPresentCount { get; set; }
    public int AllExamCount { get; set; }
    public int AttendanceCount => AllAttendanceCount;
    public int PresentCount => AllPresentCount;
    public int AttendanceRate => AttendanceCount == 0 ? 0 :
        (int)Math.Round(PresentCount * 100d / AttendanceCount);
    public decimal TotalFeeBalance => FeePlans.Sum(x => x.Balance);
    public decimal TotalFeePaid => PaymentHistory.Sum(x => x.Amount);
    public decimal TotalFeeNet => FeePlans.Sum(x => x.NetAmount);
}

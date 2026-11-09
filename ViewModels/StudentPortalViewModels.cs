using ReliableAcademy.Models;

namespace ReliableAcademy.ViewModels;

public sealed class StudentDashboardViewModel
{
    public Student Student { get; set; } = null!;
    public List<AttendanceRecord> RecentAttendance { get; set; } = new();
    public List<ExamResult> RecentResults { get; set; } = new();
    public List<FeePlan> FeePlans { get; set; } = new();
    public List<Announcement> Announcements { get; set; } = new();

    public int AttendanceCount => RecentAttendance.Count;
    public int PresentCount => RecentAttendance.Count(x =>
        x.Status == AttendanceStatus.Present || x.Status == AttendanceStatus.Late);
    public int AttendanceRate => AttendanceCount == 0 ? 0 :
        (int)Math.Round(PresentCount * 100d / AttendanceCount);
    public decimal TotalFeeBalance => FeePlans.Sum(x => x.Balance);
}

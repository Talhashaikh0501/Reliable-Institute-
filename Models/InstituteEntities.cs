using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ReliableAcademy.Models;

public enum StudentStandard { Eighth = 8, Ninth = 9, Tenth = 10, Eleventh = 11, Twelfth = 12 }
public enum AttendanceStatus { Present, Absent, Late, Leave }
public enum AnnouncementAudience { Everyone, Teachers, Students }

public class Student
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string AdmissionNumber { get; set; } = "";
    [Required, StringLength(160)] public string FullName { get; set; } = "";
    public StudentStandard Standard { get; set; }
    [Required, StringLength(20)] public string AcademicYear { get; set; } = "";
    [StringLength(80)] public string? Division { get; set; }
    [StringLength(20)] public string? PhoneNumber { get; set; }
    [EmailAddress, StringLength(256)] public string? Email { get; set; }
    [StringLength(160)] public string? GuardianName { get; set; }
    [StringLength(20)] public string? GuardianPhone { get; set; }
    [StringLength(500)] public string? Address { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateOnly AdmissionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public bool IsActive { get; set; } = true;
    public int? BatchId { get; set; }
    public TeachingBatch? Batch { get; set; }
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<FeePlan> FeePlans { get; set; } = new List<FeePlan>();
}
public class TeachingBatch
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Name { get; set; } = "";
    public StudentStandard Standard { get; set; }
    [Required, StringLength(20)] public string AcademicYear { get; set; } = "";
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
public class Subject
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    public StudentStandard Standard { get; set; }
    public bool IsActive { get; set; } = true;
}
public class AttendanceRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public DateOnly AttendanceDate { get; set; }
    public AttendanceStatus Status { get; set; }
    [StringLength(300)] public string? Note { get; set; }
    public string? MarkedByUserId { get; set; }
    public IdentityUser? MarkedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
public class FeePlan
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [Range(0, 99999999)] public decimal TotalAmount { get; set; }
    [Range(0, 99999999)] public decimal DiscountAmount { get; set; }
    public DateOnly DueDate { get; set; }
    [Required, StringLength(20)] public string AcademicYear { get; set; } = "";
    [StringLength(500)] public string? Notes { get; set; }
    public ICollection<FeePayment> Payments { get; set; } = new List<FeePayment>();
    [NotMapped] public decimal NetAmount => Math.Max(0, TotalAmount - DiscountAmount);
    [NotMapped] public decimal PaidAmount => Payments?.Sum(x => x.Amount) ?? 0;
    [NotMapped] public decimal Balance => Math.Max(0, NetAmount - PaidAmount);
}
public class FeePayment
{
    public int Id { get; set; }
    public int FeePlanId { get; set; }
    public FeePlan FeePlan { get; set; } = null!;
    [Required, StringLength(40)] public string ReceiptNumber { get; set; } = "";
    [Range(typeof(decimal), "0.01", "99999999")] public decimal Amount { get; set; }
    public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;
    [Required, StringLength(30)] public string PaymentMethod { get; set; } = "Cash";
    [StringLength(300)] public string? TransactionReference { get; set; }
    public string? ReceivedByUserId { get; set; }
    public IdentityUser? ReceivedBy { get; set; }
}
public class Exam
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = "";
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public int? BatchId { get; set; }
    public TeachingBatch? Batch { get; set; }
    public DateOnly ExamDate { get; set; }
    [Range(1, 1000)] public decimal MaximumMarks { get; set; }
    [Range(0, 1000)] public decimal PassingMarks { get; set; }
    public ICollection<ExamResult> Results { get; set; } = new List<ExamResult>();
}
public class ExamResult
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    [Range(0, 1000)] public decimal MarksObtained { get; set; }
    [StringLength(500)] public string? Remarks { get; set; }
}
public class Announcement
{
    public int Id { get; set; }
    [Required, StringLength(160)] public string Title { get; set; } = "";
    [Required, StringLength(3000)] public string Message { get; set; } = "";
    public AnnouncementAudience Audience { get; set; } = AnnouncementAudience.Everyone;
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsPublished { get; set; } = true;
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Models;

namespace ReliableAcademy.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    public DbSet<Student> Students => Set<Student>();
    public DbSet<TeachingBatch> TeachingBatches => Set<TeachingBatch>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<FeePlan> FeePlans => Set<FeePlan>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamResult> ExamResults => Set<ExamResult>();
    public DbSet<Announcement> Announcements => Set<Announcement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<IdentityUser>().ToTable("RCI_AspNetUsers");
        builder.Entity<IdentityRole>().ToTable("RCI_AspNetRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("RCI_AspNetUserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("RCI_AspNetUserLogins");
        builder.Entity<IdentityUserRole<string>>().ToTable("RCI_AspNetUserRoles");
        builder.Entity<IdentityUserToken<string>>().ToTable("RCI_AspNetUserTokens");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RCI_AspNetRoleClaims");

        builder.Entity<Student>().ToTable("RCI_Students");
        builder.Entity<TeachingBatch>().ToTable("RCI_TeachingBatches");
        builder.Entity<Subject>().ToTable("RCI_Subjects");
        builder.Entity<AttendanceRecord>().ToTable("RCI_AttendanceRecords");
        builder.Entity<FeePlan>().ToTable("RCI_FeePlans");
        builder.Entity<FeePayment>().ToTable("RCI_FeePayments");
        builder.Entity<Exam>().ToTable("RCI_Exams");
        builder.Entity<ExamResult>().ToTable("RCI_ExamResults");
        builder.Entity<Announcement>().ToTable("RCI_Announcements");

        builder.Entity<Student>(e =>
        {
            e.HasIndex(x => x.AdmissionNumber).IsUnique();
            e.HasIndex(x => new { x.Standard, x.AcademicYear, x.IsActive });
            e.Property(x => x.FullName).HasMaxLength(160).IsRequired();
            e.Property(x => x.AdmissionNumber).HasMaxLength(30).IsRequired();
            e.Property(x => x.AcademicYear).HasMaxLength(20).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.GuardianPhone).HasMaxLength(20);
            e.HasOne(x => x.Batch).WithMany(x => x.Students).HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<TeachingBatch>(e =>
        {
            e.HasIndex(x => new { x.Name, x.Standard, x.AcademicYear }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.AcademicYear).HasMaxLength(20).IsRequired();
        });
        builder.Entity<Subject>(e =>
        {
            e.HasIndex(x => new { x.Name, x.Standard }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });
        builder.Entity<AttendanceRecord>(e =>
        {
            e.HasIndex(x => new { x.StudentId, x.AttendanceDate }).IsUnique();
            e.HasOne(x => x.Student).WithMany(x => x.AttendanceRecords).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MarkedBy).WithMany().HasForeignKey(x => x.MarkedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<FeePlan>(e =>
        {
            e.Property(x => x.TotalAmount).HasPrecision(12, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(12, 2);
            e.HasOne(x => x.Student).WithMany(x => x.FeePlans).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<FeePayment>(e =>
        {
            e.HasIndex(x => x.ReceiptNumber).IsUnique();
            e.Property(x => x.Amount).HasPrecision(12, 2);
            e.Property(x => x.ReceiptNumber).HasMaxLength(40).IsRequired();
            e.Property(x => x.PaymentMethod).HasMaxLength(30).IsRequired();
            e.HasOne(x => x.FeePlan).WithMany(x => x.Payments).HasForeignKey(x => x.FeePlanId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReceivedBy).WithMany().HasForeignKey(x => x.ReceivedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Exam>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.HasOne(x => x.Batch).WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ExamResult>(e =>
        {
            e.HasIndex(x => new { x.ExamId, x.StudentId }).IsUnique();
            e.HasOne(x => x.Exam).WithMany(x => x.Results).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Announcement>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(160).IsRequired();
            e.Property(x => x.Message).HasMaxLength(3000).IsRequired();
            e.HasIndex(x => x.BatchId);
            e.HasIndex(x => x.CreatedByUserId);
            e.HasOne(x => x.Batch).WithMany(x => x.Announcements)
                .HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CreatedBy).WithMany()
                .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}

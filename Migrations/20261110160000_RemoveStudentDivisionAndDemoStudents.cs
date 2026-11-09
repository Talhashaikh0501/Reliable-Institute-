using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ReliableAcademy.Data;

namespace ReliableAcademy.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261110160000_RemoveStudentDivisionAndDemoStudents")]
public sealed class RemoveStudentDivisionAndDemoStudents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Remove only the fictional records created by the former DEMO-RCI generator.
        // Their related records and portal identities are removed in dependency order.
        migrationBuilder.Sql("""
            DECLARE @DemoStudentIds TABLE (Id int PRIMARY KEY);
            INSERT INTO @DemoStudentIds (Id)
            SELECT Id
            FROM RCI_Students
            WHERE AdmissionNumber LIKE 'DEMO-RCI-%';

            DECLARE @DemoUserIds TABLE (Id nvarchar(450) PRIMARY KEY);
            INSERT INTO @DemoUserIds (Id)
            SELECT DISTINCT claims.UserId
            FROM RCI_AspNetUserClaims AS claims
            INNER JOIN @DemoStudentIds AS students
                ON claims.ClaimType = 'rci:student_id'
                AND claims.ClaimValue = CONVERT(nvarchar(20), students.Id);

            DELETE payments
            FROM RCI_FeePayments AS payments
            INNER JOIN RCI_FeePlans AS plans ON plans.Id = payments.FeePlanId
            INNER JOIN @DemoStudentIds AS students ON students.Id = plans.StudentId;

            DELETE results
            FROM RCI_ExamResults AS results
            INNER JOIN @DemoStudentIds AS students ON students.Id = results.StudentId;

            DELETE attendance
            FROM RCI_AttendanceRecords AS attendance
            INNER JOIN @DemoStudentIds AS students ON students.Id = attendance.StudentId;

            DELETE plans
            FROM RCI_FeePlans AS plans
            INNER JOIN @DemoStudentIds AS students ON students.Id = plans.StudentId;

            DELETE users
            FROM RCI_AspNetUsers AS users
            INNER JOIN @DemoUserIds AS demoUsers ON demoUsers.Id = users.Id;

            DELETE students
            FROM RCI_Students AS students
            INNER JOIN @DemoStudentIds AS demoStudents ON demoStudents.Id = students.Id;
            """);

        migrationBuilder.DropColumn(
            name: "Division",
            table: "RCI_Students");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Division",
            table: "RCI_Students",
            type: "nvarchar(80)",
            maxLength: 80,
            nullable: true);
    }
}

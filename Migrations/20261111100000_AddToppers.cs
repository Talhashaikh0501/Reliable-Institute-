using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ReliableAcademy.Data;

namespace ReliableAcademy.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261111100000_AddToppers")]
public sealed class AddToppers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RCI_Toppers",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                StudentName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Standard = table.Column<int>(type: "int", nullable: false),
                AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ExamName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                MarksObtained = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                MaximumMarks = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                PhotoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                DisplayOrder = table.Column<int>(type: "int", nullable: false),
                IsPublished = table.Column<bool>(type: "bit", nullable: false),
                HasPublicationConsent = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_RCI_Toppers", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_RCI_Toppers_IsPublished_DisplayOrder",
            table: "RCI_Toppers",
            columns: new[] { "IsPublished", "DisplayOrder" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RCI_Toppers");
    }
}

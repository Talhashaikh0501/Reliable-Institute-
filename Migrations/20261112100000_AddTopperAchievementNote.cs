using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ReliableAcademy.Data;

namespace ReliableAcademy.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261112100000_AddTopperAchievementNote")]
public sealed class AddTopperAchievementNote : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AchievementNote",
            table: "RCI_Toppers",
            type: "nvarchar(240)",
            maxLength: 240,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AchievementNote",
            table: "RCI_Toppers");
    }
}
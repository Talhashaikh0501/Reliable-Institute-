using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ReliableAcademy.Data;

namespace ReliableAcademy.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261110120000_AddTeacherScopedAnnouncements")]
public sealed class AddTeacherScopedAnnouncements : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "BatchId",
            table: "RCI_Announcements",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CreatedByUserId",
            table: "RCI_Announcements",
            type: "nvarchar(450)",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_RCI_Announcements_BatchId",
            table: "RCI_Announcements",
            column: "BatchId");

        migrationBuilder.CreateIndex(
            name: "IX_RCI_Announcements_CreatedByUserId",
            table: "RCI_Announcements",
            column: "CreatedByUserId");

        migrationBuilder.AddForeignKey(
            name: "FK_RCI_Announcements_RCI_TeachingBatches_BatchId",
            table: "RCI_Announcements",
            column: "BatchId",
            principalTable: "RCI_TeachingBatches",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_RCI_Announcements_RCI_AspNetUsers_CreatedByUserId",
            table: "RCI_Announcements",
            column: "CreatedByUserId",
            principalTable: "RCI_AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_RCI_Announcements_RCI_TeachingBatches_BatchId",
            table: "RCI_Announcements");

        migrationBuilder.DropForeignKey(
            name: "FK_RCI_Announcements_RCI_AspNetUsers_CreatedByUserId",
            table: "RCI_Announcements");

        migrationBuilder.DropIndex(name: "IX_RCI_Announcements_BatchId", table: "RCI_Announcements");
        migrationBuilder.DropIndex(name: "IX_RCI_Announcements_CreatedByUserId", table: "RCI_Announcements");
        migrationBuilder.DropColumn(name: "BatchId", table: "RCI_Announcements");
        migrationBuilder.DropColumn(name: "CreatedByUserId", table: "RCI_Announcements");
    }
}

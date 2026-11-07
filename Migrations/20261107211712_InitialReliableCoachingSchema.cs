using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReliableAcademy.Migrations
{
    /// <inheritdoc />
    public partial class InitialReliableCoachingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RCI_Announcements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    Audience = table.Column<int>(type: "int", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RCI_AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RCI_AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RCI_Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Standard = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_Subjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RCI_TeachingBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Standard = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_TeachingBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RCI_AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RCI_AspNetRoleClaims_RCI_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RCI_AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RCI_AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RCI_AspNetUserClaims_RCI_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "RCI_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RCI_AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_RCI_AspNetUserLogins_RCI_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "RCI_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RCI_AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_RCI_AspNetUserRoles_RCI_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "RCI_AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RCI_AspNetUserRoles_RCI_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "RCI_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RCI_AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_RCI_AspNetUserTokens_RCI_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "RCI_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RCI_Exams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: true),
                    ExamDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MaximumMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PassingMarks = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_Exams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RCI_Exams_RCI_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "RCI_Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RCI_Exams_RCI_TeachingBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "RCI_TeachingBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RCI_Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdmissionNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Standard = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Division = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    GuardianName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    GuardianPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    AdmissionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_Students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RCI_Students_RCI_TeachingBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "RCI_TeachingBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RCI_AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    AttendanceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MarkedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RCI_AttendanceRecords_RCI_AspNetUsers_MarkedByUserId",
                        column: x => x.MarkedByUserId,
                        principalTable: "RCI_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RCI_AttendanceRecords_RCI_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "RCI_Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RCI_ExamResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    MarksObtained = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_ExamResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RCI_ExamResults_RCI_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "RCI_Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RCI_ExamResults_RCI_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "RCI_Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RCI_FeePlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_FeePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RCI_FeePlans_RCI_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "RCI_Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RCI_FeePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeePlanId = table.Column<int>(type: "int", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ReceivedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RCI_FeePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RCI_FeePayments_RCI_AspNetUsers_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "RCI_AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RCI_FeePayments_RCI_FeePlans_FeePlanId",
                        column: x => x.FeePlanId,
                        principalTable: "RCI_FeePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RCI_AspNetRoleClaims_RoleId",
                table: "RCI_AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "RCI_AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RCI_AspNetUserClaims_UserId",
                table: "RCI_AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RCI_AspNetUserLogins_UserId",
                table: "RCI_AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RCI_AspNetUserRoles_RoleId",
                table: "RCI_AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "RCI_AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "RCI_AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RCI_AttendanceRecords_MarkedByUserId",
                table: "RCI_AttendanceRecords",
                column: "MarkedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RCI_AttendanceRecords_StudentId_AttendanceDate",
                table: "RCI_AttendanceRecords",
                columns: new[] { "StudentId", "AttendanceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RCI_ExamResults_ExamId_StudentId",
                table: "RCI_ExamResults",
                columns: new[] { "ExamId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RCI_ExamResults_StudentId",
                table: "RCI_ExamResults",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_RCI_Exams_BatchId",
                table: "RCI_Exams",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RCI_Exams_SubjectId",
                table: "RCI_Exams",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RCI_FeePayments_FeePlanId",
                table: "RCI_FeePayments",
                column: "FeePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_RCI_FeePayments_ReceiptNumber",
                table: "RCI_FeePayments",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RCI_FeePayments_ReceivedByUserId",
                table: "RCI_FeePayments",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RCI_FeePlans_StudentId",
                table: "RCI_FeePlans",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_RCI_Students_AdmissionNumber",
                table: "RCI_Students",
                column: "AdmissionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RCI_Students_BatchId",
                table: "RCI_Students",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RCI_Students_Standard_AcademicYear_IsActive",
                table: "RCI_Students",
                columns: new[] { "Standard", "AcademicYear", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RCI_Subjects_Name_Standard",
                table: "RCI_Subjects",
                columns: new[] { "Name", "Standard" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RCI_TeachingBatches_Name_Standard_AcademicYear",
                table: "RCI_TeachingBatches",
                columns: new[] { "Name", "Standard", "AcademicYear" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RCI_Announcements");

            migrationBuilder.DropTable(
                name: "RCI_AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "RCI_AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "RCI_AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "RCI_AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "RCI_AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "RCI_AttendanceRecords");

            migrationBuilder.DropTable(
                name: "RCI_ExamResults");

            migrationBuilder.DropTable(
                name: "RCI_FeePayments");

            migrationBuilder.DropTable(
                name: "RCI_AspNetRoles");

            migrationBuilder.DropTable(
                name: "RCI_Exams");

            migrationBuilder.DropTable(
                name: "RCI_AspNetUsers");

            migrationBuilder.DropTable(
                name: "RCI_FeePlans");

            migrationBuilder.DropTable(
                name: "RCI_Subjects");

            migrationBuilder.DropTable(
                name: "RCI_Students");

            migrationBuilder.DropTable(
                name: "RCI_TeachingBatches");
        }
    }
}

# Reliable Coaching Classes — Management System

A commercial-quality coaching institute management system for Reliable Coaching Classes, Roshan Gate, Aurangabad.

## Technology
- ASP.NET Core MVC / C# on .NET 9
- Entity Framework Core 9 and SQL Server
- ASP.NET Core Identity with Admin and Teacher roles
- Razor Views, Bootstrap and custom responsive CSS

## Academic scope
The institute teaches board-curriculum standards 8th, 9th, 10th, 11th and 12th. Individual student dashboards are a later phase and must be restricted to students in standards 10th, 11th and 12th. Students in 8th and 9th do not receive individual dashboards.

## Implemented modules
- Identity authentication foundation, Admin / Teacher roles and optional bootstrap Admin seeding.
- Admin-only teacher account creation and access suspension / restoration.
- Student admissions, searchable directory, profiles, edit, archive and reactivation. Teachers can view student records; Admin manages changes.
- Class and batch creation, schedules, archive / restore, student counts and standard-matched student assignment.
- Daily attendance register by batch and date, Present / Absent / Late / Leave, optional notes and audit attribution. Admin and Teachers can manage attendance.
- Fee plans with discounts, outstanding balances, partial / full payments, serializable payment recording and printable receipts. Admin-only.
- Subject directory by standard, with archive / restore.
- Exam scheduling and result entry, with validation against maximum marks and eligible students.
- Institute announcements with audience targeting and publish / unpublish controls. Teacher dashboards only receive announcements intended for Everyone or Teachers.
- Role-aware management dashboard; financial figures are hidden from Teachers.

## Database safety
The application uses the RCI_ prefix for every table it owns, including ASP.NET Identity tables and the EF migration history table (RCI_EFMigrationsHistory). This isolates the application from unrelated tables in the shared SQL Server database. Never remove the prefix or run destructive database scripts against unprefixed tables.

## Local development
1. Open ReliableAcademy.sln in Visual Studio 2022 and restore NuGet packages.
2. Configure ConnectionStrings:DefaultConnection in User Secrets. Never commit connection credentials.
3. Optionally configure the first Admin account in User Secrets:
   ```powershell
   dotnet user-secrets set "BootstrapAdmin:Email" "your-admin-email@example.com"
   dotnet user-secrets set "BootstrapAdmin:Password" "A-Strong-Unique-Password"
   ```
4. Run the application. The startup initializer creates the Admin and Teacher roles. If both bootstrap settings are present, it creates the Admin account and assigns the Admin role.
5. For schema changes, review the generated migration and SQL script before applying it to the shared database.

## Git workflow
All development is committed directly to main. Do not create or use other branches.

## Production readiness
.NET 9 reaches end of support on 10 November 2026. Upgrade to a supported LTS release and verify the hosting environment before production deployment.

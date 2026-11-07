# Reliable Coaching Classes — Management System

A commercial-quality coaching institute management system for Reliable Coaching Classes, Roshan Gate, Aurangabad.

## Technology

- ASP.NET Core MVC / C# (.NET 9 during initial development)
- Entity Framework Core
- SQL Server / LocalDB
- ASP.NET Core Identity with Admin and Teacher roles
- Bootstrap, Razor Views, CSS and JavaScript

> .NET 9 reaches end of support on 10 November 2026. Upgrade to a supported LTS release before production deployment.

## Academic scope

The institute teaches 8th, 9th, 10th, 11th and 12th standards (board curriculum only). Individual student dashboards are planned only for 10th, 11th and 12th standards, as a later phase.

## Current implementation

- Identity authentication foundation and Admin / Teacher roles
- Admin-only teacher account creation and access suspension / restoration
- Student admissions, directory search and standard filtering
- Student profiles, edit, archive and reactivation (Admin write access; Teachers can view)
- Class and batch creation, schedules, archive / restore and student counts (Admin manages; Teachers can view)
- Batch assignment from active batches matching the selected standard
- Daily attendance register by batch and date, including Present / Absent / Late / Leave, optional notes and audit attribution (Admin and Teachers)
- Fee plans with discounts, outstanding balance calculations, partial / full payments, serializable payment recording and printable receipts (Admin only)
- Student profile summary for recorded fees and attendance; financial figures are hidden from Teachers

## Run locally

1. Install Visual Studio 2022 (17.14 recommended), the ASP.NET and web development workload, .NET 9 SDK and SQL Server LocalDB.
2. Open `ReliableAcademy.sln`.
3. Restore NuGet packages.
4. Open **Tools → NuGet Package Manager → Package Manager Console**. The repository includes a design-time DbContext factory so EF migrations can be generated without running the app's startup role seeding.
5. Set the default project to `ReliableAcademy` and run:

   ```powershell
   Add-Migration AddInstituteEntities
   Update-Database
   ```

   The checked-in initial migration creates the ASP.NET Core Identity tables. The current institute entities are already in the EF model; the command above generates their first database migration from the model.

6. Configure a local Admin account using User Secrets (never commit credentials):

   ```powershell
   dotnet user-secrets set "BootstrapAdmin:Email" "your-admin-email@example.com"
   dotnet user-secrets set "BootstrapAdmin:Password" "Use-A-Strong-Unique-Password"
   ```

7. Run the project with HTTPS.

The Admin seed is optional and runs only when both settings are present. It creates the account if needed and assigns the Admin role. Teacher accounts and staff administration will be added in a later module.

## Git workflow

All project development is committed directly to the `main` branch. Do not create or use other branches.

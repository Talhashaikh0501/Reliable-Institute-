using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Services;
using ReliableAcademy.ViewModels;

namespace ReliableAcademy.Areas.Teacher.Controllers;

[Area("Teacher")]
[Authorize(Roles = "Teacher")]
public sealed class StudentsController(ApplicationDbContext db, UserManager<IdentityUser> users) : Controller
{
    public async Task<IActionResult> Index(int? batchId, string? search)
    {
        var assignedIds = await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User);
        var batches = await db.TeachingBatches.AsNoTracking()
            .Where(x => x.IsActive && assignedIds.Contains(x.Id))
            .OrderBy(x => x.Standard).ThenBy(x => x.Name)
            .Select(x => new TeacherBatchOptionViewModel
            {
                Id = x.Id, Name = x.Name, Standard = x.Standard, AcademicYear = x.AcademicYear,
                StartTime = x.StartTime, EndTime = x.EndTime,
                StudentCount = x.Students.Count(s => s.IsActive)
            }).ToListAsync();

        if (batchId.HasValue && !batches.Any(x => x.Id == batchId.Value))
            return Forbid();

        var query = db.Students.AsNoTracking()
            .Where(x => x.IsActive && x.BatchId.HasValue && assignedIds.Contains(x.BatchId.Value));
        if (batchId.HasValue) query = query.Where(x => x.BatchId == batchId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.FullName.Contains(term) || x.AdmissionNumber.Contains(term));
        }

        var model = new TeacherStudentsViewModel
        {
            BatchId = batchId,
            Search = search,
            Batches = batches,
            Students = await query.OrderBy(x => x.FullName).Take(500)
                .Select(x => new TeacherStudentRowViewModel
                {
                    Id = x.Id, AdmissionNumber = x.AdmissionNumber, FullName = x.FullName,
                    Standard = x.Standard, BatchName = x.Batch!.Name
                }).ToListAsync()
        };
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var assignedIds = await TeacherBatchAccess.GetAssignedBatchIdsAsync(users, User);
        var student = await db.Students.AsNoTracking().Include(x => x.Batch)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive &&
                x.BatchId.HasValue && assignedIds.Contains(x.BatchId.Value));
        if (student is null) return NotFound();

        var attendance = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.StudentId == id)
            .OrderByDescending(x => x.AttendanceDate).Take(12).ToListAsync();

        return View(new TeacherStudentDetailsViewModel { Student = student, RecentAttendance = attendance });
    }
}

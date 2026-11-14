using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReliableAcademy.Data;
using ReliableAcademy.Models;

namespace ReliableAcademy.Services;

public sealed class AssistantDataTools(ApplicationDbContext db, UserManager<IdentityUser> users)
{
    public async Task<string> ExecuteAsync(string name, JsonElement arguments, CancellationToken cancellationToken)
    {
        try
        {
            object result = name switch
            {
                "get_institute_overview" => await GetInstituteOverviewAsync(cancellationToken),
                "search_students" => await SearchStudentsAsync(arguments, cancellationToken),
                "get_student_academic_profile" => await GetStudentAcademicProfileAsync(arguments, cancellationToken),
                "get_attendance_summary" => await GetAttendanceSummaryAsync(arguments, cancellationToken),
                "find_low_attendance_students" => await FindLowAttendanceStudentsAsync(arguments, cancellationToken),
                "get_fee_summary" => await GetFeeSummaryAsync(arguments, cancellationToken),
                "list_fee_balances" => await ListFeeBalancesAsync(arguments, cancellationToken),
                "get_exam_performance" => await GetExamPerformanceAsync(arguments, cancellationToken),
                "get_top_exam_results" => await GetTopExamResultsAsync(arguments, cancellationToken),
                "get_batch_overview" => await GetBatchOverviewAsync(cancellationToken),
                "get_teacher_overview" => await GetTeacherOverviewAsync(cancellationToken),
                "get_teacher_assignments" => await GetTeacherAssignmentsAsync(arguments, cancellationToken),
                "get_recent_announcements" => await GetRecentAnnouncementsAsync(cancellationToken),
                "get_published_toppers" => await GetPublishedToppersAsync(cancellationToken),
                _ => new { error = "This analysis function is not available." }
            };

            return JsonSerializer.Serialize(result);
        }
        catch (Exception)
        {
            return JsonSerializer.Serialize(new
            {
                error = "The requested institute data could not be loaded. Try narrowing the question or try again."
            });
        }
    }

    private async Task<object> GetInstituteOverviewAsync(CancellationToken ct)
    {
        var today = GetIndiaToday();
        var monthStartDate = new DateOnly(today.Year, today.Month, 1);
        var nextMonthDate = monthStartDate.AddMonths(1);
        var monthStartUtc = IndiaMidnightUtc(monthStartDate);
        var nextMonthUtc = IndiaMidnightUtc(nextMonthDate);

        var activeStudents = await db.Students.CountAsync(x => x.IsActive, ct);
        var inactiveStudents = await db.Students.CountAsync(x => !x.IsActive, ct);
        var activeBatches = await db.TeachingBatches.CountAsync(x => x.IsActive, ct);
        var subjects = await db.Subjects.CountAsync(x => x.IsActive, ct);
        var teachersCount = (await users.GetUsersInRoleAsync("Teacher")).Count;
        var admissionsThisMonth = await db.Students.CountAsync(x =>
            x.AdmissionDate >= monthStartDate &&
            x.AdmissionDate < nextMonthDate, ct);
        var feesCollectedThisMonth = await db.FeePayments.AsNoTracking()
            .Where(x => x.PaidAtUtc >= monthStartUtc && x.PaidAtUtc < nextMonthUtc)
            .Select(x => (decimal?)x.Amount).SumAsync(ct) ?? 0m;
        var feeRows = await db.FeePlans.AsNoTracking()
            .Select(x => new
            {
                x.TotalAmount,
                x.DiscountAmount,
                Paid = x.Payments.Select(p => (decimal?)p.Amount).Sum() ?? 0m,
                x.DueDate
            }).ToListAsync(ct);
        var outstanding = feeRows.Sum(x => Math.Max(0m, x.TotalAmount - x.DiscountAmount - x.Paid));
        var overdue = feeRows.Where(x => x.DueDate < today)
            .Sum(x => Math.Max(0m, x.TotalAmount - x.DiscountAmount - x.Paid));
        var withoutBatch = await db.Students.CountAsync(x => x.IsActive && x.BatchId == null, ct);

        return new
        {
            asOf = today.ToString("yyyy-MM-dd"),
            activeStudents,
            inactiveStudents,
            activeBatches,
            activeSubjects = subjects,
            teachers = teachersCount,
            admissionsThisMonth,
            feesCollectedThisMonth,
            outstandingFeeBalance = outstanding,
            overdueFeeBalance = overdue,
            activeStudentsWithoutBatch = withoutBatch
        };
    }

    private async Task<object> SearchStudentsAsync(JsonElement args, CancellationToken ct)
    {
        var search = GetString(args, "query");
        if (string.IsNullOrWhiteSpace(search))
            return new { error = "Provide a student name or admission number to search." };

        var standard = GetStandard(args, "standard");
        var activeOnly = GetBoolean(args, "active_only", true);
        var limit = Math.Clamp(GetInteger(args, "max_results", 10), 1, 25);

        var query = db.Students.AsNoTracking().Include(x => x.Batch).AsQueryable();
        if (activeOnly) query = query.Where(x => x.IsActive);
        if (standard.HasValue) query = query.Where(x => x.Standard == standard.Value);
        query = query.Where(x => x.FullName.Contains(search) || x.AdmissionNumber.Contains(search));

        var rows = await query.OrderBy(x => x.FullName).Take(limit)
            .Select(x => new
            {
                x.FullName,
                x.AdmissionNumber,
                Standard = (int)x.Standard,
                Batch = x.Batch == null ? "Not assigned" : x.Batch.Name,
                x.AcademicYear,
                x.IsActive
            }).ToListAsync(ct);

        return new { matchedRecords = rows.Count, results = rows };
    }


    private async Task<object> GetStudentAcademicProfileAsync(JsonElement args, CancellationToken ct)
    {
        var search = GetString(args, "student");
        if (string.IsNullOrWhiteSpace(search))
            return new { error = "Provide a student's name or admission number." };

        var matches = await db.Students.AsNoTracking()
            .Include(x => x.Batch)
            .Where(x => x.AdmissionNumber == search || x.FullName.Contains(search))
            .OrderByDescending(x => x.AdmissionNumber == search)
            .ThenBy(x => x.FullName)
            .Take(5)
            .ToListAsync(ct);

        if (matches.Count == 0)
            return new { error = "No student record matched that search." };

        if (matches.Count > 1)
        {
            return new
            {
                ambiguous = true,
                message = "More than one student matched. Ask for an admission number or a more specific name.",
                candidates = matches.Select(x => new
                {
                    x.FullName,
                    x.AdmissionNumber,
                    standard = (int)x.Standard,
                    batch = x.Batch?.Name ?? "Not assigned"
                })
            };
        }

        var student = matches[0];
        var attendance = await db.AttendanceRecords.AsNoTracking()
            .Where(x => x.StudentId == student.Id)
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var attendanceTotal = attendance.Sum(x => x.Count);
        var attended = attendance.Where(x =>
            x.Status == AttendanceStatus.Present || x.Status == AttendanceStatus.Late).Sum(x => x.Count);

        var examResults = await db.ExamResults.AsNoTracking()
            .Where(x => x.StudentId == student.Id)
            .Include(x => x.Exam).ThenInclude(x => x.Subject)
            .OrderByDescending(x => x.Exam.ExamDate)
            .Take(12)
            .Select(x => new
            {
                exam = x.Exam.Name,
                subject = x.Exam.Subject.Name,
                date = x.Exam.ExamDate,
                marks = x.MarksObtained,
                maximumMarks = x.Exam.MaximumMarks,
                percentage = x.Exam.MaximumMarks <= 0 ? 0 : Math.Round(x.MarksObtained / x.Exam.MaximumMarks * 100m, 1),
                passed = x.MarksObtained >= x.Exam.PassingMarks
            }).ToListAsync(ct);

        var feePlans = await db.FeePlans.AsNoTracking()
            .Where(x => x.StudentId == student.Id)
            .Include(x => x.Payments)
            .OrderByDescending(x => x.DueDate)
            .ToListAsync(ct);

        return new
        {
            student = student.FullName,
            student.AdmissionNumber,
            standard = (int)student.Standard,
            student.AcademicYear,
            batch = student.Batch?.Name ?? "Not assigned",
            student.IsActive,
            attendance = new
            {
                totalMarked = attendanceTotal,
                present = attendance.FirstOrDefault(x => x.Status == AttendanceStatus.Present)?.Count ?? 0,
                late = attendance.FirstOrDefault(x => x.Status == AttendanceStatus.Late)?.Count ?? 0,
                absent = attendance.FirstOrDefault(x => x.Status == AttendanceStatus.Absent)?.Count ?? 0,
                leave = attendance.FirstOrDefault(x => x.Status == AttendanceStatus.Leave)?.Count ?? 0,
                attendancePercentage = attendanceTotal == 0 ? 0 : Math.Round(attended * 100d / attendanceTotal, 1),
                percentageRule = "Present and Late count as attended."
            },
            recentExamResults = examResults,
            feePlans = feePlans.Select(x => new
            {
                x.AcademicYear,
                dueDate = x.DueDate.ToString("yyyy-MM-dd"),
                netFee = Math.Max(0m, x.TotalAmount - x.DiscountAmount),
                paid = x.Payments.Sum(p => p.Amount),
                balance = Math.Max(0m, x.TotalAmount - x.DiscountAmount - x.Payments.Sum(p => p.Amount))
            })
        };
    }

    private async Task<object> GetAttendanceSummaryAsync(JsonElement args, CancellationToken ct)
    {
        var (from, to) = GetDateRange(args, defaultToMonth: true);
        var batchName = GetString(args, "batch_name");
        var standard = GetStandard(args, "standard");

        var query = db.AttendanceRecords.AsNoTracking()
            .Where(x => x.AttendanceDate >= from && x.AttendanceDate <= to);
        if (!string.IsNullOrWhiteSpace(batchName))
            query = query.Where(x => x.Student.Batch != null && x.Student.Batch.Name.Contains(batchName));
        if (standard.HasValue)
            query = query.Where(x => x.Student.Standard == standard.Value);

        var grouped = await query.GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var total = grouped.Sum(x => x.Count);
        var present = grouped.Where(x => x.Status == AttendanceStatus.Present || x.Status == AttendanceStatus.Late)
            .Sum(x => x.Count);
        var absent = grouped.FirstOrDefault(x => x.Status == AttendanceStatus.Absent)?.Count ?? 0;
        var late = grouped.FirstOrDefault(x => x.Status == AttendanceStatus.Late)?.Count ?? 0;
        var leave = grouped.FirstOrDefault(x => x.Status == AttendanceStatus.Leave)?.Count ?? 0;

        return new
        {
            from = from.ToString("yyyy-MM-dd"),
            to = to.ToString("yyyy-MM-dd"),
            batch = string.IsNullOrWhiteSpace(batchName) ? "All batches" : batchName,
            standard = standard.HasValue ? ((int)standard.Value).ToString() : "All standards",
            totalMarkedRecords = total,
            presentOrLate = present,
            absent,
            late,
            leave,
            attendanceRateIncludingLate = total == 0 ? 0 : Math.Round(present * 100d / total, 1)
        };
    }

    private async Task<object> FindLowAttendanceStudentsAsync(JsonElement args, CancellationToken ct)
    {
        var minimum = GetDecimal(args, "minimum_percentage", 75m);
        minimum = Math.Clamp(minimum, 0m, 100m);
        var standard = GetStandard(args, "standard");
        var batchName = GetString(args, "batch_name");
        var (from, to) = GetOptionalDateRange(args);
        var limit = Math.Clamp(GetInteger(args, "max_results", 20), 1, 25);

        var query = db.Students.AsNoTracking().Where(x => x.IsActive);
        if (standard.HasValue) query = query.Where(x => x.Standard == standard.Value);
        if (!string.IsNullOrWhiteSpace(batchName))
            query = query.Where(x => x.Batch != null && x.Batch.Name.Contains(batchName));

        var rows = await query.Select(s => new
        {
            s.Id,
            s.FullName,
            s.AdmissionNumber,
            Standard = (int)s.Standard,
            Batch = s.Batch == null ? "Not assigned" : s.Batch.Name,
            Total = s.AttendanceRecords.Count(a =>
                (!from.HasValue || a.AttendanceDate >= from.Value) &&
                (!to.HasValue || a.AttendanceDate <= to.Value)),
            PresentOrLate = s.AttendanceRecords.Count(a =>
                (!from.HasValue || a.AttendanceDate >= from.Value) &&
                (!to.HasValue || a.AttendanceDate <= to.Value) &&
                (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late))
        }).ToListAsync(ct);

        var matches = rows.Where(x => x.Total > 0)
            .Select(x => new
            {
                x.FullName,
                x.AdmissionNumber,
                x.Standard,
                x.Batch,
                markedRecords = x.Total,
                attendancePercentage = Math.Round(x.PresentOrLate * 100m / x.Total, 1)
            })
            .Where(x => x.attendancePercentage < minimum)
            .OrderBy(x => x.attendancePercentage)
            .Take(limit)
            .ToList();

        return new
        {
            thresholdBelow = minimum,
            dateRange = from.HasValue || to.HasValue
                ? $"{from?.ToString("yyyy-MM-dd") ?? "earliest"} to {to?.ToString("yyyy-MM-dd") ?? "today"}"
                : "All recorded attendance",
            percentageRule = "Present and Late count as attended; Absent and Leave do not.",
            matchedStudents = matches.Count,
            results = matches
        };
    }

    private async Task<object> GetFeeSummaryAsync(JsonElement args, CancellationToken ct)
    {
        var today = GetIndiaToday();
        var (from, to) = GetDateRange(args, defaultToMonth: true);
        var plans = await db.FeePlans.AsNoTracking()
            .Select(x => new
            {
                x.TotalAmount,
                x.DiscountAmount,
                x.DueDate,
                Paid = x.Payments.Select(p => (decimal?)p.Amount).Sum() ?? 0m
            }).ToListAsync(ct);

        var balances = plans.Select(x => new
        {
            Net = Math.Max(0m, x.TotalAmount - x.DiscountAmount),
            Paid = x.Paid,
            Balance = Math.Max(0m, x.TotalAmount - x.DiscountAmount - x.Paid),
            x.DueDate
        }).ToList();

        var start = IndiaMidnightUtc(from);
        var end = IndiaMidnightUtc(to.AddDays(1));
        var payments = await db.FeePayments.AsNoTracking()
            .Where(x => x.PaidAtUtc >= start && x.PaidAtUtc < end)
            .Select(x => x.Amount).ToListAsync(ct);

        return new
        {
            paymentPeriodFrom = from.ToString("yyyy-MM-dd"),
            paymentPeriodTo = to.ToString("yyyy-MM-dd"),
            feePlanCount = plans.Count,
            totalNetFees = balances.Sum(x => x.Net),
            totalPaidAcrossAllPlans = balances.Sum(x => x.Paid),
            totalOutstanding = balances.Sum(x => x.Balance),
            overduePlanCount = balances.Count(x => x.Balance > 0 && x.DueDate < today),
            overdueBalance = balances.Where(x => x.DueDate < today).Sum(x => x.Balance),
            collectedDuringPeriod = payments.Sum(),
            paymentCountDuringPeriod = payments.Count
        };
    }

    private async Task<object> ListFeeBalancesAsync(JsonElement args, CancellationToken ct)
    {
        var overdueOnly = GetBoolean(args, "overdue_only", false);
        var outstandingOnly = GetBoolean(args, "outstanding_only", true);
        var limit = Math.Clamp(GetInteger(args, "max_results", 20), 1, 25);
        var today = GetIndiaToday();

        var plans = await db.FeePlans.AsNoTracking()
            .Include(x => x.Student).ThenInclude(x => x.Batch).Include(x => x.Payments)
            .ToListAsync(ct);

        var rows = plans.Select(x =>
        {
            var net = Math.Max(0m, x.TotalAmount - x.DiscountAmount);
            var paid = x.Payments.Sum(p => p.Amount);
            return new
            {
                student = x.Student.FullName,
                admissionNumber = x.Student.AdmissionNumber,
                standard = (int)x.Student.Standard,
                batch = x.Student.Batch?.Name ?? "Not assigned",
                academicYear = x.AcademicYear,
                dueDate = x.DueDate.ToString("yyyy-MM-dd"),
                netFee = net,
                paid,
                balance = Math.Max(0m, net - paid),
                isOverdue = x.DueDate < today && net > paid
            };
        });

        if (outstandingOnly) rows = rows.Where(x => x.balance > 0);
        if (overdueOnly) rows = rows.Where(x => x.isOverdue);

        var results = rows.OrderByDescending(x => x.balance).Take(limit).ToList();
        return new { matchedRecords = results.Count, results };
    }

    private async Task<object> GetExamPerformanceAsync(JsonElement args, CancellationToken ct)
    {
        var subjectName = GetString(args, "subject_name");
        var batchName = GetString(args, "batch_name");
        var standard = GetStandard(args, "standard");

        var query =
            from result in db.ExamResults.AsNoTracking()
            join exam in db.Exams.AsNoTracking() on result.ExamId equals exam.Id
            join subject in db.Subjects.AsNoTracking() on exam.SubjectId equals subject.Id
            join student in db.Students.AsNoTracking() on result.StudentId equals student.Id
            select new
            {
                Subject = subject.Name,
                Standard = (int)subject.Standard,
                Exam = exam.Name,
                exam.ExamDate,
                exam.MaximumMarks,
                exam.PassingMarks,
                result.MarksObtained,
                StudentBatchId = student.BatchId,
                StudentBatch = student.Batch == null ? "Not assigned" : student.Batch.Name
            };

        if (!string.IsNullOrWhiteSpace(subjectName))
            query = query.Where(x => x.Subject.Contains(subjectName));
        if (standard.HasValue)
            query = query.Where(x => x.Standard == (int)standard.Value);
        if (!string.IsNullOrWhiteSpace(batchName))
            query = query.Where(x => x.StudentBatch.Contains(batchName));

        var rows = await query.ToListAsync(ct);
        var grouped = rows.GroupBy(x => new { x.Subject, x.Standard })
            .Select(g => new
            {
                subject = g.Key.Subject,
                standard = g.Key.Standard,
                resultEntries = g.Count(),
                averagePercentage = Math.Round(g.Average(x =>
                    x.MaximumMarks <= 0 ? 0d : (double)(x.MarksObtained / x.MaximumMarks * 100m)), 1),
                passed = g.Count(x => x.MarksObtained >= x.PassingMarks),
                belowPassingMark = g.Count(x => x.MarksObtained < x.PassingMarks),
                examCount = g.Select(x => x.Exam).Distinct().Count()
            })
            .OrderByDescending(x => x.averagePercentage)
            .ToList();

        return new { resultEntries = rows.Count, subjectPerformance = grouped };
    }

    private async Task<object> GetTopExamResultsAsync(JsonElement args, CancellationToken ct)
    {
        var subjectName = GetString(args, "subject_name");
        var standard = GetStandard(args, "standard");
        var limit = Math.Clamp(GetInteger(args, "max_results", 10), 1, 20);

        var query =
            from result in db.ExamResults.AsNoTracking()
            join exam in db.Exams.AsNoTracking() on result.ExamId equals exam.Id
            join subject in db.Subjects.AsNoTracking() on exam.SubjectId equals subject.Id
            join student in db.Students.AsNoTracking() on result.StudentId equals student.Id
            select new
            {
                student.FullName,
                Standard = (int)student.Standard,
                Subject = subject.Name,
                Exam = exam.Name,
                result.MarksObtained,
                exam.MaximumMarks,
                exam.ExamDate
            };

        if (!string.IsNullOrWhiteSpace(subjectName))
            query = query.Where(x => x.Subject.Contains(subjectName));
        if (standard.HasValue)
            query = query.Where(x => x.Standard == (int)standard.Value);

        var rows = await query.OrderByDescending(x => x.MarksObtained / x.MaximumMarks)
            .ThenByDescending(x => x.ExamDate).Take(limit).ToListAsync(ct);

        return new
        {
            results = rows.Select(x => new
            {
                student = x.FullName,
                standard = x.Standard,
                subject = x.Subject,
                exam = x.Exam,
                marks = x.MarksObtained,
                maximumMarks = x.MaximumMarks,
                percentage = x.MaximumMarks <= 0 ? 0 : Math.Round(x.MarksObtained / x.MaximumMarks * 100m, 1),
                examDate = x.ExamDate.ToString("yyyy-MM-dd")
            })
        };
    }

    private async Task<object> GetBatchOverviewAsync(CancellationToken ct)
    {
        var batches = await db.TeachingBatches.AsNoTracking()
            .OrderBy(x => x.Standard).ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                Standard = (int)x.Standard,
                x.AcademicYear,
                x.IsActive,
                ActiveStudents = x.Students.Count(s => s.IsActive),
                TotalStudents = x.Students.Count()
            }).ToListAsync(ct);

        var teacherBatchClaims = await db.UserClaims.AsNoTracking()
            .Where(x => x.ClaimType == TeacherBatchAccess.ClaimType)
            .Select(x => x.ClaimValue).ToListAsync(ct);

        var results = batches.Select(x => new
        {
            x.Name,
            x.Standard,
            x.AcademicYear,
            x.IsActive,
            x.ActiveStudents,
            x.TotalStudents,
            AssignedTeachers = teacherBatchClaims.Count(value => value == x.Id.ToString())
        }).ToList();

        return new { batchCount = results.Count, batches = results };
    }

    private async Task<object> GetTeacherOverviewAsync(CancellationToken ct)
    {
        var teachersList = await users.GetUsersInRoleAsync("Teacher");
        var rows = new List<object>();
        foreach (var teacher in teachersList)
        {
            var claims = await users.GetClaimsAsync(teacher);
            var locked = teacher.LockoutEnd.HasValue && teacher.LockoutEnd.Value > DateTimeOffset.UtcNow;
            rows.Add(new
            {
                teacherLabel = teacher.UserName?.Split('@')[0] ?? "Teacher",
                assignedBatches = claims.Count(x => x.Type == TeacherBatchAccess.ClaimType),
                assignedSubjects = claims.Count(x => x.Type == TeacherSubjectAccess.ClaimType),
                accessStatus = locked ? "Suspended" : "Active"
            });
        }

        return new { teacherCount = rows.Count, teachers = rows };
    }


    private async Task<object> GetTeacherAssignmentsAsync(JsonElement args, CancellationToken ct)
    {
        var search = GetString(args, "teacher");
        var teacherUsers = await users.GetUsersInRoleAsync("Teacher");
        if (!string.IsNullOrWhiteSpace(search))
        {
            teacherUsers = teacherUsers.Where(x =>
                (x.UserName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }

        var rows = new List<object>();
        foreach (var teacher in teacherUsers.Take(15))
        {
            var claims = await users.GetClaimsAsync(teacher);
            var batchIds = claims.Where(x => x.Type == TeacherBatchAccess.ClaimType)
                .Select(x => int.TryParse(x.Value, out var id) ? id : 0).Where(x => x > 0).Distinct().ToList();
            var subjectIds = claims.Where(x => x.Type == TeacherSubjectAccess.ClaimType)
                .Select(x => int.TryParse(x.Value, out var id) ? id : 0).Where(x => x > 0).Distinct().ToList();

            var batches = await db.TeachingBatches.AsNoTracking()
                .Where(x => batchIds.Contains(x.Id))
                .OrderBy(x => x.Standard).ThenBy(x => x.Name)
                .Select(x => new { x.Name, Standard = (int)x.Standard, x.AcademicYear, x.IsActive })
                .ToListAsync(ct);
            var subjects = await db.Subjects.AsNoTracking()
                .Where(x => subjectIds.Contains(x.Id))
                .OrderBy(x => x.Standard).ThenBy(x => x.Name)
                .Select(x => new { x.Name, Standard = (int)x.Standard, x.IsActive })
                .ToListAsync(ct);

            rows.Add(new
            {
                teacher = teacher.UserName?.Split('@')[0] ?? "Teacher",
                assignedBatches = batches,
                assignedSubjects = subjects
            });
        }

        return new
        {
            matchedTeachers = rows.Count,
            teachers = rows,
            note = string.IsNullOrWhiteSpace(search) ? "Showing up to 15 teacher accounts." : null
        };
    }

    private async Task<object> GetRecentAnnouncementsAsync(CancellationToken ct)
    {
        var rows = await db.Announcements.AsNoTracking()
            .Where(x => x.IsPublished && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > DateTime.UtcNow))
            .OrderByDescending(x => x.PublishedAtUtc).Take(10)
            .Select(x => new
            {
                x.Title,
                Audience = x.Audience.ToString(),
                Message = x.Message.Length > 500 ? x.Message.Substring(0, 500) : x.Message,
                PublishedAt = x.PublishedAtUtc
            }).ToListAsync(ct);

        return new { announcements = rows };
    }


    private async Task<object> GetPublishedToppersAsync(CancellationToken ct)
    {
        var totalRecords = await db.Toppers.CountAsync(ct);
        var publishedCount = await db.Toppers.CountAsync(x => x.IsPublished && x.HasPublicationConsent, ct);
        var published = await db.Toppers.AsNoTracking()
            .Where(x => x.IsPublished && x.HasPublicationConsent)
            .OrderByDescending(x => x.MarksObtained / x.MaximumMarks)
            .ThenBy(x => x.DisplayOrder)
            .Take(20)
            .Select(x => new
            {
                x.StudentName,
                Standard = (int)x.Standard,
                x.AcademicYear,
                x.ExamName,
                x.MarksObtained,
                x.MaximumMarks,
                x.AchievementNote
            }).ToListAsync(ct);

        return new
        {
            totalTopperRecords = totalRecords,
            publiclyVisibleWithConsent = publishedCount,
            publishedAchievers = published.Select(x => new
            {
                x.StudentName,
                x.Standard,
                x.AcademicYear,
                x.ExamName,
                x.MarksObtained,
                x.MaximumMarks,
                percentage = x.MaximumMarks <= 0 ? 0 : Math.Round(x.MarksObtained / x.MaximumMarks * 100m, 1),
                x.AchievementNote
            })
        };
    }

    private static TimeZoneInfo IndiaTimeZone =>
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata");

    private static DateOnly GetIndiaToday()
    {
        var indiaNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);
        return DateOnly.FromDateTime(indiaNow);
    }

    private static DateTime IndiaMidnightUtc(DateOnly date)
    {
        var indiaMidnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(indiaMidnight, IndiaTimeZone);
    }

    private static (DateOnly? From, DateOnly? To) GetOptionalDateRange(JsonElement args)
    {
        var today = GetIndiaToday();
        var from = GetDate(args, "from_date");
        var to = GetDate(args, "to_date");

        // No dates means analyse the complete attendance history.
        // A supplied start date without an end date runs through today.
        if (from.HasValue && !to.HasValue)
            to = today;

        if (from.HasValue && to.HasValue && from.Value > to.Value)
            (from, to) = (to, from);

        // Keep broad date filters bounded while preserving the requested end date.
        if (from.HasValue && to.HasValue && to.Value.DayNumber - from.Value.DayNumber > 366)
            from = to.Value.AddDays(-366);

        return (from, to);
    }

    private static (DateOnly From, DateOnly To) GetDateRange(JsonElement args, bool defaultToMonth)
    {
        var today = GetIndiaToday();
        var defaultFrom = defaultToMonth ? new DateOnly(today.Year, today.Month, 1) : today.AddDays(-29);
        var from = GetDate(args, "from_date") ?? defaultFrom;
        var to = GetDate(args, "to_date") ?? today;

        if (from > to)
            (from, to) = (to, from);
        if (to.DayNumber - from.DayNumber > 366)
            from = to.AddDays(-366);

        return (from, to);
    }

    private static string? GetString(JsonElement args, string name)
    {
        if (!args.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
            return null;
        var result = value.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(result) ? null : result.Length > 120 ? result[..120] : result;
    }

    private static int GetInteger(JsonElement args, string name, int fallback)
    {
        return args.TryGetProperty(name, out var value) && value.TryGetInt32(out var result)
            ? result : fallback;
    }

    private static decimal GetDecimal(JsonElement args, string name, decimal fallback)
    {
        return args.TryGetProperty(name, out var value) && value.TryGetDecimal(out var result)
            ? result : fallback;
    }

    private static bool GetBoolean(JsonElement args, string name, bool fallback)
    {
        return args.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean() : fallback;
    }

    private static StudentStandard? GetStandard(JsonElement args, string name)
    {
        var number = GetInteger(args, name, 0);
        return Enum.IsDefined(typeof(StudentStandard), number) ? (StudentStandard)number : null;
    }

    private static DateOnly? GetDate(JsonElement args, string name)
    {
        var value = GetString(args, name);
        return DateOnly.TryParse(value, out var result) ? result : null;
    }
}

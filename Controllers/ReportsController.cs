using AttendanceApi.Data;
using AttendanceApi.Dtos;
using AttendanceApi.Models;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Controllers;

// تقارير الحضور والغياب (FR-33, FR-34, BR-03, BR-04)
[ApiController]
[Authorize(Roles = "Doctor,Admin")]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ActorService _actor;
    private readonly SessionService _sessions;

    public ReportsController(AppDbContext db, ActorService actor, SessionService sessions)
    {
        _db = db;
        _actor = actor;
        _sessions = sessions;
    }

    // GET /api/reports/courses/7
    // الجلسات "اللي اتعقدت فعلًا" = الجلسات المقفولة بس. الغياب مابيتخزنش، بيتحسب:
    // طالب مسجل في المادة ومالوش سجل حضور في جلسة مقفولة = غايب
    [HttpGet("courses/{courseId:int}")]
    public async Task<ActionResult<CourseReportDto>> CourseReport(int courseId)
    {
        await _sessions.CloseExpiredAsync();

        var course = await _db.Courses
            .Where(c => c.Id == courseId)
            .Select(c => new { c.Id, c.Code, c.Name, c.DoctorId })
            .FirstOrDefaultAsync();
        if (course is null)
            return NotFound(new { error = "course_not_found" });

        // الدكتور يشوف تقارير مواده هو بس
        if (!User.IsInRole("Admin"))
        {
            var doctorId = await _actor.GetDoctorIdAsync(User);
            if (doctorId is null || doctorId != course.DoctorId)
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "not_your_course" });
        }

        var held = await _db.Sessions
            .Where(s => s.CourseId == courseId && s.Status == SessionStatus.Closed)
            .OrderBy(s => s.StartsAt)
            .Select(s => new ReportSessionDto(s.Id, s.StartsAt, s.EndsAt))
            .ToListAsync();
        var heldIds = held.Select(h => h.SessionId).ToList();

        var enrolled = await _db.Enrollments
            .Where(e => e.CourseId == courseId)
            .OrderBy(e => e.Student.Name)
            .Select(e => new { e.StudentId, e.Student.Code, e.Student.Name })
            .ToListAsync();

        var records = await _db.Attendances
            .Where(a => heldIds.Contains(a.SessionId))
            .Select(a => new { a.SessionId, a.StudentId, a.Method, a.RecordedAt })
            .ToListAsync();
        var byKey = records.ToDictionary(r => (r.StudentId, r.SessionId));

        var students = new List<ReportStudentDto>();
        foreach (var st in enrolled)
        {
            var cells = new List<ReportCellDto>();
            var attended = 0;
            foreach (var s in held)
            {
                if (byKey.TryGetValue((st.StudentId, s.SessionId), out var rec))
                {
                    attended++;
                    cells.Add(new ReportCellDto(s.SessionId, true, rec.Method, rec.RecordedAt));
                }
                else
                {
                    cells.Add(new ReportCellDto(s.SessionId, false, null, null));
                }
            }

            // BR-04: النسبة = الجلسات اللي حضرها ÷ الجلسات اللي اتعقدت فعلًا
            var percentage = held.Count == 0 ? 0 : Math.Round(attended * 100.0 / held.Count, 1);
            students.Add(new ReportStudentDto(st.StudentId, st.Code, st.Name, attended, held.Count, percentage, cells));
        }

        return Ok(new CourseReportDto(course.Id, course.Code, course.Name, held.Count, held, students));
    }
}

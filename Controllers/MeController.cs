using AttendanceApi.Data;
using AttendanceApi.Dtos;
using AttendanceApi.Models;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Controllers;

// واجهة الطالب (FR-35, FR-36): الطالب بيشوف بياناته هو بس، والـ id بييجي من الـ token مش من الـ URL
// فمفيش طريقة يطلب بيها بيانات حد تاني (BR-07: ومفيش أي endpoint هنا بيعدّل حاجة)
[ApiController]
[Authorize(Roles = "Student")]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ActorService _actor;
    private readonly SessionService _sessions;

    public MeController(AppDbContext db, ActorService actor, SessionService sessions)
    {
        _db = db;
        _actor = actor;
        _sessions = sessions;
    }

    // GET /api/me/attendance   : حضوره ونسبته في كل مادة
    [HttpGet("attendance")]
    public async Task<ActionResult<List<MyCourseAttendanceDto>>> MyAttendance()
    {
        var studentId = await _actor.GetStudentIdAsync(User);
        if (studentId is null) return Forbid();

        await _sessions.CloseExpiredAsync();

        var courses = await _db.Enrollments
            .Where(e => e.StudentId == studentId)
            .OrderBy(e => e.Course.Code)
            .Select(e => new { e.CourseId, e.Course.Code, e.Course.Name, DoctorName = e.Course.Doctor.Name })
            .ToListAsync();
        var courseIds = courses.Select(c => c.CourseId).ToList();

        var heldByCourse = await _db.Sessions
            .Where(s => s.Status == SessionStatus.Closed && courseIds.Contains(s.CourseId))
            .GroupBy(s => s.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count);

        var attendedByCourse = await _db.Attendances
            .Where(a => a.StudentId == studentId
                        && a.Session.Status == SessionStatus.Closed
                        && courseIds.Contains(a.Session.CourseId))
            .GroupBy(a => a.Session.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count);

        var result = courses.Select(c =>
        {
            var held = heldByCourse.GetValueOrDefault(c.CourseId);
            var attended = attendedByCourse.GetValueOrDefault(c.CourseId);
            var percentage = held == 0 ? 0 : Math.Round(attended * 100.0 / held, 1);
            return new MyCourseAttendanceDto(c.CourseId, c.Code, c.Name, c.DoctorName, held, attended, percentage);
        }).ToList();

        return Ok(result);
    }

    // GET /api/me/courses/7/sessions   : سجل جلساته في مادة (حاضر/غايب في كل جلسة)
    [HttpGet("courses/{courseId:int}/sessions")]
    public async Task<ActionResult<List<MySessionLogDto>>> MySessions(int courseId)
    {
        var studentId = await _actor.GetStudentIdAsync(User);
        if (studentId is null) return Forbid();

        await _sessions.CloseExpiredAsync();

        if (!await _db.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId))
            return NotFound(new { error = "not_enrolled_in_course" });

        var held = await _db.Sessions
            .Where(s => s.CourseId == courseId && s.Status == SessionStatus.Closed)
            .OrderByDescending(s => s.StartsAt)
            .Select(s => new { s.Id, s.StartsAt, s.EndsAt })
            .ToListAsync();
        var heldIds = held.Select(h => h.Id).ToList();

        var mine = await _db.Attendances
            .Where(a => a.StudentId == studentId && heldIds.Contains(a.SessionId))
            .Select(a => new { a.SessionId, a.Method, a.RecordedAt })
            .ToDictionaryAsync(a => a.SessionId);

        var log = held.Select(h => mine.TryGetValue(h.Id, out var rec)
                ? new MySessionLogDto(h.Id, h.StartsAt, h.EndsAt, true, rec.Method, rec.RecordedAt)
                : new MySessionLogDto(h.Id, h.StartsAt, h.EndsAt, false, null, null))
            .ToList();

        return Ok(log);
    }

    // GET /api/me/card   (FR-36): حالة كارته
    [HttpGet("card")]
    public async Task<ActionResult<MyCardsDto>> MyCard()
    {
        var studentId = await _actor.GetStudentIdAsync(User);
        if (studentId is null) return Forbid();

        var cards = await _db.Cards
            .Where(c => c.StudentId == studentId)
            .OrderByDescending(c => c.FirstSeenAt)
            .Select(c => new MyCardDto(c.Uid, c.Status))
            .ToListAsync();

        var status = cards.Any(c => c.Status == CardStatus.Active) ? "Active"
            : cards.Any(c => c.Status == CardStatus.Blocked) ? "Blocked"
            : "None";

        return Ok(new MyCardsDto(status, cards));
    }
}

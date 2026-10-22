using AttendanceApi.Data;
using AttendanceApi.Dtos;
using AttendanceApi.Models;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Controllers;

// جلسات الحضور (FR-14 لـ FR-17) + التسجيل اليدوي (FR-27 لـ FR-30) + العرض اللحظي (FR-31, FR-32)
// الكلاس مفتوح للدكتور والأدمن، والدكتور بيشوف ويدير جلسات مواده هو بس (BR-06)
[ApiController]
[Authorize(Roles = "Doctor,Admin")]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ActorService _actor;
    private readonly SessionService _sessions;
    private readonly LiveNotifier _notifier;
    private readonly IConfiguration _config;

    public SessionsController(
        AppDbContext db, ActorService actor, SessionService sessions, LiveNotifier notifier, IConfiguration config)
    {
        _db = db;
        _actor = actor;
        _sessions = sessions;
        _notifier = notifier;
        _config = config;
    }

    private bool IsAdmin => User.IsInRole("Admin");

    // بعد انتهاء الجلسة بالمدة دي (بالدقايق) التعديل بيقتصر على الأدمن (FR-30)
    private int GraceMinutes => _config.GetValue("Attendance:EditGraceMinutes", 15);

    private bool CanEdit(AttendanceSession s) =>
        IsAdmin || s.Status == SessionStatus.Open || DateTime.UtcNow <= s.EndsAt.AddMinutes(GraceMinutes);

    // بتحمّل الجلسة وتتأكد إنها بتاعة الدكتور (أو إن اللي بيطلب أدمن)
    private async Task<(AttendanceSession? Session, ActionResult? Error)> LoadOwnedAsync(int id)
    {
        var session = await _db.Sessions.Include(s => s.Course).FirstOrDefaultAsync(s => s.Id == id);
        if (session is null)
            return (null, NotFound(new { error = "session_not_found" }));

        if (!IsAdmin)
        {
            var doctorId = await _actor.GetDoctorIdAsync(User);
            if (doctorId is null || session.Course.DoctorId != doctorId)
                return (null, StatusCode(StatusCodes.Status403Forbidden, new { error = "not_your_session" }));
        }

        return (session, null);
    }

    // GET /api/sessions?courseId=&status=Open&page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<SessionDto>>> GetAll(
        int? courseId, SessionStatus? status, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        await _sessions.CloseExpiredAsync();

        var query = _db.Sessions.AsQueryable();
        if (!IsAdmin)
        {
            var doctorId = await _actor.GetDoctorIdAsync(User);
            if (doctorId is null) return Forbid();
            query = query.Where(s => s.Course.DoctorId == doctorId.Value);
        }
        if (courseId.HasValue)
            query = query.Where(s => s.CourseId == courseId.Value);
        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        var total = await query.CountAsync();
        var items = await _sessions
            .Project(query.OrderByDescending(s => s.StartsAt).Skip((page - 1) * pageSize).Take(pageSize))
            .ToListAsync();

        return Ok(new PagedResult<SessionDto>(items, total, page, pageSize));
    }

    // GET /api/sessions/12
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SessionDto>> GetById(int id)
    {
        await _sessions.CloseExpiredAsync();
        var (session, error) = await LoadOwnedAsync(id);
        if (error is not null) return error;

        var dto = await _sessions.Project(_db.Sessions.Where(s => s.Id == id)).FirstAsync();
        return Ok(dto);
    }

    // POST /api/sessions   body: { "courseId": 1, "deviceId": 1, "durationMinutes": 90 }   (FR-14, FR-15)
    [Authorize(Roles = "Doctor")]
    [HttpPost]
    public async Task<ActionResult<SessionDto>> Open(OpenSessionRequest req)
    {
        var doctorId = await _actor.GetDoctorIdAsync(User);
        if (doctorId is null) return Forbid();

        await _sessions.CloseExpiredAsync();

        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == req.CourseId);
        if (course is null)
            return NotFound(new { error = "course_not_found" });
        if (course.DoctorId != doctorId)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "not_your_course" });
        if (!course.IsActive)
            return BadRequest(new { error = "course_inactive" });

        var device = await _db.Devices.FirstOrDefaultAsync(d => d.Id == req.DeviceId);
        if (device is null || !device.IsActive)
            return BadRequest(new { error = "device_not_found_or_inactive" });

        // FR-15: جلسة مفتوحة واحدة بس على الجهاز (وفيه كمان unique index في الداتابيز بيضمن ده)
        if (await _db.Sessions.AnyAsync(s => s.DeviceId == device.Id && s.Status == SessionStatus.Open))
            return Conflict(new { error = "device_has_open_session" });

        var now = DateTime.UtcNow;
        var session = new AttendanceSession
        {
            CourseId = course.Id,
            DeviceId = device.Id,
            StartsAt = now,
            EndsAt = now.AddMinutes(req.DurationMinutes),
            Status = SessionStatus.Open
        };
        _db.Sessions.Add(session);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "device_has_open_session" });   // دكتورين فتحوا على نفس الجهاز مع بعض
        }

        var dto = await _sessions.Project(_db.Sessions.Where(s => s.Id == session.Id)).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = session.Id }, dto);
    }

    // POST /api/sessions/12/close   (FR-16): إغلاق يدوي
    [Authorize(Roles = "Doctor")]
    [HttpPost("{id:int}/close")]
    public async Task<IActionResult> Close(int id)
    {
        await _sessions.CloseExpiredAsync();
        var (session, error) = await LoadOwnedAsync(id);
        if (error is not null) return error;

        if (session!.Status == SessionStatus.Closed)
            return Conflict(new { error = "session_already_closed" });

        var now = DateTime.UtcNow;
        session.Status = SessionStatus.Closed;
        if (session.EndsAt > now)
            session.EndsAt = now;   // وقت النهاية الفعلي
        await _db.SaveChangesAsync();

        // لو دكتور تاني (أو الشاشة نفسها) فاتحة الجلسة تتحدّث
        await _notifier.SessionClosedAsync(session.Id);
        return NoContent();
    }

    // POST /api/sessions/12/extend   body: { "extraMinutes": 15 }   (FR-17)
    [Authorize(Roles = "Doctor")]
    [HttpPost("{id:int}/extend")]
    public async Task<ActionResult<SessionDto>> Extend(int id, ExtendSessionRequest req)
    {
        await _sessions.CloseExpiredAsync();   // الأول نقفل المنتهي، فالتمديد مايفتحش جلسة انتهت فعلًا
        var (session, error) = await LoadOwnedAsync(id);
        if (error is not null) return error;

        if (session!.Status != SessionStatus.Open)
            return Conflict(new { error = "session_closed" });

        session.EndsAt = session.EndsAt.AddMinutes(req.ExtraMinutes);
        await _db.SaveChangesAsync();

        var dto = await _sessions.Project(_db.Sessions.Where(s => s.Id == id)).FirstAsync();
        return Ok(dto);
    }

    // GET /api/sessions/12/live   (FR-31, FR-32): الحاضرين + اللي لسه ماحضروش + الأعداد
    [HttpGet("{id:int}/live")]
    public async Task<ActionResult<LiveSessionDto>> Live(int id)
    {
        await _sessions.CloseExpiredAsync();
        var (_, error) = await LoadOwnedAsync(id);
        if (error is not null) return error;

        var live = await _sessions.GetLiveAsync(id);
        return live is null ? NotFound() : Ok(live);
    }

    // POST /api/sessions/12/attendance/manual   body: { "studentId": 3 }   (FR-27, FR-28, FR-29)
    [HttpPost("{id:int}/attendance/manual")]
    public async Task<IActionResult> AddManual(int id, ManualAttendanceRequest req)
    {
        await _sessions.CloseExpiredAsync();
        var (session, error) = await LoadOwnedAsync(id);
        if (error is not null) return error;

        if (!CanEdit(session!))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "edit_window_closed_admin_only" });

        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == req.StudentId);
        if (student is null)
            return NotFound(new { error = "student_not_found" });
        if (!student.IsActive)
            return BadRequest(new { error = "student_inactive" });

        // FR-28: المسجلين في المادة بس
        var enrolled = await _db.Enrollments
            .AnyAsync(e => e.CourseId == session!.CourseId && e.StudentId == student.Id);
        if (!enrolled)
            return BadRequest(new { error = "student_not_enrolled" });

        if (await _db.Attendances.AnyAsync(a => a.SessionId == id && a.StudentId == student.Id))
            return Conflict(new { error = "already_recorded" });

        // FR-29: بنحفظ اسم الدكتور اللي سجّل (لو الأدمن هو اللي سجّل بيفضل فاضي وبيظهر "Admin")
        int? doctorId = IsAdmin ? null : await _actor.GetDoctorIdAsync(User);

        var att = new Attendance
        {
            SessionId = id,
            StudentId = student.Id,
            Method = AttendanceMethod.Manual,
            RecordedByDoctorId = doctorId,
            RecordedAt = DateTime.UtcNow
        };
        _db.Attendances.Add(att);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "already_recorded" });
        }

        var evt = await _sessions.NotifyRecordedAsync(session!, student, att);
        return Ok(evt);
    }

    // DELETE /api/sessions/12/attendance/3   (FR-30): إلغاء تسجيل يدوي غلط
    // الدكتور: يدوي بس، وفي وقت الجلسة أو فترة قصيرة بعدها. الأدمن: أي تسجيل وفي أي وقت
    [HttpDelete("{id:int}/attendance/{studentId:int}")]
    public async Task<IActionResult> Cancel(int id, int studentId)
    {
        await _sessions.CloseExpiredAsync();
        var (session, error) = await LoadOwnedAsync(id);
        if (error is not null) return error;

        var att = await _db.Attendances
            .Include(a => a.Student)
            .FirstOrDefaultAsync(a => a.SessionId == id && a.StudentId == studentId);
        if (att is null)
            return NotFound(new { error = "attendance_not_found" });

        if (!IsAdmin)
        {
            if (att.Method != AttendanceMethod.Manual)
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "only_manual_can_be_cancelled" });
            if (!CanEdit(session!))
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "edit_window_closed_admin_only" });
        }

        _db.Attendances.Remove(att);
        await _db.SaveChangesAsync();

        await _sessions.NotifyCancelledAsync(session!, att.Student, att);
        return NoContent();
    }
}

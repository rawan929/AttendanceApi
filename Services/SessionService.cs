using AttendanceApi.Data;
using AttendanceApi.Dtos;
using AttendanceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Services;

// شغل الجلسات المشترك: الإغلاق التلقائي، الأعداد، العرض اللحظي، وإرسال الأحداث
public class SessionService
{
    private readonly AppDbContext _db;
    private readonly LiveNotifier _notifier;

    public SessionService(AppDbContext db, LiveNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    // FR-16: بتقفل أي جلسة مفتوحة انتهى وقتها. بتتنده من الـ background service كل 30 ثانية،
    // وكمان قبل أي قراءة أو مسح عشان النتيجة تبقى صح حتى لو الـ timer لسه ماجاش
    public async Task<int> CloseExpiredAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expired = await _db.Sessions
            .Where(s => s.Status == SessionStatus.Open && s.EndsAt <= now)
            .ToListAsync(ct);
        if (expired.Count == 0) return 0;

        foreach (var s in expired)
            s.Status = SessionStatus.Closed;
        await _db.SaveChangesAsync(ct);

        foreach (var s in expired)
            await _notifier.SessionClosedAsync(s.Id);

        return expired.Count;
    }

    // شكل الجلسة اللي بيرجع للعميل (مع عدد الحاضرين)
    public IQueryable<SessionDto> Project(IQueryable<AttendanceSession> query) =>
        query.Select(s => new SessionDto(
            s.Id, s.CourseId, s.Course.Code, s.Course.Name,
            s.DeviceId, s.Device.Name,
            s.StartsAt, s.EndsAt, s.Status,
            _db.Attendances.Count(a => a.SessionId == s.Id)));

    // FR-32: عدد الحاضرين وعدد المسجلين في المادة اللي لسه ماحضروش
    public async Task<(int Present, int NotYet)> GetCountsAsync(int sessionId, int courseId)
    {
        var present = await _db.Attendances.CountAsync(a => a.SessionId == sessionId);
        var notYet = await _db.Enrollments.CountAsync(e =>
            e.CourseId == courseId &&
            !_db.Attendances.Any(a => a.SessionId == sessionId && a.StudentId == e.StudentId));
        return (present, notYet);
    }

    // FR-31: لقطة كاملة للجلسة (بتتحمّل أول ما الشاشة تفتح، وبعدها الأحداث بتحدّثها)
    public async Task<LiveSessionDto?> GetLiveAsync(int sessionId)
    {
        var session = await Project(_db.Sessions.Where(s => s.Id == sessionId)).FirstOrDefaultAsync();
        if (session is null) return null;

        var courseId = session.CourseId;

        var rows = await _db.Attendances
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.RecordedAt)
            .Select(a => new
            {
                a.StudentId,
                a.Student.Code,
                a.Student.Name,
                a.Method,
                a.RecordedAt,
                a.RecordedByDoctorId
            })
            .ToListAsync();

        var doctorIds = rows
            .Where(r => r.RecordedByDoctorId.HasValue)
            .Select(r => r.RecordedByDoctorId!.Value)
            .Distinct()
            .ToList();
        var doctorNames = doctorIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Doctors.Where(d => doctorIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.Name);

        var present = rows.Select(r => new PresentStudentDto(
            r.StudentId, r.Code, r.Name, r.Method, r.RecordedAt, RecordedByLabel(r.Method, r.RecordedByDoctorId, doctorNames)))
            .ToList();

        var notYet = await _db.Enrollments
            .Where(e => e.CourseId == courseId &&
                        !_db.Attendances.Any(a => a.SessionId == sessionId && a.StudentId == e.StudentId))
            .OrderBy(e => e.Student.Name)
            .Select(e => new NotYetStudentDto(e.StudentId, e.Student.Code, e.Student.Name))
            .ToListAsync();

        return new LiveSessionDto(session, present.Count, notYet.Count, present, notYet);
    }

    // FR-29: التسجيل اليدوي بيتعلّم باسم الدكتور اللي عمله (أو "Admin" لو الأدمن هو اللي سجّل)
    private static string? RecordedByLabel(AttendanceMethod method, int? doctorId, Dictionary<int, string> names)
    {
        if (method != AttendanceMethod.Manual) return null;
        if (doctorId.HasValue && names.TryGetValue(doctorId.Value, out var name)) return name;
        return "Admin";
    }

    // بتبني حدث التسجيل (بالأعداد الحالية) وتبعته للدكتور، وبترجّعه عشان يترد بيه في الـ API
    public async Task<AttendanceEventDto> NotifyRecordedAsync(AttendanceSession session, Student student, Attendance att)
    {
        var evt = await BuildEventAsync(session, student, att);
        await _notifier.RecordedAsync(evt);
        return evt;
    }

    public async Task NotifyCancelledAsync(AttendanceSession session, Student student, Attendance att)
    {
        var evt = await BuildEventAsync(session, student, att);
        await _notifier.CancelledAsync(evt);
    }

    private async Task<AttendanceEventDto> BuildEventAsync(AttendanceSession session, Student student, Attendance att)
    {
        var (present, notYet) = await GetCountsAsync(session.Id, session.CourseId);
        return new AttendanceEventDto(
            session.Id, student.Id, student.Code, student.Name, att.Method, att.RecordedAt, present, notYet);
    }
}

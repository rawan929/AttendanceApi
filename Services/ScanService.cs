using AttendanceApi.Data;
using AttendanceApi.Dtos;
using AttendanceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Services;

// قلب النظام: اللي بيحصل لما كارت يتمرّر على الجهاز (FR-19 لـ FR-25)
public class ScanService
{
    private readonly AppDbContext _db;
    private readonly SessionService _sessions;

    public ScanService(AppDbContext db, SessionService sessions)
    {
        _db = db;
        _sessions = sessions;
    }

    public async Task<ScanResponse> ProcessAsync(int deviceId, string rawUid)
    {
        var uid = CardUid.Normalize(rawUid);
        var now = DateTime.UtcNow;

        // آخر مرة الجهاز كلّم السيرفر (بيفيد الأدمن يعرف إن الجهاز شغال)
        await _db.Devices
            .Where(d => d.Id == deviceId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.LastSeenAt, (DateTime?)now));

        if (uid.Length == 0)
            return new ScanResponse("unknown_card", "كارت غير معروف");

        await _sessions.CloseExpiredAsync();

        // FR-18, FR-20: لازم تكون فيه جلسة مفتوحة على الجهاز ده في الوقت الحالي
        var session = await _db.Sessions
            .Include(s => s.Course)
            .Where(s => s.DeviceId == deviceId
                        && s.Status == SessionStatus.Open
                        && s.StartsAt <= now
                        && s.EndsAt > now)
            .FirstOrDefaultAsync();
        if (session is null)
            return new ScanResponse("no_session", "لا توجد جلسة");

        // FR-21: نتعرف على الكارت
        var card = await _db.Cards.Include(c => c.Student).FirstOrDefaultAsync(c => c.Uid == uid);

        // FR-25: كارت جديد = بنحفظه Pending مرة واحدة بس (الـ unique index على Uid بيضمن ده حتى لو جهازين مسحوه مع بعض)
        if (card is null)
        {
            _db.Cards.Add(new Card { Uid = uid, Status = CardStatus.Pending, FirstSeenAt = now });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();   // جهاز تاني حفظه في نفس اللحظة، وده مش خطأ
            }
            return new ScanResponse("unknown_card", "كارت غير معروف");
        }

        // كارت معروف بس لسه مش مربوط بطالب (اتحفظ قبل كده)، من غير ما نحفظه تاني
        if (card.Status == CardStatus.Pending || card.Student is null)
            return new ScanResponse("unknown_card", "كارت غير معروف");

        if (card.Status == CardStatus.Blocked)
            return new ScanResponse("card_blocked", "الكارت محجوب");

        var student = card.Student;
        if (!student.IsActive)
            return new ScanResponse("student_inactive", "الطالب موقوف", student.Name);

        // FR-21, BR-02: لازم الطالب يكون مسجل في مادة الجلسة
        var enrolled = await _db.Enrollments
            .AnyAsync(e => e.CourseId == session.CourseId && e.StudentId == student.Id);
        if (!enrolled)
            return new ScanResponse("not_enrolled", "غير مسجل في المادة", student.Name, session.Course.Name);

        // FR-24, BR-01: مرة واحدة في الجلسة
        var already = await _db.Attendances
            .AnyAsync(a => a.SessionId == session.Id && a.StudentId == student.Id);
        if (already)
            return new ScanResponse("already_recorded", "تم تسجيلك مسبقاً", student.Name, session.Course.Name);

        // FR-22: نحفظ الحضور بوقته الفعلي
        var att = new Attendance
        {
            SessionId = session.Id,
            StudentId = student.Id,
            DeviceId = deviceId,
            Method = AttendanceMethod.Card,
            RecordedAt = now
        };
        _db.Attendances.Add(att);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // لو مسحتين وصلوا في نفس اللحظة، القيد في الداتابيز (Session+Student) هو اللي بيمنع التكرار
            _db.Entry(att).State = EntityState.Detached;
            var exists = await _db.Attendances
                .AnyAsync(a => a.SessionId == session.Id && a.StudentId == student.Id);
            if (exists)
                return new ScanResponse("already_recorded", "تم تسجيلك مسبقاً", student.Name, session.Course.Name);
            throw;
        }

        // FR-31: نبلّغ شاشة الدكتور فورًا
        await _sessions.NotifyRecordedAsync(session, student, att);

        return new ScanResponse("recorded", "تم التسجيل", student.Name, session.Course.Name, now);
    }
}

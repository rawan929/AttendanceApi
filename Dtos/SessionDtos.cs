using System.ComponentModel.DataAnnotations;
using AttendanceApi.Models;

namespace AttendanceApi.Dtos;

public record SessionDto(
    int Id, int CourseId, string CourseCode, string CourseName,
    int DeviceId, string DeviceName,
    DateTime StartsAt, DateTime EndsAt, SessionStatus Status, int PresentCount);

// FR-14: الدكتور بيفتح الجلسة دلوقتي ولمدة معينة، فالبداية = وقت السيرفر والنهاية = البداية + المدة
public class OpenSessionRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [Range(1, int.MaxValue)]
    public int DeviceId { get; set; }

    [Range(1, 480)]
    public int DurationMinutes { get; set; } = 90;
}

// FR-17
public class ExtendSessionRequest
{
    [Range(1, 240)]
    public int ExtraMinutes { get; set; }
}

// FR-27
public class ManualAttendanceRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }
}

// ===== العرض اللحظي (FR-31, FR-32) =====

public record PresentStudentDto(
    int StudentId, string Code, string Name,
    AttendanceMethod Method, DateTime RecordedAt, string? RecordedBy);

public record NotYetStudentDto(int StudentId, string Code, string Name);

public record LiveSessionDto(
    SessionDto Session, int PresentCount, int NotYetCount,
    List<PresentStudentDto> Present, List<NotYetStudentDto> NotYet);

// ده اللي بيتبعت لحظيًا للدكتور عن طريق SignalR مع كل تسجيل أو إلغاء
public record AttendanceEventDto(
    int SessionId, int StudentId, string StudentCode, string StudentName,
    AttendanceMethod Method, DateTime RecordedAt, int PresentCount, int NotYetCount);

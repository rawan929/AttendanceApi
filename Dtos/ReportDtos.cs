using AttendanceApi.Models;

namespace AttendanceApi.Dtos;

// ===== تقرير المادة (FR-33) =====

public record ReportSessionDto(int SessionId, DateTime StartsAt, DateTime EndsAt);

public record ReportCellDto(int SessionId, bool Present, AttendanceMethod? Method, DateTime? RecordedAt);

public record ReportStudentDto(
    int StudentId, string Code, string Name,
    int Attended, int Held, double Percentage, List<ReportCellDto> Cells);

public record CourseReportDto(
    int CourseId, string CourseCode, string CourseName, int HeldSessions,
    List<ReportSessionDto> Sessions, List<ReportStudentDto> Students);

// ===== واجهة الطالب (FR-35, FR-36) =====

public record MyCourseAttendanceDto(
    int CourseId, string Code, string Name, string DoctorName,
    int Held, int Attended, double Percentage);

public record MySessionLogDto(
    int SessionId, DateTime StartsAt, DateTime EndsAt,
    bool Present, AttendanceMethod? Method, DateTime? RecordedAt);

public record MyCardDto(string Uid, CardStatus Status);

// Status الإجمالي: Active لو عنده كارت مفعّل، وإلا Blocked لو عنده كارت محجوب، وإلا None
public record MyCardsDto(string Status, List<MyCardDto> Cards);

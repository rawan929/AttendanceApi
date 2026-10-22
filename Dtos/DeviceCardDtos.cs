using System.ComponentModel.DataAnnotations;
using AttendanceApi.Models;

namespace AttendanceApi.Dtos;

// ===== الأجهزة (FR-08) =====

public record DeviceDto(int Id, string Name, string Location, bool IsActive, DateTime? LastSeenAt);

// الـ ApiKey بيظهر في الرد ده بس (مرة واحدة)، وبعدها مبنقدرش نرجعه لأننا مخزنين الـ hash بس
public record DeviceCreatedDto(int Id, string Name, string Location, bool IsActive, string ApiKey);

public class CreateDeviceRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(150)]
    public string Location { get; set; } = "";
}

public class UpdateDeviceRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [MaxLength(150)]
    public string Location { get; set; } = "";
}

// ===== الكروت (FR-09, FR-25, FR-26) =====

public record CardDto(
    int Id, string Uid, CardStatus Status,
    int? StudentId, string? StudentCode, string? StudentName, DateTime FirstSeenAt);

public class CreateCardRequest
{
    [Required, MaxLength(100)]
    public string Uid { get; set; } = "";

    // لو اتبعت الكارت بيتربط بالطالب فورًا ويبقى Active، لو لأ بيبقى Pending
    public int? StudentId { get; set; }
}

public class LinkCardRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }
}

public class ReplaceCardRequest
{
    [Required, MaxLength(100)]
    public string NewUid { get; set; } = "";
}

// ===== مسح الكارت من الجهاز (FR-19 لـ FR-24) =====

public class ScanRequest
{
    [Required, MaxLength(100)]
    public string Uid { get; set; } = "";
}

// Result بتبقى واحدة من:
// recorded | already_recorded | not_enrolled | unknown_card | no_session | card_blocked | student_inactive
public record ScanResponse(
    string Result, string Message,
    string? StudentName = null, string? CourseName = null, DateTime? RecordedAt = null);

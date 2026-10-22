using System.ComponentModel.DataAnnotations;

namespace AttendanceApi.Dtos;

// ===== الردود (اللي بيرجع من الـ API) =====

public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);

public record StudentDto(int Id, string Code, string Name, string? Email, bool IsActive);

public record DoctorDto(int Id, string Name, string? Email, bool IsActive);

public record CourseDto(int Id, string Code, string Name, int DoctorId, string DoctorName, bool IsActive);

public record EnrolledStudentDto(int Id, string Code, string Name, bool IsActive);

public record EnrollResult(int Added, int AlreadyEnrolled, List<int> NotFoundOrInactive);

// ===== الطلبات (اللي بيدخل للـ API) =====

public class CreateStudentRequest
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = "";

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = "";

    [Required, MinLength(8)]
    public string Password { get; set; } = "";
}

public class UpdateStudentRequest
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = "";

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";
}

public class CreateDoctorRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = "";

    [Required, MinLength(8)]
    public string Password { get; set; } = "";
}

public class UpdateDoctorRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = "";
}

public class CreateCourseRequest
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = "";

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int DoctorId { get; set; }
}

public class UpdateCourseRequest
{
    [Required, MaxLength(50)]
    public string Code { get; set; } = "";

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    [Range(1, int.MaxValue)]
    public int DoctorId { get; set; }
}

public class EnrollStudentsRequest
{
    [Required, MinLength(1)]
    public List<int> StudentIds { get; set; } = new();
}

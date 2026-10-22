namespace AttendanceApi.Models;

public class Student
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string? UserId { get; set; }          // هنربطه بحساب الدخول في الخطوة 2

    public List<Card> Cards { get; set; } = new();
    public List<Enrollment> Enrollments { get; set; } = new();
}

public class Doctor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string? UserId { get; set; }

    public List<Course> Courses { get; set; } = new();
}

public class Course
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public List<Enrollment> Enrollments { get; set; } = new();
}

public class Enrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}

public enum CardStatus { Pending, Active, Blocked }

public class Card
{
    public int Id { get; set; }
    public string Uid { get; set; } = "";
    public int? StudentId { get; set; }           // null = كارت معلق
    public Student? Student { get; set; }
    public CardStatus Status { get; set; } = CardStatus.Pending;
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
}

public class Device
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Location { get; set; } = "";
    public string ApiKeyHash { get; set; } = "";
    public DateTime? LastSeenAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum SessionStatus { Open, Closed }

public class AttendanceSession
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int DeviceId { get; set; }
    public Device Device { get; set; } = null!;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Open;
}

public enum AttendanceMethod { Card, Manual }

public class Attendance
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public AttendanceSession Session { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int? DeviceId { get; set; }            // فاضي في التسجيل اليدوي
    public int? RecordedByDoctorId { get; set; }  // فاضي في تسجيل الكارت
    public AttendanceMethod Method { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

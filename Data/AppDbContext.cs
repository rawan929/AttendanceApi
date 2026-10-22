using AttendanceApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Data;

// IdentityDbContext بيضيف جداول المستخدمين والأدوار (AspNetUsers, AspNetRoles...) جنب جداولنا
public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<AttendanceSession> Sessions => Set<AttendanceSession>();
    public DbSet<Attendance> Attendances => Set<Attendance>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);   // لازم تتنده الأول عشان جداول Identity تتعمل

        // قيود التفرد (BR-01, FR-05, FR-07, FR-12)
        b.Entity<Student>().HasIndex(s => s.Code).IsUnique();
        b.Entity<Course>().HasIndex(c => c.Code).IsUnique();
        b.Entity<Card>().HasIndex(c => c.Uid).IsUnique();
        b.Entity<Enrollment>().HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
        b.Entity<Attendance>().HasIndex(a => new { a.SessionId, a.StudentId }).IsUnique();

        // FR-08: مفتاح الجهاز (hash) فريد ويتدوّر عليه في كل مسح، فمحتاج index
        b.Entity<Device>().Property(d => d.ApiKeyHash).HasMaxLength(64);
        b.Entity<Device>().HasIndex(d => d.ApiKeyHash).IsUnique();

        // FR-15: جلسة مفتوحة واحدة بس على الجهاز. القيد في الداتابيز نفسها (filtered unique index)
        // فحتى لو دكتورين فتحوا في نفس اللحظة واحد بس هيعدّي
        b.Entity<AttendanceSession>().HasIndex(s => s.DeviceId).IsUnique().HasFilter("[Status] = 'Open'");

        // نخزن الـ enums كنص عشان الداتابيز تبقى مقروءة
        b.Entity<Card>().Property(c => c.Status).HasConversion<string>();
        b.Entity<AttendanceSession>().Property(s => s.Status).HasConversion<string>();
        b.Entity<Attendance>().Property(a => a.Method).HasConversion<string>();

        // مفيش حذف متتالي في جداولنا (بنوقف بدل ما نمسح، وSQL Server بيرفض multiple cascade paths)
        // الفلتر بيمنع التأثير على جداول Identity
        var ourNamespace = typeof(Student).Namespace;
        foreach (var fk in b.Model.GetEntityTypes()
                     .Where(t => t.ClrType.Namespace == ourNamespace)
                     .SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}

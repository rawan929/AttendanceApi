using System.Security.Claims;
using AttendanceApi.Data;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Services;

// بتحوّل المستخدم اللي جاي من الـ token (sub) لرقم الدكتور أو الطالب في جداولنا
public class ActorService
{
    private readonly AppDbContext _db;
    public ActorService(AppDbContext db) => _db = db;

    public async Task<int?> GetDoctorIdAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (userId is null) return null;

        return await _db.Doctors
            .Where(d => d.UserId == userId && d.IsActive)
            .Select(d => (int?)d.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetStudentIdAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirst("sub")?.Value;
        if (userId is null) return null;

        return await _db.Students
            .Where(s => s.UserId == userId && s.IsActive)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
    }
}

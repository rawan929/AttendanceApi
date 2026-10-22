using AttendanceApi.Data;
using AttendanceApi.Dtos;
using AttendanceApi.Models;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AccountService _accounts;

    public DoctorsController(AppDbContext db, AccountService accounts)
    {
        _db = db;
        _accounts = accounts;
    }

    private static DoctorDto ToDto(Doctor d, string? email) =>
        new(d.Id, d.Name, email, d.IsActive);

    // GET /api/doctors?search=&includeInactive=false&page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<DoctorDto>>> GetAll(
        string? search, bool includeInactive = false, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Doctors.AsQueryable();
        if (!includeInactive)
            query = query.Where(d => d.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d => d.Name.Contains(term));
        }

        var total = await query.CountAsync();
        var doctors = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var emails = await _accounts.GetEmailsAsync(doctors.Select(d => d.UserId));
        var items = doctors
            .Select(d => ToDto(d, d.UserId != null && emails.TryGetValue(d.UserId, out var e) ? e : null))
            .ToList();

        return Ok(new PagedResult<DoctorDto>(items, total, page, pageSize));
    }

    // GET /api/doctors/3
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DoctorDto>> GetById(int id)
    {
        var doctor = await _db.Doctors.FindAsync(id);
        if (doctor is null) return NotFound();

        var emails = await _accounts.GetEmailsAsync(new[] { doctor.UserId });
        var email = doctor.UserId != null && emails.TryGetValue(doctor.UserId, out var e) ? e : null;
        return Ok(ToDto(doctor, email));
    }

    // POST /api/doctors   (FR-06)
    [HttpPost]
    public async Task<ActionResult<DoctorDto>> Create(CreateDoctorRequest req)
    {
        var email = req.Email.Trim();
        if (await _accounts.EmailExistsAsync(email))
            return Conflict(new { error = "email_exists" });

        await using var tx = await _db.Database.BeginTransactionAsync();

        var (user, errors) = await _accounts.CreateAsync(email, req.Password, "Doctor");
        if (user is null)
            return BadRequest(new { errors });

        var doctor = new Doctor { Name = req.Name.Trim(), UserId = user.Id };
        _db.Doctors.Add(doctor);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return CreatedAtAction(nameof(GetById), new { id = doctor.Id }, ToDto(doctor, user.Email));
    }

    // PUT /api/doctors/3
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateDoctorRequest req)
    {
        var doctor = await _db.Doctors.FindAsync(id);
        if (doctor is null) return NotFound();

        doctor.Name = req.Name.Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/doctors/3/deactivate
    // قاعدة: ماينفعش نوقف دكتور عنده مواد نشطة، لأن كل مادة لازم يبقى لها دكتور
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var doctor = await _db.Doctors.FindAsync(id);
        if (doctor is null) return NotFound();

        if (await _db.Courses.AnyAsync(c => c.DoctorId == id && c.IsActive))
            return Conflict(new { error = "doctor_has_active_courses" });

        doctor.IsActive = false;
        await _db.SaveChangesAsync();
        await _accounts.SetActiveAsync(doctor.UserId, false);
        return NoContent();
    }

    // POST /api/doctors/3/activate
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var doctor = await _db.Doctors.FindAsync(id);
        if (doctor is null) return NotFound();

        doctor.IsActive = true;
        await _db.SaveChangesAsync();
        await _accounts.SetActiveAsync(doctor.UserId, true);
        return NoContent();
    }

    // GET /api/doctors/3/courses   (FR-13): مواد الدكتور
    [HttpGet("{id:int}/courses")]
    public async Task<ActionResult<List<CourseDto>>> GetCourses(int id)
    {
        if (!await _db.Doctors.AnyAsync(d => d.Id == id))
            return NotFound();

        var courses = await _db.Courses
            .Where(c => c.DoctorId == id)
            .OrderBy(c => c.Code)
            .Select(c => new CourseDto(c.Id, c.Code, c.Name, c.DoctorId, c.Doctor.Name, c.IsActive))
            .ToListAsync();

        return Ok(courses);
    }
}

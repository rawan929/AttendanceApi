using AttendanceApi.Data;
using AttendanceApi.Dtos;
using AttendanceApi.Models;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Doctor")]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AccountService _accounts;

    public StudentsController(AppDbContext db, AccountService accounts)
    {
        _db = db;
        _accounts = accounts;
    }

    private static StudentDto ToDto(Student s, string? email) =>
        new(s.Id, s.Code, s.Name, email, s.IsActive);

    // GET /api/students?search=&includeInactive=false&page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<StudentDto>>> GetAll(
        string? search, bool includeInactive = false, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Students.AsQueryable();
        if (!includeInactive)
            query = query.Where(s => s.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Name.Contains(term) || s.Code.Contains(term));
        }

        var total = await query.CountAsync();
        var students = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var emails = await _accounts.GetEmailsAsync(students.Select(s => s.UserId));
        var items = students
            .Select(s => ToDto(s, s.UserId != null && emails.TryGetValue(s.UserId, out var e) ? e : null))
            .ToList();

        return Ok(new PagedResult<StudentDto>(items, total, page, pageSize));
    }

    // GET /api/students/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<StudentDto>> GetById(int id)
    {
        var student = await _db.Students.FindAsync(id);
        if (student is null) return NotFound();

        var emails = await _accounts.GetEmailsAsync(new[] { student.UserId });
        var email = student.UserId != null && emails.TryGetValue(student.UserId, out var e) ? e : null;
        return Ok(ToDto(student, email));
    }

    // POST /api/students   (FR-05): بيعمل حساب الدخول + صف الطالب مع بعض في transaction واحدة
    [HttpPost]
    public async Task<ActionResult<StudentDto>> Create(CreateStudentRequest req)
    {
        var code = req.Code.Trim();
        var email = req.Email.Trim();

        if (await _db.Students.AnyAsync(s => s.Code == code))
            return Conflict(new { error = "student_code_exists" });
        if (await _accounts.EmailExistsAsync(email))
            return Conflict(new { error = "email_exists" });

        // لو أي حاجة فشلت قبل الـ Commit، كل اللي اتعمل بيترجع (حساب + طالب)
        await using var tx = await _db.Database.BeginTransactionAsync();

        var (user, errors) = await _accounts.CreateAsync(email, req.Password, "Student");
        if (user is null)
            return BadRequest(new { errors });

        var student = new Student { Code = code, Name = req.Name.Trim(), UserId = user.Id };
        _db.Students.Add(student);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return CreatedAtAction(nameof(GetById), new { id = student.Id }, ToDto(student, user.Email));
    }

    // PUT /api/students/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateStudentRequest req)
    {
        var student = await _db.Students.FindAsync(id);
        if (student is null) return NotFound();

        var code = req.Code.Trim();
        if (await _db.Students.AnyAsync(s => s.Code == code && s.Id != id))
            return Conflict(new { error = "student_code_exists" });

        student.Code = code;
        student.Name = req.Name.Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/students/5/deactivate   (FR-10): إيقاف بدل الحذف، وتاريخ الحضور يفضل
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var student = await _db.Students.FindAsync(id);
        if (student is null) return NotFound();

        student.IsActive = false;
        await _db.SaveChangesAsync();
        await _accounts.SetActiveAsync(student.UserId, false);   // يمنعه من الدخول
        return NoContent();
    }

    // POST /api/students/5/activate
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var student = await _db.Students.FindAsync(id);
        if (student is null) return NotFound();

        student.IsActive = true;
        await _db.SaveChangesAsync();
        await _accounts.SetActiveAsync(student.UserId, true);
        return NoContent();
    }

    // GET /api/students/5/courses   (FR-13): مواد الطالب
    [HttpGet("{id:int}/courses")]
    public async Task<ActionResult<List<CourseDto>>> GetCourses(int id)
    {
        if (!await _db.Students.AnyAsync(s => s.Id == id))
            return NotFound();

        var courses = await _db.Enrollments
            .Where(e => e.StudentId == id)
            .OrderBy(e => e.Course.Code)
            .Select(e => new CourseDto(
                e.Course.Id, e.Course.Code, e.Course.Name,
                e.Course.DoctorId, e.Course.Doctor.Name, e.Course.IsActive))
            .ToListAsync();

        return Ok(courses);
    }
}

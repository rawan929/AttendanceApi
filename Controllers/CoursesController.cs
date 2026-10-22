using AttendanceApi.Data;
using AttendanceApi.Dtos;
using AttendanceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CoursesController(AppDbContext db) => _db = db;

    // GET /api/courses?search=&doctorId=&includeInactive=false&page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PagedResult<CourseDto>>> GetAll(
        string? search, int? doctorId, bool includeInactive = false, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Courses.AsQueryable();
        if (!includeInactive)
            query = query.Where(c => c.IsActive);
        if (doctorId.HasValue)
            query = query.Where(c => c.DoctorId == doctorId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Name.Contains(term) || c.Code.Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseDto(c.Id, c.Code, c.Name, c.DoctorId, c.Doctor.Name, c.IsActive))
            .ToListAsync();

        return Ok(new PagedResult<CourseDto>(items, total, page, pageSize));
    }

    // GET /api/courses/7
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CourseDto>> GetById(int id)
    {
        var course = await _db.Courses
            .Where(c => c.Id == id)
            .Select(c => new CourseDto(c.Id, c.Code, c.Name, c.DoctorId, c.Doctor.Name, c.IsActive))
            .FirstOrDefaultAsync();

        return course is null ? NotFound() : Ok(course);
    }

    // POST /api/courses   (FR-07)
    [HttpPost]
    public async Task<ActionResult<CourseDto>> Create(CreateCourseRequest req)
    {
        var code = req.Code.Trim();

        if (await _db.Courses.AnyAsync(c => c.Code == code))
            return Conflict(new { error = "course_code_exists" });

        var doctor = await _db.Doctors.FirstOrDefaultAsync(d => d.Id == req.DoctorId && d.IsActive);
        if (doctor is null)
            return BadRequest(new { error = "doctor_not_found_or_inactive" });

        var course = new Course { Code = code, Name = req.Name.Trim(), DoctorId = doctor.Id };
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        var dto = new CourseDto(course.Id, course.Code, course.Name, doctor.Id, doctor.Name, course.IsActive);
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, dto);
    }

    // PUT /api/courses/7   (تعديل المادة أو تغيير الدكتور)
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCourseRequest req)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        var code = req.Code.Trim();
        if (await _db.Courses.AnyAsync(c => c.Code == code && c.Id != id))
            return Conflict(new { error = "course_code_exists" });

        if (!await _db.Doctors.AnyAsync(d => d.Id == req.DoctorId && d.IsActive))
            return BadRequest(new { error = "doctor_not_found_or_inactive" });

        course.Code = code;
        course.Name = req.Name.Trim();
        course.DoctorId = req.DoctorId;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/courses/7/deactivate   (FR-10): إيقاف بدل الحذف
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        course.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/courses/7/activate
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null) return NotFound();

        course.IsActive = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

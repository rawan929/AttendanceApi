using AttendanceApi.Data;
using AttendanceApi.Dtos;
using AttendanceApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Controllers;

// تسجيل الطلاب في المواد (FR-11 لـ FR-13)
// العنوان بيوصف الشكل: طلاب المادة رقم كذا
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/courses/{courseId:int}/students")]
public class EnrollmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public EnrollmentsController(AppDbContext db) => _db = db;

    // GET /api/courses/7/students   : قايمة طلاب المادة
    [HttpGet]
    public async Task<ActionResult<List<EnrolledStudentDto>>> GetStudents(int courseId)
    {
        if (!await _db.Courses.AnyAsync(c => c.Id == courseId))
            return NotFound(new { error = "course_not_found" });

        var students = await _db.Enrollments
            .Where(e => e.CourseId == courseId)
            .OrderBy(e => e.Student.Name)
            .Select(e => new EnrolledStudentDto(e.Student.Id, e.Student.Code, e.Student.Name, e.Student.IsActive))
            .ToListAsync();

        return Ok(students);
    }

    // POST /api/courses/7/students   body: { "studentIds": [1, 2, 3] }
    // بيضيف طالب أو أكتر، وبيتخطى اللي مسجل قبل كده (FR-11, FR-12)
    [HttpPost]
    public async Task<ActionResult<EnrollResult>> Enroll(int courseId, EnrollStudentsRequest req)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId && c.IsActive);
        if (course is null)
            return NotFound(new { error = "course_not_found_or_inactive" });

        var requested = req.StudentIds.Distinct().ToList();

        // الطلاب الموجودين والنشطين فعلاً
        var validIds = await _db.Students
            .Where(s => requested.Contains(s.Id) && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync();

        // المسجلين في المادة قبل كده
        var alreadyIds = await _db.Enrollments
            .Where(e => e.CourseId == courseId && requested.Contains(e.StudentId))
            .Select(e => e.StudentId)
            .ToListAsync();

        var toAdd = validIds.Except(alreadyIds).ToList();
        _db.Enrollments.AddRange(toAdd.Select(sid => new Enrollment { StudentId = sid, CourseId = courseId }));
        await _db.SaveChangesAsync();

        var rejected = requested.Except(validIds).ToList();
        return Ok(new EnrollResult(toAdd.Count, alreadyIds.Count, rejected));
    }

    // DELETE /api/courses/7/students/3   : شيل طالب من المادة
    // سجلات حضوره القديمة بتفضل زي ما هي (مابنمسحش تاريخه)
    [HttpDelete("{studentId:int}")]
    public async Task<IActionResult> Remove(int courseId, int studentId)
    {
        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == studentId);
        if (enrollment is null) return NotFound();

        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

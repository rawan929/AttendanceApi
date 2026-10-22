using AttendanceApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Controllers;

// Controller تجريبي: بيتأكد إن الـ API شغال وإنه بيوصل للداتابيز.
// بيتشال أو يتحول لحاجة أبسط لما نخلص التجارب.
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;
    public HealthController(AppDbContext db) => _db = db;

    // GET /api/health
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var canConnect = await _db.Database.CanConnectAsync();
        if (!canConnect)
            return StatusCode(503, new { api = "ok", database = "unreachable" });

        return Ok(new
        {
            api = "ok",
            database = "ok",
            students = await _db.Students.CountAsync(),
            courses = await _db.Courses.CountAsync(),
            cards = await _db.Cards.CountAsync()
        });
    }
}

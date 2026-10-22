using System.Linq.Expressions;
using AttendanceApi.Data;
using AttendanceApi.Dtos;
using AttendanceApi.Models;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Controllers;

// الكروت: ربط/حجب/استبدال (FR-09) والكروت المعلقة (FR-25, FR-26)
// الكلاس مفتوح للأدمن والدكتور، والأكشنز الخاصة بالأدمن بس عليها [Authorize(Roles = "Admin")]
[ApiController]
[Authorize(Roles = "Admin,Doctor")]
[Route("api/[controller]")]
public class CardsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ActorService _actor;

    public CardsController(AppDbContext db, ActorService actor)
    {
        _db = db;
        _actor = actor;
    }

    private bool IsAdmin => User.IsInRole("Admin");

    private static readonly Expression<Func<Card, CardDto>> ToDto = c => new CardDto(
        c.Id, c.Uid, c.Status, c.StudentId, c.Student!.Code, c.Student!.Name, c.FirstSeenAt);

    private Task<CardDto> LoadDtoAsync(int id) =>
        _db.Cards.Where(c => c.Id == id).Select(ToDto).FirstAsync();

    // GET /api/cards?status=Active&studentId=&search=&page=1&pageSize=20   (أدمن بس)
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<PagedResult<CardDto>>> GetAll(
        CardStatus? status, int? studentId, string? search, int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Cards.AsQueryable();
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);
        if (studentId.HasValue)
            query = query.Where(c => c.StudentId == studentId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = CardUid.Normalize(search);
            query = query.Where(c => c.Uid.Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.FirstSeenAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync();

        return Ok(new PagedResult<CardDto>(items, total, page, pageSize));
    }

    // GET /api/cards/pending   (FR-25, FR-26): الكروت المجهولة اللي اتمسحت ولسه محدش ربطها
    [HttpGet("pending")]
    public async Task<ActionResult<PagedResult<CardDto>>> GetPending(int page = 1, int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Cards.Where(c => c.Status == CardStatus.Pending && c.StudentId == null);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.FirstSeenAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync();

        return Ok(new PagedResult<CardDto>(items, total, page, pageSize));
    }

    // POST /api/cards   body: { "uid": "04A32B1C", "studentId": 1 }   (أدمن: تسجيل كارت يدويًا)
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CardDto>> Create(CreateCardRequest req)
    {
        var uid = CardUid.Normalize(req.Uid);
        if (uid.Length == 0)
            return BadRequest(new { error = "invalid_uid" });

        if (await _db.Cards.AnyAsync(c => c.Uid == uid))
            return Conflict(new { error = "card_exists" });

        var card = new Card { Uid = uid, Status = CardStatus.Pending };

        if (req.StudentId.HasValue)
        {
            var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == req.StudentId.Value);
            if (student is null) return NotFound(new { error = "student_not_found" });
            if (!student.IsActive) return BadRequest(new { error = "student_inactive" });
            if (await HasActiveCardAsync(student.Id, exceptCardId: null))
                return Conflict(new { error = "student_already_has_active_card" });

            card.StudentId = student.Id;
            card.Status = CardStatus.Active;
        }

        _db.Cards.Add(card);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "card_exists" });
        }

        return Ok(await LoadDtoAsync(card.Id));
    }

    // POST /api/cards/5/link   body: { "studentId": 1 }   (FR-09, FR-26): ربط كارت معلق بطالب
    [HttpPost("{id:int}/link")]
    public async Task<ActionResult<CardDto>> Link(int id, LinkCardRequest req)
    {
        var card = await _db.Cards.FindAsync(id);
        if (card is null) return NotFound(new { error = "card_not_found" });

        if (card.Status != CardStatus.Pending || card.StudentId != null)
            return Conflict(new { error = "card_not_pending" });

        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == req.StudentId);
        if (student is null) return NotFound(new { error = "student_not_found" });
        if (!student.IsActive) return BadRequest(new { error = "student_inactive" });

        // الدكتور يربط كروت طلاب مادة من موادّه بس (الأدمن مالوش قيد)
        if (!IsAdmin)
        {
            var doctorId = await _actor.GetDoctorIdAsync(User);
            if (doctorId is null) return Forbid();

            var inMyCourses = await _db.Enrollments
                .AnyAsync(e => e.StudentId == student.Id && e.Course.DoctorId == doctorId);
            if (!inMyCourses)
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "student_not_in_your_courses" });
        }

        if (await HasActiveCardAsync(student.Id, exceptCardId: null))
            return Conflict(new { error = "student_already_has_active_card" });

        card.StudentId = student.Id;
        card.Status = CardStatus.Active;
        await _db.SaveChangesAsync();

        return Ok(await LoadDtoAsync(card.Id));
    }

    // POST /api/cards/5/block   (FR-09): حجب كارت (ضاع مثلًا)
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/block")]
    public async Task<IActionResult> Block(int id)
    {
        var card = await _db.Cards.FindAsync(id);
        if (card is null) return NotFound();

        card.Status = CardStatus.Blocked;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/cards/5/activate   : فك الحجب (لازم يكون مربوط بطالب)
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var card = await _db.Cards.FindAsync(id);
        if (card is null) return NotFound();

        if (card.StudentId is null)
            return Conflict(new { error = "card_not_linked" });
        if (await HasActiveCardAsync(card.StudentId.Value, exceptCardId: card.Id))
            return Conflict(new { error = "student_already_has_active_card" });

        card.Status = CardStatus.Active;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/cards/5/replace   body: { "newUid": "AABBCCDD" }   (FR-09): استبدال الكارت
    // القديم بيتحجب والجديد بيتربط بنفس الطالب. الجديد يا إما كارت جديد خالص يا إما كارت معلق
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/replace")]
    public async Task<ActionResult<CardDto>> Replace(int id, ReplaceCardRequest req)
    {
        var old = await _db.Cards.FindAsync(id);
        if (old is null) return NotFound(new { error = "card_not_found" });
        if (old.StudentId is null) return Conflict(new { error = "card_not_linked" });

        var newUid = CardUid.Normalize(req.NewUid);
        if (newUid.Length == 0) return BadRequest(new { error = "invalid_uid" });
        if (newUid == old.Uid) return BadRequest(new { error = "same_card" });

        var studentId = old.StudentId.Value;
        if (await HasActiveCardAsync(studentId, exceptCardId: old.Id))
            return Conflict(new { error = "student_already_has_active_card" });

        var target = await _db.Cards.FirstOrDefaultAsync(c => c.Uid == newUid);
        if (target is not null && (target.Status != CardStatus.Pending || target.StudentId != null))
            return Conflict(new { error = "new_card_already_in_use" });

        old.Status = CardStatus.Blocked;
        if (target is null)
        {
            target = new Card { Uid = newUid };
            _db.Cards.Add(target);
        }
        target.StudentId = studentId;
        target.Status = CardStatus.Active;

        try
        {
            await _db.SaveChangesAsync();   // حفظ واحد = يا الاتنين يا ولا واحد
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "new_card_already_in_use" });
        }

        return Ok(await LoadDtoAsync(target.Id));
    }

    private Task<bool> HasActiveCardAsync(int studentId, int? exceptCardId) =>
        _db.Cards.AnyAsync(c =>
            c.StudentId == studentId &&
            c.Status == CardStatus.Active &&
            (exceptCardId == null || c.Id != exceptCardId));
}

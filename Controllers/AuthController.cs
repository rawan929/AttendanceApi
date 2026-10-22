using AttendanceApi.Dtos;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _users;
    private readonly TokenService _tokens;

    public AuthController(UserManager<IdentityUser> users, TokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    // POST /api/auth/login   (FR-01)
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null)
            return Unauthorized(new { error = "invalid_credentials" });

        // حساب موقوف أو مقفول بسبب محاولات غلط (FR-03)
        if (await _users.IsLockedOutAsync(user))
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "account_locked_or_disabled" });

        if (!await _users.CheckPasswordAsync(user, req.Password))
        {
            await _users.AccessFailedAsync(user);   // بيعد المحاولات الغلط وبيقفل الحساب بعد 5
            return Unauthorized(new { error = "invalid_credentials" });
        }

        await _users.ResetAccessFailedCountAsync(user);

        var roles = await _users.GetRolesAsync(user);
        var (token, expiresAt) = _tokens.Create(user, roles);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email ?? "",
            Roles = roles.ToList()
        });
    }

    // GET /api/auth/me   (محمي: لازم token)  -- بيوريكي مين اللي داخل ودوره إيه (FR-02)
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirst("sub")?.Value,
            email = User.FindFirst("email")?.Value,
            roles = User.FindAll("role").Select(c => c.Value)
        });
    }

    // GET /api/auth/admin-only   (للتجربة: بيرد بس لو الدور Admin)
    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly() => Ok(new { message = "أهلاً أدمن" });

    // POST /api/auth/change-password
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        var id = User.FindFirst("sub")?.Value;
        var user = id is null ? null : await _users.FindByIdAsync(id);
        if (user is null) return Unauthorized();

        var result = await _users.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return NoContent();
    }
}

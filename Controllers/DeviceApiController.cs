using AttendanceApi.Dtos;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AttendanceApi.Controllers;

// الـ endpoints اللي الجهاز نفسه بيكلمها (مش المستخدمين). بتتحقق بـ header اسمه X-Api-Key
[ApiController]
[Authorize(AuthenticationSchemes = ApiKeyAuthHandler.Scheme)]
[Route("api/device")]
public class DeviceApiController : ControllerBase
{
    private readonly ScanService _scan;
    public DeviceApiController(ScanService scan) => _scan = scan;

    private int DeviceId => int.Parse(User.FindFirst("device_id")!.Value);

    // POST /api/device/scan   body: { "uid": "04A32B1C" }   (FR-19)
    // كل النتايج التجارية بترجع 200 وفيها حقل result يعرضه الجهاز (FR-23)
    // المفتاح الغلط بس هو اللي بيرجع 401
    [HttpPost("scan")]
    public async Task<ActionResult<ScanResponse>> Scan(ScanRequest req)
    {
        var result = await _scan.ProcessAsync(DeviceId, req.Uid);
        return Ok(result);
    }

    // GET /api/device/ping   : الجهاز يتأكد إن مفتاحه شغال والسيرفر متاح
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new
    {
        device = User.Identity?.Name,
        serverTimeUtc = DateTime.UtcNow
    });
}

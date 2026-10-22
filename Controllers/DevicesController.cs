using AttendanceApi.Data;
using AttendanceApi.Dtos;
using AttendanceApi.Models;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Controllers;

// إدارة الأجهزة (FR-08)
[ApiController]
[Authorize(Roles = "Admin,Doctor")]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly AppDbContext _db;
    public DevicesController(AppDbContext db) => _db = db;

    private static DeviceDto ToDto(Device d) => new(d.Id, d.Name, d.Location, d.IsActive, d.LastSeenAt);

    // GET /api/devices?includeInactive=false
    [HttpGet]
    public async Task<ActionResult<List<DeviceDto>>> GetAll(bool includeInactive = false)
    {
        var query = _db.Devices.AsQueryable();
        if (!includeInactive)
            query = query.Where(d => d.IsActive);

        var devices = await query.OrderBy(d => d.Name).ToListAsync();
        return Ok(devices.Select(ToDto).ToList());
    }

    // GET /api/devices/2
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DeviceDto>> GetById(int id)
    {
        var device = await _db.Devices.FindAsync(id);
        return device is null ? NotFound() : Ok(ToDto(device));
    }

    // POST /api/devices
    // المفتاح بيظهر في الرد ده مرة واحدة بس، لازم يتنسخ ويتحط في الجهاز
    [HttpPost]
    public async Task<ActionResult<DeviceCreatedDto>> Create(CreateDeviceRequest req)
    {
        var key = ApiKeys.Generate();
        var device = new Device
        {
            Name = req.Name.Trim(),
            Location = req.Location.Trim(),
            ApiKeyHash = ApiKeys.Hash(key)
        };
        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        var dto = new DeviceCreatedDto(device.Id, device.Name, device.Location, device.IsActive, key);
        return CreatedAtAction(nameof(GetById), new { id = device.Id }, dto);
    }

    // PUT /api/devices/2
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateDeviceRequest req)
    {
        var device = await _db.Devices.FindAsync(id);
        if (device is null) return NotFound();

        device.Name = req.Name.Trim();
        device.Location = req.Location.Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/devices/2/deactivate   (BR-09): الجهاز بيتوقف فورًا لأن المفتاح بيتشيك في كل request
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var device = await _db.Devices.FindAsync(id);
        if (device is null) return NotFound();

        device.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/devices/2/activate
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var device = await _db.Devices.FindAsync(id);
        if (device is null) return NotFound();

        device.IsActive = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/devices/2/rotate-key   : لو المفتاح اتنسي أو اتسرب. المفتاح القديم بيبطل فورًا
    [HttpPost("{id:int}/rotate-key")]
    public async Task<ActionResult<DeviceCreatedDto>> RotateKey(int id)
    {
        var device = await _db.Devices.FindAsync(id);
        if (device is null) return NotFound();

        var key = ApiKeys.Generate();
        device.ApiKeyHash = ApiKeys.Hash(key);
        await _db.SaveChangesAsync();

        return Ok(new DeviceCreatedDto(device.Id, device.Name, device.Location, device.IsActive, key));
    }
}

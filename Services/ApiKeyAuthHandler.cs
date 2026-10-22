using System.Security.Claims;
using System.Text.Encodings.Web;
using AttendanceApi.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AttendanceApi.Services;

// تحقق الجهاز بالمفتاح بتاعه (FR-20, BR-09): الجهاز مش مستخدم بشري فمفيش login ولا JWT
// بيتشيك في كل request، فلما الأدمن يعطّل جهاز بيتوقف فورًا
public class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Scheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    private readonly AppDbContext _db;

    public ApiKeyAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppDbContext db) : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var values))
            return AuthenticateResult.NoResult();

        var key = values.ToString().Trim();
        if (key.Length == 0)
            return AuthenticateResult.Fail("missing_api_key");

        var hash = ApiKeys.Hash(key);
        var device = await _db.Devices.FirstOrDefaultAsync(d => d.ApiKeyHash == hash);
        if (device is null || !device.IsActive)
            return AuthenticateResult.Fail("invalid_or_disabled_device");

        var claims = new[]
        {
            new Claim("device_id", device.Id.ToString()),
            new Claim("name", device.Name),
            new Claim("role", "Device")
        };
        var identity = new ClaimsIdentity(claims, Scheme, "name", "role");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme);
        return AuthenticateResult.Success(ticket);
    }
}

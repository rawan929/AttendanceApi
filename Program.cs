using System.Text;
using System.Text.Json.Serialization;
using AttendanceApi.Data;
using AttendanceApi.Hubs;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // الـ enums تطلع نص (Open/Closed/Card/Manual) بدل أرقام، والأوقات تطلع UTC فيها Z (BR-08)
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    });
builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// SignalR: البث اللحظي لشاشة الدكتور (FR-31)
builder.Services.AddSignalR()
    .AddJsonProtocol(o =>
    {
        o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.PayloadSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    });
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// Identity: المستخدمين والأدوار وتشفير الباسورد (FR-04)
builder.Services.AddIdentityCore<IdentityUser>(o =>
    {
        o.User.RequireUniqueEmail = true;
        o.Password.RequiredLength = 8;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<ActorService>();
builder.Services.AddScoped<LiveNotifier>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<ScanService>();
builder.Services.AddHostedService<SessionExpiryService>();   // بيقفل الجلسات المنتهية (FR-16)

// JWT: السيرفر بيتحقق من الـ token اللي جاي في header
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;   // يخلي أسماء الـ claims زي ما كتبناها (sub, email, role)
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "role",
            NameClaimType = "sub"
        };

        // الـ WebSocket مبيقدرش يبعت header، فالـ SignalR بيبعت الـ token في الـ query (للـ hub بس)
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"].ToString();
                if (!string.IsNullOrEmpty(accessToken) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    })
    // مفتاح الجهاز (FR-19, FR-20): scheme تاني، بيستخدمه DeviceApiController بس
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>(ApiKeyAuthHandler.Scheme, null);
builder.Services.AddAuthorization();

var app = builder.Build();

// إنشاء الأدوار وحساب الأدمن الأول
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();   // الصفحة على /scalar/v1
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseStaticFiles();      // صفحة التجربة /live-test.html
app.UseAuthentication();   // الأول: مين انت؟
app.UseAuthorization();    // بعدين: مسموحلك؟
app.MapControllers();
app.MapHub<AttendanceHub>("/hubs/attendance");
app.Run();

using AttendanceApi.Data;
using AttendanceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Hubs;

// قناة البث اللحظي للدكتور (FR-31): الدكتور بيشترك في جلسة، وكل تسجيل جديد بيوصله فورًا
[Authorize(Roles = "Doctor,Admin")]
public class AttendanceHub : Hub
{
    private readonly AppDbContext _db;
    private readonly ActorService _actor;

    public AttendanceHub(AppDbContext db, ActorService actor)
    {
        _db = db;
        _actor = actor;
    }

    public static string GroupName(int sessionId) => $"session-{sessionId}";

    public async Task JoinSession(int sessionId)
    {
        var info = await _db.Sessions
            .Where(s => s.Id == sessionId)
            .Select(s => new { s.Course.DoctorId })
            .FirstOrDefaultAsync();
        if (info is null)
            throw new HubException("session_not_found");

        var user = Context.User!;
        if (!user.IsInRole("Admin"))
        {
            var doctorId = await _actor.GetDoctorIdAsync(user);
            if (doctorId is null || doctorId != info.DoctorId)
                throw new HubException("not_your_session");   // BR-06
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(sessionId));
    }

    public Task LeaveSession(int sessionId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(sessionId));
}

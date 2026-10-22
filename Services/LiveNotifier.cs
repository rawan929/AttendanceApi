using AttendanceApi.Dtos;
using AttendanceApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AttendanceApi.Services;

// بتبعت الأحداث للدكتور. لو البث فشل لأي سبب، التسجيل نفسه (اللي اتحفظ في الداتابيز) مايتأثرش
public class LiveNotifier
{
    private readonly IHubContext<AttendanceHub> _hub;
    private readonly ILogger<LiveNotifier> _log;

    public LiveNotifier(IHubContext<AttendanceHub> hub, ILogger<LiveNotifier> log)
    {
        _hub = hub;
        _log = log;
    }

    public Task RecordedAsync(AttendanceEventDto e) =>
        SendAsync(e.SessionId, "attendanceRecorded", e);

    public Task CancelledAsync(AttendanceEventDto e) =>
        SendAsync(e.SessionId, "attendanceCancelled", e);

    public Task SessionClosedAsync(int sessionId) =>
        SendAsync(sessionId, "sessionClosed", new { sessionId });

    private async Task SendAsync(int sessionId, string method, object payload)
    {
        try
        {
            await _hub.Clients.Group(AttendanceHub.GroupName(sessionId)).SendAsync(method, payload);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Live notification {Method} failed for session {SessionId}", method, sessionId);
        }
    }
}

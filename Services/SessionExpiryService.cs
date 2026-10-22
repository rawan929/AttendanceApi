namespace AttendanceApi.Services;

// FR-16: بيقفل الجلسات المنتهية تلقائيًا كل 30 ثانية
public class SessionExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<SessionExpiryService> _log;

    public SessionExpiryService(IServiceScopeFactory scopes, ILogger<SessionExpiryService> log)
    {
        _scopes = scopes;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var sessions = scope.ServiceProvider.GetRequiredService<SessionService>();
                    await sessions.CloseExpiredAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.LogError(ex, "Closing expired sessions failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // التطبيق بيقفل، عادي
        }
    }
}

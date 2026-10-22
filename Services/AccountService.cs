using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AttendanceApi.Services;

// شغل حسابات الدخول (Identity) المشترك بين الطلاب والدكاترة،
// عشان مانكررش نفس الكود في كل Controller
public class AccountService
{
    private readonly UserManager<IdentityUser> _users;
    public AccountService(UserManager<IdentityUser> users) => _users = users;

    public async Task<bool> EmailExistsAsync(string email) =>
        await _users.FindByEmailAsync(email) is not null;

    // بتعمل حساب دخول جديد وتديله الدور. لو فشلت بترجع الأخطاء (مثلاً الباسورد ضعيف)
    public async Task<(IdentityUser? User, string[] Errors)> CreateAsync(string email, string password, string role)
    {
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };

        var result = await _users.CreateAsync(user, password);
        if (!result.Succeeded)
            return (null, result.Errors.Select(e => e.Description).ToArray());

        var roleResult = await _users.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
            return (null, roleResult.Errors.Select(e => e.Description).ToArray());

        return (user, Array.Empty<string>());
    }

    // إيقاف/تفعيل الحساب: الإيقاف = قفل الحساب لأجل غير مسمى، فالـ login بيرفضه (FR-03)
    public async Task SetActiveAsync(string? userId, bool active)
    {
        if (userId is null) return;
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return;

        await _users.SetLockoutEnabledAsync(user, true);
        if (active)
        {
            await _users.SetLockoutEndDateAsync(user, null);
            await _users.ResetAccessFailedCountAsync(user);
        }
        else
        {
            await _users.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }
    }

    // بتجيب إيميلات مجموعة مستخدمين في استعلام واحد: userId -> email
    public async Task<Dictionary<string, string?>> GetEmailsAsync(IEnumerable<string?> userIds)
    {
        var ids = userIds.Where(i => i != null).Select(i => i!).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, string?>();

        return await _users.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email);
    }
}

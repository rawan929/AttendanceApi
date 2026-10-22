using System.Security.Cryptography;
using System.Text;

namespace AttendanceApi.Services;

// مفاتيح الأجهزة (FR-08): بنولّد مفتاح عشوائي طويل، وبنخزن الـ hash بتاعه بس
public static class ApiKeys
{
    public static string Generate() =>
        "dev_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    // SHA-256 كفاية هنا لأن المفتاح عشوائي وطويل (مش باسورد بيتخمّن)، والناتج 64 حرف
    public static string Hash(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
}

public static class CardUid
{
    // بنوحّد شكل الـ UID: بدون فواصل أو مسافات وحروف كبيرة، فـ "04:a3:2b" و "04A32B" نفس الكارت
    public static string Normalize(string uid) =>
        new string(uid.Where(ch => ch != ':' && ch != '-' && !char.IsWhiteSpace(ch)).ToArray())
            .ToUpperInvariant();
}

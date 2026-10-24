using System.Security.Cryptography;
using System.Text;

namespace Ecommerce.Infrastructure.Marketing;

/// <summary>Both Meta's Conversions API and TikTok's Events API require PII (here, just the phone
/// number — COD guest checkout never collects an email) sent as a lowercase SHA-256 hash of the
/// E.164-normalized value, never in the clear.</summary>
internal static class PhoneHashing
{
    /// <summary>Algerian local numbers are stored as "0X XXXXXXXX" (10 digits, leading 0). E.164 drops
    /// that leading 0 and prepends the country code "213". Already-international numbers (already
    /// starting with 213) are left as-is.</summary>
    internal static string ToE164Digits(string rawPhone)
    {
        var digits = new string(rawPhone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("213", StringComparison.Ordinal)) return digits;
        if (digits.StartsWith('0')) return "213" + digits[1..];
        return "213" + digits;
    }

    internal static string Sha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(bytes);
    }

    internal static string HashedPhone(string rawPhone) => Sha256Hex(ToE164Digits(rawPhone));
}

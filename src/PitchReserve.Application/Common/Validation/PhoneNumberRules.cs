using System.Text.RegularExpressions;

namespace PitchReserve.Application.Common.Validation;

public static partial class PhoneNumberRules
{
    public static bool IsValid(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone) || phone.Trim().Length > 30) return false;
        var normalized = Regex.Replace(phone.Trim(), @"[\s()\-]", "");
        // Egyptian mobile: 010/011/012/015 locally, or +20 followed by the number without its first zero.
        return MyRegex().IsMatch(normalized);
    }

    [GeneratedRegex(@"^(0|\+20)1[0125][0-9]{8}$")]
    private static partial Regex MyRegex();
}

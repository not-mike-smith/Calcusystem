using System.Collections.Generic;
using System.Linq;

namespace Measurement.Extensions;

public static class IntExponentExtensions
{
    private static readonly IReadOnlyDictionary<char, char> Superscripts = new Dictionary<char, char>
    {
        {'-', '⁻'},
        {'0', '⁰'},
        {'1', '¹'},
        {'2', '²'},
        {'3', '³'},
        {'4', '⁴'},
        {'5', '⁵'},
        {'6', '⁶'},
        {'7', '⁷'},
        {'8', '⁸'},
        {'9', '⁹'},
    };

    public static string ToSuperscript(this int i)
    {
        var s = i.ToString();
        var superscript = s.Select(c => Superscripts.TryGetValue(c, out var v) ? v : c);
        return string.Concat(superscript.Take(s.Length));
    }
}

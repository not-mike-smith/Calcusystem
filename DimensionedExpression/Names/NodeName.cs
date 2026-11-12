using System.Globalization;
using System.Text;

namespace Calcusystem.DimensionedExpression.Names;

/// <summary>
/// The rule for the name of an expression or a relationship, and the form a name is stored in.
/// </summary>
/// <remarks>
/// A valid name:
/// <list type="bullet">
/// <item>is not empty, and has no whitespace at either end;</item>
/// <item>holds no format or control character (Unicode categories Cf and Cc), such as a zero-width joiner;</item>
/// <item>is valid Unicode text.</item>
/// </list>
/// Names are case-sensitive. Emoji and look-alike characters from different scripts are allowed.
/// </remarks>
public static class NodeName
{
    /// <summary>Why <paramref name="name"/> is not a valid name, or null if it is one.</summary>
    /// <remarks>Null and the empty string are not valid names. A caller that allows no name asks first.</remarks>
    public static string? Problem(string? name)
    {
        if (string.IsNullOrEmpty(name)) return "A name cannot be empty.";
        if (! IsValidUnicode(name)) return "The name is not valid Unicode text.";
        if (string.IsNullOrWhiteSpace(name)) return "A name cannot be only whitespace.";

        if (char.IsWhiteSpace(name[0]) || char.IsWhiteSpace(name[^1]))
        {
            return $"The name '{name}' starts or ends with whitespace.";
        }

        foreach (var rune in name.EnumerateRunes())
        {
            if (Rune.GetUnicodeCategory(rune) is UnicodeCategory.Format or UnicodeCategory.Control)
            {
                return $"The name holds an invisible character, U+{rune.Value:X4}.";
            }
        }

        return null;
    }

    /// <summary>
    /// The form <paramref name="name"/> is stored and compared in: Unicode NFC, so the same characters encoded
    /// differently give one name.
    /// </summary>
    /// <remarks>
    /// Does not check the name. Text that is not valid Unicode cannot be normalized and is returned unchanged.
    /// </remarks>
    public static string Normalized(string name) =>
        IsValidUnicode(name) ? name.Normalize(NormalizationForm.FormC) : name;

    /// <summary>The stored form of <paramref name="name"/>, which must be a valid name.</summary>
    /// <exception cref="Exceptions.InvalidNameException">
    /// <paramref name="name"/> is not a valid name. The message is what <see cref="Problem"/> answers.
    /// </exception>
    public static string Checked(string? name) =>
        Problem(name) is { } problem
            ? throw new Exceptions.InvalidNameException(problem)
            : Normalized(name!);

    // A surrogate with no partner is the one thing a .NET string can hold that is not text.
    private static bool IsValidUnicode(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]))
            {
                if (i + 1 >= text.Length || ! char.IsLowSurrogate(text[i + 1])) return false;
                i++;
            }
            else if (char.IsLowSurrogate(text[i]))
            {
                return false;
            }
        }

        return true;
    }
}

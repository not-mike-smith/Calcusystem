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
        if (! TryNormalize(name, out _)) return "The name is not valid Unicode text.";
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
    public static string Normalized(string name) => TryNormalize(name, out var normalized) ? normalized : name;

    /// <summary>The stored form of <paramref name="name"/>, which must be a valid name.</summary>
    /// <exception cref="Exceptions.InvalidNameException">
    /// <paramref name="name"/> is not a valid name. The message is what <see cref="Problem"/> answers.
    /// </exception>
    public static string Checked(string? name) =>
        Problem(name) is { } problem
            ? throw new Exceptions.InvalidNameException(problem)
            : Normalized(name!);

    // What cannot be normalized is not text: a surrogate with no partner, or a noncharacter such as U+FFFE.
    // The runtime is the judge, so this cannot disagree with it about which strings those are.
    private static bool TryNormalize(string text, out string normalized)
    {
        try
        {
            normalized = text.Normalize(NormalizationForm.FormC);
            return true;
        }
        catch (ArgumentException)
        {
            normalized = text;
            return false;
        }
    }
}

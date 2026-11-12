namespace Calcusystem.DimensionedExpression.Names;

/// <summary>The rule for the name of an expression or a relationship, and the form a name is stored in.</summary>
public static class NodeName
{
    /// <summary>Why <paramref name="name"/> is not a valid name, or null if it is one.</summary>
    public static string? Problem(string? name) => throw new NotImplementedException();

    /// <summary>The form <paramref name="name"/> is stored and compared in.</summary>
    public static string Normalized(string name) => throw new NotImplementedException();

    /// <summary>The stored form of <paramref name="name"/>, which must be a valid name.</summary>
    public static string Checked(string? name) => throw new NotImplementedException();
}

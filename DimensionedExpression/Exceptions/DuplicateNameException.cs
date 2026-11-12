namespace Calcusystem.DimensionedExpression.Exceptions;

/// <summary>A system was given a node whose name another of its nodes already has.</summary>
public sealed class DuplicateNameException(string name)
    : InvalidOperationException($"The name '{name}' is already used by another node in the system.")
{
    /// <summary>The name two nodes claim.</summary>
    public string Name { get; } = name;
}

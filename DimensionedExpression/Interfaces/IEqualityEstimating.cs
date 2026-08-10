using Calcusystem.Measurement;

namespace Calcusystem.DimensionedExpression.Interfaces;

/// <summary>
/// Strategy for deciding when two <see cref="Measurand"/>s count as equal. Because measured values carry
/// uncertainty and floating-point noise, exact equality is rarely meaningful; the concrete strategy defines what
/// "equal enough" means (e.g. overlapping uncertainty intervals, or a relative tolerance).
/// </summary>
/// <remarks>
/// This is the one dependency <c>EqualityOperator</c> requires — it is injected at construction, and the
/// deserializer supplies it when rebuilding equality operators.
/// </remarks>
public interface IEqualityEstimating
{
    /// <summary>Whether <paramref name="lhs"/> and <paramref name="rhs"/> are considered equal.</summary>
    bool AreEqual(Measurand lhs, Measurand rhs);
}

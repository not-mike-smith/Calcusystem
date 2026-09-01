namespace Calcusystem.Core.Extensions;

public static class DoubleExtensions
{
    /// <summary>
    /// Whether an ordering between <paramref name="lhs"/> and <paramref name="rhs"/> is meaningful at all.
    /// </summary>
    /// <remarks>
    /// Three cases are not. A <see cref="double.NaN"/> compares false against everything including itself, so
    /// no ordering holds and none is denied. Two infinities of the same sign are worse than unordered — IEEE
    /// reports them equal, but they stand for "grew without bound", which says nothing about whether one
    /// outgrew the other. Answering "equal" there would manufacture agreement out of two unknowns.
    /// <para>
    /// Infinities of <i>opposite</i> sign are deliberately absent: those are perfectly ordered, and a bounded
    /// value against either is ordered too.
    /// </para>
    /// </remarks>
    public static bool IsComparisonDetermined(this double lhs, double rhs)
    {
        if (double.IsNaN(lhs) || double.IsNaN(rhs)) return false;
        if (double.IsPositiveInfinity(lhs) && double.IsPositiveInfinity(rhs)) return false;
        if (double.IsNegativeInfinity(lhs) && double.IsNegativeInfinity(rhs)) return false;

        return true;
    }
}

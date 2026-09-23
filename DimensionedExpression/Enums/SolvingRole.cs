namespace Calcusystem.DimensionedExpression.Enums;

/// <summary>
/// What a relationship does to the problem: whether it produces a value, asserts that separately-computed values
/// agree, or bounds a value someone else produced.
/// </summary>
/// <remarks>
/// A relationship's role in <i>solving</i>, distinct from the roles of its two <i>sides</i>. Whether it is
/// <i>enforced or merely reported</i> is a separate question and deliberately not here. No member is zero, so an
/// unsupplied role is detectable rather than silently a <see cref="Requirement"/>.
/// </remarks>
public enum SolvingRole : byte
{
    /// <summary>
    /// Bounds a value without producing one, so it removes no degree of freedom. Yield strength, a sonic
    /// velocity limit, an advertised maximum mass — identical in arithmetic, and distinguished from one another
    /// only by their provenance.
    /// </summary>
    /// <remarks>
    /// The default, and the only role a non-equality operator can have: an ordering or tolerance
    /// relation confines a value to an interval, and no solver can turn an interval into a point.
    /// </remarks>
    Requirement = 1,

    /// <summary>
    /// Contributes a residual a solver drives to zero, determining a value. Counted against the unknowns when
    /// degrees of freedom are computed.
    /// </summary>
    /// <remarks>
    /// Nothing has to be algebraically invertible: <c>T_eos - T_path = 0</c> is a perfectly good residual for a
    /// solver that cannot isolate either side.
    /// </remarks>
    Equation = 2,

    /// <summary>
    /// Asserts that quantities computed by different routes agree — two paths to one temperature, a redundant
    /// conservation law. Contributes a residual exactly as <see cref="Equation"/> does, and so also removes a
    /// degree of freedom.
    /// </summary>
    /// <remarks>
    /// Choose this over <see cref="Equation"/> when neither side defines the quantity and the two are
    /// independent routes to it — a distinction no predicate can recover.
    /// </remarks>
    Coherence = 3,
}

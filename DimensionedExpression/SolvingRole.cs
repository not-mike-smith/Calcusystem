namespace Calcusystem.DimensionedExpression;

/// <summary>
/// What a relationship does to the problem: whether it produces a value, asserts that separately-computed values
/// agree, or bounds a value someone else produced.
/// </summary>
/// <remarks>
/// <para>
/// Named for the axis rather than the carrier. This is a relationship's role in <i>solving</i> — distinct from
/// the roles of its two <i>sides</i> (subject and criterion), which are about presenting a result and say nothing
/// about the shape of the problem.
/// </para>
/// <para>
/// Three members rather than a boolean because "not an equation" was previously doing two jobs. Whether a
/// relationship is <i>enforced or merely reported</i> is a fourth thing and deliberately not here: that is a
/// search policy belonging to whoever asks for a solve, while this is structure the model owns.
/// </para>
/// <para>
/// <b>No member is zero</b>, and none means "no role" — every relationship does something. So the default value
/// of the underlying type is not a valid role, which makes an unsupplied one detectable: a default-constructed
/// <c>BinaryOperatorState</c>, or a payload missing the field, lands on nothing rather than silently claiming to
/// be a <see cref="Requirement"/>.
/// </para>
/// </remarks>
public enum SolvingRole : byte
{
    /// <summary>
    /// Bounds a value without producing one, so it removes no degree of freedom. Yield strength, a sonic
    /// velocity limit, an advertised maximum mass — identical in arithmetic, and distinguished from one another
    /// only by their provenance.
    /// </summary>
    /// <remarks>
    /// The default, and the only role the twelve non-equality operators can have: an ordering or tolerance
    /// relation confines a value to an interval, and no solver can turn an interval into a point.
    /// </remarks>
    Requirement = 1,

    /// <summary>
    /// Contributes a residual a solver drives to zero, determining a value. Counted against the unknowns when
    /// degrees of freedom are computed.
    /// </summary>
    /// <remarks>
    /// Deliberately not called "determining", which implies a direction that need not exist. Nothing has to be
    /// algebraically invertible: <c>T_eos - T_path = 0</c> is a perfectly good residual for a solver that cannot
    /// isolate either side.
    /// </remarks>
    Equation = 2,

    /// <summary>
    /// Asserts that quantities computed by different routes agree — two paths to one temperature, a redundant
    /// conservation law. Contributes a residual exactly as <see cref="Equation"/> does, and so also removes a
    /// degree of freedom.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A separate member because the distinction is <b>not recoverable from the predicate</b>: both assert
    /// equality, and only the modeller knows whether one side defines a quantity or the two are independent
    /// routes to it. A solver wants that intent — any path is a usable initial estimate for the others, and a
    /// coherence group is where to relax an over-determined system.
    /// </para>
    /// <para>
    /// When both sides are already known this determines nothing and becomes a redundancy check; that falls out
    /// of incidence rather than needing a role of its own (see <c>FlatSystem.RedundantEquations</c>).
    /// </para>
    /// </remarks>
    Coherence = 3,
}

using Calcusystem.Core.Interfaces;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.DimensionedExpression.Interfaces;

/// <summary>
/// A relationship asserted between two expressions — equality, tolerance compatibility, or ordering. Used both
/// as a definition (a relationship that should hold) and as a constraint (a check to run). The full taxonomy of
/// concrete operators, with symbols and exact interval conditions, is in <c>BinaryOperators/OPERATORS.md</c>.
/// </summary>
public interface IBinaryOperator : IIdentified
{

    /// <summary>Optional human-readable name for the relationship.</summary>
    public string? Name { get; set; }

    /// <summary>Optional human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// The left-hand operand. For non-commutative operators this is the value under test.
    /// </summary>
    /// <remarks>
    /// Fixed at construction. A relationship's operands are structure, and structure is immutable — see
    /// <see cref="IExpression"/>. Values still change; what a relationship is asserted <i>between</i> does not.
    /// </remarks>
    IExpression Lhs { get; }

    /// <summary>The right-hand operand. For non-commutative operators this is the bound or reference.</summary>
    /// <remarks>Fixed at construction, as <see cref="Lhs"/> is.</remarks>
    IExpression Rhs { get; }

    /// <summary>Whether swapping <see cref="Lhs"/> and <see cref="Rhs"/> leaves the result unchanged.</summary>
    bool IsCommutative { get; }

    /// <summary>
    /// The operator's notation — <c>&lt;&lt;</c>, <c>=}</c>, <c>==</c>. Unique across the operators, and the
    /// name they are documented under in <c>BinaryOperators/OPERATORS.md</c>.
    /// </summary>
    /// <remarks>
    /// On the interface because it is how a relationship identifies itself to a reader — <c>ToString()</c> is
    /// <c>{Lhs} {Symbol} {Rhs}</c> — so anything holding an <see cref="IBinaryOperator"/> and reporting on it
    /// needs it. Presentation only: nothing dispatches on it, and <c>BinaryOperatorKind</c> is what the wire
    /// carries.
    /// </remarks>
    string Symbol { get; }

    /// <summary>
    /// What this relationship does to the problem — see <see cref="DimensionedExpression.SolvingRole"/>.
    /// </summary>
    /// <remarks>
    /// Read-only, and settable only where it can meaningfully be anything else. Ordering and tolerance relations
    /// confine a value to an interval rather than producing a point, so nothing can be derived from them: their
    /// implementations return <see cref="Enums.SolvingRole.Requirement"/> unconditionally and
    /// their constructors offer no way to say otherwise. This is why the property needs no validation — an
    /// operator that cannot determine cannot be built claiming it does.
    /// </remarks>
    SolvingRole SolvingRole { get; }

    /// <summary>
    /// Whether this relationship contributes a residual, and so is counted against the unknowns when degrees of
    /// freedom are calculated. True for <see cref="Enums.SolvingRole.Equation"/> and
    /// <see cref="Enums.SolvingRole.Coherence"/> alike.
    /// </summary>
    /// <remarks>
    /// Derived from <see cref="SolvingRole"/> rather than stored beside it, so the two cannot disagree. It stays
    /// as a named property because "does this affect the count" is the question degrees-of-freedom code actually
    /// asks, and re-deriving it at each call site would spread one decision across several.
    /// </remarks>
    bool IsDetermining { get; }

    /// <summary>
    /// Which side is being judged, or <see langword="null"/> where the relationship draws no such distinction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Derived from <see cref="SolvingRole"/> and the operand positions, never stored. A
    /// <see cref="Enums.SolvingRole.Requirement"/> tests one thing against another, and by
    /// construction the thing under test is <see cref="Lhs"/>; an
    /// <see cref="Enums.SolvingRole.Equation"/> or
    /// <see cref="Enums.SolvingRole.Coherence"/> has no such asymmetry — neither side of
    /// <c>T_eos == T_path</c> is the one being judged — so both are null there.
    /// </para>
    /// <para>
    /// Deriving rather than storing is what keeps this from going stale: there is nothing beside the operands
    /// that a later change could leave pointing at the wrong one.
    /// </para>
    /// </remarks>
    IExpression? Subject { get; }

    /// <summary>
    /// What <see cref="Subject"/> is being judged against, or <see langword="null"/> where the relationship
    /// draws no such distinction. Non-null exactly when <see cref="Subject"/> is.
    /// </summary>
    /// <remarks>
    /// "Criterion" rather than "reference", which is already spoken for by <c>ProvenanceFactory.Reference</c>,
    /// and rather than "expected", which lies about corroboration — where two peers are compared and neither
    /// was expected — and about a failed equation, where neither side is the authority.
    /// </remarks>
    IExpression? Criterion { get; }

    /// <summary>
    /// Whether the relationship holds for the two values supplied — the predicate alone, with no reading of the
    /// model and no traversal.
    /// </summary>
    /// <remarks>
    /// The counterpart of <see cref="IExpression.ComputeFrom"/> for relationships, and the reason it exists is
    /// the same: a verdict must be a function of the values it was handed, not of a fresh read of the model.
    /// Without this seam a check evaluated during a calculation-at-trial-values would silently report on the
    /// <i>stored</i> values instead, and would re-walk both subgraphs the calculation had just finished walking.
    /// </remarks>
    /// <param name="lhs">The value of <see cref="Lhs"/>.</param>
    /// <param name="rhs">The value of <see cref="Rhs"/>.</param>
    /// <returns>
    /// <see langword="null"/> when the comparison has no answer — the two values carry different dimensions, or
    /// one of the landmarks under test is not a number. Distinct from <see langword="false"/>, which says the
    /// relationship was evaluated and does not hold.
    /// </returns>
    bool? IsSatisfiedGiven(Measurand lhs, Measurand rhs);

    /// <summary>
    /// Whether the relationship holds: three-valued — <see langword="true"/> / <see langword="false"/>, or
    /// <see langword="null"/> when the answer is unknown, either because a side did not resolve or because the
    /// values it produced cannot be compared.
    /// </summary>
    /// <remarks>
    /// Computes both sides and delegates to <see cref="IsSatisfiedGiven"/>. Convenient for asking about one
    /// relationship in isolation; a caller checking a whole system should use <c>Calculate</c>, which resolves
    /// every node once and reads the operands out of what it already computed.
    /// </remarks>
    /// <param name="overrides">
    /// Values supplied for this evaluation only, taking precedence over a variable's own — the same bindings
    /// <see cref="IExpression.ComputeIfDetermined"/> takes.
    /// </param>
    /// <param name="propagator">How uncertainties are combined, or null for the conservative default.</param>
    bool? IsSatisfied(
        IReadOnlyDictionary<Variable, Measurand>? overrides = null,
        IErrorPropagator? propagator = null);

    /// <summary>Whether both operands have values, so <see cref="IsSatisfied"/> can return a definite result.</summary>
    bool AreBothSidesFullyDescribed { get; }

    /// <summary>
    /// The distinct unbound variables reachable from either side — the unknowns this relationship is incident
    /// on, and its row of the incidence structure a structural analysis matches over.
    /// </summary>
    IEnumerable<Variable> FreeVariables();

    /// <summary>
    /// Optional audit annotation describing where this relationship came from (e.g. a citation for a
    /// constitutive equation). Null means provenance is not tracked; purely descriptive.
    /// </summary>
    IProvenance? Provenance { get; set; }

    /// <summary>
    /// Returns the complete stored state of this operator — which operator it is, its operand ids, and its
    /// annotations. Rebuild via <c>BinaryOperatorFactory.FromState</c>.
    /// </summary>
    BinaryOperatorState GetState();
}

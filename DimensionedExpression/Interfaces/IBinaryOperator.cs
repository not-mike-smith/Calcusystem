using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Core;
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

    /// <summary>The left-hand operand. For non-commutative operators this is the value under test.</summary>
    IExpression Lhs { get; set; }

    /// <summary>The right-hand operand. For non-commutative operators this is the bound or reference.</summary>
    IExpression Rhs { get; set; }

    /// <summary>Whether swapping <see cref="Lhs"/> and <see cref="Rhs"/> leaves the result unchanged.</summary>
    bool IsCommutative { get; }

    /// <summary>
    /// Whether this relationship <i>determines</i> a value — an equation the solver may use to compute an
    /// unknown — rather than merely <i>checking</i> one. Determining relationships are the equations counted
    /// against the unknowns when a system's degrees of freedom are calculated; every other relationship is a
    /// check and reduces degrees of freedom by nothing.
    /// </summary>
    /// <remarks>
    /// Read-only, and settable only where it can meaningfully be true. Ordering and tolerance relations yield an
    /// interval rather than a point, so no value can be derived from them; their implementations return
    /// <see langword="false"/> unconditionally and their constructors offer no way to say otherwise. This is why
    /// the property needs no validation — an operator that cannot determine cannot be built claiming it does.
    /// </remarks>
    bool IsDetermining { get; }

    /// <summary>
    /// Whether the relationship holds: three-valued — <see langword="true"/> / <see langword="false"/>, or
    /// <see langword="null"/> when <see cref="AreBothSidesFullyDescribed"/> is false and the answer is unknown.
    /// </summary>
    bool? IsSatisfied();

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

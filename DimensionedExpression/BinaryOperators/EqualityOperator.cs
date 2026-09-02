using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the Lhs and Rhs agree, to the strictness named by <see cref="AgreementRule"/>.
/// <br/>
/// Symbol: <b>·==·</b>, <b>{·==·}</b> or <b>{>=<}</b> — each is the tolerance operator asserting the same
/// condition with an <c>=</c> inserted at its centre, which is what marks the equality family.
/// <br/>
/// Use when two quantities are expected to be the same, and say how nearly the same they must be.
/// </summary>
/// <remarks>
/// <para>
/// The only operator whose <see cref="SolvingRole"/> can be anything but a requirement: an equality is the one
/// relation from which a solver can derive a value. Which of the three a given instance is remains the
/// modeller's call, so <paramref name="solvingRole"/> has no default — every construction states its intent.
/// </para>
/// <para>
/// <paramref name="agreementRule"/> has no default for the same reason. "Equal" is not one thing for measured
/// values, and picking a reading on the modeller's behalf is what let equality semantics go unrecorded for as
/// long as they did.
/// </para>
/// </remarks>
/// <param name="agreementRule">How strictly "equal" is read — see <see cref="Enums.AgreementRule"/>.</param>
/// <param name="solvingRole">
/// <see cref="Enums.SolvingRole.Equation"/> when this defines a quantity the solver may compute
/// (<c>mass_in == mass_out</c>); <see cref="Enums.SolvingRole.Coherence"/> when it asserts that
/// two independently computed routes to one quantity agree (<c>T_eos == T_path</c>);
/// <see cref="Enums.SolvingRole.Requirement"/> when it checks a value against a criterion
/// (<c>measured_T == design_T</c>).
/// </param>
public class EqualityOperator(AgreementRule agreementRule, SolvingRole solvingRole)
    : CommutativeOperatorBase
{
    /// <summary>Nominal agreement: the two reported values are the same number.</summary>
    public static readonly IReadOnlyList<ComparisonRule> NominalRules =
        [new(Landmark.Nominal, ComparisonType.EqualTo, Landmark.Nominal)];

    protected override BinaryOperatorKind Kind => BinaryOperatorKind.Equality;

    /// <summary>How strictly this instance reads "equal".</summary>
    /// <remarks>
    /// Stored, and part of the operator's state, so the reading survives a round trip. Named
    /// <c>Agreement</c> rather than repeating the type name, which would shadow it inside this class.
    /// </remarks>
    public AgreementRule Agreement { get; } = agreementRule;

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// One rule, applied three times: <b>take the symbol of the operator asserting the same condition and insert
    /// an <c>=</c> at its centre</b>. <c>·=·</c> → <c>·==·</c>, <c>{·=·}</c> → <c>{·==·}</c>,
    /// <c>{&gt;&lt;}</c> → <c>{&gt;=&lt;}</c>. No member of the family is a special case, which is what a
    /// notation is for.
    /// </para>
    /// <para>
    /// It also cannot break the commutativity invariant, and not by luck: <c>=</c> is its own mirror image and
    /// the centre is the fixed point of mirror-reversal, so inserting one there maps a palindrome to a
    /// palindrome. That is what ruled out the earlier <c>≃=</c> and <c>≈=</c> — a <i>trailing</i> marker reads
    /// the same way round from one side only, which is exactly what a commutative relation must not do.
    /// </para>
    /// <para>
    /// <c>·==·</c> rather than the conventional <c>==</c> deliberately. "Equal" is not one thing for measured
    /// values — that is why <see cref="Enums.AgreementRule"/> exists — and <c>==</c> is silent about
    /// which statistic participates, while <c>·==·</c> says the reported values and nothing else.
    /// </para>
    /// <para>
    /// Asserting the same rules as the tolerance operators is deliberate too. Those state the condition as a
    /// requirement; an equality can additionally be an <see cref="Enums.SolvingRole.Equation"/>
    /// or a <see cref="Enums.SolvingRole.Coherence"/>, and a report needs to tell the two apart.
    /// </para>
    /// </remarks>
    public override string Symbol => Agreement switch
    {
        AgreementRule.Nominal => "·==·",
        AgreementRule.Mutual => "{·==·}",
        AgreementRule.Overlapping => "{>=<}",
        _ => throw new ArgumentOutOfRangeException(
            nameof(Agreement), Agreement, "Unknown agreement rule."),
    };

    /// <inheritdoc/>
    public override SolvingRole SolvingRole { get; } = solvingRole;

    /// <inheritdoc/>
    /// <remarks>
    /// The looser two readings borrow their rules from <see cref="ContainmentLadder"/>, so an equality asserting
    /// mutual containment and the operator named after that rung are the same condition rather than two
    /// implementations of it.
    /// </remarks>
    public override IReadOnlyList<ComparisonRule> Rules { get; } = agreementRule switch
    {
        AgreementRule.Nominal => NominalRules,
        AgreementRule.Mutual =>
        [
            new(Landmark.Nominal, ComparisonType.GreaterThanOrEqualTo, Landmark.LowerBound),
            new(Landmark.Nominal, ComparisonType.LessThanOrEqualTo, Landmark.UpperBound),
            new(Landmark.LowerBound, ComparisonType.LessThanOrEqualTo, Landmark.Nominal),
            new(Landmark.UpperBound, ComparisonType.GreaterThanOrEqualTo, Landmark.Nominal),
        ],
        AgreementRule.Overlapping =>
        [
            new(Landmark.UpperBound, ComparisonType.GreaterThanOrEqualTo, Landmark.LowerBound),
            new(Landmark.LowerBound, ComparisonType.LessThanOrEqualTo, Landmark.UpperBound),
        ],
        _ => throw new ArgumentOutOfRangeException(
            nameof(agreementRule), agreementRule, "Unknown agreement rule."),
    };

    /// <inheritdoc/>
    public override BinaryOperatorState GetState() => base.GetState() with { Agreement = Agreement };
}

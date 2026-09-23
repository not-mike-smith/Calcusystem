using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.Measurement.Enums;

namespace Calcusystem.DimensionedExpression.BinaryOperators;

/// <summary>
/// Satisfied when the Lhs and Rhs agree, to the strictness named by <see cref="AgreementRule"/>.
/// <br/><br/>
/// Symbol: <b>·==·</b>, <b>{·==·}</b>, or <b>{&gt;=&lt;}</b>
/// </summary>
/// <remarks>
/// The only operator whose <see cref="SolvingRole"/> can be anything but a requirement.
/// </remarks>
/// <param name="agreementRule">How strictly "equal" is read — see <see cref="Enums.AgreementRule"/>.</param>
/// <param name="solvingRole">
/// <list type="bullet">
/// <item>
/// <see cref="Enums.SolvingRole.Equation"/> when this defines a quantity the solver may compute
/// (<c>mass_in == mass_out</c>).
/// </item>
/// <item>
/// <see cref="Enums.SolvingRole.Coherence"/> when it asserts that
/// two independently computed routes to one quantity agree (<c>T_eos == T_path</c>).
/// </item>
/// <item>
/// <see cref="Enums.SolvingRole.Requirement"/> when it checks a value against a criterion
/// (<c>measured_T == design_T</c>).
/// </item>
/// </list>
/// </param>
public class EqualityOperator(AgreementRule agreementRule, SolvingRole solvingRole)
    : CommutativeOperatorBase
{
    /// <summary>Nominal agreement: the two reported values are the same number.</summary>
    public static readonly IReadOnlyList<ComparisonRule> NominalRules =
        [new(Landmark.Nominal, MustBe.EqualTo, Landmark.Nominal)];

    protected override BinaryOperatorType Type => BinaryOperatorType.Equality;

    /// <summary>How strictly this instance reads "equal".</summary>
    /// <remarks>
    /// Stored, and part of the operator's state, so the reading survives a round trip. Named
    /// <c>Agreement</c> rather than repeating the type name, which would shadow it inside this class.
    /// </remarks>
    public AgreementRule Agreement { get; } = agreementRule;

    /// <summary><inheritdoc/></summary>
    /// <remarks>
    /// <para>
    /// One rule, applied three times: <b>take the symbol of the operator asserting the same condition and insert
    /// an <c>=</c> at its centre</b>. <c>·=·</c> → <c>·==·</c>, <c>{·=·}</c> → <c>{·==·}</c>,
    /// <c>{&gt;&lt;}</c> → <c>{&gt;=&lt;}</c>. No member of the family is a special case, which is what a
    /// notation is for.
    /// </para>
    /// <para>
    /// Not the conventional <c>==</c>, which is silent about which statistic participates; <c>·==·</c> says the
    /// reported values and nothing else. See <c>BinaryOperators/OPERATORS.md</c> for the notation in full.
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

    /// <summary><inheritdoc/></summary>
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
            new(Landmark.Nominal, MustBe.GreaterThanOrEqualTo, Landmark.LowerBound),
            new(Landmark.Nominal, MustBe.LessThanOrEqualTo, Landmark.UpperBound),
            new(Landmark.LowerBound, MustBe.LessThanOrEqualTo, Landmark.Nominal),
            new(Landmark.UpperBound, MustBe.GreaterThanOrEqualTo, Landmark.Nominal),
        ],
        AgreementRule.Overlapping =>
        [
            new(Landmark.UpperBound, MustBe.GreaterThanOrEqualTo, Landmark.LowerBound),
            new(Landmark.LowerBound, MustBe.LessThanOrEqualTo, Landmark.UpperBound),
        ],
        _ => throw new ArgumentOutOfRangeException(
            nameof(agreementRule), agreementRule, "Unknown agreement rule."),
    };

    /// <inheritdoc/>
    public override BinaryOperatorSnapshot GetSnapshot() => base.GetSnapshot() with { Agreement = Agreement };
}

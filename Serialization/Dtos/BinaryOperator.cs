using Calcusystem.DimensionedExpression.BinaryOperators;
using Calcusystem.DimensionedExpression;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.Measurement.Enums;

﻿using Calcusystem.Serialization.Interfaces;

namespace Calcusystem.Serialization.Dtos;

public class BinaryOperator : ISerializedObject
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string? Name { get; init; }
    public required string? Description { get; init; }
    public required string LhsId { get; init; }
    public required string RhsId { get; init; }

    /// <summary>Whether this relationship determines a value rather than merely checking one.</summary>
    public SolvingRole SolvingRole { get; init; }

    /// <summary>How strictly an equality reads "equal"; absent for every other operator.</summary>
    public AgreementRule? Agreement { get; init; }

    /// <summary>
    /// The comparison a simple comparison asserts, as its three parts; absent for every other operator.
    /// </summary>
    /// <remarks>
    /// Written out rather than as the generated symbol. The symbol is presentation — derived from these — and
    /// a wire format that stored it would be parsing its own notation back on load.
    /// </remarks>
    public Landmark? RuleLhs { get; init; }

    /// <inheritdoc cref="RuleLhs"/>
    public ComparisonType? RuleComparison { get; init; }

    /// <inheritdoc cref="RuleLhs"/>
    public Landmark? RuleRhs { get; init; }

    public Provenance? Provenance { get; init; }
}

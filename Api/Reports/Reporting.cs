using Calcusystem.Analysis.Outcomes;
using Calcusystem.Authoring.Enums;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.Measurement.Comparison;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Serialization.Mappers;

namespace Calcusystem.Api.Reports;

/// <summary>Turns an analysis outcome into the shape served for it.</summary>
public static class Reporting
{
    /// <summary>The decomposition, with the count the flattened system carries.</summary>
    /// <param name="flat">The flattened system the decomposition came from.</param>
    /// <param name="decomposition">Its parts.</param>
    public static DecompositionReport Report(FlatSystem flat, Decomposition decomposition) => new(
        flat.DegreesOfFreedom,
        flat.Determination,
        decomposition.IsWellPosed,
        Report(decomposition.Underdetermined),
        Report(decomposition.WellDetermined),
        Report(decomposition.Overdetermined),
        flat.RedundantEquations.Select(e => e.Relationship.Id).ToList(),
        flat.UnknownsWithNoEquation.Select(v => v.Id).ToList());

    /// <summary>What the calculation produced.</summary>
    public static CalculationReport Report(Calculation calculation) => new(
        calculation.IsComplete,
        calculation.AllRelationshipsHold,
        calculation.Values.Select(pair => new ValueReport(pair.Key.Id, Report(pair.Value))).ToList(),
        calculation.Unresolved.Select(e => e.Id).ToList(),
        calculation.MissingValues.Select(v => v.Id).ToList(),
        calculation.Outcomes.Select(Report).ToList());

    /// <summary>Every choice a client can offer an author.</summary>
    /// <remarks>
    /// Read off the enums, so a kind added to one is offered without this being touched.
    /// </remarks>
    public static VocabularyReport Vocabulary() => new(
        Enum.GetNames<ExpressionDraftType>(),
        Enum.GetNames<BinaryOperatorType>(),
        Enum.GetNames<SolvingRole>(),
        Enum.GetNames<AgreementRule>(),
        Enum.GetNames<Landmark>(),
        Enum.GetNames<MustBe>(),
        Enum.GetNames<UncertaintyCorrelation>(),
        FundamentalDimension.All.Select(d => new DimensionReport(d.Symbol, d.Name)).ToList());

    private static BlockReport Report(SystemBlock block) => new(
        block.DegreesOfFreedom,
        block.Determination,
        block.Unknowns.Select(Report).ToList(),
        block.Equations.Select(Report).ToList());

    private static EquationReport Report(Equation equation) => new(
        equation.Relationship.Id,
        equation.Unknowns.Select(v => v.Id).ToList());

    private static UnknownReport Report(Variable variable) => new(
        variable.Id,
        variable.Symbol,
        Encode(variable.Dimensionality));

    private static OutcomeReport Report(RelationshipOutcome outcome) => new(
        outcome.Relationship.Id,
        outcome.Relationship.Symbol,
        outcome.IsSatisfied,
        outcome.IsViolation,
        outcome.IsInconsistency,
        outcome.Lhs is { } lhs ? Report(lhs) : null,
        outcome.Rhs is { } rhs ? Report(rhs) : null);

    private static MeasurandReport Report(Measurand measurand) => new(
        measurand.KmsValue,
        Encode(measurand.Dimensionality),
        measurand.KmsUpperAbsoluteUncertainty,
        measurand.KmsLowerAbsoluteUncertainty);

    private static string Encode(Dimensionality dimensionality) =>
        DimensionalityCodec.Encode(dimensionality.GetSnapshot());
}

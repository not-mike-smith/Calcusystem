using Calcusystem.Analysis.Outcomes;
using Calcusystem.Authoring.Enums;
using Calcusystem.DimensionedExpression.Enums;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.Measurement.Comparison;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Units;
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

    /// <summary>Every unit, grouped by what it measures.</summary>
    /// <remarks>
    /// Grouped from the unit lists rather than read off <c>Lists.UnitTypes.ByDimensionality</c>, which keys a
    /// dictionary by dimensionality and so holds one list per dimensionality. Two lists sharing one — which
    /// only the angle dimension on torque currently prevents — would be a lost list there and a merged group
    /// here, and a picker wants both sets of units either way.
    /// </remarks>
    public static UnitCatalogueReport Units() => new(
        Lists.UnitTypes.All
            .SelectMany(list => list.All)
            .GroupBy(unit => Encode(unit.Dimensionality))
            .Select(group => new DimensionalityUnitsReport(
                group.Key,
                group.FirstOrDefault(IsBase)?.Symbol,
                group.Select(Report).ToList()))
            .ToList());

    /// <summary>The unit a KMS value is already in.</summary>
    private static bool IsBase(UnitOfMeasure unit) =>
        unit.ConvertFromKmsValue(0) == 0 && unit.ConvertFromKmsValue(1) == 1;

    /// <remarks>
    /// <para>
    /// The conversion is measured, not read: an affine function is determined by two points, and probing the
    /// unit's own method keeps this from being a second copy of a rule <c>Measurement</c> owns.
    /// </para>
    /// <para>
    /// The slope comes from the offset unit's <c>DeltaUnit</c> where there is one, rather than from
    /// <c>from(1) - from(0)</c>. Differencing two readings either side of a large offset cancels most of their
    /// significant digits — it put °F's slope out by a part in 10¹⁴ — and the delta unit is the same scale
    /// without the offset, which is also exactly what a band is converted by.
    /// </para>
    /// </remarks>
    private static UnitReport Report(UnitOfMeasure unit)
    {
        var delta = unit is OffsetUnitOfMeasure offset ? offset.DeltaUnit : unit;

        return new UnitReport(
            unit.Symbol,
            delta.ConvertFromKmsValue(1),
            unit.ConvertFromKmsValue(0),
            unit is OffsetUnitOfMeasure named ? named.DeltaUnit.Symbol : null);
    }

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

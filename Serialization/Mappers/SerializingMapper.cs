using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Snapshots;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Snapshots;
using Calcusystem.Measurement.Uncertainties;

namespace Calcusystem.Serialization.Mappers;

/// <summary>
/// Maps a live <see cref="ExpressionSystem"/> to flat, id-referenced DTOs.
/// </summary>
/// <remarks>
/// Reads nothing but snapshots. Every domain type hands out a snapshot; this class decides only how that data
/// is labelled and laid out on the wire. It never touches an expression's children, an operator's operands, or a
/// value's internals directly.
/// </remarks>
public class SerializingMapper
{
    public Dtos.ExpressionSystem Map(ExpressionSystem system)
    {
        var snapshot = system.GetSnapshot();

        var value = new Dtos.ExpressionSystem
        {
            Id = snapshot.Id,
            Type = nameof(ExpressionSystem),
            Name = snapshot.Name,
            Description = snapshot.Description,
        };

        value.Variables.AddRange(system.Variables.Select(MapVariable));

        foreach (var dto in system.DerivedExpressions.Select(MapDerivedExpression))
        {
            switch (dto)
            {
                case Dtos.SingleDerivedVariable single: value.SingleDerivedVariables.Add(single); break;
                case Dtos.ListDerivedVariable list: value.ListDerivedVariables.Add(list); break;
                case Dtos.PairDerivedVariable pair: value.PairDerivedVariables.Add(pair); break;
            }
        }

        value.Relationships.AddRange(system.Relationships.Select(Map));
        return value;
    }

    public Dtos.SingleVariable MapVariable(Variable v)
    {
        var snapshot = v.GetSnapshot();

        return new Dtos.SingleVariable
        {
            Id = snapshot.Id,
            Type = nameof(Variable),
            Symbol = snapshot.Symbol,
            Dimensionality = DimensionalityCodec.Encode(snapshot.Dimensionality),
            KmsValue = snapshot.Value?.Quantity.KmsValue,
            Uncertainty = snapshot.Value is { } value ? Map(value.Uncertainty) : null,
            Provenance = snapshot.Provenance is { } provenance ? Map(provenance) : null,
        };
    }

    /// <remarks>
    /// The type switch is only here to pick which snapshot to ask for — the kind discriminator inside that
    /// snapshot, not this switch, is what determines the wire name.
    /// </remarks>
    private Dtos.ExpressionBase MapDerivedExpression(IExpression expression) => expression switch
    {
        ReciprocalExpression x => Map(x.GetSnapshot()),
        NegatedExpression x => Map(x.GetSnapshot()),
        SqrtExpression x => Map(x.GetSnapshot()),
        ExponentialExpression x => Map(x.GetSnapshot()),
        NaturalLogExpression x => Map(x.GetSnapshot()),
        ProductExpression x => Map(x.GetSnapshot()),
        SumExpression x => Map(x.GetSnapshot()),
        QuotientExpression x => Map(x.GetSnapshot()),
        _ => throw new NotImplementedException(
            $"No mapping for derived expression of type {expression.GetType().Name}")
    };

    private Dtos.SingleDerivedVariable Map(UnaryExpressionSnapshot snapshot) => new()
    {
        Id = snapshot.Id,
        Type = WireNames.Of(snapshot.Type),
        InnerId = snapshot.InnerId,
    };

    private Dtos.ListDerivedVariable Map(NaryExpressionSnapshot snapshot) => new()
    {
        Id = snapshot.Id,
        Type = WireNames.Of(snapshot.Type),
        InnerIds = snapshot.InnerIds.ToList(),
        UncertaintyCorrelation = snapshot.UncertaintyCorrelation,
    };

    private Dtos.PairDerivedVariable Map(BinaryExpressionSnapshot snapshot) => new()
    {
        Id = snapshot.Id,
        Type = WireNames.Of(snapshot.Type),
        InnerId1 = snapshot.InnerId1,
        InnerId2 = snapshot.InnerId2,
        UncertaintyCorrelation = snapshot.UncertaintyCorrelation,
    };

    public Dtos.BinaryOperator Map(IBinaryOperator op)
    {
        var snapshot = op.GetSnapshot();

        return new Dtos.BinaryOperator
        {
            Id = snapshot.Id,
            Type = WireNames.Of(snapshot.Type),
            Name = snapshot.Name,
            Description = snapshot.Description,
            LhsId = snapshot.LhsId,
            RhsId = snapshot.RhsId,
            SolvingRole = snapshot.SolvingRole,
            Agreement = snapshot.Agreement,
            RuleLhs = snapshot.Rule?.Lhs,
            RuleMustBe = snapshot.Rule?.MustBe,
            RuleRhs = snapshot.Rule?.Rhs,
            Provenance = snapshot.Provenance is { } provenance ? Map(provenance) : null,
        };
    }

    private Dtos.Provenance Map(ProvenanceSnapshot snapshot) => new()
    {
        Id = snapshot.Id,
        Type = WireNames.Of(snapshot.Type),
        InstrumentId = snapshot.InstrumentId,
        CalibrationDate = snapshot.CalibrationDate,
        Citation = snapshot.Citation,
        Url = snapshot.Url,
        Year = snapshot.Year,
        SpecReference = snapshot.SpecReference,
        ModelName = snapshot.ModelName,
        FittingReference = snapshot.FittingReference,
    };

    private Dtos.Uncertainty Map(UncertaintySnapshot snapshot) => snapshot.Type switch
    {
        UncertaintyType.Symmetric => new Dtos.Uncertainty
        {
            Type = nameof(SymmetricUncertainty),
            IsStoredAsAbs = snapshot.IsStoredAsAbs,
            Magnitude = snapshot.UpperMagnitude,
        },
        UncertaintyType.Asymmetric => new Dtos.Uncertainty
        {
            Type = nameof(AsymmetricUncertainty),
            IsStoredAsAbs = snapshot.IsStoredAsAbs,
            UpperMagnitude = snapshot.UpperMagnitude,
            LowerMagnitude = snapshot.LowerMagnitude,
        },
        _ => throw new NotImplementedException($"No mapping for uncertainty shape {snapshot.Type}")
    };
}

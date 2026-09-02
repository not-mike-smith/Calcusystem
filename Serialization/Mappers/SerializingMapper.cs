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
/// Reads nothing but state. Every domain type hands out a state record; this class decides only how that state
/// is labelled and laid out on the wire. It never touches an expression's children, an operator's operands, or a
/// value's internals directly.
/// </remarks>
public class SerializingMapper
{
    public Dtos.ExpressionSystem Map(ExpressionSystem system)
    {
        var state = system.GetSnapshot();

        var value = new Dtos.ExpressionSystem
        {
            Id = state.Id,
            Type = nameof(ExpressionSystem),
            Name = state.Name,
            Description = state.Description,
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
        var state = v.GetSnapshot();

        return new Dtos.SingleVariable
        {
            Id = state.Id,
            Type = nameof(Variable),
            Symbol = state.Symbol,
            Dimensionality = DimensionalityCodec.Encode(state.Dimensionality),
            KmsValue = state.Value?.Quantity.KmsValue,
            Uncertainty = state.Value is { } value ? Map(value.Uncertainty) : null,
            Provenance = state.Provenance is { } provenance ? Map(provenance) : null,
        };
    }

    /// <remarks>
    /// The type switch is only here to pick which state record to ask for — the kind discriminator inside that
    /// state, not this switch, is what determines the wire name.
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

    private Dtos.SingleDerivedVariable Map(UnaryExpressionSnapshot state) => new()
    {
        Id = state.Id,
        Type = WireNames.Of(state.Type),
        InnerId = state.InnerId,
    };

    private Dtos.ListDerivedVariable Map(NaryExpressionSnapshot state) => new()
    {
        Id = state.Id,
        Type = WireNames.Of(state.Type),
        InnerIds = state.InnerIds.ToList(),
        UncertaintyPropagation = state.UncertaintyPropagation,
    };

    private Dtos.PairDerivedVariable Map(BinaryExpressionSnapshot state) => new()
    {
        Id = state.Id,
        Type = WireNames.Of(state.Type),
        InnerId1 = state.InnerId1,
        InnerId2 = state.InnerId2,
        UncertaintyPropagation = state.UncertaintyPropagation,
    };

    public Dtos.BinaryOperator Map(IBinaryOperator op)
    {
        var state = op.GetSnapshot();

        return new Dtos.BinaryOperator
        {
            Id = state.Id,
            Type = WireNames.Of(state.Type),
            Name = state.Name,
            Description = state.Description,
            LhsId = state.LhsId,
            RhsId = state.RhsId,
            SolvingRole = state.SolvingRole,
            Agreement = state.Agreement,
            RuleLhs = state.Rule?.Lhs,
            RuleMustBe = state.Rule?.MustBe,
            RuleRhs = state.Rule?.Rhs,
            Provenance = state.Provenance is { } provenance ? Map(provenance) : null,
        };
    }

    private Dtos.Provenance Map(ProvenanceSnapshot state) => new()
    {
        Id = state.Id,
        Type = WireNames.Of(state.Type),
        InstrumentId = state.InstrumentId,
        CalibrationDate = state.CalibrationDate,
        Citation = state.Citation,
        Url = state.Url,
        Year = state.Year,
        SpecReference = state.SpecReference,
        ModelName = state.ModelName,
        FittingReference = state.FittingReference,
    };

    private Dtos.Uncertainty Map(UncertaintySnapshot state) => state.Type switch
    {
        UncertaintyType.Symmetric => new Dtos.Uncertainty
        {
            Type = nameof(SymmetricUncertainty),
            IsStoredAsAbs = state.IsStoredAsAbs,
            Magnitude = state.UpperMagnitude,
        },
        UncertaintyType.Asymmetric => new Dtos.Uncertainty
        {
            Type = nameof(AsymmetricUncertainty),
            IsStoredAsAbs = state.IsStoredAsAbs,
            UpperMagnitude = state.UpperMagnitude,
            LowerMagnitude = state.LowerMagnitude,
        },
        _ => throw new NotImplementedException($"No mapping for uncertainty shape {state.Type}")
    };
}

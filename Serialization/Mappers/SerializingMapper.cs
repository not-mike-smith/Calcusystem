using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.State;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.State;

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
        var state = system.GetState();

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
        var state = v.GetState();

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
        ReciprocalExpression x => Map(x.GetState()),
        NegatedExpression x => Map(x.GetState()),
        SqrtExpression x => Map(x.GetState()),
        ExponentialExpression x => Map(x.GetState()),
        NaturalLogExpression x => Map(x.GetState()),
        ProductExpression x => Map(x.GetState()),
        SumExpression x => Map(x.GetState()),
        QuotientExpression x => Map(x.GetState()),
        _ => throw new NotImplementedException(
            $"No mapping for derived expression of type {expression.GetType().Name}")
    };

    private Dtos.SingleDerivedVariable Map(UnaryExpressionState state) => new()
    {
        Id = state.Id,
        Type = WireNames.Of(state.Kind),
        InnerId = state.InnerId,
    };

    private Dtos.ListDerivedVariable Map(NaryExpressionState state) => new()
    {
        Id = state.Id,
        Type = WireNames.Of(state.Kind),
        InnerIds = state.InnerIds.ToList(),
        ErrorPropagation = state.ErrorPropagation,
    };

    private Dtos.PairDerivedVariable Map(BinaryExpressionState state) => new()
    {
        Id = state.Id,
        Type = WireNames.Of(state.Kind),
        InnerId1 = state.InnerId1,
        InnerId2 = state.InnerId2,
        ErrorPropagation = state.ErrorPropagation,
    };

    public Dtos.BinaryOperator Map(IBinaryOperator op)
    {
        var state = op.GetState();

        return new Dtos.BinaryOperator
        {
            Id = state.Id,
            Type = WireNames.Of(state.Kind),
            Name = state.Name,
            Description = state.Description,
            LhsId = state.LhsId,
            RhsId = state.RhsId,
            SolvingRole = state.SolvingRole,
            Provenance = state.Provenance is { } provenance ? Map(provenance) : null,
        };
    }

    private Dtos.Provenance Map(ProvenanceState state) => new()
    {
        Id = state.Id,
        Type = WireNames.Of(state.Kind),
        InstrumentId = state.InstrumentId,
        CalibrationDate = state.CalibrationDate,
        Citation = state.Citation,
        Url = state.Url,
        Year = state.Year,
        SpecReference = state.SpecReference,
        ModelName = state.ModelName,
        FittingReference = state.FittingReference,
    };

    private Dtos.Uncertainty Map(UncertaintyState state) => state.Shape switch
    {
        UncertaintyShape.Symmetric => new Dtos.Uncertainty
        {
            Type = nameof(SymmetricUncertainty),
            IsStoredAsAbs = state.IsStoredAsAbs,
            Magnitude = state.UpperMagnitude,
        },
        UncertaintyShape.Asymmetric => new Dtos.Uncertainty
        {
            Type = nameof(AsymmetricUncertainty),
            IsStoredAsAbs = state.IsStoredAsAbs,
            UpperMagnitude = state.UpperMagnitude,
            LowerMagnitude = state.LowerMagnitude,
        },
        _ => throw new NotImplementedException($"No mapping for uncertainty shape {state.Shape}")
    };
}

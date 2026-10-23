using Calcusystem.Measurement.Enums;
using Calcusystem.Measurement.Snapshots;
using Calcusystem.Measurement.Uncertainties;

namespace Calcusystem.Serialization.Mappers;

/// <summary>Reads and writes the flat uncertainty DTO.</summary>
public static class UncertaintyCodec
{
    /// <summary>The wire form of <paramref name="snapshot"/>.</summary>
    /// <exception cref="NotImplementedException">The snapshot names a shape with no wire form.</exception>
    public static Dtos.Uncertainty ToWire(UncertaintySnapshot snapshot) => snapshot.Type switch
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

    /// <summary>The snapshot <paramref name="uncertainty"/> describes, or an exact one where it is null.</summary>
    /// <exception cref="InvalidOperationException">A field the named shape requires is absent.</exception>
    /// <exception cref="NotImplementedException">The <c>Type</c> names no known shape.</exception>
    public static UncertaintySnapshot FromWire(Dtos.Uncertainty? uncertainty)
    {
        if (uncertainty is null) return UncertaintySnapshot.Symmetric(false, 0);

        return uncertainty.Type switch
        {
            nameof(SymmetricUncertainty) => UncertaintySnapshot.Symmetric(
                uncertainty.IsStoredAsAbs,
                Required(uncertainty.Magnitude, nameof(uncertainty.Magnitude), uncertainty.Type)),

            nameof(AsymmetricUncertainty) => UncertaintySnapshot.Asymmetric(
                uncertainty.IsStoredAsAbs,
                Required(uncertainty.UpperMagnitude, nameof(uncertainty.UpperMagnitude), uncertainty.Type),
                Required(uncertainty.LowerMagnitude, nameof(uncertainty.LowerMagnitude), uncertainty.Type)),

            _ => throw new NotImplementedException(
                $"No deserialization method defined for uncertainty type {uncertainty.Type}")
        };
    }

    /// <summary>
    /// Reads a field of the flat uncertainty DTO that is required for the shape named by its discriminator.
    /// </summary>
    /// <remarks>Refused rather than defaulted — a missing magnitude would silently change the interval.</remarks>
    private static double Required(double? value, string field, string type) =>
        value ?? throw new InvalidOperationException(
            $"Uncertainty of type {type} is missing required field {field}.");
}

using Measurement.Models;

namespace Measurement.Factories;

public class Deserializer
{
    public Delta? DeserializeDelta(Dimensionality dimensionality, double? kmsValue, double? relativeError)
    {
        return Delta.Deserialize(dimensionality, kmsValue, relativeError);
    }

    public Magnitude? DeserializeMagnitude(Dimensionality dimensionality, double? kmsValue, double? relativeError)
    {
        return Magnitude.Deserialize(dimensionality, kmsValue, relativeError);
    }
}

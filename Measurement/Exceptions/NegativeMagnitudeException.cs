using System;

namespace Calcusystem.Measurement.Exceptions;

public class NegativeMagnitudeException : InvalidOperationException
{
    internal NegativeMagnitudeException(string message) : base(message)
    {

    }
}

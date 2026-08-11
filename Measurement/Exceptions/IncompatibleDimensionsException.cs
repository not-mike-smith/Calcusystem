using System;

namespace Calcusystem.Measurement.Exceptions;

public class IncompatibleDimensionsException : InvalidOperationException
{
    public IncompatibleDimensionsException(string message) : base(message)
    {

    }
}

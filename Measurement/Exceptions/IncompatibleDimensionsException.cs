using System;

namespace Measurement.Exceptions;

public class IncompatibleDimensionsException : InvalidOperationException
{
    public IncompatibleDimensionsException(string message) : base(message)
    {

    }
}

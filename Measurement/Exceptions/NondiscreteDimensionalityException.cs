using System;

namespace Measurement.Exceptions;

public class NondiscreteDimensionalityException : InvalidOperationException
{
    internal NondiscreteDimensionalityException(string message) : base(message)
    {

    }
}

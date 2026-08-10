using System;

namespace Calcusystem.Measurement.Exceptions;

public class NondiscreteDimensionalityException : InvalidOperationException
{
    internal NondiscreteDimensionalityException(string message) : base(message)
    {

    }
}

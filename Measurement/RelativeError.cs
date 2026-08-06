namespace Measurement;

/// <summary>
/// An error expressed as a fraction of the value it qualifies — 0.01 meaning one percent.
/// </summary>
/// <remarks>
/// A type rather than a bare <see langword="double"/> so that a call site cannot be read two ways. Given a mass
/// in kilograms, <c>0.001</c> could as easily mean one gram as one tenth of a percent; <c>0.1.Percent()</c> and
/// <c>1.0.Units(Mass.Gram)</c> cannot be confused for one another. Build one with the
/// <see cref="Extensions.DoubleExtensions.Percent"/> or <see cref="Extensions.DoubleExtensions.Fraction"/>
/// extensions rather than by hand.
/// </remarks>
/// <param name="Value">The error as a fraction: 0.01 is one percent.</param>
public readonly record struct RelativeError(double Value);

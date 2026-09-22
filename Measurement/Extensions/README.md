# Calcusystem.Measurement.Extensions

The shorthand that makes describing a measurement read like one.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `QuantityExtensions` | static class | `Units` — a bare number plus a unit becomes a `Quantity`. |
| `DoubleExtensions` | static class | `Percent`, `Fraction` — a number becomes a `RelativeUncertainty`. Plus `SafeDivide` and `RootSumOfSquares`. |
| `IntExponentExtensions` | static class | `ToSuperscript` — renders an exponent as `⁻²`. |

## Start here

```csharp
using Calcusystem.Measurement.Extensions;

var length = 3.0.Units(Length.Meter);              // same as Length.Meter.Quantity(3.0)
var tol    = 1.0.Percent();                        // RelativeUncertainty, not a double
var frac   = 0.01.Fraction();                      // the same value, stated the other way
```

`Percent` and `Fraction` exist so that a relative uncertainty can never be passed as a bare
number. `1.0.Percent()` and `0.01.Fraction()` are equal; `1.0.Fraction()` is 100%, and the
difference is visible at the call site rather than buried in a convention.

## Guarantees

- **`Percent` divides by 100; `Fraction` does not.** Both return the same type, so the choice
  is made once, where the number is written.
- **`SafeDivide` returns zero rather than infinity when the denominator is zero.** This is what
  lets an absolute uncertainty be reported as a relative one on a value of zero without
  producing a non-finite result.
- **`RootSumOfSquares` is the quadrature sum** used for uncorrelated propagation; the generic
  overload projects first, so a caller need not materialize an intermediate list.

## Surprises

- **`Units` is a second spelling of `UnitOfMeasure.Quantity`.** `3.0.Units(Length.Meter)` and
  `Length.Meter.Quantity(3.0)` produce the same value. The first reads better in prose-like
  code, the second in a table of unit definitions; both are kept deliberately.
- **`ToSuperscript` is presentation, not arithmetic.** It exists so dimensionality symbols print
  as `M·L·T⁻²`, and is unrelated to `ToPower`.

## What does not belong here

- Anything that changes what a value *means*. These add spelling, not semantics.
- Uncertainty construction beyond the magnitude → `Uncertainties/Uncertainty`

## Related

`Primitives/` (`Quantity`) · `Uncertainties/` (`RelativeUncertainty`) · `Units/`.

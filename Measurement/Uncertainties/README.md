# Calcusystem.Measurement.Uncertainties

The uncertainty interval around a measured value, and the vocabulary for describing one.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `Uncertainty` | static class | The construction vocabulary. Every uncertainty a caller makes comes from here. |
| `SymmetricUncertainty` | sealed class | Equal magnitude above and below the nominal value. |
| `AsymmetricUncertainty` | sealed class | Independent upper and lower magnitudes. |
| `RelativeUncertainty` | readonly record struct | A fraction *of* the value, as distinct from an amount. |
| `ConservativeGaussianPropagator` | class | Combines child uncertainties when an expression is evaluated. |

## Start here

Most callers never name a type in this namespace. They start from a `Quantity`:

```csharp
using Calcusystem.Measurement.Extensions;   // Percent, Fraction
using Calcusystem.Measurement.Units;

var mass = Mass.Kilogram.Quantity(2).WithUncertainty(1.0.Percent());
var span = Length.Meter.Quantity(3).WithAsymmetricUncertainty(2.0.Percent(), 0.5.Percent());
var exact = Time.Second.Quantity(60).WithoutUncertainty();
```

Building one directly is the same set of choices, spelled out:

```csharp
Uncertainty.Exact();                                          // none at all
Uncertainty.Relative(1.0.Percent());                          // symmetric, a fraction
Uncertainty.Absolute(Mass.Gram.Quantity(5));                  // symmetric, an amount
Uncertainty.Relative(2.0.Percent(), 0.5.Percent());           // asymmetric, fractions
Uncertainty.Absolute(Mass.Gram.Quantity(5), Mass.Gram.Quantity(3));   // asymmetric, amounts
```

`Relative` takes a `RelativeUncertainty`, never a bare `double`. A number on its own cannot
say whether it means a fraction of the value or an amount of it, and that is the ambiguity
this library exists to remove. Use `1.0.Percent()` or `0.01.Fraction()` to make one.

## Guarantees

- **Storage form is preserved.** An uncertainty given as relative stays relative; one given
  as absolute stays absolute. The two answers diverge at and near zero — a relative
  uncertainty on a value of zero is zero, an absolute one is not — so the form is recorded
  rather than normalised to one of them.
- **Both readings are always available.** `RelativeUncertainty(nominal)` and
  `AbsoluteUncertainty(nominal)` answer whichever form was stored, converting on demand.
- **A non-finite magnitude sets no scale.** `MeasurandComparer` skips it, so an unbounded
  uncertainty does not make every value compare equal to every other.
- **Negation preserves the stored form.** `Negated` returns a symmetric uncertainty
  unchanged, because flipping a value's sign changes neither its fraction nor its amount.

## Surprises

- **Rebuilding a stored uncertainty does not start here.** `Factories.UncertaintyFactory` is
  that door, kept separate so nobody describing a measurement is offered a storage-form flag
  that only means something to a deserializer.
- **`Uncertainty` the static class and `IUncertainty` the contract are different things.**
  The class only makes instances; it is not an implementation of the interface.
- **Asymmetric uncertainty answers directionally.** `UpperAbsoluteUncertainty` and
  `LowerAbsoluteUncertainty` differ, and the undirected `AbsoluteUncertainty` reports the
  larger of the two. Comparisons that care about a direction ask for that side by name.

## What does not belong here

- The `IUncertainty` and `ISymmetricUncertainty` contracts → `Interfaces/`
- Reconstruction from stored form → `Factories/`, `Snapshots/`
- Whether operands are correlated → `Enums/UncertaintyCorrelation`
- Deciding whether two uncertain values agree → `Comparison/`

## Related

`Interfaces/` · `Factories/` · `Snapshots/` · `Extensions/` (`Percent`, `Fraction`) ·
`Primitives/` (`Measurand` pairs a value with one of these).

See the [assembly README](../README.md) for KMS normalization and the uncertainty model as a whole.

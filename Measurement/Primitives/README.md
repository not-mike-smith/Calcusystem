# Calcusystem.Measurement.Primitives

The value types everything else is built from: what a quantity is, what it is a quantity *of*,
and what it is worth once uncertainty is attached.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `Measurand` | class | A `Quantity` paired with an `IUncertainty`. The currency of the library. |
| `Quantity` | readonly struct | A KMS value and a `Dimensionality`. No uncertainty. |
| `Dimensionality` | readonly struct | Fundamental dimension → integer exponent, with algebra. |
| `FundamentalDimension` | class | The nine base dimensions: mass, length, time, temperature, current, angle, amount, luminous intensity, currency. |

## Start here

```csharp
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Units;

var mass  = Mass.Kilogram.Quantity(2).WithUncertainty(1.0.Percent());
var accel = Length.Meter.Quantity(9.81).WithUncertainty(0.5.Percent())
                  .DividedBy(Time.Second.Quantity(1).WithoutUncertainty().Times(
                             Time.Second.Quantity(1).WithoutUncertainty()));

var force = mass.Times(accel);   // M·L·T⁻², uncertainty combined
force.In(Force.PoundForce);      // read back in any compatible unit
```

Dimensionality is available without any value at all, and composes algebraically:

```csharp
var velocity = Dimensionality.Length / Dimensionality.Time;   // L·T⁻¹
var energy   = Dimensionality.Mass * velocity * velocity;     // M·L²·T⁻²
var back     = energy / 2;                                    // integer root
```

## Guarantees

- **Every value is stored in KMS.** Units matter only at the boundary — supplying a value or
  reading one back. All arithmetic, comparison, and propagation runs on KMS doubles.
- **`Plus` and `Minus` require matching dimensionality** and throw otherwise. `Times` and
  `DividedBy` combine dimensions freely and cannot fail on them.
- **Zero-exponent entries are stripped**, so `Dimensionality.Length / Dimensionality.Length`
  equals `Dimensionless` rather than carrying `L0`.
- **`Dimensionality` has value equality and a usable hash**, so it works as a dictionary key.
- **`measurand[Landmark.UpperBound]` indexes the three statistics**, which is how comparison
  reads a measurand without knowing which landmark it was asked for.

## Surprises

- **`Try*` returns a `NaN`-valued result rather than throwing or returning null.** `TryAdd`,
  `TrySubtract`, and `TryIn` still carry a dimensionality, so an unchecked result propagates a
  `NaN` instead of failing loudly.
- **Exponents are integers, so some roots are refused.** `energy / 3` throws
  `NondiscreteDimensionalityException` rather than producing a fractional exponent.
- **`Dimensionality` carries `Epsilon` and `PlausibleMaximum`, composed from the fundamentals.**
  `Epsilon` is read by `MeasurandComparer` as a near-zero floor. `PlausibleMaximum` is
  `internal` and currently has no reader at all — the composed ceiling is over-permissive,
  because a product over independent maxima cannot see a coupling constraint like the speed of
  light. <!-- TO_REVIEW: this is future work, not a usage note. Keep here, move to the
  assembly README, or drop until PhysicalBounds exists? -->
- **Currency is a fundamental dimension.** It is not physical, but it is dimensionally
  independent and engineering models cost things, so it sits alongside the SI seven plus angle.

## What does not belong here

- Units and conversion factors → `Units/`
- The uncertainty implementations → `Uncertainties/`
- Deciding whether two measurands agree → `Comparison/`

## Related

`Units/` · `Uncertainties/` · `Comparison/` · `Snapshots/` · `Exceptions/`.
See the [assembly README](../README.md) for KMS normalization, which is the invariant every
type here depends on.

# Calcusystem.Measurement.Factories

Where units are defined and where a stored uncertainty is rebuilt.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `UnitFactory` | static class | `Create` — every way a `UnitOfMeasure` comes into existence. |
| `Metric` | class | The SI prefixes, applied to a unit to derive a scaled one. |
| `UncertaintyFactory` | static class | `FromSnapshot` — rebuilds an `IUncertainty` from stored form. |

## Start here

A unit is defined either against KMS directly, or against a unit that already exists:

```csharp
// against KMS: symbol, dimensionality, conversion factor
public static readonly UnitOfMeasure Kilogram = UnitFactory.Create("kg", Dimensionality.Mass, 1);

// against another unit: symbol, scale, reference unit
public static readonly UnitOfMeasure Gram = UnitFactory.Create("g", 0.001, Kilogram);

// by metric prefix
public static readonly UnitOfMeasure Milligram = Metric.m(Gram);

// composed from constituent units and exponents
public static readonly UnitOfMeasure MetersPerSecond =
    UnitFactory.Create((Length.Meter, 1), (Time.Second, -1));
```

Defining against an existing unit is preferred: it keeps one number authoritative, so a
correction to `Kilogram` reaches everything derived from it.

## Guarantees

- **Composition derives all three parts.** Dimensionality, KMS conversion factor, and symbol
  are each reduced from the constituent units, so a composed unit cannot disagree with its
  parts.
- **A metric prefix is a scale and a symbol prefix, nothing more.** `Metric.k(Meter)` is a unit
  of the same dimensionality with a factor of 1000 and the symbol `km`.
- **Offset units are a separate overload.** A zero-point offset is baked in at construction,
  never read from ambient state.

## Surprises

- **`UncertaintyFactory` is here, but it is not construction.** It rebuilds from a stored
  snapshot, including the storage-form flag, and is deliberately kept out of
  `Uncertainties/Uncertainty` so that nobody describing a measurement is offered a flag that
  only means something to a deserializer.
- **Two prefixes mean a thousand and two mean a million.** `M` is Mega (10⁶) in SI and a Roman
  thousand in some engineering conventions, so both are defined and neither is `M` alone:
  `MInRomanNumerals` is 10³, `Mega` is 10⁶, and `MegaMega` (`MM`) is also 10⁶. Reach for the
  one your domain writes.

## What does not belong here

- The unit catalog itself → `Units/`
- The `UnitOfMeasure` type → `Units/`
- Making an uncertainty from a measurement → `Uncertainties/Uncertainty`

## Related

`Units/` · `Uncertainties/` · `Snapshots/` (`UncertaintySnapshot`).
See the [assembly README](../README.md) for KMS normalization, which is what a conversion
factor is relative to.

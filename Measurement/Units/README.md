# Calcusystem.Measurement.Units

Everything about units: the type, the offset variant, the catalog of 43 quantity families,
and the registry that finds them.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `UnitOfMeasure` | class | Symbol, dimensionality, and KMS conversion factor. |
| `OffsetUnitOfMeasure` | class | A `UnitOfMeasure` with a fixed zero-point offset. |
| `UnitList` | abstract class | The contract a quantity family exposes: `All`, `ByName`, `BySymbol`, `Dimensionality`. |
| `ReflectiveUnitList<T>` | abstract class | Implements that contract by reflecting over the derived class's fields. |
| `Lists` | class | The registry of every unit family in the assembly. |
| 43 families | classes | `Mass`, `Length`, `Time`, `Pressure`, `Energy`, `Power`, … |

## Start here

A family exposes its units as static fields, and a `Units` singleton for lookup:

```csharp
using Calcusystem.Measurement.Units;

Mass.Kilogram.Quantity(2.0);          // build a Quantity
Mass.Units.All;                       // every mass unit
Mass.Units.BySymbol["lb"];            // look one up
Mass.Units.Dimensionality;            // M
```

Adding a family is a class, a private constructor, a `Units` singleton, and the unit fields:

```csharp
public class Mass : ReflectiveUnitList<Mass>
{
    private Mass() { }
    public static readonly Mass Units = new();

    public static readonly UnitOfMeasure Kilogram = UnitFactory.Create("kg", Dimensionality.Mass, 1);
    public static readonly UnitOfMeasure Gram     = UnitFactory.Create("g", 0.001, Kilogram);
    public static readonly UnitOfMeasure Milligram = Metric.m(Gram);
}
```

Nothing registers the class anywhere — `ReflectiveUnitList<T>` discovers the fields, and `Lists`
discovers the classes.

## Guarantees

- **One unit is authoritative per family** and everything else is defined against it, so a
  correction to `Kilogram` reaches `Gram`, `Milligram`, and the rest.
- **Conversion happens only at the boundary.** A `UnitOfMeasure` is a factor relative to KMS;
  values in flight are always KMS.
- **Discovery is lazy.** `All`, `ByName`, `BySymbol`, and `Dimensionality` are computed on first
  access, so declaring a family costs nothing until it is used.
- **`Dimensionality` is read from the first unit in the family**, which is sound because a
  family is by definition units of one dimensionality.

## Surprises

- **`OffsetUnitOfMeasure` bakes its offset in at construction; it is not an ambient reading.**
  It covers temperature scales where 0 °C ≠ 0 K, and gauge pressure where zero is *nominal*
  atmospheric pressure (101 325 Pa), not the actual ambient. If real ambient pressure matters,
  correcting for it is the caller's job.
- **An offset unit has a `DeltaUnit`.** A *difference* of 10 °C is not 10 °C on the scale, so
  changes are expressed in the delta unit to avoid re-adding the offset.
- **`UnitList` declares a property whose name is its own type** — `Dimensionality Dimensionality`.
  Inside a derived family, a static field initialiser referring to `Dimensionality.Mass` needs
  `Calcusystem.Measurement.Primitives` in scope, or the inherited property shadows the type and
  the compiler reports `CS0176`.
- **Two prefixes mean a thousand.** `Metric.k` is kilo; `Metric.M` is a Roman thousand
  (`MInRomanNumerals`), while SI mega is `Metric.Mega`. Check which your domain writes.

## What does not belong here

- Creating a `UnitOfMeasure` → `Factories/UnitFactory`, `Factories/Metric`
- `Dimensionality` itself → `Primitives/`
- Anything about uncertainty. A unit is exact.

## Related

`Factories/` · `Primitives/` · `Extensions/` (`Units`).
See the [assembly README](../README.md) for KMS normalization and the offset-unit notes.

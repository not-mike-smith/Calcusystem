# Calcusystem.Measurement.Snapshots

What each measurement type hands out so it can be rebuilt later: enough to reconstruct it, and
nothing more.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `MeasurandSnapshot` | readonly record struct | A `QuantitySnapshot` plus an `UncertaintySnapshot`. |
| `QuantitySnapshot` | readonly record struct | A KMS value and a `DimensionalitySnapshot`. |
| `DimensionalitySnapshot` | readonly record struct | Fundamental dimension → exponent pairs, in canonical order. |
| `UncertaintySnapshot` | readonly record struct | The uncertainty's type, storage form, and magnitudes. |

## Start here

Every type that has one exposes `GetSnapshot()`, and rebuilds through `FromSnapshot`:

```csharp
var snapshot = quantity.GetSnapshot();
var restored = Quantity.FromSnapshot(snapshot);
```

`UncertaintySnapshot` is the exception: the concrete uncertainty is chosen by inspecting the
snapshot, so it rebuilds through `Factories.UncertaintyFactory.FromSnapshot` instead.

## Guarantees

- **`DimensionalitySnapshot.Pairs` comes out in canonical dimension order**, so
  dimensionally-equal values always produce identical output — which is what lets the wire
  encoding be diffed, compared, or hashed.
- **`UncertaintySnapshot` records the storage form**, not just the magnitude. An uncertainty
  given as relative comes back relative.
- **These are mementos, not DTOs.** No wire type-name, no schema version, no encoding. Adding a
  third storage form changes this record and every consumer gets a compile error at its mapping
  site, which is exactly where the decision about migrating old data belongs.

## Surprises

- **`IsStoredAsAbs` crosses the assembly boundary here and nowhere else.** The uncertainty
  classes keep it off their own public surface deliberately — their construction vocabulary
  should not offer a flag that only means something to a deserializer.
- **`UncertaintySnapshot` is built through named statics, not a constructor.**
  `UncertaintySnapshot.Symmetric(...)` and `.Asymmetric(...)` set the type and the magnitudes
  together, so the two cannot disagree.
- **`DimensionalitySnapshot` implements its own `Equals` and `GetHashCode`**, because the
  default record equality on a dictionary field would compare by reference.

## What does not belong here

- Wire shapes and type discriminators → `Calcusystem.Serialization.Dtos`
- The encoding of a dimensionality as a string → `Calcusystem.Serialization.Mappers`
- Reconstruction logic for a polymorphic type → `Factories/`

## Related

`Primitives/` · `Uncertainties/` · `Factories/` · `Enums/` (`UncertaintyType`) ·
`Calcusystem.Core.Interfaces` (`ISnapshotting`).

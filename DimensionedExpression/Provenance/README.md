# Calcusystem.DimensionedExpression.Provenance

Where a value came from, recorded so a report can cite it.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `ProvenanceFactory` | static class | The way every provenance is made, and rebuilt. |
| `MeasuredProvenance` | sealed class | An instrument or sensor reading. |
| `ReferenceProvenance` | sealed class | A literature or tabulated value. |
| `DesignProvenance` | sealed class | An engineer-specified value. |
| `ModelProvenance` | sealed class | An empirically fitted constant inside a constitutive relationship. |

## Start here

```csharp
using Calcusystem.DimensionedExpression.Provenance;

variable.Provenance = ProvenanceFactory.Measured(instrumentId: "PT-101", calibrationDate: cal);
variable.Provenance = ProvenanceFactory.Reference("NIST SRD 69", url: nist, year: 2023);
variable.Provenance = ProvenanceFactory.Design(specReference: "DWG-4412 rev C");
variable.Provenance = ProvenanceFactory.Model("Peng-Robinson", fittingReference: "…");

variable.Provenance?.Summary();   // one line, suitable for a report
```

Every constructor is internal; the factory is the only way in.

## Guarantees

- **Every kind carries an id that round-trips**, so a rebuilt document's citations are the same
  objects they were.
- **`Summary()` is the only rendering.** Each kind formats its own metadata, so a report never
  has to switch on the kind.
- **The metadata is `internal`.** A mapper in another assembly reads it through the snapshot,
  which is why these properties are not public.

## Surprises

- **The kinds differ in metadata, not in behaviour.** None of them changes how a value is
  computed or compared. Provenance is for the audit trail, and choosing the wrong kind is a
  documentation error, not an arithmetic one.
- **`IProvenance` does not implement `ISnapshotting`.** The concrete kind is chosen by
  inspecting the snapshot, and `static abstract FromSnapshot` must be declared on a type the
  caller already knows, so reconstruction goes through `ProvenanceFactory.FromSnapshot`.
- **Uncertainty is not here.** A measured value's uncertainty characterises the instrument, but
  it lives on the `Measurand`. This records *what the source was*, not *how good it was*.

## What does not belong here

- The `IProvenance` contract → `Interfaces/`
- The stored shape → `Snapshots/ProvenanceSnapshot`
- Anything that affects a value or a verdict.

## Related

`Interfaces/` · `Snapshots/` · `Enums/` (`ProvenanceType`) · `Expressions/` (`Variable`, which
carries one).

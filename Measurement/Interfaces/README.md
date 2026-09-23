# Calcusystem.Measurement.Interfaces

The two contracts measurement arithmetic is written against: what an uncertainty interval can
be asked, and how two of them combine.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `IUncertainty` | interface | The interval around a value: its magnitudes, and how it transforms under arithmetic. |
| `ISymmetricUncertainty` | interface | An `IUncertainty` whose upper and lower readings are the same. |
| `IUncertaintyPropagator` | interface | Combines child uncertainties through a sum or a product. |

## Start here

You implement these only to add a new uncertainty form or a new propagation method. To *use*
them, ask an interval for the reading you need:

```csharp
IUncertainty u = measurand.Uncertainty;

u.AbsoluteUncertainty(nominalKmsValue);        // undirected, the larger side
u.UpperAbsoluteUncertainty(nominalKmsValue);   // directional
u.RelativeUncertainty(nominalKmsValue);
```

## Guarantees

- **`ISymmetricUncertainty` supplies the directional members from the undirected ones**, as
  default interface implementations. A symmetric type declares two methods and gets six.
- **Transforms return a new `IUncertainty`**, never mutate. `Reciprocal`, `Negated`, and
  `Exponentiated` each produce the interval that belongs to the transformed value.
- **A propagator takes the correlation assumption as a parameter.** It does not decide whether
  operands are correlated; the model does, and passes it in.

## Surprises

- **Every member takes `nominalKmsValue`, including those that cannot use it.** An uncertainty
  holds one magnitude in one form and does not hold the value it describes, so the caller
  supplies it on each call. The signature is uniform, so `Negated(nominalKmsValue)` asks for a
  value it ignores.
- **`AbsoluteUncertainty` on an asymmetric interval reports the larger side.** Code that cares
  about a direction must ask for that side by name; the undirected reading is deliberately
  conservative.
- **Propagation and correlation are different axes and are always passed together.**
  `IUncertaintyPropagator` is the numerical method and belongs to a calculation;
  `UncertaintyCorrelation` is a statement about where the quantities came from and belongs to
  the model.

## What does not belong here

- The implementations → `Uncertainties/`
- Rebuilding one from stored form → `Factories/UncertaintyFactory`
- The correlation enum → `Enums/UncertaintyCorrelation`

## Related

`Uncertainties/` · `Factories/` · `Enums/` · `Primitives/` (`Measurand`, which pairs a value
with an `IUncertainty`).

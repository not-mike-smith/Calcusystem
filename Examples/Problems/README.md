# Calcusystem.Examples.Problems

The three models. Each is a static class with a `Build()` that returns a finished `WorkedExample` — values,
formulas, checks, and the labels to print them under.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `OrificeDrainTime` | static class | Chemical: Torricelli discharge and the integrated drain time for a prismatic tank. |
| `ParallelDcLoads` | static class | Electrical: three resistive branches on a 24 V bus, against their protective devices. |
| `GyroscopicPrecession` | static class | Mechanical: steady precession of a gimballed flywheel under a constant torque. |
| `Catalogue` | static class | All of them. One list, so the runner and the tests cannot cover different sets. |
| `Inputs` | static class (internal) | Builds a valued leaf: a symbol, a number in a unit, an uncertainty, a provenance. |

## Start here

```csharp
var example = OrificeDrainTime.Build();
var calculation = example.System.Calculate();

calculation.ValueOf(example.Node(OrificeDrainTime.DrainTimeId));   // 50.85 min ± 2.1 %
calculation.Violations;                                            // none here
```

Each class publishes the ids worth reaching for as constants — `DrainTimeId`, `SupplyCurrentId`,
`PrecessionRateId`. Every other node is reachable by the symbol it carries in the problem statement:
`example.Node("A_tank")`, `example.Leaf("V_bus")`.

To see what a half-built model reports, take a value away:

```csharp
example.Leaf(OrificeDrainTime.DischargeCoefficientId).Value = null;
```

## Guarantees

- **Every expression tree is written with the real constructors.** `Inputs` is the only shorthand in this
  folder, it builds leaves only, and its body is the call it replaces. Nothing here hides the API the examples
  exist to show.
- **Every given carries a provenance.** Where a number came from is part of the number, and a test holds every
  given in every example to it.
- **Every readout names a unit of its node's own dimensionality**, so a report can always be produced. A
  mismatch throws on conversion rather than printing something odd.
- **The numbers in the tests are worked by hand**, not read back off a run. A test that recomputes the model's
  own formula asserts only that arithmetic is deterministic.
- **`Catalogue.Build()` builds fresh every time.** A `Variable`'s value is mutable, so a caller withholding one
  must not be editing a model something else is reading.

## Surprises

- **A symbol is also an id.** `Inputs` passes the symbol for both, so `example.Node("v_jet")` works and a
  report can name a quantity the way the problem statement does. Two leaves sharing a symbol would therefore
  collide; none here do.
- **A relationship's id is a sentence.** `"supply breaker holds"`, not a GUID. Ids are free-form, the report
  prints them, and a check that reads as a claim is easier to scan than one that reads as a formula.
- **A limit is exact.** A breaker rating and a batch window are decisions rather than measurements, so they
  carry no uncertainty. The tolerance belongs to the thing being limited.
- **`ParallelDcLoads` is violated on purpose**, and `PowerWrittenWithARepeatedFactor` is deliberately the wrong
  way to write a power — it is not in the model, and exists so a test can hold the two forms against each
  other.
- **The gyroscope model multiplies by one radian.** It is not a fudge factor and it changes no number; see the
  [assembly README](../README.md) for why angle being a dimension requires it.

## What does not belong here

- Printing → `Presentation/`
- A fourth example that shows nothing the three do not
- Problems that need a solver. Nothing here inverts an equation toward an unknown, so every model computes
  forward from values that are all supplied.

## Related

`Presentation/` (`WorkedExample`, `Readout`) · `Calcusystem.Analysis.Extensions` (`Calculate`, `Flatten`,
`Decompose`) · `Calcusystem.DimensionedExpression.BinaryOperators` ([`OPERATORS.md`](../../DimensionedExpression/BinaryOperators/OPERATORS.md)).
See the [assembly README](../README.md) for why every example computes forward, and for the three modeling
lessons these were built to carry.

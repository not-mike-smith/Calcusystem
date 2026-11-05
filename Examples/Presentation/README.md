# Calcusystem.Examples.Presentation

What a worked example is, and how one is turned into a page of text.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `WorkedExample` | record | A problem: a system, plus the labels and units to print it under. |
| `Readout` | record | One line — a node, a unit, a label, and an optional note. |
| `ExampleReport` | static class | Renders a worked example against a calculation of it. |
| `Figures` | static class | Renders one `Measurand` as a value and an uncertainty. |

## Start here

```csharp
Console.WriteLine(ExampleReport.Render(example));
```

Or against a calculation of your own, which is how the runner shows a model with a value withheld:

```csharp
Console.WriteLine(ExampleReport.Render(example, example.System.Calculate()));
```

A report has five blocks: `GIVEN`, `POSED`, `COMPUTED`, `CHECKS`, and `OUTSTANDING` — the last only when
something did not resolve.

## Guarantees

- **`Render` returns a string.** Nothing here writes to the console, so the shape of a report is something a
  test can hold to.
- **Every relationship appears under `CHECKS`, including the ones that could not be judged.** A check that
  never ran prints `?`, which is distinct from the `✓` of one that passed. A relationship missing from the
  report would be indistinguishable from one that held.
- **A withheld value prints as `— withheld —` and an unreachable node as `— unresolved —`.** Neither is left
  blank, and neither is rendered as zero.
- **Columns size to their own block**, so a check whose label is a sentence does not widen the two-word labels
  above it.

## Surprises

- **A node shown twice is compared in the unit it was reported in first.** The gyroscope prints its precession
  rate in rpm and again in °/s; the check against the validity limit reads in rpm, which is the column above
  it. Later listings are ignored for that purpose.
- **A bare count prints with no symbol.** `Dimensionless.One` carries the symbol `unit` so that it has one,
  which is right for a unit list and wrong on a page — a discharge coefficient reads as `0.6100`. Every other
  dimensionless unit keeps its symbol, because there the symbol is the whole of what distinguishes it.
- **`Figures` fixes its significant figures rather than choosing them from the uncertainty.** The rule that
  chooses them lives in `web/src/format/significance.ts`, and a second implementation of it here would be a
  second thing to keep right.
- **A relationship side falls back to KMS with its dimensionality beside it** when the report shows that node
  nowhere. A bare KMS number with no dimension on it is the one reading an engineer cannot check.
- **`WorkedExample.Node` and `.Leaf` throw** rather than returning null. They are used with ids the example
  itself declares, so a miss is a typo rather than a case to handle.

## What does not belong here

- The models → `Problems/`
- Anything that decides *what* is worth reporting. A `WorkedExample` is told its readouts; it does not pick
  them.
- A second output format. If one is ever wanted, `Render` returning a string is the seam.

## Related

`Problems/` · `Calcusystem.Analysis.Outcomes` (`Calculation`, `RelationshipOutcome`) ·
`Calcusystem.Measurement.Units` (`UnitOfMeasure`).
See the [assembly README](../README.md) for what the examples are for.

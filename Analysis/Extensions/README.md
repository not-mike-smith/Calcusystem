# Calcusystem.Analysis.Extensions

The two questions you can ask an `ExpressionSystem`: can it be solved, and what does it
currently compute to.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `SystemFlattener` | static class | `Flatten` — reduces a system to unknowns × equations. |
| `SystemCalculation` | static class | `Calculate` — walks the system once and evaluates everything it can. |

## Start here

```csharp
using Calcusystem.Analysis.Extensions;

var flat = system.Flatten();
var calc = system.Calculate();
```

Both take an optional `overrides` dictionary — values supplied for this question only, without
being written into the model. `Calculate` also takes an optional `IUncertaintyPropagator`,
defaulting to the conservative Gaussian one.

```csharp
var trial = system.Calculate(overrides: new Dictionary<Variable, Measurand> { [mass] = heavier });
```

## Guarantees

- **Both are pure in `(system, overrides)`.** Neither writes to the system, so trial values can
  be probed freely and independent calls parallelize.
- **`Calculate` computes each node exactly once per run**, walking in dependency order. A
  sub-expression shared by three parents costs one evaluation. Prefer it to calling
  `ComputeIfFullyDescribed()` per node, which re-walks to the leaves every time and caches
  nothing.
- **An override makes a variable known without setting it.** `Flatten` excludes overridden
  variables from its unknowns, so the two agree about what is still missing.

## Surprises

- **These are extension methods, so `Analysis.Outcomes` has no constructor you can reach.**
  Every result type there is produced here and nowhere else, which means the namespace holding
  the results is not the one you call. That split is under review and may not survive the
  Milestone 4 structural analysis.
- **Only determining relationships become equations.** Ordering and tolerance relationships
  bound a value rather than producing one, so they never appear in `FlatSystem.Equations`,
  regardless of how many of them a system carries. They are still evaluated by `Calculate` and still
  produce an outcome.

## What does not belong here

- The result types → `Outcomes/`
- `Determination`, the enum → `Enums/`
- Building or editing a system → `Calcusystem.DimensionedExpression.Systems`

## Related

`Outcomes/` (`FlatSystem`, `Calculation`) · `Enums/` (`Determination`).
See the [assembly README](../README.md) for degrees of freedom and what the counts do and do
not promise.

# Calcusystem.DimensionedExpression.Expressions

The nodes a formula is built from: one mutable leaf and nine ways to combine.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `Variable` | class | The leaf. The only node whose value is set rather than computed. |
| `SumExpression` | class | N-ary `+`. Addends must share a dimensionality. |
| `ProductExpression` | class | N-ary `×`. Dimensions combine. |
| `QuotientExpression` | class | `Numerator / Denominator`. |
| `ReciprocalExpression` | class | `1/x`. |
| `NegatedExpression` | class | `−x`. |
| `SqrtExpression` | class | `√x`. |
| `ExponentialExpression` | class | `eˣ` — requires a dimensionless operand. |
| `NaturalLogExpression` | class | `ln x` — requires a dimensionless operand. |
| `ExpressionBase` | abstract class | Identity, the derived walks, and cycle detection. |
| `ComputedExpressionBase` | abstract class | Adds the correlation assumption; not directly mutable. |
| `ExpressionFactory` | static class | Rebuilds a node from its snapshot. |

## Start here

```csharp
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.Measurement.Primitives;

var m = new Variable("m", Dimensionality.Mass);
var a = new Variable("a", Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time));

var force = new ProductExpression([m, a]);

force.Dimensionality;             // M·L·T⁻² — known before any value exists
force.UnsetVariables();           // [m, a]
force.ComputeIfFullyDescribed();  // null until both leaves are set
```

Operands are constructor arguments; the two-child types use required initialisers:

```csharp
var q = new QuotientExpression { Numerator = work, Denominator = time };
```

## Guarantees

- **Structure is fixed at construction.** There is no `AddFactor`, no operand setter, and no
  way to re-point a child. What a leaf is *worth* stays mutable; what the graph *is* does not.
- **Cycles are unconstructible through these types**, because a node's operands must exist
  before it does.
- **Validation happens once, where the operand arrives.** `SumExpression` checks that its
  addends share a dimensionality in its constructor and nowhere else; the unary types check
  dimensionlessness there and nowhere else.
- **Dimensionality is derived, never stored.** A quotient reports
  `Numerator.Dimensionality / Denominator.Dimensionality`, so it cannot disagree with its
  children.

## Surprises

- **`ComputeIfFullyDescribed()` re-walks to the leaves on every call and caches nothing.** A
  sub-expression shared by three parents is computed three times. Nothing is memoised
  deliberately — a node cannot learn that a leaf beneath it was reassigned, so a cached answer
  there could go stale. Use `Calcusystem.Analysis`'s `system.Calculate()` for anything beyond a
  one-off read.
- **The graph is a DAG, not a tree.** One node can be a child of several parents, which is the
  point of building formulas from shared pieces, and the reason `UnsetVariables()` returns a
  deduplicated set rather than a count.
- **`ExpressionGraph` is `internal` and not a member of anything.** Ordering several roots at
  once is not a question any single node can be asked, so there is nothing to hang it on. It is
  reached through `IExpression.InDependencyOrder()` and `ExpressionSystem.InDependencyOrder()`.
- **`ExpressionFactory` exists because the concrete type is chosen by inspecting the snapshot.**
  The nodes cannot use `ISnapshottingNode`'s `static abstract FromSnapshot` for reconstruction
  from a discriminator, so a static gateway covers the closed set instead.

## What does not belong here

- Relationships between expressions → `BinaryOperators/`
- The container that owns them → `Systems/`
- Evaluating a whole system → `Calcusystem.Analysis`

## Related

`Interfaces/` · `BinaryOperators/` · `Systems/` · `Snapshots/` · `Provenance/`.
See the [assembly README](../README.md) for the lazy, dimension-checked graph and why structure
is immutable.

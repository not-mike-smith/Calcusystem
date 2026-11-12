# Calcusystem.DimensionedExpression.Expressions

The nodes a formula is built from: a settable leaf, three fixed ones, and the ways to combine them.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `Variable` | class | The settable leaf. The only node whose value can change. |
| `Constant` | sealed class | A named value with no uncertainty, fixed when built. |
| `ReferenceValue` | sealed class | A named value with uncertainty, fixed when built. |
| `Scalar` | sealed class | An unnamed, dimensionless coefficient with no uncertainty. |
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
| `FixedExpressionBase` | abstract class | What the three fixed values share: a value that never changes. |
| `FixedValueRule` | static class | Which value each kind of fixed value accepts. |
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

Operands are constructor arguments; the two-child types use required initializers:

```csharp
var q = new QuotientExpression { Numerator = work, Denominator = time };
```

A variable's name is its first constructor argument. A computed node's name is optional and goes in an object
initializer:

```csharp
var named = new ProductExpression([m, a]) { Name = "F" };
```

A fixed value has no public constructor. Outside a draft it is rebuilt from a snapshot:

```csharp
var gravity = new Quantity(9.80665, Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time))
    .WithoutUncertainty();

var g = ExpressionFactory.FromSnapshot(
    new FixedExpressionSnapshot(FixedExpressionType.Constant, "const-g", "g", gravity.GetSnapshot(), null));

g.Value;              // 9.80665 m/s², never null
g.IsFullyDescribed;   // always true

FixedValueRule.Problem(FixedExpressionType.Scalar, gravity);   // "A scalar is dimensionless, and …"
```

## Guarantees

- **Structure is fixed at construction.** There is no `AddFactor`, no operand setter, and no
  way to re-point a child. What a leaf is *worth* stays mutable; what the graph *is* does not.
- **Cycles are unconstructible through these types**, because a node's operands must exist
  before it does.
- **Validation happens once, where the operand arrives.** `SumExpression` checks that its
  addends share a dimensionality in its constructor and nowhere else; the unary types check
  dimensionlessness there and nowhere else.
- **A name is fixed at construction and stored normalized.** A variable always has one. An invalid name
  throws `InvalidNameException` there. The rule is `Names.NodeName`.
- **A fixed value's value never changes, and is never null.** `Constant`, `ReferenceValue` and `Scalar` are
  never unset, so none is ever an unknown. `ComputeFrom` answers the node's own value whatever it is handed.
- **A fixed value's kind and its value agree.** A constant has no uncertainty, a reference value has some, and
  a scalar has none and is dimensionless. Anything else throws `InvalidFixedValueException` where the node is
  built, with the message `FixedValueRule.Problem` answers.
- **Dimensionality is derived, never stored.** A quotient reports
  `Numerator.Dimensionality / Denominator.Dimensionality`, so it cannot disagree with its
  children.

## Surprises

- **`ComputeIfFullyDescribed()` re-walks to the leaves on every call and caches nothing.** A
  sub-expression shared by three parents is computed three times. Nothing is memoized
  deliberately — a node cannot learn that a leaf beneath it was reassigned, so a cached answer
  there could go stale. Use `Calcusystem.Analysis`'s `system.Calculate()` for anything beyond a
  one-off read.
- **A name is supplied three ways, by the shape of each constructor.** A variable takes it as an argument; every
  other node takes it by object initializer. `Variable.Name` is a `string`, hiding the `string?` every other
  node has, so `new Variable(…) { Name = … }` does not compile.
- **A constant with uncertainty is refused, not turned into a reference value**, and the reverse.
- **A scalar has no name.** `Scalar.Name` is always null, and a scalar's snapshot that carries a name or a
  provenance is refused.
- **Nothing here stops two nodes having one name.** Uniqueness belongs to the system that holds them:
  `ExpressionSystem.Add` refuses the second.
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

`Interfaces/` · `BinaryOperators/` · `Systems/` · `Snapshots/` · `Provenance/` · `Names/` (`NodeName`, the rule
for a name).
See the [assembly README](../README.md) for the lazy, dimension-checked graph and why structure
is immutable.

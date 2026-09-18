# Calcusystem.DimensionedExpression.Interfaces

The three contracts this layer is written against: a node in an expression graph, a
relationship between two of them, and where a value came from.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `IExpression` | interface | Any node: knows its dimensionality always, its value only when it can. |
| `IComputedExpression` | interface | An `IExpression` that computes from children, so it carries a correlation assumption. |
| `IDirectExpression` | interface | An `IExpression` whose value is set rather than computed — a mutable leaf. |
| `IBinaryOperator` | interface | A relationship over two expressions, and what it asserts about them. |
| `IProvenance` | interface | Where a value came from, with the audit metadata for its kind. |

## Start here

```csharp
IExpression node = force;

node.Dimensionality;                // always available, before any value exists
node.ComputeIfFullyDescribed();     // the Measurand, or null
node.UnsetVariables();              // the distinct leaves still without values
node.Children;                      // one level down

IBinaryOperator rel = requirement;
rel.IsSatisfiedGiven(lhs, rhs);     // bool? — null means no answer
rel.Symbol;                         // ⌜<⌟
rel.SolvingRole;                    // Requirement / Equation / Coherence
```

## Guarantees

- **Dimensionality is total; value is partial.** Ask for a dimensionality at any time. A null
  from `ComputeIfFullyDescribed()` *is* the "not yet" answer, so prefer checking the result
  over calling `IsFullyDescribed` first — the latter is itself a walk.
- **A verdict is three-valued.** `IsSatisfiedGiven` returns `bool?`, and `null` means the
  comparison had no answer — different dimensions, a `NaN`, same-signed infinities. It reaches
  a report as undetermined, never as a violation.
- **`Subject` and `Criterion` are about presentation, `SolvingRole` about structure.** A
  relationship may name which side is being judged without that saying anything about whether
  it determines a value.
- **`UnsetVariables()` deduplicates.** The graph is a DAG, so a shared sub-expression is
  reachable by several paths and anything counting distinct unknowns must dedupe.

## Surprises

- **`ComputeIfFullyDescribed()` is a method, and named for what it costs.** It walks the whole
  graph beneath the node on every call and caches nothing, so a sub-expression shared by three
  parents is computed three times. For anything beyond a one-off read, use
  `Calcusystem.Analysis`'s `system.Calculate()`, which computes each node once.
- **Nothing is memoised, deliberately.** A node cannot learn that a leaf beneath it was
  reassigned, so a cached answer there could silently go stale. Caching belongs to a caller
  that knows over what scope the graph is unchanged.
- **Only `IDirectExpression` has a settable `Value`.** Structure is fixed at construction —
  there is no way to add an operand or re-point a relationship's side — while what a leaf is
  *worth* stays mutable.
- **`IComputedExpression.UncertaintyCorrelation` is a statement about the model, not a method.**
  It says whether *these* operands' uncertainties move together; the numerical method is the
  `IUncertaintyPropagator` a calculation supplies, and both are passed on together.

## What does not belong here

- The concrete nodes → `Expressions/`
- The operators → `BinaryOperators/`
- The container → `Systems/`

## Related

`Expressions/` · `BinaryOperators/` · `Provenance/` · `Systems/` · `Enums/` (`SolvingRole`).
See the [assembly README](../README.md) for the lazy, dimension-checked graph and why structure
is immutable.

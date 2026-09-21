# Calcusystem.Core.Interfaces

The contracts every layer above shares: what it means to have an identity, and how an object
hands out the state that defines it.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `IIdentified` | interface | Anything with a stable string `Id` that survives persistence. |
| `ISnapshotting<TSelf, TSnapshot>` | interface | A type that can be rebuilt from its own snapshot alone. |
| `ISnapshottingNode<TSelf, TSnapshot>` | interface | A type whose snapshot names *other* objects by id. |
| `INodeResolver` | interface | Turns an id back into the object it names. |

## Start here

Pick between the two snapshot interfaces by asking whether rebuilding needs outside help:

```csharp
// self-contained — Quantity, Measurand, Dimensionality, Variable
TSnapshot GetSnapshot();
static abstract TSelf FromSnapshot(TSnapshot snapshot);

// part of a graph — anything whose snapshot holds ids of other nodes
TSnapshot GetSnapshot();
static abstract TSelf FromSnapshot(TSnapshot snapshot, INodeResolver resolve);
```

A graph is not a tree — one node can be shared by several parents — so nesting children inside
a parent's snapshot would duplicate the shared ones and could not express the sharing at all.
Naming them by id keeps each snapshot flat and the graph intact, at the cost of needing
something to turn an id back into an object.

## Guarantees

- **An implementation of `INodeResolver` throws** when an id cannot be resolved, or names
  something of a different type. Callers rebuild in an order that makes each referenced object
  available before it is asked for; a failure means the source data is not internally
  consistent.
- **The type argument to `Resolve<TNode>` is checked at resolution.** That check is necessarily
  a runtime one — an id reference carries no type information, so no signature could have
  proved it statically.

## Surprises

- **Neither interface suits a polymorphic hierarchy.** `static abstract FromSnapshot` has to be
  declared on a type the caller already knows, so where the concrete type is chosen by
  *inspecting* the snapshot, reconstruction goes through a factory instead —
  `UncertaintyFactory`, `ProvenanceFactory`, `ExpressionFactory`, `BinaryOperatorFactory`. Each
  still pairs with a `GetSnapshot()` on its interface.

## What does not belong here

- The snapshot records themselves → the assembly that owns the type they describe
- DTOs, wire formats, type discriminators, schema migration → `Calcusystem.Serialization`
- The `IdBase` implementation → `Identity/`

## Related

`Identity/` (`IdBase`, `CREATE_NEW_ID`).
See the [assembly README](../README.md) for why the snapshot seam is split this way, and what
belongs in `Core` at all.

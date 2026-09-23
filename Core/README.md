# Calcusystem.Core

The basement. Contracts shared by every layer above: what it means to have an identity, and how an object hands out the data that defines it.

Depends on nothing. Contains no behavior beyond `IdBase`'s id validation — everything else here is an interface or a constant, so there is nothing to test in isolation and no test project.

> **Using this assembly:** two folders, each with its own README. [`Interfaces/`](Interfaces/README.md) holds the four contracts; [`Identity/`](Identity/README.md) holds the one implementation and its sentinel. This README covers why the seams are shaped the way they are — read it when a design question comes up, not to look up a signature.

---

## Identity

| Type | Role |
| --- | --- |
| `IIdentified` | Anything with a stable string `Id` that survives persistence. |
| `IdBase` | The usual implementation: validates the id and interprets the create-new sentinel. |
| `Constants.CREATE_NEW_ID` | Sentinel meaning "mint a fresh identity". |

Identity lives here rather than in `DimensionedExpression` because it is not an expression concept — the solver will need persistence and ids without depending on the expression layer.

---

## The persistence seam

The layers above own **what data defines an object**. `Calcusystem.Serialization` owns **how that data is encoded, versioned, and migrated**. These interfaces are the joint between those two questions, and the reason a DTO never has to appear in a domain assembly.

The snapshots themselves (`QuantitySnapshot`, `VariableSnapshot`, …) live with the types they describe, not here — only the shape of the seam is shared.

### `ISnapshotting<TSelf, TSnapshot>` — self-contained

For a type that can be rebuilt from its own snapshot alone:

```csharp
TSnapshot GetSnapshot();
static abstract TSelf FromSnapshot(TSnapshot snapshot);
```

Implemented by `Quantity`, `Measurand`, `Dimensionality`, and `Variable`.

### `ISnapshottingNode<TSelf, TSnapshot>` — part of a graph

For a type whose snapshot names *other* objects by id rather than containing them:

```csharp
TSnapshot GetSnapshot();
static abstract TSelf FromSnapshot(TSnapshot snapshot, INodeResolver resolve);
```

**The axis is whether rebuilding needs outside help, not where a type sits in a tree.** `Variable` is a genuine leaf of the expression graph and uses `ISnapshotting`; that it is a leaf is incidental — what matters is that it has no references to resolve.

### `INodeResolver`

```csharp
TNode Resolve<TNode>(string id) where TNode : class, IIdentified;
```

A generic method rather than a typed delegate, because a node's neighbors need not all be the same type — an `ExpressionSystem` refers to expressions in two of its lists and to operators in the other two, and a composed system would refer to sub-systems as well.

A resolver throws on an id it cannot resolve rather than returning null, because a dangling reference means the source data is not internally consistent — which is not something a domain type should be asked to paper over.

---

## Why polymorphic hierarchies use factories instead

Neither seam suits a hierarchy where the concrete type is chosen by *inspecting* the snapshot — a `static abstract FromSnapshot` has to be declared on a type already known to the caller. Those reconstruct through a static gateway over the closed set instead: `UncertaintyFactory`, `ProvenanceFactory`, `ExpressionFactory`, `BinaryOperatorFactory`. Each pairs with a `GetSnapshot()` on the interface, which is the half that *is* declared here in spirit even when the type does not implement `ISnapshotting`.

---

## Scope boundaries

**What belongs here:** contracts shared by two or more layers — identity, the persistence seams, and constants that go with them.

**What does NOT belong here:**

- Snapshots themselves → the assembly that owns the type they describe
- DTOs, wire formats, type discriminators, schema migration → `Calcusystem.Serialization`
- Anything with real behavior. This assembly is a vocabulary; if a change here needs a test, it probably belongs a layer up.
- Types only one layer uses. `UncertaintyCorrelation` is a standing example: `DimensionedExpression` and the serializer both touch it, but [`project-plan.md`](../project-plan.md) records a deliberate decision that it stays in `Measurement`, because uncertainty propagation is a first-class concern of that layer rather than something to be exiled into a shared bucket.

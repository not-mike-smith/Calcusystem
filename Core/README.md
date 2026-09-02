# Calcusystem.Core

The basement. Contracts shared by every layer above: what it means to have an identity, and how an object hands out the state that defines it.

Depends on nothing. Contains no behaviour beyond `IdBase`'s id validation — everything else here is an interface or a constant, so there is nothing to test in isolation and no test project.

> **Using this assembly:** unlike the layers above, there is no `Interfaces/` directory to read — the interfaces *are* the assembly, and they sit at its root. This README plus those five files is the whole of it.

---

## Identity

| Type | Role |
| --- | --- |
| `IIdentified` | Anything with a stable string `Id` that survives persistence. |
| `IdBase` | The usual implementation: validates the id and interprets the create-new sentinel. |
| `Constants.CREATE_NEW_ID` | Sentinel meaning "mint a fresh identity". |

Passing `CREATE_NEW_ID` generates a GUID; passing any other non-blank string adopts it verbatim, which is how a rebuilt graph restores the references between its nodes. Null or blank throws — an object with no identity cannot be referred to, so there is no useful default.

**`IIdentified` means "has a stable identity", not "is referenceable by id."** Those come apart: provenance carries an id that round-trips for fidelity, but it is owned inline by a single node and never named by another. Don't read the stronger meaning into the interface.

Identity lives here rather than in `DimensionedExpression` because it is not an expression concept — the solver will need persistence and ids without depending on the expression layer.

---

## The persistence seam

The layers above own **what state defines an object**. `Calcusystem.Serialization` owns **how that state is encoded, versioned, and migrated**. These interfaces are the joint between those two questions, and the reason a DTO never has to appear in a domain assembly.

The state records themselves (`QuantitySnapshot`, `VariableSnapshot`, …) live with the types they describe, not here — only the shape of the seam is shared.

### `ISnapshotting<TSelf, TSnapshot>` — self-contained

For a type that can be rebuilt from its own state alone:

```csharp
TSnapshot GetSnapshot();
static abstract TSelf FromSnapshot(TSnapshot state);
```

Implemented by `Quantity`, `Measurand`, `Dimensionality`, and `Variable`.

### `ISnapshottingNode<TSelf, TSnapshot>` — part of a graph

For a type whose state names *other* objects by id rather than containing them:

```csharp
TSnapshot GetSnapshot();
static abstract TSelf FromSnapshot(TSnapshot state, INodeResolver resolve);
```

A graph is not a tree — one node can be shared by several parents — so nesting children inside a parent's state would duplicate the shared ones and could not express the sharing at all. Referring to them by id keeps the state flat and the graph intact, at the cost of needing something to turn an id back into an object.

**The axis is whether rebuilding needs outside help, not where a type sits in a tree.** `Variable` is a genuine leaf of the expression graph and uses `ISnapshotting`; that it is a leaf is incidental — what matters is that it has no references to resolve.

### `INodeResolver`

```csharp
TNode Resolve<TNode>(string id) where TNode : class, IIdentified;
```

A generic method rather than a typed delegate, because a node's neighbours need not all be the same type — an `ExpressionSystem` refers to expressions in two of its lists and to operators in the other two, and a composed system would refer to sub-systems as well.

The type argument is a claim about what the id names, checked when it is resolved. That check is necessarily a runtime one: an id reference carries no type information, so no signature could have proved it statically.

**Implementations throw when an id cannot be resolved**, or names something of a different type. Callers are expected to rebuild in an order that makes each referenced object available before it is asked for; a failure means the source data is not internally consistent, which is not something a domain type should be asked to paper over.

---

## Why polymorphic hierarchies use factories instead

Neither seam suits a hierarchy where the concrete type is chosen by *inspecting* the state — a `static abstract FromSnapshot` has to be declared on a type already known to the caller. Those reconstruct through a static gateway over the closed set instead: `UncertaintyFactory`, `ProvenanceFactory`, `ExpressionFactory`, `BinaryOperatorFactory`. Each pairs with a `GetSnapshot()` on the interface, which is the half that *is* declared here in spirit even when the type does not implement `ISnapshotting`.

---

## Scope boundaries

**What belongs here:** contracts shared by two or more layers — identity, the persistence seams, and constants that go with them.

**What does NOT belong here:**

- State records themselves → the assembly that owns the type they describe
- DTOs, wire formats, type discriminators, schema migration → `Calcusystem.Serialization`
- Anything with real behaviour. This assembly is a vocabulary; if a change here needs a test, it probably belongs a layer up.
- Types only one layer uses. `UncertaintyCorrelation` is a standing example: `DimensionedExpression` and the serializer both touch it, but [`project-plan.md`](../project-plan.md) records a deliberate decision that it stays in `Measurement`, because uncertainty propagation is a first-class concern of that layer rather than something to be exiled into a shared bucket.

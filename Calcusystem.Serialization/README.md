# Calcusystem.Serialization

The persistence layer. Converts a live `ExpressionSystem` object graph (from `DimensionedExpression`) to and from a set of flat, serializer-friendly DTOs, so a model can be written to disk and rebuilt later.

Depends on `DimensionedExpression` (and transitively `Measurement`). Nothing depends on it — it sits at the top of the stack.

---

## What this assembly is (and is not)

This layer does **object-to-object mapping**, not byte encoding. It turns domain objects into plain DTO classes (and back); it does **not** itself produce JSON/XML/binary. The DTOs are deliberately flat POCOs with `required` init properties, meant to be handed to a serializer (e.g. `System.Text.Json`) by the caller. Choosing and invoking that serializer is out of scope here.

```
ExpressionSystem  ──SerializingMapper──▶  Dtos.ExpressionSystem  ──(your JSON lib)──▶  text
text  ──(your JSON lib)──▶  Dtos.ExpressionSystem  ──DeserializingMapper──▶  ExpressionSystem
```

---

## The central idea: flatten the graph, reference by `Id`

An `ExpressionSystem` is a **graph**, not a tree — a single sub-expression can be shared by several parents, and operators point at expressions. Nesting that directly would duplicate shared nodes and can't represent sharing at all.

Instead the graph is **flattened into id-keyed lists**. Every node keeps its string `Id` (see `IdBase`/`CREATE_NEW` in `DimensionedExpression`), and parents reference children **by id string**, never by nesting. Rebuilding the graph is then a matter of resolving those id references against a lookup table. This is why preserving `Id`s across a round-trip is essential.

DTOs are bucketed by **structural arity**, not by domain type — the concrete type is recovered from a `Type` discriminator string within each bucket:

| DTO (`Dtos/`) | Shape | Domain types it carries |
| --- | --- | --- |
| `SingleVariable` | leaf: `Symbol`, `Dimensionality`, `KmsValue?`, `Uncertainty?` | `Variable` |
| `SingleDerivedVariable` | one child: `InnerId` | `ReciprocalExpression`, `NegatedExpression` |
| `PairDerivedVariable` | two children: `InnerId1`, `InnerId2` | `QuotientExpression` |
| `ListDerivedVariable` | n children: `InnerIds`, plus `ErrorPropagation` | `ProductExpression`, `SumExpression` |
| `BinaryOperator` | `LhsId`, `RhsId`, `Name?`, `Description?` | all equality / tolerance / inequality operators |

Uncertainty has its own small DTO hierarchy (`Dtos/Expression.cs`): `SymmetricUncertainty` (`RelativeError`) for `GaussianUncertainty`, and `AsymmetricUncertainty` (`UpperRelativeError`/`LowerRelativeError`) for the domain `AsymmetricUncertainty`. Both DTO and domain sides carry a `Type` string. (When serializing a Gaussian, the relative error is read out via `RelativeError(1)` — for a symmetric uncertainty the nominal value is irrelevant, so `1` just extracts the stored fraction.)

---

## The `Type` discriminator (the main coupling point)

Serialization writes `Type = x.GetType().Name`; deserialization switches on `nameof(ConcreteType)` to pick the mapping method. This string is the contract between the two sides and the on-disk format:

- **Renaming a domain class breaks previously-serialized data** (the stored `Type` no longer matches any `nameof` case). The `NegatedVariable` → `NegatedExpression` rename is an example — old files carrying `"NegatedVariable"` will not deserialize.
- **Adding a new expression or operator type requires touching both mappers**: a `Map(...)` overload in `SerializingMapper`, and a `nameof(...)` switch case plus a `MapX` method in `DeserializingMapper`. An unrecognized `Type` throws `NotImplementedException` with the offending name.

---

## Deserialization is dependency-ordered

The flattened lists arrive in arbitrary order, so a parent may be read before the children it references exist yet. `DeserializingMapper.MapAllDerivedExpressions` resolves this with a **worklist that retries**:

1. Direct expressions (leaf `Variable`s) are mapped first — they have no dependencies — and registered in the `DeserializationContext` (an `Id → IExpression` dictionary).
2. Each derived-expression mapping is wrapped in a deferred function and queued. Running one either:
   - **succeeds** — all its child ids were already in the context — and the result is registered; or
   - **defers** — a child id isn't in the context yet, so the mapper returns `null` and the function is pushed to the back of the queue to try again later.
3. The queue drains as dependencies fill in, without needing a topological pre-sort.

`DeserializationContext` is the shared id-resolution table threaded through this process. `ExpressionNotFoundDeserializationException` carries the missing id and the DTO that referenced it.

> ⚠️ **Cycle / dangling-reference caveat:** the retry loop assumes the graph is acyclic (expression trees always are) and that every referenced id is present. A genuinely missing or cyclic reference among derived expressions leaves at least one function permanently deferring — an **infinite loop**, not a clean error. There is currently no max-iteration or no-progress guard.

---

## Constructing the mappers

`SerializingMapper` is stateless — construct and call `Map(system)`.

`DeserializingMapper` needs two things: a fresh `DeserializationContext` (one per deserialization run — it accumulates state) and an `IEqualityEstimating` strategy. The latter is required because `EqualityOperator` is the one operator with a dependency (it cannot decide equality without a strategy); the mapper injects it into every `EqualityOperator` it rebuilds.

```csharp
var dto = new SerializingMapper().Map(system);
// … hand `dto` to your JSON serializer, persist, later reload into `dto` …
var mapper = new DeserializingMapper(new DeserializationContext(), myEqualityEstimator);
ExpressionSystem restored = mapper.Map(dto);
```

---

## Scope boundaries

**What belongs here:** DTO definitions, the two mappers, the deserialization context and its exception.

**What does NOT belong here:**

- The expression types, operators, and `ExpressionSystem` themselves → `DimensionedExpression`
- Physical quantities, units, uncertainty math → `Measurement`
- Actual text/binary encoding (JSON etc.) → the caller's choice of serializer
- Evaluation and solving → future assemblies

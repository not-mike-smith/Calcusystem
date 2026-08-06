# Calcusystem.Serialization

The persistence layer. Converts a live `ExpressionSystem` object graph (from `DimensionedExpression`) to and from a set of flat, serializer-friendly DTOs, so a model can be written to disk and rebuilt later.

Depends on `DimensionedExpression` (and transitively `Measurement`). Nothing depends on it — it sits at the top of the stack.

---

## What this assembly is (and is not)

This layer does **object-to-object mapping**, not byte encoding. It turns domain objects into plain DTO classes (and back); it does **not** itself produce JSON/XML/binary. The DTOs are deliberately flat POCOs with `required` init properties, meant to be handed to a serializer (e.g. `System.Text.Json`) by the caller. Choosing and invoking that serializer is out of scope here.

> **But the DTOs must still actually be serializable.** "The caller picks the serializer" is not a licence to ignore what serializers can do, and for a long time these DTOs could not survive a round trip through one — three separate defects, each losing data *silently*: a domain struct with private state that wrote as `{}`, get-only collection properties that no serializer can restore, and an abstract DTO base that none can instantiate. None of it was caught, because mapping tests never invoke a serializer. `JsonRoundTripTests` now pushes the DTOs through real `System.Text.Json` to hold that line. The invariants it protects are listed under [Serializer compatibility](#serializer-compatibility) below.

```text
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
| `SingleVariable` | leaf: `Symbol`, `Dimensionality` (encoded string), `KmsValue?`, `Uncertainty?`, `Provenance?` | `Variable` |
| `SingleDerivedVariable` | one child: `InnerId` | `ReciprocalExpression`, `NegatedExpression`, `SqrtExpression`, `ExponentialExpression`, `NaturalLogExpression` |
| `PairDerivedVariable` | two children: `InnerId1`, `InnerId2`, plus `ErrorPropagation` | `QuotientExpression` |
| `ListDerivedVariable` | n children: `InnerIds`, plus `ErrorPropagation` | `ProductExpression`, `SumExpression` |
| `BinaryOperator` | `LhsId`, `RhsId`, `Name?`, `Description?`, `Provenance?` | all equality / tolerance / inequality operators |

Uncertainty is a single flat `Dtos.Uncertainty` (`Dtos/Expression.cs`): a `Type` discriminator, an `IsStoredAsAbs` flag recording whether the magnitudes are relative fractions or absolute KMS values, and the union of the shapes' nullable fields — `Magnitude` for the symmetric case, `UpperMagnitude`/`LowerMagnitude` for the asymmetric one. Fields required by the named shape are validated on read rather than defaulted, since a missing magnitude would silently change the error band.

**Everything crosses the boundary as domain *state*, never as domain internals.** The mappers construct no domain object and read no domain property: each one calls `GetState()` on the way out, and on the way in translates a DTO into the matching state record and hands it to that type's own reconstruction — `Variable.FromState`, `ExpressionFactory.FromState`, `BinaryOperatorFactory.FromState`, `ExpressionSystem.FromState`, `UncertaintyFactory.FromState`, `Dimensionality.FromState`, `ProvenanceFactory.FromState`. See the persistence sections of the [Measurement](../Measurement/README.md) and [DimensionedExpression](../DimensionedExpression/README.md) READMEs.

Those state records are structural and carry no format concerns, which is exactly what leaves this layer free to own the format: the `Type` strings, the dimensionality encoding, and any fix-up of older payloads are all decided here. Neither domain assembly ever sees a schema version.

### What this layer still owns

Two things that look like they could move into the domain but should not:

- **Rebuild order.** The flattened lists arrive in arbitrary order, so `DeserializingMapper` rebuilds leaves first, then derived expressions as their dependencies appear, then operators, then the system. By the time any `FromState` asks the resolver for a neighbour, it is present. Choosing that order is a persistence strategy.
- **What a dangling reference means.** `DeserializationContext` implements `INodeResolver` and throws `ReferencedNodeNotFoundException` when an id names nothing, or names a node of the wrong type. A domain object is never handed a null and asked to decide.

### The `WireNames` table

The state records discriminate with enums; the payload uses concrete type-name strings. `Mappers/WireNames.cs` is the whole of the translation between them, in both directions. It is deliberately the only place the two vocabularies meet — and the names are the *class* names rather than the enum member names, so introducing the state layer did not invalidate a single previously-written payload. If a domain class is renamed, the string here should **not** follow it; that mapping is a migration point, not a mirror.

### `DimensionalityCodec`

A `Dimensionality` arrives as exponent pairs and is written as a compact canonical string — each present dimension's symbol followed by its integer exponent, comma-separated, in canonical dimension order:

```text
force  →  "M1,L1,T-2"          dimensionless  →  ""
```

Compact rather than a nested object because the content is tightly constrained (nine symbols, small integer exponents) and a string needs no custom converter in any serializer. `Measurement` guarantees canonical ordering of the pairs, so dimensionally-equal values always produce the identical string — safe to diff, compare, or hash. Decoding tolerates any entry order and re-normalizes on write, so a hand-edited file converges on re-save.

Unknown symbols, duplicates, and malformed entries throw `FormatException` rather than being skipped. That strictness is the point: silently dropping an entry yields a *quietly dimensionless* quantity, which is exactly the failure this encoding exists to prevent.

Provenance is serialized similarly, as a single flat `Dtos.Provenance` (`Id` + `Type` discriminator + the union of the kinds' nullable fields) nested inline in `SingleVariable` and `BinaryOperator`. `Map`/`MapProvenance` dispatch on the concrete type / `Type` string; reconstruction routes back through `ProvenanceFactory` (whose `internal`-ctor'd kinds are public so the mapper can read their fields). Unlike expressions, provenance is owned inline rather than referenced by id — its `Id` rides along for fidelity but is not part of the reference graph.

---

## The `Type` discriminator (the main coupling point)

Serialization writes `Type = x.GetType().Name`; deserialization switches on `nameof(ConcreteType)` to pick the mapping method. This string is the contract between the two sides and the on-disk format:

- **Renaming a domain class breaks previously-serialized data** (the stored `Type` no longer matches any `nameof` case). The `NegatedVariable` → `NegatedExpression` rename is an example — old files carrying `"NegatedVariable"` will not deserialize.
- **Adding a new expression or operator type requires touching both mappers**: a `Map(...)` overload in `SerializingMapper`, and a `nameof(...)` switch case plus a `MapX` method in `DeserializingMapper`. An unrecognized `Type` throws `NotImplementedException` with the offending name.
- **Renaming a fundamental-dimension symbol likewise breaks stored data**, since a `Dimensionality` is encoded from its symbols. Migrating those payloads is this layer's job.

---

## Serializer compatibility

The DTOs are format-agnostic, but not serializer-*indifferent* — a POCO can be perfectly valid C# and still be unable to survive a round trip. Three rules, each learned from a bug that lost data without raising anything:

| Rule | Why |
| --- | --- |
| **No domain types in DTOs — only primitives, strings, enums, and other DTOs.** | `SingleVariable.Dimensionality` was the `Dimensionality` struct, whose exponent map is private and keyed by a class. `System.Text.Json` wrote `{}` and read back `default` — a dimensionless value, no exception. It is now the encoded string. |
| **Collection properties need `init` setters, not just getters.** | A serializer writes a get-only collection correctly but cannot restore it; STJ skips the property rather than adding to the existing instance. All six lists on `Dtos.ExpressionSystem` came back empty. |
| **No abstract or interface-typed DTO properties.** | No serializer can instantiate an abstract type without provider-specific polymorphism configuration, which would tie this assembly to one serializer. Where a discriminated shape is needed, use one flat concrete class with a `Type` string and a union of nullable fields — as `Uncertainty` and `Provenance` both do. |

`JsonRoundTripTests` enforces these by pushing DTOs through real `System.Text.Json`. The object-to-object suites (`RoundTripTests`, `ProvenanceRoundTripTests`) cannot: they never touch a serializer, which is precisely how all three defects survived so long. **Any new DTO or DTO property should get a case there.**

---

## Deserialization is dependency-ordered

The flattened lists arrive in arbitrary order, so a parent may be read before the children it references exist yet. `DeserializingMapper.MapAllDerivedExpressions` resolves this with a **worklist that retries**:

1. Direct expressions (leaf `Variable`s) are mapped first — they have no dependencies — and registered in the `DeserializationContext` (an `Id → IExpression` dictionary).
2. Each derived-expression mapping is wrapped in a deferred function and queued. Running one either:
   - **succeeds** — all its child ids were already in the context — and the result is registered; or
   - **defers** — a child id isn't in the context yet, so the mapper returns `null` and the function is pushed to the back of the queue to try again later.
3. The queue drains as dependencies fill in, without needing a topological pre-sort.

`DeserializationContext` is the shared id-resolution table threaded through this process. `ReferencedNodeNotFoundException` carries the missing id and the DTO that referenced it — it covers any referenced node, not only expressions.

**Termination is guarded, not assumed.** The loop only ends if deferrals keep becoming buildable, which a missing or cyclic reference breaks. A counter tracks *consecutive* deferrals: once a full pass over the remaining queue produces no progress, nothing can change and the payload is rejected with `UnresolvableGraphException`. Without that check the loop spins forever — and since the retry is iterative, not even a stack overflow would end it.

The exception separates the two causes, which is worth having because they mean different things: an id referenced but **absent from the payload** (`MissingIds`) is truncated or hand-edited data, whereas an id that is **present but itself unbuilt** (`CyclicIds`) is a reference cycle. Expression trees are acyclic by construction, so a cycle means the payload came from something other than `SerializingMapper`, or was edited afterwards.

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

# Calcusystem.Authoring

Holds a system while someone is still building it, and turns it into an `ExpressionSystem` once it is coherent.

Depends on `DimensionedExpression` and `Measurement`. Nothing depends on it — it sits beside `Serialization` and `Analysis`, not under them.

---

## Why a second model exists

An `ExpressionSystem` refuses to exist in a half-built state, deliberately. Operands are fixed at construction, a sum checks on the way in that its addends share a dimensionality, `Add` rejects a relationship whose sides have different dimensionalities, and a cycle is unconstructible. Those rules are what make the built graph safe to absorb eagerly, cache for the duration of a calculation, and walk without defending against rearrangement.

They are also exactly what an editor cannot honor between one keystroke and the next. A person building a model passes through every one of these:

| Mid-edit state | What the domain does |
| --- | --- |
| a sum with one mass and one length in it | `SumExpression` throws on construction |
| an exponential whose argument is not yet dimensionless | `ExponentialExpression` throws |
| a √ over an odd exponent | throws, but **lazily** — on reading `Dimensionality`, not on construction |
| a value typed in before its variable's dimensionality is corrected | `Variable.Value` throws |
| a variable with no dimensionality yet | unrepresentable — both constructors require one |
| a comparison across two dimensionalities | `ExpressionSystem.Add` throws |
| two nodes briefly pointing at each other | the graph walks refuse it |
| a node with no kind chosen yet | unrepresentable — there is no such type |

So the flexibility goes in a separate model rather than into the domain types. **A `SystemDraft` is freely invalid; building is where the rules apply.** This is the same resolution `project-plan.md` records for tolerant deserialization — *"invalid state is unrepresentable is the right rule for the domain types, but intake is exactly the boundary where invalid state arrives, so the flexibility belongs in the mode, not in the types"* — applied to authoring instead of loading.

---

## Using it

```csharp
using Calcusystem.Authoring.Drafts;
using Calcusystem.Authoring.Enums;

var draft = SystemDraft.Create("Newton's second law", "F = m·a");

var m = draft.AddVariable("m", Dimensionality.Mass);
m.Value = Mass.Kilogram.Quantity(2).WithUncertainty(1.0.Percent());
var a = draft.AddVariable("a", Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time));

var f = draft.AddExpression(ExpressionDraftType.Product);
f.OperandIds.Add(m.Id);
f.OperandIds.Add(a.Id);

foreach (var problem in draft.Validate())
    Console.WriteLine($"{problem.NodeId}: {problem.Message}");

if (draft.TryBuild(out var system, out var problems))
{
    // An ordinary ExpressionSystem from here on.
}
```

What comes back is an `ExpressionSystem` like any other, so `Calcusystem.Analysis` takes it from there — `system.Flatten()`, `system.Decompose()`, `system.Calculate()`.

---

## What makes it fit a web front-end

**Nothing throws for a modelling mistake.** Problems come back as data — a `DraftProblem` carrying a `Kind`, a `Message`, and the `NodeId` at fault, so an editor can highlight the node rather than show a stack trace. A `Kind` means the client branches without matching on message text.

**Ids exist before validity.** A node gets its id the moment it is created, before it is typed, let alone correct. A front-end has something to reference immediately.

**That id carries through the build.** The built `Variable` has the id its draft had, so references held outside the draft survive. This is why `Build` emits snapshots and feeds them to `ExpressionFactory` — the snapshot path already preserves ids, because that is what deserialization needs.

**Everything is referenced by id, not by object.** `Analysis`'s `overrides` is keyed by `Variable` *instance* on purpose, so a caller must have the variable in hand. A browser only ever has strings, so this assembly is where id-to-node resolution belongs.

**A draft is a document, not a session.** It holds no connection to a built system and nothing is computed from it, so the client can own it and post it when it wants an answer. The server needs no draft store.

---

## Building: two passes, and one of them asks the domain

The domain cannot be asked about a node until the node can be constructed at all, so `TryBuild` works in two stages.

**Shape**, checked here: a kind chosen, operand slots filled, the right number of them, every reference resolvable, no cycles, no id claimed twice. These are the questions that must be answered before a constructor can even be called.

**Rules**, asked of the domain: each node is constructed in dependency order and whatever the domain refuses is relayed as a `DomainRule` problem carrying its exception message.

**The second pass deliberately does not restate the domain's rules.** A draft validator holding its own copy of "addends must agree" is two things answering one question, which is the shape of three bugs this codebase has already fixed. Here the domain stays the only place a rule is written, so a rule added to a constructor shows up in authoring for free.

One consequence worth knowing: a node is also asked for its `Dimensionality` after construction, because `SqrtExpression` checks its exponents there rather than in its constructor. Without that probe a bad √ would build and the refusal would surface later, somewhere with no idea which node caused it.

### Dependency order, and what is left over

Construction runs in repeated passes: build anything whose operands are already built, until a pass achieves nothing. What remains when progress stops is exactly what could not be ordered, and it is one of two things:

- **above something that failed** — stays silent, because one cause should give one problem rather than a cascade up every node above it;
- **circular** — reported, since nothing else explains the wait.

The two are told apart by spreading a mark from the failures through the leftovers; whatever is left unmarked is the cycle. One mechanism serves both, and neither needs the ordering itself, which is why there is no topological sort here.

### `Validate` is `TryBuild` with the result discarded

So the two cannot disagree about whether a draft is valid. Had `Validate` been written separately it would be a second answer to one question, and the interesting failure is the one where it says a draft is fine and the build then throws.

---

## Scope boundaries

**What belongs here:** holding a system under construction, reporting what stands between it and a built one, and building it.

**What does NOT belong here:**

- The expression graph, operators, and `ExpressionSystem` → `DimensionedExpression`
- Dimensional algebra and uncertainty → `Measurement`
- Degrees of freedom, decomposition, calculating → `Calcusystem.Analysis`
- Wire formats → `Calcusystem.Serialization` today, and a draft's own wire shape when it arrives (see below)
- Restating a domain rule. If a constructor enforces it, relay the refusal instead.

---

## Next

- **A wire shape for a draft**, so the client can post one. Its own DTOs with every field optional, rather than loosening the snapshots, which the serializer relies on being complete.
- **`FromSystem`**, to open a built system for editing. Needed the moment a saved model can be reopened.
- **Editing in place**: nothing yet reorders an operand list, and `Remove` leaves references dangling on purpose. What else an editor needs is better learned from the editor than guessed at here.

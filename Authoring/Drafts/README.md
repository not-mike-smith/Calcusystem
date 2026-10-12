# Calcusystem.Authoring.Drafts

A system under construction, and what stands between it and a built one.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `SystemDraft` | sealed class | The mutable root. Holds the nodes, reports problems, builds the system. |
| `ExpressionDraft` | sealed class | One expression node being built. Every field optional. |
| `RelationshipDraft` | sealed class | One relationship being built. Every field optional. |
| `DraftProblem` | sealed record | One reason the draft cannot be built, naming the node at fault. |

## Start here

```csharp
using Calcusystem.Authoring.Drafts;
using Calcusystem.Authoring.Enums;

var draft = SystemDraft.Create("a model", "");

var m = draft.AddVariable("m", Dimensionality.Mass);
var doubled = draft.AddExpression(ExpressionDraftType.Sum);
doubled.OperandIds.Add(m.Id);
doubled.OperandIds.Add(m.Id);

if (draft.TryBuild(out var system, out var problems)) { /* … */ }
```

Nodes are only ever created through `SystemDraft.AddExpression`, `AddVariable` and `AddRelationship` — the two
draft types have internal constructors, so a node cannot exist outside the draft holding it.

## Guarantees

- **No member throws because a model is wrong.** Every complaint is a `DraftProblem`. A draft can be left in
  any state an editor can reach, including untyped nodes, empty operand slots and transient cycles.
- **`Validate()` and `TryBuild()` never disagree.** The first runs the second and discards the system.
- **An id is fixed when a node is created and survives the build.** The built expression has the id its draft
  had, so references held elsewhere stay good.
- **A problem names its node.** `DraftProblem.NodeId` is null only for a problem about the draft as a whole.
- **Building reads the draft and nothing else.** No ambient state, no connection to a previously built system.
- **One cause gives one problem.** A node above a failed one is not reported as well.

## Surprises

- **`Remove` leaves references to the removed node in place**, and they come back as
  `ReferenceNotFound`. Scrubbing them would silently discard structure the author built.
- **Changing a node's `Type` does not clear the fields that no longer apply.** A variable's `Symbol` survives
  being flipped to a product and back. Fields that do not apply to the current kind are ignored, not cleared.
- **`OperandIds` accepts nulls**, which is how an opened-but-unfilled slot is represented. It is a plain
  `List<string?>` and order matters where the kind cares — a quotient divides the first by the second.
- **A `DomainRule` problem's message comes from the domain's exception**, not from this assembly. The wording
  belongs to whichever constructor refused.
- **An empty draft builds successfully**, into a system with nothing in it. Nothing is missing, so nothing is
  wrong.
- **A variable with no dimensionality is a problem, not a default.** There is no dimensionless fallback, because
  guessing one would silently build a different model than the one intended.

## What does not belong here

- `ExpressionDraftType` and `DraftProblemKind`, the enums → `Enums/`
- Anything that evaluates, counts or solves → `Calcusystem.Analysis`
- A wire shape for a draft → not built yet; see the assembly README's *Next*

## Related

`Enums/` · `Calcusystem.DimensionedExpression.Systems` (`ExpressionSystem`, what a draft builds).

See the [assembly README](../README.md) for why a second mutable model exists at all, and for how the two
build passes divide the work with the domain.

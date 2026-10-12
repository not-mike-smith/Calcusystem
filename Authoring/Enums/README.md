# Calcusystem.Authoring.Enums

The two closed sets authoring names: which kind of node to build, and why one cannot be built.

## What's here

| Type | Role |
| --- | --- |
| `ExpressionDraftType` | Which of the nine expression kinds a draft node is to become. |
| `DraftProblemKind` | Why a draft cannot be built, in a form a front-end can branch on. |

## Guarantees

- **No member of either is zero.** A draft deserialized without a kind is detectably untyped rather than
  silently a `Variable`, and the same for a problem. Same reasoning as `SolvingRole` and `AgreementRule`.
- **`ExpressionDraftType` covers every expression kind the domain can rebuild.** `DraftTypeCoverageTests`
  checks it both ways, so a kind added to the domain and not here fails the build rather than becoming
  quietly unauthorable.

## Surprises

- **`ExpressionDraftType` is one flat set where the domain has three.** The snapshots group kinds by arity —
  `UnaryExpressionType`, `NaryExpressionType`, `BinaryExpressionType` — because reconstruction dispatches on
  arity. Nobody building a model thinks in arities, so authoring offers one list and converts.
- **`Variable` is in it, and in none of the three.** A variable has a snapshot of its own rather than a kind
  within a grouped one, being the only node rebuilt without resolving references.
- **There is no draft enum for relationships.** `RelationshipDraft.Type` is `BinaryOperatorType` itself, which
  is already one flat set over all fourteen operators. Nothing to convert, so nothing to drift.
- **`DraftProblemKind.DomainRule` is not a rule of this layer.** It means a domain constructor refused, and the
  message is that exception's. Every other member is a shape this layer checks itself, because the domain
  cannot be called until the shape is right.

## What does not belong here

- The drafts and the problem record → `Drafts/`
- Reconstruction discriminators → `Calcusystem.DimensionedExpression.Enums`

## Related

`Drafts/` · `Calcusystem.DimensionedExpression.Enums` (`BinaryOperatorType`, used directly by
`RelationshipDraft`).

See the [assembly README](../README.md) for how the two build passes divide the work with the domain.

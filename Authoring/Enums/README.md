# Calcusystem.Authoring.Enums

The two closed sets authoring names: which kind of node to build, and why one cannot be built.

## What's here

| Type | Role |
| --- | --- |
| `ExpressionDraftType` | Which kind of expression a draft node is to become. |
| `DraftProblemKind` | Why a draft cannot be built, in a form a front-end can branch on. |
| `ExpressionDraftTypeExtensions` | `IsLeaf()`: whether a kind has no operands. |

## Guarantees

- **No member of either is zero.** A draft deserialized without a kind is detectably untyped rather than
  silently a `Variable`, and the same for a problem. Same reasoning as `SolvingRole` and `AgreementRule`.
- **`ExpressionDraftType` covers every expression kind the domain can rebuild.** `DraftTypeCoverageTests`
  checks it both ways, so a kind added to the domain and not here fails the build rather than becoming
  quietly unauthorable.

## Surprises

- **`ExpressionDraftType` is one flat set where the domain has several.** The snapshots group kinds by what
  they store — `UnaryExpressionType`, `NaryExpressionType`, `BinaryExpressionType`, `FixedExpressionType` —
  because reconstruction dispatches on it. Nobody building a model thinks in those groups, so authoring
  offers one list and converts.
- **`Variable` is in it, and in none of the domain's.** A variable has a snapshot of its own rather than a
  kind within a grouped one.
- **There is no draft enum for relationships.** `RelationshipDraft.Type` is `BinaryOperatorType` itself, which
  is already one flat set over all fourteen operators. Nothing to convert, so nothing to drift.
- **`DraftProblemKind.DomainRule` is not a rule of this layer.** It means a domain constructor refused, and the
  message is that exception's. Every other member is a shape this layer checks itself, because the domain
  cannot be called until the shape is right.
- **`NameInvalid` is checked here and decided by the domain.** The kind is this layer's; whether a name is
  valid, and the message, come from `DimensionedExpression`'s `NodeName.Problem`.
- **`FixedValueInvalid` is checked here and decided by the domain**, as `NameInvalid` is. Which value a
  constant, a reference value or a scalar accepts comes from `DimensionedExpression`'s
  `FixedValueRule.Problem`. The kind also covers a value whose dimensionality is not the one the node declares,
  which is this layer's own check.
- **`NameMissing` and `DimensionalityMissing` apply to a constant and a reference value as to a variable.**
- **`DuplicateName` is reported once for each node that has the name; `DuplicateId` once for the id.**

## What does not belong here

- The drafts and the problem record → `Drafts/`
- Reconstruction discriminators → `Calcusystem.DimensionedExpression.Enums`

## Related

`Drafts/` · `Calcusystem.DimensionedExpression.Enums` (`BinaryOperatorType`, used directly by
`RelationshipDraft`).

See the [assembly README](../README.md) for how the two build passes divide the work with the domain.

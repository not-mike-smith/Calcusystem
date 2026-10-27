# Calcusystem.Api.Reports

What an analysis outcome looks like once it is JSON. Records holding ids, numbers and strings.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `Reporting` | static class | Outcome → report. The whole of this namespace's behavior. |
| `CalculationReport` | sealed record | Values, what is missing, and what each relationship did. |
| `ValueReport` | sealed record | One node's computed value, by id. |
| `OutcomeReport` | sealed record | One relationship's verdict, with both operands' values. |
| `DecompositionReport` | sealed record | The count, and the three parts of a decomposition. |
| `BlockReport` | sealed record | One part: its own count, its unknowns and its equations. |
| `UnknownReport` | sealed record | An unknown, with its symbol and dimensionality. |
| `EquationReport` | sealed record | A determining relationship and the unknowns it reaches. |
| `MeasurandReport` | sealed record | A value: KMS magnitude, dimensionality, and both uncertainty bounds. |
| `VocabularyReport` | sealed record | Every kind, role, rule and dimension a client can offer. |
| `UnitCatalogueReport` | sealed record | Every unit, grouped by what it measures. |
| `DimensionalityUnitsReport` | sealed record | One dimensionality's units, and which is the KMS one. |
| `UnitReport` | sealed record | One unit, as the two numbers `UnitOfMeasure` converts with. |
| `DimensionReport` | sealed record | One fundamental dimension, by symbol. |

## Start here

```csharp
var flat = system.Flatten();
return Reporting.Report(flat, flat.Decompose());
```

## Guarantees

- **Everything is named by id.** `Analysis` keys its overrides by `Variable` *instance*, which a
  browser cannot hold. Ids are what crosses this boundary, and a draft's ids survive its build.
- **Nothing here computes anything.** A report relays what an outcome already decided — including
  `IsViolation` and `IsInconsistency`, which stay the domain's answer rather than a rule restated in
  a client.
- **A decomposition report carries the whole system's count as well as each part's.** The system's is
  the sum over the parts, and the two come from one flattening rather than two.
- **The vocabulary is read off the enums.** A kind added to `ExpressionDraftType` is served without
  this namespace being touched.

## Surprises

- **Values are KMS doubles, and no unit is named on one.** A dimensionality is encoded
  (`"M1,L1,T-2"`), and `UnitCatalogueReport` says how to convert — but which unit to show is the
  client's choice, and nothing on the wire remembers what an author typed.
- **A unit carries the domain's own numbers under the domain's own names.** `KmsConversionFactor`
  and `ZeroOffset`, so a client's two conversion functions are `ConvertFromKmsValue` and
  `ConvertToKmsValue` spelled the same way and the two sides read against each other. What is
  carried elsewhere is the formula; an inverted factor is what that invites, and it looks right
  at exactly 1.
- **A band converts by the factor alone.** The offset would turn ±1 K into −457.87 °F.
- **The catalogue is grouped here, not read off `Lists.UnitTypes.ByDimensionality`.** That keys a
  dictionary by dimensionality and so holds one list per dimensionality; two lists sharing one
  would lose a list there and merge here, and a picker wants both sets either way.
- **Uncertainty is served as two absolute bounds**, upper and lower, rather than as the stored form.
  A view draws a bar; whether the model stored a fraction or an absolute is not its business. They
  are equal where the uncertainty is symmetric.
- **`IsUndetermined` is absent** from `OutcomeReport`, because `IsSatisfied` being null already says
  it. Two fields answering one question is the shape this codebase keeps removing.
- **A report is not a DTO.** The persisted documents live in `Calcusystem.Serialization.Dtos`; these
  are answers to a question and are never read back in.

## What does not belong here

- Anything saved → `Calcusystem.Serialization.Dtos`
- Asking `Analysis` anything → `Workflows/DraftWorkflow`
- Status codes → `Http/FailureTranslation`

## Related

`Workflows/` · `Calcusystem.Analysis.Outcomes` · `Calcusystem.Serialization.Mappers`
(`DimensionalityCodec`, which encodes every dimensionality here).
See the [assembly README](../README.md) for the wire decisions these shapes follow.

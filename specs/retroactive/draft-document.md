# Retroactive spec — the draft document

**Status:** Draft, 2026-11-11. Written from the code and tests as they stand at commit `ff3f378`. Findings decided the same day (section 9). Spec A slice 2 (fixed values) applied on 2026-11-12: DD-11, DD-13 to DD-15 and DD-19 widened, with no new field, and DD-16 changed for a scalar.
**Covers:** the wire form of a `SystemDraft`: `Serialization`'s `Dtos.SystemDraft`, `Dtos.ExpressionDraft` and `Dtos.RelationshipDraft`; `DraftMapper`; and the codecs it uses (`DimensionalityCodec`, `UncertaintyCodec`, `ProvenanceCodec`). Also the JSON conventions the API applies to it (`Api/Http/WireJson`).
**Does not cover:** the built-system document (`Dtos.ExpressionSystem` and its mappers), or what a draft does once read (that is the [Authoring spec](authoring.md)).
**Needed by:** [feature set B](../../project-plan.md#b--saving-and-loading). A saved file holds a draft, and B must keep this document's **behavior**.

---

## How to read this

**Behavior is normative; structure is not.** Nothing written before B needs to be read after it, so B introduces version 1.0.0 and is free to rename, add or regroup fields. What B must keep is the behavior: absent means not supplied (DD-3), enums by name (DD-2), how ids, slots and disagreeing values survive (DD-7, DD-10, DD-12, DD-16), the round trip (section 6) and the refusals (section 7). The field tables in sections 2 to 5 record the structure as it stands, so the behavior rules have something to point at.

Otherwise the conventions are the [Authoring spec's](authoring.md#how-to-read-this), including **Known bug** markers on rules the code does not yet meet: numbered rules (`DD-n` here), each naming its test or saying *Untested*; rationale cited from the [Serialization README](../../Serialization/README.md#a-draft-is-a-different-document), not repeated.

Test names are in `Serialization.Test/DraftWireTests.cs` unless stated.

---

## 1. Encoding

| Id | Rule | Test |
| --- | --- | --- |
| DD-1 | The document is JSON. Property names are written in PascalCase and **matched without regard to case on read.** Values are not: a node's name, an id or a unit symbol is case-sensitive. The conventions belong to `Serialization`, as one set of options the API and file reading share. **Known bug:** they live in `Api`, and read with default options a camelCase document becomes an empty draft without error ([DF6](#9-findings)). | Untested |
| DD-2 | **Enum values are written as member names,** never numbers. | Every test (they use `JsonStringEnumConverter`) |
| DD-3 | **An absent property and a null one mean the same thing: not supplied yet.** A writer may omit nulls; the API does. | `AHalfBuiltDraftSurvivesExactlyAsItWas` |
| DD-4 | **A property the reader does not know is refused** (DD-35). Once drafts report warnings, this may become a warning. **Known bug:** it is ignored ([DF5](#9-findings)). | Untested |
| DD-5 | An enum name the reader does not know is refused (DD-30). | Untested |
| DD-6 | **There is no version field.** B adds one. | — |

## 2. The root

| Id | Field | Rule | Test |
| --- | --- | --- | --- |
| DD-7 | `Id` | The draft's id. Absent: a new id is generated on read. | `ADraftSurvivesTheWireWithItsIdentityIntact` |
| DD-8 | `Name`, `Description` | Absent: read as empty text. | `ADraftSurvivesTheWireWithItsIdentityIntact` |
| DD-9 | `Expressions`, `Relationships` | Lists of nodes, **in no particular order.** Absent: empty. | `AnEmptyPayloadReadsAsAnEmptyDraft` |

## 3. An expression node

| Id | Field | Rule | Test |
| --- | --- | --- | --- |
| DD-10 | `Id` | The node's id. Absent: a new id is generated on read. | `ADraftSurvivesTheWireWithItsIdentityIntact` |
| DD-11 | `Type` | An `ExpressionDraftType` name, or absent for a node with no kind chosen. *Widened by Spec A slice 2:* `Constant`, `ReferenceValue` and `Scalar` are names of it. A fixed value uses the fields a variable uses (DD-13 to DD-19), and the document gains no field for one. | `AHalfBuiltDraftSurvivesExactlyAsItWas`, `FixedValuesSurviveTheWireAndBuildTheSameSystem`, `TheWireNamesTheFixedKindsByName` |
| DD-12 | `OperandIds` | Ordered. A null entry is an empty slot. Ids are not checked against anything. | `AnOperandListKeepsItsOrderAndItsCorrelation`, `AHalfBuiltDraftSurvivesExactlyAsItWas` |
| DD-13 | `Name` | What the node is called, on any kind of node. Absent: none. Read into the draft NFC-normalized, so a name written in another encoding of the same characters is rewritten on the next save. *Changed by Spec A slice 1:* it was `Symbol`, on variables only. | `AVariableSurvivesWithItsSymbolDimensionalityValueAndUncertainty`, `AComputedNodesNameSurvivesAndAnAbsentOneStaysAbsent`, `TheWireCallsANodesNameName` |
| DD-14 | `Dimensionality` | The declared dimensionality, encoded as in DD-24. Absent: none declared. | `AVariableSurvivesWithItsSymbolDimensionalityValueAndUncertainty` |
| DD-15 | `KmsValue` | The value in KMS base units. Absent: no value. | `AVariableSurvivesWithItsSymbolDimensionalityValueAndUncertainty` |
| DD-16 | `ValueDimensionality` | **Written only when the value's dimensionality differs from the declared one,** so that a draft holding that disagreement still holds it after the round trip. Absent: the value has the declared dimensionality, or is dimensionless when none is declared. *Changed by Spec A slice 2:* a scalar ignores what it declares, so a scalar's value is dimensionless when this is absent, and this is written whenever a scalar's value is not dimensionless. | `AValueThatDisagreesWithItsNodesDimensionalityStillDisagreesAfterTheWire`, `AScalarsValueDoesNotTakeTheNodesDeclaredDimensionality`, `AScalarsDimensionedValueStillDisagreesAfterTheWire` |
| DD-17 | `Uncertainty` | The value's uncertainty, as in DD-25. Absent with a value: the value is exact. Present without a value: refused (DD-36). **Known bug:** ignored ([DF4](#9-findings)). | `AnUncertaintyKeepsTheFormItWasStoredIn` |
| DD-18 | `ValueUnit` | The unit symbol the value was written in. Carried, never checked, never used. | `AUnitTheValueWasWrittenInIsCarriedButDecidesNothing` |
| DD-19 | `Provenance` | As in DD-26. Absent: none. | `ProvenanceSurvivesForEveryKindThatWasSupplied` |
| DD-20 | `UncertaintyCorrelation` | A name. Absent: `Uncorrelated`. | `AnOperandListKeepsItsOrderAndItsCorrelation` |

## 4. A relationship node

| Id | Field | Rule | Test |
| --- | --- | --- | --- |
| DD-21 | `Id`, `Type`, `LhsId`, `RhsId` | As for an expression: absent id is generated; absent type means none chosen; sides are not checked. | `ARelationshipSurvivesWithItsRoleRuleAndText` |
| DD-22 | `SolvingRole`, `Agreement`, `Name`, `Description`, `Provenance` | Carried as written. Absent: not supplied. | `ARelationshipSurvivesWithItsRoleRuleAndText` |
| DD-23 | `RuleLhs`, `RuleMustBe`, `RuleRhs` | A comparison rule, as three fields. **All three present make a rule, none means no rule, and anything between is refused** (DD-37). **Known bug:** a partial rule reads as no rule ([DF7](#9-findings)). | `ARelationshipSurvivesWithItsRoleRuleAndText` |

## 5. Encodings

| Id | Rule | Test |
| --- | --- | --- |
| DD-24 | **Dimensionality** is each present dimension's symbol followed by its integer exponent, comma-separated, in canonical order: force is `"M1,L1,T-2"`; dimensionless is `""`. On read, entry order does not matter and the writer re-normalizes. **The symbols are the contract.** | `DimensionalityCodecTests` |
| DD-25 | **Uncertainty** is a flat object with a `Type` of `SymmetricUncertainty` (needs `Magnitude`) or `AsymmetricUncertainty` (needs `UpperMagnitude` and `LowerMagnitude`), and `IsStoredAsAbs`, which says whether magnitudes are relative fractions or absolute KMS values. | `AnUncertaintyKeepsTheFormItWasStoredIn` |
| DD-26 | **Provenance** is a flat object with an `Id`, a `Type` naming its kind (`MeasuredProvenance`, `ReferenceProvenance`, `DesignProvenance`, `ModelProvenance`), and the fields that kind uses. A field the kind requires (a reference's `Citation`, a model's `ModelName`) must be present (DD-38). **Known bug:** a missing one is accepted ([DF3](#9-findings)). | `ProvenanceSurvivesForEveryKindThatWasSupplied` |

## 6. Round trip

| Id | Rule | Test |
| --- | --- | --- |
| DD-27 | **Write then read gives back the same draft,** field for field, including every half-built state: an untyped node, an empty slot, a reference to nothing, a value that disagrees with its node. Nothing is mended in transit. | `AHalfBuiltDraftSurvivesExactlyAsItWas`, `AValueThatDisagreesWithItsNodesDimensionalityStillDisagreesAfterTheWire` |
| DD-28 | A draft that went through the wire builds the same system as the original. | `ARoundTrippedDraftBuildsTheSameSystem` |
| DD-29 | An id shared by an expression and a relationship is read, held, and reported by the draft as `DuplicateId`. | `AnIdSharedByAnExpressionAndARelationshipIsHeldAndReported` |

## 7. Refusals

A document that cannot be **held** as a draft is refused when read. A document that can be held but would not build is never refused; the draft reports it.

| Id | Refused when | Raised as | Test |
| --- | --- | --- | --- |
| DD-30 | An enum value is not a known name. | `JsonException` | Untested |
| DD-31 | *Removed (DF1):* two nodes in one list with the same id are no longer refused. They are held, and the draft reports `DuplicateId`, as DD-29 does across lists. **Known bug:** still refused. | `FormatException` | `TwoNodesInOneListClaimingOneIdAreRefused` (to be inverted) |
| DD-32 | A dimensionality is malformed, names an unknown symbol, or repeats one. Never read as dimensionless. | `FormatException` | `AMalformedDimensionalityIsRefusedRatherThanReadAsDimensionless` |
| DD-33 | An uncertainty lacks a magnitude its `Type` needs. | `InvalidOperationException` | Untested |
| DD-34 | An uncertainty or provenance `Type` names no known kind. | `NotImplementedException` | Untested |
| DD-35 | A property the reader does not know (DD-4). | — | Untested |
| DD-36 | An uncertainty with no value (DD-17). | — | Untested |
| DD-37 | A comparison rule with one or two of its three fields (DD-23). | — | Untested |
| DD-38 | A provenance missing a field its kind requires (DD-26). | — | Untested |

**Reading returns an `Either`.** Its failure carries every refusal found, not just the first, each with a refusal kind (an enum) and where in the document it is. The *Raised as* column is today's behavior. **Known bug:** refusals are four exception types, and each stops at the first ([DF2](#9-findings)).

## 8. Not specified

- The order of nodes in either list, and of properties within an object.
- Whether a writer omits nulls (DD-3 makes both readings equal).
- The wording of any refusal message.

---

## 9. Findings

Decided 2026-11-11.

| # | Behavior found | Decision |
| --- | --- | --- |
| DF1 | Two nodes in one list with the same id are **refused** (DD-31), which predates Authoring F1. | **Bug:** hold both and let the draft report `DuplicateId`. Reporting is friendlier than refusing. |
| DF2 | Refusals arrive as **four exception types**, each stopping at the first. | **Bug:** reading returns an `Either` whose failure carries every refusal, each with a kind and a location. |
| DF3 | A reference provenance with no `Citation`, or a model provenance with no `ModelName`, is **accepted**. Confirmed with a throwaway test. | **Bug:** refuse it (DD-38). |
| DF4 | An uncertainty with no value is **dropped silently.** Confirmed with a throwaway test. | **Bug:** refuse it (DD-36). |
| DF5 | Unknown properties are **ignored** (DD-4). | **Bug:** refuse them (DD-35) until drafts report warnings. A newer minor version then fails to open in an older reader; a user who knows what they are doing can edit the version number and take the consequences. |
| DF6 | The JSON conventions live in `Api`. Read with default options, a camelCase document becomes an **empty draft** with no error. Confirmed with a throwaway test. | **Bug:** the conventions move into `Serialization`. Property names match without regard to case; values stay case-sensitive (DD-1). |
| DF7 | A comparison rule with one or two of its three fields **reads as no rule.** Read from the code. | **Bug:** refuse it (DD-37). |
| DF8 | Spec A changes this document: `Symbol` becomes `Name`, computed nodes gain names, fixed values add three `ExpressionDraftType`s. | **Superseded by Spec A.** Names done in slice 1 (DD-13); fixed values done in slice 2 (DD-11). No migration: nothing written before B is preserved, and B introduces version 1.0.0. |

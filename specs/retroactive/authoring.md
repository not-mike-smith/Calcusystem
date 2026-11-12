# Retroactive spec — Authoring: drafts and building

**Status:** Draft, 2026-11-11. Written from the code and tests as they stand at commit `4a8cabd`. Findings decided the same day (section 7). F1–F4 and F9 fixed on 2026-11-11 (branch `fix-authoring-known-bugs`). Spec A slice 1 (names) applied on 2026-11-11: AU-4, AU-10, AU-20, AU-32 and AU-38 changed, and AU-41 to AU-46 added. Spec A slice 2 (fixed values) applied on 2026-11-12: AU-10, AU-26, AU-32, AU-38, AU-39, AU-42 and AU-45 changed, AU-47 to AU-52 added, and F10 found and fixed.
**Covers:** `Calcusystem.Authoring`: `SystemDraft`, `ExpressionDraft`, `RelationshipDraft`, `DraftProblem`, `ExpressionDraftType`, `DraftProblemKind`.
**Does not cover:** a draft's wire form (`Serialization`'s `Dtos.SystemDraft` and `DraftMapper`), the HTTP routes over it (`Api`), or what the domain constructors refuse (`DimensionedExpression`).
**Needed by:** [Spec A](../A-building-a-model.md), which puts every C# model through `SystemDraft`.

---

## How to read this

- **Each rule is normative.** Code that breaks a rule has a bug. Behavior that no rule covers is unspecified: it may change without notice, and nothing may rely on it.
- **Rules have ids** (`AU-n`) so a bug report, a test or another spec can cite one. Ids are never reused.
- **Each rule names the test that holds it,** or says *Untested*. An untested rule is still a rule; it is a gap in coverage.
- **Rationale stays in the READMEs.** This spec says *what*; the [assembly README](../../Authoring/README.md) and the [Drafts README](../../Authoring/Drafts/README.md) say *why*, and are cited instead of repeated.
- **Changing a rule** is a decision, recorded here with its date, not a side effect of a code change.
- **A rule can disagree with today's code.** Where a finding was ruled a bug, the rule states the intended behavior and is marked **Known bug**, linking to the finding. The code is what is wrong.
- **A rule marked *Superseded by Spec A*** holds until Spec A is implemented, and then changes as Spec A says.

Test names below are in `Authoring.Test/SystemDraftTests.cs` unless stated.

---

## 1. Holding a draft

| Id | Rule | Test |
| --- | --- | --- |
| AU-1 | `SystemDraft.Create(name, description, id)` makes an empty draft. With no id, it generates one. | Untested |
| AU-2 | A node exists only inside a draft. `ExpressionDraft` and `RelationshipDraft` have internal constructors; `AddExpression`, `AddVariable` and `AddRelationship` are the only ways to make one. | Untested (enforced by the compiler) |
| AU-3 | A node's id is fixed when the node is created. With no id given, a new GUID is generated. **Adding a node with an id the draft already holds keeps both nodes;** the duplicate is reported (AU-23). | `ANodesIdCarriesThroughToTheBuiltExpression`, `ReusingAnExpressionsIdKeepsBothNodesAndReportsIt`, `ReusingARelationshipsIdKeepsBothAndReportsIt` |
| AU-4 | `AddVariable(name, dimensionality, id)` is `AddExpression(Variable, id)` with the name and dimensionality set. | Untested |
| AU-51 | `AddConstant(name, value, id)` and `AddReferenceValue(name, value, id)` are `AddExpression` of that kind with the name and value set, and the dimensionality set to the value's, or to none where no value is given. `AddScalar(number, id)` is `AddExpression(Scalar, id)` with the value set to that number, dimensionless and with no uncertainty, or to none. | `TheAddMethodsSetWhatTheKindReads` (`DraftFixedValueTests.cs`) |
| AU-5 | **No member throws because a model is wrong.** Every field can be set to any value, including null, an empty operand slot, a reference to nothing, and a cycle. Nothing is checked when it is set. | `AnyHalfFinishedStateCanBeHeldWithoutThrowing` |
| AU-6 | `Expressions` and `Relationships` hold every node of their kind, **in no particular order.** | Untested |
| AU-7 | `Expression(id)` and `Relationship(id)` return the node with that id, or null. Where the id is reused, they return the first node added with it. | Untested |
| AU-8 | `Remove(id)` removes every node with that id, expression or relationship, and returns whether anything was removed. **References to a removed node stay in place** and are reported as `ReferenceNotFound` (AU-24). | `RemovingANodeLeavesReferencesToItDanglingAndReported`, `RemovingAReusedIdRemovesEveryNodeWithIt` |
| AU-9 | **Changing an expression's `Type` clears nothing.** Fields that do not apply to the current kind are kept and ignored. | Untested |
| AU-10 | Which fields each expression kind reads. All others are ignored: | Untested |
| | • **Every kind but a scalar:** `Name`. | |
| | • **Variable, Constant, ReferenceValue:** `Dimensionality`, `Value`, `Provenance`. | |
| | • **Scalar:** `Value` only. Its `Name`, `Dimensionality` and `Provenance` are ignored (AU-50). | |
| | • **Every leaf (the kinds above):** `OperandIds` is ignored (AU-52). `ExpressionDraftType.IsLeaf()` says which kinds those are. | |
| | • **Sum, Product, Quotient:** `OperandIds`, `UncertaintyCorrelation`. | |
| | • **Negated, Reciprocal, Sqrt, Exponential, NaturalLog:** `OperandIds` only. | |
| AU-11 | `OperandIds` is ordered. A null or blank entry is an empty slot. Order matters where the kind cares: a quotient divides the first operand by the second. | `AnEmptyOperandSlotIsReported` |
| AU-12 | `ValueUnit` records the unit a value was written in. Nothing reads it while building, it is never checked, and it is not carried into the built system. | Untested |
| AU-13 | Which fields each relationship reads: `Type`, `LhsId`, `RhsId`, `SolvingRole`, `Name`, `Description`, `Provenance`; `Agreement` for an equality only; `Rule` for a simple comparison only. Otherwise those two are ignored. | Untested |

## 2. Building: the outcome

| Id | Rule | Test |
| --- | --- | --- |
| AU-14 | `Build()` returns an `Either<ExpressionSystem>`: the system when there are no problems, otherwise a failure. **There is no third outcome,** and `Build` does not throw because a model is wrong. | `AFailedBuildCarriesEveryProblemAsTheFailuresDetail` |
| AU-15 | A failure is a `Failure<IReadOnlyList<DraftProblem>>` with reason `Unprocessable`, carrying **every** problem found. | `AFailedBuildCarriesEveryProblemAsTheFailuresDetail`, `ACallerThatOnlyAsksWhetherItBuiltNeedsNoneOfThat` |
| AU-16 | `Validate()` returns exactly the problems a `Build()` of the same draft would carry, and is empty exactly when `Build()` succeeds. | `ValidateAndBuildNeverDisagree` |
| AU-17 | **Building reads the draft and changes nothing in it.** Each build makes a new system. | Untested |
| AU-18 | The built system has the draft's id, name and description. | `ANodesIdCarriesThroughToTheBuiltExpression` |
| AU-19 | **Every expression draft becomes a node with the same id** in the built system, including one that nothing refers to. Every relationship draft becomes an operator with the same id. | `ANodesIdCarriesThroughToTheBuiltExpression`, `ADraftOfNewtonsSecondLawBuilds` |
| AU-20 | **A built variable** has the draft's `Name` as its name, and a variable without one is a problem (AU-42). *Changed by Spec A slice 1:* it was `Symbol`, with the id as a fallback. It has the draft's dimensionality and provenance. Its value is assigned after construction, so a value whose dimensionality disagrees is refused (AU-30). | `AValueDisagreeingWithItsVariablesDimensionalityIsReported` |
| AU-21 | **A built relationship** has the draft's name, description, provenance, agreement and rule where they apply. **Every relationship other than an equality is a requirement;** with no role stated, it builds as one. **An equality builds with the role its draft states, and must state one** (AU-29). A role other than `Requirement` set on a non-equality is a problem (AU-40). | `ARelationshipBuildsAndKeepsItsSolvingRole`, `AnUnstatedSolvingRoleBuildsARequirement`, `ARequirementStatedOnANonEqualityBuilds` |
| AU-22 | An empty draft builds an empty system. | `AnEmptyDraftBuildsAnEmptySystem` |
| AU-47 | **A built fixed value** is a `Constant`, a `ReferenceValue` or a `Scalar`, by its draft's kind, with the draft's value. A constant and a reference value have the draft's name and provenance and are in the system's `Givens`. A scalar has neither and is in no public list. None is in `Variables`. | `EachKindBuildsIntoItsOwnNodeAndNoneIsAVariable`, `AGivensProvenanceCarriesThroughToTheBuiltNode`, `AFixedValuesIdCarriesThroughToTheBuiltNode` (`DraftFixedValueTests.cs`) |

## 3. Problems

A `DraftProblem` has a `Kind`, the `NodeId` at fault, and a `Message`. The message is for people; a caller branches on `Kind`.

| Id | Kind | Reported when | On |
| --- | --- | --- | --- |
| AU-23 | `DuplicateId` | Two nodes have the same id: two expressions, two relationships, or one of each. Reported once for the id. Expressions sharing an id are not built, since an operand naming it could mean either, so nodes referring to it stay silent (AU-35). | That id |
| AU-24 | `ReferenceNotFound` | An operand slot, or a relationship's side, names an id that no **expression** draft has. An id that only a relationship has counts as nothing. | The node holding the reference |
| AU-25 | `TypeNotChosen` | An expression or a relationship has no `Type`. | That node |
| AU-26 | `DimensionalityMissing` | A variable, a constant or a reference value has no `Dimensionality`. There is no default. *Widened by Spec A slice 2.* | That node |
| AU-27 | `OperandMissing` | An expression has an empty operand slot, or a relationship has an empty side. | That node |
| AU-28 | `OperandCountWrong` | A unary kind (Negated, Reciprocal, Sqrt, Exponential, NaturalLog) does not have exactly one operand, a quotient does not have exactly two, or a sum or product has none. | That expression |
| AU-29 | `RelationshipRuleMissing` | An equality has no `Agreement` or no `SolvingRole`, or a simple comparison has no `Rule`. | That relationship |
| AU-30 | `DomainRule` | The domain refused to construct the node, or `ExpressionSystem.Add` refused a relationship (sides of different dimensionality). The message is the domain's exception message. | That node |
| AU-40 | `RoleNotAllowed` | A relationship other than an equality states a solving role other than `Requirement`. | That relationship |
| AU-41 | `DuplicateName` | Two or more nodes have the same valid name: expressions, relationships, or both. Reported **once for each node** that has it, in addition to any other problem (as `DuplicateId` is). A node with a duplicate name is still built, so nodes referring to it are still checked. An invalid name is never a duplicate. | Each node with that name |
| AU-42 | `NameMissing` | A variable, a constant or a reference value has no name. *Widened by Spec A slice 2.* | That node |
| AU-43 | `NameInvalid` | A node's name breaks the rule for names (Spec A §4.1). The rule and the message are the domain's, from `NodeName.Problem`. | That node |
| AU-48 | `ValueMissing` | A constant, a reference value or a scalar has no `Value`. | That node |
| AU-49 | `FixedValueInvalid` | A fixed value's `Value` is not one its kind accepts: a constant with uncertainty, a reference value with none, a scalar with uncertainty or a dimension. The rule and the message are the domain's, from `FixedValueRule.Problem`. Also reported, with this layer's own message, where a constant's or a reference value's `Value` has a dimensionality other than the one the node declares. | That node |
| AU-31 | `CircularReference` | An expression is reached from its own operands. Only nodes **in** the cycle are reported; a node that depends on a cycle says nothing (AU-35). | Each node in the cycle |

Tests for AU-41 to AU-43 are in `Authoring.Test/DraftNameTests.cs`: `ADuplicateNameIsReportedOnceForEachNodeInvolved`, `ARelationshipSharingAnExpressionsNameIsADuplicate`, `NamesThatNormalizeAlikeAreDuplicates`, `ADuplicateNameDoesNotHideOtherProblems`, `AVariableWithNoNameIsReported`, `AnInvalidNameIsReportedOnWhicheverNodeHasIt`, `ANameThatIsNotValidUnicodeIsReportedAsInvalid`.

Tests for AU-48 and AU-49, and for AU-26 and AU-42 on a fixed value, are in `Authoring.Test/DraftFixedValueTests.cs`: `AFixedValueWithNoValueIsReported`, `AValueTheKindDoesNotAcceptIsReportedAsTheDomainStatesIt`, `AGivenWhoseValueDisagreesWithItsDeclaredDimensionalityIsReported`, `AGivenWithNoNameIsReported`, `AGivenWithNoDimensionalityIsReported`.

Tests: `ANodeWithNoKindChosenIsReported`, `AVariableWithNoDimensionalityIsReported`, `AnEmptyOperandSlotIsReported`, `TheWrongNumberOfOperandsIsReported`, `AnOperandNamingNothingIsReported`, `ACycleIsReportedRatherThanHanging`, `ASumOfDisagreeingDimensionalitiesIsReportedAsTheDomainStatesIt`, `AnExponentialOfADimensionedArgumentIsReported`, `ASqrtOfAnOddExponentIsReported`, `AValueDisagreeingWithItsVariablesDimensionalityIsReported`, `ARelationshipAcrossDimensionalitiesIsReported`, `AnEqualityWithNoAgreementRuleIsReported`, `ASimpleComparisonWithNoRuleIsReported`, `AnEqualityWithNoSolvingRoleIsReported`, `ARoleOtherThanRequirementOnANonEqualityIsReported`, `ANodeAboveACycleStaysSilent`, `ANodeThatIsItsOwnOperandIsReported`, `AnIdSharedByAnExpressionAndARelationshipIsReported`.

| Id | Rule | Test |
| --- | --- | --- |
| AU-32 | **At most one problem per node,** apart from `DuplicateId` and `DuplicateName`, which are reported in addition. A node's checks run in a fixed order and stop at the first that fails: for an expression, kind, then name, dimensionality (variables, constants and reference values), a missing value and then the value's rule (fixed values), empty slots, operand count, references, then the domain; for a relationship, kind, name, left side, right side, rule and role, then the domain. | `AFixedValueReportsOnlyItsFirstProblem` (`DraftFixedValueTests.cs`), for a fixed value |
| AU-33 | **The order of problems in the list is unspecified.** | — |
| AU-34 | `NodeId` is null only for a problem about the draft as a whole. No such problem exists today. | Untested |

## 4. One cause, one problem

| Id | Rule | Test |
| --- | --- | --- |
| AU-35 | An expression whose operand could not be built says nothing. Only the operand that failed is reported. A cycle counts as the failure for the nodes above it (AU-31). | `NodesAboveAFailedOneStaySilent`, `NodesReferringToAReusedIdStaySilent`, `ANodeAboveACycleStaysSilent` |
| AU-36 | A relationship whose side could not be built says nothing. | `NodesAboveAFailedOneStaySilent` |
| AU-37 | Silence spreads through every level above a failed node, however deep. | `NodesAboveAFailedOneStaySilent` |

## 5. Domain rules are relayed, never restated

| Id | Rule | Test |
| --- | --- | --- |
| AU-38 | The draft checks only shape: AU-23 to AU-29, AU-31, AU-41 to AU-43, and AU-48. What makes a name valid is the domain's rule, asked through `NodeName.Problem` and reported as `NameInvalid`. Which value a fixed value accepts is the domain's rule too, asked through `FixedValueRule.Problem` and reported as `FixedValueInvalid` (AU-49). Every other rule comes from the domain, through `DomainRule`. A rule added to a domain constructor is reported by drafts with no change to `Authoring`. | `ASumOfDisagreeingDimensionalitiesIsReportedAsTheDomainStatesIt` |
| AU-39 | `ExpressionDraftType` covers exactly the expression kinds the domain can rebuild, and neither enum has a member equal to zero. | `DraftTypeCoverageTests` |

## 5a. Names

Tests are in `Authoring.Test/DraftNameTests.cs`.

| Id | Rule | Test |
| --- | --- | --- |
| AU-44 | A draft holds a name NFC-normalized, and otherwise as given. Setting one never throws; text that is not valid Unicode is held unchanged. **Null and the empty string both mean no name.** | `ADraftHoldsANameNormalized`, `AnyNameCanBeHeldWithoutThrowing`, `AComputedNodeOrARelationshipNeedsNoName` |
| AU-45 | `ByName(name)` returns the expression with that name (never a scalar, AU-50), and `RelationshipByName(name)` the relationship, or null. Names are compared after normalization and case-sensitively. Where a name is reused, the first node added with it. | `ANodeIsFoundByItsNameInADraft`, `WhereANameIsReusedTheFirstNodeAddedIsFound`, `AnUnnamedNodeIsNotFoundByAnEmptyName` |
| AU-46 | A computed node and a relationship build with the draft's name, or with none. `ExpressionSystem.Add` can refuse an expression only for a duplicate name, which AU-41 reports first, so `Build` does not guard that call (F8). | `AComputedNodesNameCarriesThroughToTheBuiltNode`, `ADraftWithADuplicateNameDoesNotBuild` |

## 5b. Fixed values

Tests are in `Authoring.Test/DraftFixedValueTests.cs`.

| Id | Rule | Test |
| --- | --- | --- |
| AU-50 | **A scalar's `Name` is ignored,** like any field that does not apply to a node's kind. It is not checked, is never a duplicate, and `ByName` does not find the scalar by it. The built scalar has no name. Its `Dimensionality` and `Provenance` are ignored the same way. | `AScalarsNameIsIgnored`, `AScalarIgnoresADeclaredDimensionalityAndAProvenance` |
| AU-52 | **A leaf ignores `OperandIds`:** a variable, a constant, a reference value and a scalar. Entries left over from another kind, including an empty slot and a reference to nothing, are not reported and do not stop the node building. | `ALeafIgnoresOperandsLeftOverFromAnotherKind` |

## 6. Not specified

Nothing may rely on these:

- The order of `Expressions`, `Relationships` and the problem list.
- The wording of any problem message.
- How many passes a build takes, or the order in which nodes are constructed.

---

## 7. Findings

Found while writing this spec. Each was current behavior that was undocumented, contradicted a doc comment, or conflicted with Spec A. Decided 2026-11-11 unless marked open.

| # | Behavior found | Decision |
| --- | --- | --- |
| F1 | `AddExpression` or `AddRelationship` with an id the draft already holds **replaces the earlier node silently.** Confirmed with a throwaway test. | **Bug** against AU-3 and AU-23: keep both, report `DuplicateId`. **Fixed.** |
| F2 | An equality with no `SolvingRole` builds as a `Requirement`, contradicting `RelationshipDraft.SolvingRole`'s doc ("an equality must say which it is"). | **Bug** against AU-21 and AU-29: a missing role on an equality is a problem. Matches Spec A, where the builder requires a role. **Fixed.** |
| F3 | A non-equality given `Equation` or `Coherence` **silently builds as a `Requirement`.** Confirmed with a throwaway test. Every non-equality *is* a requirement; the question is what to do when an author states otherwise. | **Bug** against AU-21 and AU-40: report it as a problem, so the author learns the role was not honored. Once drafts can report warnings, this may become a warning instead (see the plan's *Draft warnings* chore). **Fixed.** |
| F4 | A node that depends on a cycle, without being in it, is reported as `CircularReference`, with a message that is false for it. Confirmed with a throwaway test. | **Bug** against AU-31 and AU-35: only nodes in the cycle are reported. **Fixed.** |
| F5 | A variable with no `Symbol` builds with its id as its symbol. | **Superseded by Spec A** (AU-20): it becomes a problem. **Done** in slice 1 (AU-42). |
| F6 | The assembly README says `Build` makes snapshots for every node; variables are built with `new Variable(...)`. | **Fixed by Spec A** (slice 3 builds everything through snapshots). Not a bug in the meantime; correct the README when A lands. |
| F7 | `Remove(id)` removes both an expression and a relationship that share an id. | **Intended** (AU-8). Sharing an id is the problem, and it is reported (AU-23). Spec A's builder never takes an id, so builder-made drafts cannot collide. |
| F8 | `Build` calls `ExpressionSystem.Add` for each expression without catching anything, which is safe only because `Add(IExpression)` cannot throw today. | **Superseded by Spec A,** which makes `Add` refuse duplicate names: `Build` must relay a refused `Add` as `DomainRule`, as it already does for relationships. **Reversed 2026-11-12:** the relay was added in slice 1 and no draft could reach it, because AU-41 reports every duplicate name before `Add` runs. An unreachable guard would turn a defect in `Build` into a `DomainRule` problem blamed on the model, so the call is unguarded and a throw there is a bug (AU-46). |
| F10 | Found by Spec A slice 2, 2026-11-12. A variable whose `OperandIds` held an empty slot made `Build` throw `ArgumentNullException`, and one holding an id that was not built was itself never built and never reported, so the built system silently lacked it. | **Bug** against AU-5 and AU-10. **Fixed** in slice 2 for every leaf kind (AU-52). |
| F9 | `DuplicateId` has no test. | **A test is needed,** for AU-23 and the F1 fix. **Done.** |

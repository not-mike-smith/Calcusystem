# Spec A — Building a model in C# code

**Status:** Draft. Rescoped on 2026-11-11: reading formula text moved to [Spec H](H-formula-text.md). Later the same day, C# construction moved onto drafts (section 4.3). Loose: it fixes the goal, the shape, and the decisions. It does not fix signatures.
**Plan:** [feature set A](../project-plan.md#a--building-a-model-in-c-code).
**Depends on:** the retroactive Authoring spec (section 8). **Designed together with:** E1 (pairwise correlation). **Unblocks:** B (constants on the wire), C1, F, H.

---

## 1. Problem

Building a model in C# takes more ceremony than the formula it encodes. The worked examples show it. The time to drain a tank is one line on paper:

```text
t = (A_tank / (C_d·A₀))·√(2h₀/g)
```

In C# today it is a nested `SqrtExpression` around a `QuotientExpression` with required `Numerator`/`Denominator` init properties, built inside-out, with ids supplied three different ways. The causes:

1. A leaf needs its symbol twice: `new Variable("h₀", measurand, "h₀")`. The id is spelled out because nothing else gives a node a name a reader can use.
2. Construction takes three shapes for one concept:
   - `Sqrt`, `Reciprocal`, `Negated`, `Exponential` and `NaturalLog` take an id argument;
   - `Product` and `Sum` take an `{ Id = … }` initializer;
   - `Quotient` takes neither, and names its operands as required init properties.
3. A constant must be a `Variable`. `π/4` and `2` appear in `system.Variables` and carry a provenance slot.
4. A tree reads inside-out.
5. A modelling mistake throws at the first one found, so a newcomer fixes problems one exception at a time.
6. Nothing prints a model the way a person would write it, so a report shows ids and node kinds.

## 2. Goals

1. One builder, with one shape for every node kind, that lists everything a model can contain.
2. **Mistakes come back as a report,** all at once, not as exceptions one at a time.
3. Ids belong to the system. A C# model builder works with **names** and never sees an id.
4. Fixed values (constants, reference values, scalars) are their own node kinds, not variables.
5. A model prints as human-readable formula text.

## 3. Non-goals

- **Reading text into a model.** That is [Spec H](H-formula-text.md).
- **Printed text that parses.** The printer is for people. Two different trees may print alike.
- New expression kinds. Powers, trig, `abs`/`min`/`max` belong to F. The power helper (section 4.3) builds a product until then.
- Pairwise correlation. It belongs to E1, but section 7 makes sure A does not block it.
- Editor UI.

---

## 4. Behavior

### 4.1 Ids and names

**An id is for the system; a name is for the model builder.**

1. Every node has an id. Drafts generate ids, and the model builder never takes or shows one.
2. **Who must have a name:**
   - A variable must have one. Its existing `Symbol` becomes its `Name`. A variable draft without a name is a draft problem; the draft no longer falls back to the id.
   - A constant and a reference value must have one.
   - A scalar has none (section 4.2).
   - A computed node's name is optional. Computed nodes gain one, with snapshot, draft and wire forms.
   - Relationship operators already have an optional `Name`.
3. **A name is fixed once a node is built.** It is set at construction and cannot change, so uniqueness within a system cannot be broken afterward. A draft's names stay editable.
4. **A valid name:**
   - is not empty and not only whitespace, and has no leading or trailing whitespace;
   - contains no invisible formatting or control characters (Unicode categories Cf and Cc), which rules out zero-width characters and the characters that change text direction;
   - is compared case-sensitively, after Unicode NFC normalization, so two names that are the same characters encoded differently compare equal.
   - Emoji are allowed. Emoji joined by a zero-width joiner (the family emoji, for example) are not, because the joiner is a format character.
   - Look-alike characters from different scripts (Latin `a`, Cyrillic `а`) are allowed. Nothing guards against mixing them.
5. **Names are unique** within a draft and within a system, across expressions and relationships alike: one namespace, so `system.Find(name)` is never ambiguous.
   - A draft reports a duplicate as a new problem kind, `DuplicateName`, once for each node involved, in the same early pass that reports duplicate ids.
   - `ExpressionSystem.Add` throws on a node whose name another node already has. It guards systems rebuilt from saved data.
6. **Building is atomic.** `SystemDraft.Build` returns a system only when nothing went wrong, as it does today. `ExpressionSystem.Add` checks every node it would absorb before absorbing any, so a refused `Add` leaves the system exactly as it was. Drafts may be half-built.
7. **Finding by name.** `draft.ByName(name)` and `system.Find(name)`. To read a value by name, find the node in the system and pass it to the existing `calculation.ValueOf(node)`:

   ```csharp
   calc.ValueOf(system.Find("t_drain")!);
   ```

   A shortcut on `Calculation` is added only if the Examples read badly without it.

### 4.2 Fixed values: constants, reference values and scalars

A fixed value is set at construction and can never be set or unset afterward. `Flatten` never counts one as an unknown, and `Calculate` treats it as valued. There are three kinds:

| Kind | Named | Uncertainty | Provenance | Listed in the system | Example |
| --- | --- | --- | --- | --- | --- |
| **Constant** | Required | Exactly zero | Optional | `system.Givens` | Standard gravity, 9.80665 m/s², exact by definition |
| **Reference value** | Required | Non-zero | Optional; missing provenance becomes a warning once drafts report warnings | the same list | The gravitational constant *G* |
| **Scalar** | Never | Exactly zero | None | Not listed | The `2` and `π/4` in `π/4·D²` |

1. **A constant with uncertainty is a reference value,** and a reference value without uncertainty is a constant. The draft reports the wrong one as a problem rather than switching kinds silently.
2. **A scalar is part of the formula it appears in,** not a quantity of the model. It is always **dimensionless**. A dimensioned value without a name is not a scalar; it would need a different domain concept, which A does not add. A scalar keeps an internal id, like every node, which no model builder sees.
3. **The interfaces.** A shared leaf parent, with fixed and settable leaves as siblings. A variable would break a parent's promise that its value never changes, so the settable leaf cannot derive from the fixed one.

   ```text
   IExpression
   ├── ILeafExpression           no children; a readable value
   │   ├── IFixedExpression         value set at construction, never null
   │   │     Constant, ReferenceValue, Scalar
   │   └── IDirectExpression        value settable, may be null
   │         Variable
   └── IComputedExpression       children; a correlation setting
   ```

   This tree renames the earlier draft's `IConstantExpression` to `IFixedExpression`, because a reference value and a scalar are fixed without being constants.
4. **The name `ReferenceValue`** is close to the existing `Reference` provenance kind. That fits: a reference value usually has `Reference` provenance, though it is not required.
5. **Drafts.** Each kind is an `ExpressionDraftType`. Each reuses the draft's `Value`, `ValueUnit`, `Dimensionality` and `Provenance` fields. Each has snapshot, draft and wire forms, and a vocabulary entry. `ExpressionSystem` gains one list, `Givens`, holding constants and reference values, with a matching id list in its snapshot.

### 4.3 The model builder

**C# construction goes through a draft.** The builder is a typed layer over `SystemDraft`. It builds draft nodes, and `Build()` returns the system or every problem at once.

```text
model builder ──▶ SystemDraft ──Build()──▶ snapshots ──FromSnapshot──▶ ExpressionSystem
editor (C1) ────▶      ▲
formula text (H) ──────┘
```

1. **One door.** The builder, the editor and (later) formula text all produce drafts. `SystemDraft.Build` is the only code that turns a model into domain nodes. This reverses Spec H's earlier "two front doors" decision.
2. **Mistakes are problems, not exceptions.** A modelling mistake (dimensions that do not agree, a missing value, a duplicate name) becomes a `DraftProblem` from `Build()`, which returns `Either<ExpressionSystem>`. The builder throws only for a programming mistake, such as a null argument or a handle from a different builder.
   - A caller who wants an exception throws it. If it is easy, `Either` gains a small helper, such as `ValueOrThrow()`, that builds the exception from the failure, listing every draft problem.
3. **Handles.** Each builder method returns a handle: a receipt for the draft node it made. A handle is what later methods take as an operand. It is not an `IExpression`, so nothing can calculate with it until `Build()`.

   ```csharp
   var bldr = ModelBuilder.Create("Newton's second law");
   var m = bldr.Variable("m", Mass.Kilogram.Quantity(2).WithoutUncertainty());
   var a = bldr.Variable("a", Acceleration.MeterPerSecondSquared.Quantity(3).WithoutUncertainty());
   var F = bldr.Product([m, a], name: "F");   // takes handles, returns a handle
   var built = bldr.Build();                  // Either<ExpressionSystem>; the node named "F" exists only now
   ```

   **A handle is a new, opaque type.** It exposes nothing that could break the draft, it knows which builder made it, and the builder refuses a handle from another builder. Knowing its builder is also what a fluent layer or C# operators need (section 5).
4. **Domain constructors are internal** (section 5). `SystemDraft.Build` builds each node by making its snapshot and calling the public `FromSnapshot`, which already takes ids. No other public entry point takes an id.
5. **One shape.** Every method takes its operands first, then an optional name. No method takes an id.
6. **Required arguments are required.** A relationship's agreement rule, comparison rule and solving role are parameters with no default. A default can be added later; a new requirement would break existing code.
7. `Product` and `Sum` take an optional correlation setting, because cases like `V·I_total` still need one until E1.
8. **The power helper,** `Power(x, n)`:
   - builds its own product of `n` copies of `x`, marked `Correlated`, and never merges it into a surrounding product;
   - wraps that product in a reciprocal when `n` is negative, and the name goes on the outer reciprocal;
   - accepts `n ≥ 2` or `n ≤ −1`, and throws otherwise (`x^0` and `x^1` are not nodes worth building);
   - so `Product(m, Power(r, 2))` gives `r` its correlation with itself while `m` and `r` stay independent: the right uncertainty for `I = ½·m·r²` today. F replaces it with a real power node.
9. This absorbs the plan item *Factory-construct the binary operators*. The existing `ExpressionFactory` (which builds only from snapshots) keeps its name and job.

### 4.4 The printer

1. Any built expression, relationship or system prints as human-readable text, in **Unicode** (`√`, `·`, subscripts as written). It lives in `DimensionedExpression`.
2. **It prints built nodes only.** Printing a half-built draft does not help this feature set. The API can return printed text for whatever builds.
3. **It is not required to parse back.** Ambiguity is accepted where it reads better. For example, a `Reciprocal` and a `Quotient` with numerator 1 both print as `1/x`.
4. A named node prints by its name where it appears inside another formula. An unnamed node prints inline, and a scalar prints as its value.
5. A definition prints as `name := formula`. A relationship prints as `lhs <symbol> rhs`, using the operator's own `Symbol` (for example `·==·`).
6. A system prints its named definitions in dependency order, then its relationships, one per line. **Values print with their uncertainty** beside each variable, constant and reference value.
7. Parentheses appear only where precedence needs them.
8. **Correlation is shown.**
   - A `Correlated` product of identical operands prints as `x^n`.
   - Any other `Correlated` node prints in brackets with a tag, for example `⟦V·I_total⟧ᶜ`.
9. Ids never appear in printed text.

### 4.5 Adoption

1. Every Example is rewritten with the model builder.
2. Each Example also prints its model with the printer.
3. Each worked Example's computed values do not change. `Examples.Test` already pins them to hand-worked values, so it is the regression test.
4. The DimensionedExpression and Authoring READMEs show the builder. Their `ReadmeSnippets` compile it.

---

## 5. Constructors and the builder: tradeoffs

**Decided (2026-11-11): Option 1,** with the factory being the draft-based model builder (section 4.3). Domain constructors become internal. This reverses an earlier choice of Option 2 the same day: once the constructors had to change shape to be uniform, making them internal cost little more. Moving the builder onto drafts came after, for its problem report. The options are kept below as the record of why. They were written about a factory that built domain nodes directly; the tradeoffs carry over to the builder.

### Option 1 — Factory only; constructors internal

| For | Against |
| --- | --- |
| Exactly one way in, so anything the factory does (defaults, E1's correlation lookup) is guaranteed for every node, not just likely | The Examples cannot show the constructors, which the plan says is the point of one of them |
| Matches the precedent set by provenance | Tests in other assemblies need `InternalsVisibleTo` or must switch to the factory |
| Constructor signatures can change freely | |

### Option 2 — Factory, with constructors left public

| For | Against |
| --- | --- |
| The factory is the documented, discoverable path; the constructors stay as a lower-level route | Two ways to build each node, to document and to keep in step |
| The constructor Example keeps working | Anything only the factory does can be bypassed. Whatever E1 needs must live where both routes reach it (the base constructor, or the system at calculation time) |
| No churn in existing tests | A constructor change is a breaking change again |

### Option 3 — A fluent layer over the factory

For example: `h0.DividedBy(g).Times(2).Sqrt(name: "v_jet")`.

| For | Against |
| --- | --- |
| Reads left to right, in formula order, which removes the inside-out problem | Precedence is method-chain order, not arithmetic: `a.Plus(b).Times(c)` is `(a+b)·c`, which surprises |
| IntelliSense on any expression shows what can follow it | `a.Times(b).Times(c)` nests two products unless the layer flattens them, and flattening changes which operands share a correlation setting |
| A thin set of extension methods over the factory | Naming a node partway through a chain is awkward |

### Option 4 — C# operators over the factory

For example: `F = m * a`.

| For | Against |
| --- | --- |
| Reads like the formula, with arithmetic precedence | An operator cannot take a name. That is acceptable now that names are optional for computed nodes |
| Short | `a * b * c` nests unless the operator flattens unnamed products, with the same correlation effect as Option 3 |
| | `^` in C# is exclusive-or, with the wrong precedence, so powers stay a method call |
| | Operators must be declared on a type the operands have, which the handle type makes possible |

### Decision

- **The builder is the only way in.** Domain constructors are internal. `FromSnapshot` stays public, for drafts and saved data.
- **The constructor Example goes.** The plan's constraint that "the sugar must not become the only documented path" is reversed deliberately.
- **Consequence for E1.** Every node is built by `SystemDraft.Build`, so E1 may do its correlation lookup there.
- **Spike: a fluent layer.** Once the Examples are built with the builder, try a fluent layer (Option 3) over it on one or two of them, and see how it reads and what goes wrong. Keep it, change it or drop it based on that. Operators (Option 4) can be tried in the same spike. Both rely on the handle knowing its builder (section 4.3).

---

## 6. Sketch

The drain-time formula with the model builder. Names and shape are illustrative, not decided.

```csharp
var bldr = ModelBuilder.Create("Orifice drain time");

var h0 = bldr.Variable("h₀", Length.Meter.Quantity(3.200).WithUncertainty(0.010.Units(Length.Meter)),
                        ProvenanceFactory.Measured("LT-204", new DateOnly(2026, 3, 14)));
var g  = bldr.Constant("g", Acceleration.MeterPerSecondSquared.Quantity(9.80665),
                        ProvenanceFactory.Reference("CGPM 1901", year: 1901));
var quarterPi = bldr.Scalar(Math.PI / 4);

var tankArea    = bldr.Product([quarterPi, bldr.Power(D, 2)], name: "A_tank");
var orificeArea = bldr.Product([quarterPi, bldr.Power(d0, 2)], name: "A₀");

bldr.Product(
    [
        bldr.Quotient(tankArea, bldr.Product([Cd, orificeArea])),
        bldr.Sqrt(bldr.Quotient(bldr.Product([bldr.Scalar(2), h0]), g)),
    ],
    name: "t_drain");

Either<ExpressionSystem> built = bldr.Build();   // the system, or every problem at once
```

Printed:

```text
t_drain := A_tank/(C_d·A₀)·√(2·h₀/g)
```

This is still built inside-out, though with less ceremony. Whether that is good enough is what the fluent-layer spike tests.

---

## 7. Interaction with E1 (pairwise correlation)

Today a node carries one `UncertaintyCorrelation` for all of its operands.

- **Powers are covered** by the power helper (section 4.3).
- **Other repeated leaves are not.** `Product([D, D])` written out, or a variable reached by two operands (`V·I_total` where `I_total` depends on `V`), still takes the node's single setting. That is E1's problem, and it is no worse than today. The printer marks such nodes (section 4.4).

**Recommendation:** implement E1 after A. Every node is built in `SystemDraft.Build`, so E1 is free to do its correlation lookup there.

---

## 8. Leans on unspecified behavior

**This spec now leans heavily on `SystemDraft`,** because every C# model goes through it. By the plan's escalation rule, the [retroactive Authoring spec](retroactive/authoring.md) comes before A starts. It covers:

1. What problems `Validate` and `Build` report, in which pass, and that `Build` succeeds if and only if `Validate` returns nothing.
2. That a node whose operand failed is not reported again, so one cause gives one problem.
3. That ids carry from draft to built system.
4. How operand slots, an empty slot, and a reference to nothing behave.
5. What `Remove` does to references.

Also assumed, from other layers:

1. How `Flatten` decides a leaf is an unknown (that a valued leaf is not one).
2. That a node may be referenced by more than one parent, and that `Calculate` computes it once.

Found while reviewing, and worth deciding before names copy their pattern:

1. **`ExpressionSystem.Add` silently skips a different node that reuses an id.** Nodes are equal by id, and `Absorb` skips any node already in its set, so the second node is dropped without a word. The name check should not copy this.
2. **`SystemDraft.AddExpression` with an existing id replaces that draft silently.** The draft's duplicate-id check only compares expressions against relationships.

---

## 9. Acceptance

1. Every Example is built through the model builder and computes the same values.
2. A model with several mistakes reports all of them from one `Build()`, and throws nothing.
3. Every Example prints its model as readable formula text.
4. A constant, reference value or scalar never appears in `system.Variables` or as an unknown. A scalar never appears in `system.Givens`.
5. A node can be found by name in a draft and a system, and its value read from a calculation.
6. `Product(m, Power(r, 2))` computes the same uncertainty as the hand-built gathered form.
7. No public constructor of a domain node remains, and no builder method takes an id.

## 10. Slices

Each slice can merge on its own. The retroactive Authoring spec comes first (section 8).

1. **Names.** `Variable.Symbol` becomes `Name` in the domain, snapshots, drafts, the wire, the API reports, the Examples' presentation and the editor. Computed nodes gain a name. Validity rules, uniqueness (`DuplicateName`, an atomic `Add`), and lookup by name. **Done** (decisions S1-1 to S1-7).
2. **Fixed values.** The leaf interfaces; constant, reference value and scalar nodes; the `Givens` list; and snapshot, draft, wire and vocabulary forms.
3. **Build through snapshots.** `SystemDraft.Build` builds every node through `FromSnapshot`; domain constructors become internal.
4. **The model builder,** including handles and the power helper.
5. **The printer,** including correlation marks.
6. **The Example rewrite** and README updates.
7. **Spike:** a fluent layer over the builder, tried on one or two rewritten Examples.

---

## 11. Decisions

| # | Question | Decision |
| --- | --- | --- |
| — | Ids and names | Ids are for the system. Builders use names and never see an id. |
| — | `Symbol` versus name | `Symbol` becomes `Name`. |
| — | Who must have a name | Variables, constants and reference values. Computed nodes optionally. Scalars never. |
| — | Changing a name | Fixed once built; editable in drafts. |
| — | Valid names | Not empty or whitespace; no invisible formatting or control characters; case-sensitive; compared after NFC normalization. |
| Q11 | Emoji and look-alikes | Emoji allowed, except joiner sequences. Look-alikes allowed. |
| — | Duplicate names | A new `DuplicateName` draft problem; `ExpressionSystem.Add` refuses them. |
| — | Atomicity | Building is atomic, and a refused `Add` leaves the system unchanged. Drafts may be half-built. |
| — | Finding by name | `draft.ByName` and `system.Find`; values through the existing `calculation.ValueOf(node)`. |
| Q15 | How C# builds a model | Through a draft. A typed model builder over `SystemDraft`; `Build()` reports every problem at once. |
| Q1 | Constructors | Domain constructors internal; the builder is the only way in. (Option 2 was chosen first and reversed the same day.) A spike tries a fluent layer. |
| Q13 | How ids reach the domain | `SystemDraft.Build` makes snapshots and calls the public `FromSnapshot`. |
| — | Leaf interfaces | `ILeafExpression`, with `IFixedExpression` and `IDirectExpression` as siblings. |
| Q8 | What a constant is | Fixed values may carry uncertainty: a **constant** has none, a **reference value** has some. |
| Q9 | Names for literals | Unnamed coefficients are **scalars**: no name, not listed in `system.Givens`. |
| Q14 (part) | A scalar's id | Kept internally, like every node. |
| Q4 | Where constants live | A list on `ExpressionSystem`, and in its snapshot. |
| Q6 | Required arguments | Agreement rule, comparison rule and solving role are required. |
| — | Repeated factors before F | The power helper builds its own correlated product; `n ≥ 2` or `n ≤ −1`. |
| — | Correlation before E1 | `Product` and `Sum` take an optional correlation setting. |
| — | Printing drafts | Out of scope. The printer prints built nodes. |
| — | Definitions in printed text | `name := formula`. |
| Q5 | Printer style | Unicode. Values print with their uncertainty. |
| Q10 | The builder's name | `ModelBuilder`. |
| Q14 | Dimensioned scalars | Scalars are dimensionless. An unnamed dimensioned value would be a different concept, not in A. |
| Q17 | One list or two | One list for constants and reference values, named `Givens`. |
| Q3 | Name scope | One namespace across expressions and relationships. |
| Q16 | The handle type | A new opaque type that knows its builder. |
| Q18 | A build that throws | `Either` is enough; a small helper may build the exception from a failure. |
| Q8 (rest) | Provenance on fixed values | Optional on constants and on reference values. A reference value without provenance becomes a warning once drafts report warnings. |
| Q12 | Marking correlation in printed text | Brackets with a tag, such as `⟦V·I_total⟧ᶜ`; a correlated power prints as `x^n`. |
| — | Relationship symbols in printed text | The operator's own `Symbol`, verbatim. |
| S1-1 | A relationship's name | `IBinaryOperator.Name` is `{ get; init; }`, fixed once built like an expression's. |
| S1-2 | A missing or invalid name in a draft | New draft problem kinds, not a relayed `DomainRule`. The draft asks the domain's own name rule, so the rule is stated once. |
| S1-3 | What `Find` returns | `system.Find(name)` returns an expression. A relationship is found with a separate lookup. Names still share one namespace. |
| S1-4 | Normalization | A built node stores its name NFC-normalized, and so does a draft. |
| S1-5 | Constructor shape in slice 1 | Slice 1 adds a name the smallest way each constructor allows. One uniform way to supply an id and a name waits for slice 3, where the constructors become internal. |
| S1-6 | A reused id in `ExpressionSystem.Add` | Left as it is in slice 1 (section 8). The name check does not copy its silence. |
| S1-7 | Naming a computed node in the editor | The editor offers a name on each formula, prefilled with the first unused `eq-N`. Clearing it leaves the node unnamed. The default lives in the editor, not in the draft or the domain. |
| — | Reading text | Out of A. Moved to Spec H, after C1 and D. |
| — | Printed text | Human-readable, and not required to parse. |

The text-specific decisions made before the split are recorded in [Spec H §3](H-formula-text.md#3-decisions-carried-from-spec-a).

## 12. Open questions

None. Spec A waits on the retroactive Authoring spec (section 8).

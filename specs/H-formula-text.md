# Spec H — Formula text

**Status:** Stub. Split out of [Spec A](A-building-a-model.md) on 2026-11-11, because reading text into a model raised too many questions about internal structure for a model builder to answer. This file keeps the decisions and questions that came up, so they are not lost. It is not ready to build from.
**Plan:** [feature set H](../project-plan.md#h--formula-text).
**Depends on:** A (names, fixed values, the draft-based model builder, the printer). **Not before:** C1 and D. The editor's UX and the solver's needs should shape the language, not the other way round.

---

## 1. Scope

Writing a model as text and reading it back:

- a **parser** from formula text into a model;
- a **machine-readable writer** that the parser can read back exactly;
- **formula entry in the editor.**

A's printer is human-readable and is not required to parse. H adds the round trip.

## 2. The concern that caused the split

The questions an implementer raised were mostly about internal structure: path ids, which nodes collapse, how a tree is flattened, and what a parse error points at. A model builder should not have to think about any of these. That is *inside-out design*: the code model's needs leaking into the person's experience. H should start from what a model builder writes and expects, and work inward.

---

## 3. Decisions carried from Spec A

Made 2026-11-11 while text was still in A's scope. Revisit any of them when H starts.

| # | Question | Decision |
| --- | --- | --- |
| Q1 | An undefined identifier in a formula | Creates a variable draft; the editor highlights variables only one formula uses. |
| Q3 | A subexpression appearing twice in a statement | One node. A builder who wants two names them. |
| Q4 | Relationship symbols in text | The domain's symbols, verbatim. Revisit if too clunky. |
| Q5 | `^` before F adds powers | `x^n` builds A's correlated power node. |
| Q6 | Units in formula text | Not yet. Needs UX work first. |
| Q7 | Where the parser lives | `Authoring`. It is not useful without drafts. |
| Q8 | Collapse across statements | No. Within a statement only; across statements, name it. |
| — | Unnamed nodes made from text | Ids are for the system. A path from the statement's root is the easiest scheme. |
| — | A name defined after it is used | Never replace an existing variable. The result must not depend on the order of statements. |
| — | One door: everything goes through `SystemDraft` | Text, the editor and C# all build drafts. This replaced an earlier "two front doors" decision when Spec A moved C# construction onto drafts. |

**Order independence.** One reading of the last-but-two decision: a name resolves against every statement in the draft, wherever it appears, and a variable is auto-created only when no statement defines that name. Then `a := b*2` followed by `b := c + 1` means the same as the reverse order, and nothing is created and then replaced. Confirm this when H starts.

## 4. Statement: a missing domain concept

The text design keeps needing a unit larger than a node and smaller than a draft: something that holds the text, owns the nodes parsed from it, and scopes collapse and path ids.

**First-pass definition:** a statement has a single binary operator.

Open questions about it:

- **Definitions.** `t := expr` names a node and asserts nothing, so it has no operator. Is a definition a statement? If it is, the definition is not "a single binary operator". If it is not, what holds its text?
- **Solving role (from Spec A Q10).** A statement whose left side is a single variable could read as an assignment: an equation that determines that variable. That would make a definition an `Equation` statement rather than an operator-less one, and might resolve the first question.
- **Domain or Authoring?** Does a statement exist only in drafts, or also in a built `ExpressionSystem`? The solver (D) treats each equation as a residual, so a statement may be useful there too.
- **Ownership.** Is the text the source of truth for the nodes a statement owns, or only a way to enter them? See §6.

## 5. Writing text: two writers

Reading must be permissive, and writing must be strict. Two writers:

1. **Human-readable:** A's printer. Some different trees print alike (`1/x` for a reciprocal and for a quotient), so it does not round-trip.
2. **Machine-readable:** exact, and the parser reads it back to the same tree. It risks looking like "cursed JSON", which is the thing to avoid. One idea that keeps it close to the readable form: let `?` stand for an empty operand slot, so a half-built draft can also be written and read back.

---

## 6. Open questions from the implementer review

Raised while text was in Spec A. Unanswered unless noted.

**Gaps:**

1. **Problem positions.** A syntax error has to say *where in the text* it is, so the editor can underline it. Today a `DraftProblem` names only a node (`Kind`, `NodeId`, `Message`), and a syntax error has no node yet. Problems need an optional span: which statement, a start offset and a length. Use UTF-16 offsets, which C# and JavaScript both index by.
2. **The symbol table.** `IBinaryOperator.Symbol` is an instance property, but a parser needs to go from symbol to operator before any operator exists. Options, without reflection:
   - **A table keyed on `BinaryOperatorType`,** with a test that builds one instance of each kind and checks its `Symbol` against the table. The ordering operators already use this pattern: symbols declared by hand, and a test asserting they match the generated ones.
   - **Generate the comparison symbols.** `SimpleComparison`'s symbols are generated from a `ComparisonRule`, so the table can enumerate every rule and generate its symbol, instead of decoding glyphs.
   - **A static abstract member** on an interface each operator type implements. This still needs a list of the types, so it is no better than the enum.
   - **Reflection** over the operator types. It works, but it is the least checkable option.
3. **Names as identifiers.** Some names in the Examples cannot be read as identifiers:
   - `π/4` contains the division operator;
   - `2gh₀` starts with a digit, so it reads as the number `2` followed by `gh₀`;
   - `D²` has a superscript `²`, which would read as a power.

   These names describe a formula rather than name a quantity. Quoting is ruled out. The likely answer: a name must be an identifier if text is to refer to it, and nodes like these are written inline, not named.
4. **Ambiguous printing.** Covered by §5.

**Semantics:**

5. **Name scope.** Do relationship names share a namespace with expression names? Text cannot refer to a relationship.
6. **Renaming.** Are references in text stored as ids, so a rename follows through and the text is reprinted? Or as names, so a rename breaks them?
7. **Repeated operands not written as `^`.** `(a+b)*(a+b)` collapses into a product of one node twice, which today's single per-node setting treats as uncorrelated. Should the parser group identical operands of a product into a power node?
8. **Statement syntax.** What separates statements (newline, `;`)? Are comments allowed? Is there syntax for naming a relationship?
9. **Reserved words.** Can a variable be named `ln`, `exp` or `sqrt`?

**Architecture:**

10. **Source of truth.** Either the text is only an input method (parse once; the nodes are the truth), or the statement owns its nodes (the draft stores text, and nodes are derived on every build). The second gives one truth and lets the server parse on each post, but nodes a statement owns cannot be edited by slots without detaching them.
11. **A syntax-tree layer** between parsing and drafts, so each half is testable alone.
12. **Parsing on the server.** If statements travel inside the draft's data format, the server parses on each debounced post and the browser needs no parser.
13. **`/` in path ids** is fine in JSON. Ids never appear in a URL path.

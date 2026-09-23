# Writing docs for Calcusystem

Two surfaces, two jobs.

- A **doc comment** answers *what do I need in my hands right now*, read on hover, mid-task.
- A **README** answers *why is it like this*, read once, deliberately.

Everything below follows from that split. When a sentence could sit in either, ask who is
reading it and when.

---

## 1. Doc comments

### The relevance test

Before keeping any sentence, ask both halves:

> Is this **directly related** to the member it sits on, **and** needed to **use** it?

Anything short of a strong yes on both is cut. *Interesting to the author* is not *needed by
the caller*.

### What earns its place

| Keep | Because |
| --- | --- |
| What the member does, in one line | The reason hover exists |
| What it throws, and when | Not visible in the signature |
| What `null` means, where it is returnable | Three-valued results are a trap |
| Units, storage form, sign conventions | Silent wrongness otherwise |
| **Gotchas** — where a reasonable expectation is false | The single highest-value line |
| **Decision support** — which of these do I pick? | e.g. `Coherence` vs `Equation` |
| The **symbol**, for a type that has one | Agreed shorthand; carry it in `<summary>` |

### What gets cut

- **History.** What the code used to be belongs to git. See §4 for the one exception.
- **Rationale a README already carries.** Check the folder README, the assembly README, and any
  sibling `.md` before keeping an argument.
- **Anything visible in the signature.** "Neither argument has a default" is not worth a line.
- **Facts inherited from a base type.** Every computed node is not directly mutable; say it on
  the interface, not on all nine derived classes.
- **Roadmap.** "Deferred to Milestone 5" goes in `project-plan.md`.
- **Usage hints that restate the summary.** If the summary is *"satisfied when the entire Lhs
  interval lies below the entire Rhs interval"*, then *"use for definitive less-than checks"*
  adds nothing. Keep usage guidance only when it names a case the summary does not imply — *a
  maximum current rating*, *a minimum yield strength* — and put it in `<remarks>`.

### Hover rendering

VS Code renders XML doc as markdown in a hover card. Three consequences:

- **A single `<br/>` does not separate paragraphs.** Use `<br/><br/>` between the definition,
  the symbol, and any formula. A three-clause run-on sentence should be three paragraphs.
- **A bare `<inheritdoc/>` beside a local `<remarks>` drops the inherited summary** from the
  card, leaving only the remarks. Write `<summary><inheritdoc/></summary>` instead.
- **A run-on list reads as a wall.** Three or more alternatives get `<list type="bullet">`.

The house shape for a type that has a symbol:

```csharp
/// <summary>
/// Satisfied when the Lhs and Rhs tolerance bands overlap at all — i.e. there exists at least
/// one value consistent with both uncertainties. This is the weakest form of agreement.
/// Operator is commutative.
/// <br/><br/>
/// Symbol: <b>{&gt;&lt;}</b>
/// </summary>
```

### Where a sentence goes

`<summary>` is the one-liner and what a caller needs first. `<remarks>` takes the caveat, the
gotcha, and any guidance that is not the definition. A caveat sitting in `<summary>` is nearly
always in the wrong place.

### Document what has none

**This pass is redistribution, not subtraction.** Effort tends to land inversely to how often a
type is touched: `Measurand` is the central value type of this library and once had thirty
public methods with no doc comment, while types a caller never constructs carried essays.

Added docs should be short and mechanical, with no rationale at all:

```csharp
/// <summary>Relative uncertainty is the passed value divided by 100.</summary>
```

Self-describing members — a `public static readonly UnitOfMeasure Newton` among forty siblings —
need nothing.

---

## 2. READMEs

### The promise

**If a folder has a README, you should not have to read anything else in it to use that
namespace.** It holds per *namespace*, not per *concept*.

- **Assembly README** — the concept narrative, cross-cutting invariants, and the folder index.
- **Folder README** — one namespace's surface.

Sections, in order: **What's here** / **Start here** / **Guarantees** / **Surprises** / **What
does not belong here** / **Related**. Guarantees and Surprises may be omitted.

- **Guarantees** answers *what can I rely on without reading the code?* The test: a guarantee is
  **not visible in a signature**. `Plus` throwing on a dimension mismatch is one; `Plus` taking
  a `Measurand` is not.
- **Surprises** answers *where is a reasonable expectation false?* Usually the absence of a
  guarantee someone would assume. This is the section readers get the most from.

A README earns its place by content, not by file count — but a folder holding one file usually
has nothing to say that the file does not.

### Which layer owns a fact

The folder README owns **the mechanics and the surface**. The assembly README owns **the
rationale**. Every folder README ends by pointing at the assembly README for *why*, so moving a
"why" down into a folder breaks that promise quietly.

When rationale lives in a doc comment and **no README carries it**, move it into a README —
an appendix under the folder that owns it — and *then* cut the comment. Do not simply delete it.

### Redundancy between an assembly README and its folders

Sort each section into one of three, and treat them differently:

1. **Genuinely duplicated** → cut from the assembly README.
2. **Now false** → fix.
3. **Correctly assembly-level** → keep.

In practice (2) dwarfs (1). **When documentation has drifted, fixing beats cutting** — cutting
first destroys the evidence of what drifted.

---

## 3. Language

- **One word, one meaning.** The pattern language is fixed: *determined* (degrees of freedom
  only), *fully described*, *Type* (reconstruction discriminator only), *uncertainty* (never
  *error*), *snapshot* (never *state* or *memento*), *DTO*, *unset* vs *unknown*, *Result*
  (immediate answer) vs *Outcome* (recorded judgement), *seam*, *landmark*,
  *subject*/*criterion*, *ladder*/*rung*, *band*.
- **Simplified Technical English.** Short sentences, no unnecessary metaphor, no overloaded
  terms. Precision beats simplicity; simple beats complex.
- **US spelling.**
- **Avoid counts that rot.** "Every one of them is a conjunction", not "all thirteen are". If a
  count is genuinely wanted, point at the thing that can answer it: `Lists.UnitTypes.All`.
- **Cut the justification tail.** A sentence states a fact, then a clause explains why it had to
  be that way. Keep the fact. The tells: *"which is what lets…"*, *"that is the whole point…"*,
  *"deliberately, because…"*.
- **Prefer an example to a description of how to use something.**
- **State a guarantee as what it enables.** "Equality and hashing are by `Id`" is trivia;
  "…so these are safe as dictionary keys" is usable.

---

## 4. Turning history into a warning

A bug story is worth keeping only as a **present-tense consequence**. Compare:

> ~~Skipping infinite uncertainty bars fixed a defect where 5 kg agreed with 10 kg.~~
>
> An infinite bar **would** make the threshold infinite, so every pair of finite values **would**
> compare equal — 5 kg agreeing with 10 kg.

Same warning, same length, but it reads as *what breaks if you remove this guard* rather than
*what happened once*, and it does not decay into trivia when nobody remembers the incident.

This matters most in tests, where a regression test's bug **is** its reason to exist. Deleting
the history there deletes the rationale.

---

## 5. Before you call it done

1. **Verify every factual claim against the code.** A confident wrong sentence is worse than no
   sentence. READMEs have contradicted tests, and the test was right.
2. **Compile every code block.** This has caught a defect every single round.
3. **Resolve every link and `#anchor.`** GitHub maps ` — ` to a *double* dash in a slug.
4. **Apply the Guarantees test** — is it visible in the signature?
5. **Apply the relevance test**, per sentence rather than per bullet.
6. **Ownership check** — does this belong to the namespace that *owns* the thing, or the one
   that consumes it?
7. **Re-read cold**, last.
8. **Re-derive the pattern class, do not re-check your list.** Any list assembled by grep is a
   floor, not a ceiling. A five-word British-spelling list turned out to be forty across
   eighteen files; an "uncertainty rename" swept identifiers and left sixty-five doc comments
   still saying *error*.

### Where rot hides

The compiler checks `<see cref>` and nothing else. These all survived a build:

- Names in `<c>…</c>` — three doc comments described `DegreesOfFreedom()` years after it was
  deleted, and one documented the double-counting bug that deleting it fixed.
- Prose paths — `Measurement/State/` outlived the folder's rename to `Snapshots/`.
- Counts — *thirteen operators* when there are fourteen, in nine places.
- Parameter names — `FromSnapshot(TSnapshot state)` outlived the word *state* everywhere else.
- A document that **contradicts itself**: one table put exponentiation on the propagator and a
  parenthetical thirteen lines below put it, correctly, on `IUncertainty`. Read whole sections.

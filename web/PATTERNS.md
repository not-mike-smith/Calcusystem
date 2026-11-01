# The UX pattern language

This UI is going to get busy. A model with forty variables — each with a value, a band, a dimensionality, a
verdict and possibly a problem — is a dense page, and the question a reader has to answer at a glance is not
*what kind of control is this*. It is:

> **What happens if I touch this?**

Everything below follows from that.

## How to read this, and how to change it

| | |
| --- | --- |
| [`src/patterns/language.ts`](src/patterns/language.ts) | the language itself, as typed records |
| [`src/patterns/examples.tsx`](src/patterns/examples.tsx) | each mark done right, and done wrong, with the code for both |
| `#patterns` in the running app | the sheets, rendered |
| this document | the prose, and the argument |

### The sheets

`#patterns` is laid out as **do-and-don't sheets**, after the installation sheets an engineer gets with a
structural connector: the correct detail beside the ways it gets done wrong, each annotated with what the
mistake costs. For every mark:

- the rule, in one line;
- **✓ Do** — the real component, live, with the code to write;
- **✗ Don't** — one panel per alternative it was chosen over, each *rendered*, with what is wrong with it and
  the CSS that produces it;
- and, folded away, the reasons and the alternatives that can only be argued rather than shown.

The specimens are the real components with real state, not pictures of them — so a mark that has drifted from
what this document says shows up on that page, and a screenshot never would. There is a stylesheet box at the
top of the page that is injected after the app's own, so a mark can be changed while it is being looked at.

A rendered wrong example earns its place over a described one. *A permanent border becomes a grid of
rectangles* is an assertion to be taken on trust; forty bordered fields in a column settle it at a glance.

### Keeping the four in step

The records are the single statement, and `npm run patterns-check` holds the rest against them:

| | |
| --- | --- |
| this document mentions every id the language holds | every id below is written in backticks for it |
| every mark has a correct example | a mark with nothing to show is not documented |
| every alternative claiming it can be shown is shown, and nothing is shown that was never claimed | catches a renamed key in either direction |
| no `nono-` class is used outside the sheets | a deliberate violation must not leak into the UI it argues against |
| the router accepts every section link, and every link on the page resolves | the contents were right and unreachable: an exact match on `#patterns` sent `#patterns-affordance` to the editor |

Each of those has been wrong at least once, and each was confirmed to fail when broken.

The page's sections come from one list, so the contents and the sections cannot disagree, and
[`src/patterns/route.ts`](src/patterns/route.ts) is the only place that decides what counts as this route —
spelling the links in the gallery and matching them in the router was two answers to one question, and it cost
every link at the top of the page.

**To refine a pattern:** read its rule, look at the Do beside it, then look at the Don'ts. The rendered wrong
examples are the argument; disagreeing with one of them is how a pattern changes.

**To add one:** say which question it answers, use the channel that vocabulary already owns, put it in
`language.ts` with its reasons and its alternatives — every field is required, so a pattern cannot arrive
half-argued — give it a specimen in `examples.tsx`, and look at it on `#patterns` next to the others. If two
marks are hard to tell apart there, one of them is wrong.

---

## Principles

**Organized by consequence, not by widget kind** (`by-consequence`). Every confusion found in this editor has
been that one question going unanswered: an editor that converted a number, a readout whose two unit symbols
sat together at the end, a panel with no way of saying it would close. A language built on *what happens*
answers it; one built on *inputs look like inputs* does not.

**Two vocabularies, and no channel carries both** (`two-vocabularies`). Affordance answers *what happens if I
touch this*. Status answers *what is true about this*. This is the rule that matters, and the first one broken
— see [the collision](#the-collision-this-came-from).

**A mark is spent on approach where it can be** (`spent-on-approach`). The heavier a mark, the later it should
arrive.

**An affordance is offered only where it leads somewhere** (`only-where-it-leads`). A mark that does nothing
useful is worse than no mark, because a reader who follows it learns to distrust the next one.

**Status is always visible** (`status-always-on`). It is true whether or not anyone is pointing at it.

**Colour is never the only channel** (`never-colour-alone`). A verdict carries a word as well. Colour fails on
a projector, in print, and for a reader who cannot separate two of them — and this is a tool people will put on
a wall in a review.

---

## Channels

A channel carries one vocabulary and never the other.

| Channel | Vocabulary | Carries |
| --- | --- | --- |
| background fill | affordance | states the model |
| border and ring | affordance | a slot approached, or focused |
| chevron | affordance | changes the view, or reveals |
| dotted underline | affordance | explains |
| colour | status | a verdict |
| a glyph | status | a kind |
| weight | neither | a value is bold because it is the value |

### The collision this came from

A dotted underline meant three things in ninety lines of this UI: *hover for an explanation* on a dimension
symbol, *click to re-read* on a readout's unit, and — as a solid underline — *this is the form the model
keeps*, which is not an affordance at all. One channel, three meanings. At four hundred lines nobody would have
untangled it, and nothing would have warned them.

---

## Affordance

### `slot` — states the model

A background fill at rest, a border on hover, a ring on focus.

**Every control inside a slot writes the model.** The number, the unit, the band's form, an operand, an
exponent. The slot is the unit of consequence and its controls carry no chrome of their own, so a reader learns
one region rather than five controls. A label inside one is still a label: it may explain, but it does not
write.

A border is the heaviest inline mark there is, so it is spent on approach rather than worn all the time. The
fill alone still reads as *somewhere a value goes*, and forty fills down a column line up into something that
reads as a form.

**A control that writes the model is a slot whatever it looks like.** The dimensionality editor was first built
as a disclosure with a chevron, which said *this opens* where a reader needed *this edits*.

Chosen over a **permanent border** (unmistakable, and a grid of rectangles once a model is large — it spends
the heaviest mark on the commonest thing), an **underline like a blank on a paper form** (lightest, but it
collides with the dotted underline that means *explains*, and an empty slot with no enclosed area is nearly
invisible), and a **raised neumorphic surface** — see `no-raised-surfaces`.

### `views` — changes this view only

A chevron, which appears on approach.

Nothing it does reaches the model. The quantity is fixed; this changes how it is read.

A readout carrying two permanent chevrons is furniture, and a page of readouts is a thicket; the space is
reserved either way so nothing shifts when it appears. Re-reading a value is never the only way to see
something — the reading it replaces is already correct — so this is the one affordance that can afford to be
quiet.

Chosen over a **permanently visible chevron** (`8.0 Pa▾ ± 0.3 Pa▾●` at rest is three pieces of furniture on one
value) and a **dotted underline on the text**, which is what it was, and which made the mark mean *click* as
well as *hover*.

Unsettled: discoverability without a pointer. See `touch-and-keyboard`.

### `reveals` — shows or hides detail

A chevron that rotates, shown **always**.

The deliberate exception to spending marks on approach, because it reports whether the thing is *open* as well
as that it can be opened. Open-or-shut is information at rest, so it is worth its ink — and not showing it is
exactly how the dimensionality exponents came to have no way of saying they would close.

Inside a slot it marks the way into the rest of that slot, and the panel it opens carries the slot's fill,
because it writes the model too.

Chosen over **nothing at all, with the reading itself as the target**: clicking opened it, clicking again shut
it, and the page never said so.

### `tells` — explains, and nothing more

A dotted underline and a help cursor. **The only use of either.**

A reader who learns that a dotted underline means *hover* never has to wonder whether it also means something
will happen.

---

## Status

Always visible, because it is true whether or not anyone is pointing at it.

### `kept` — the model stores it this way

A filled dot after the label. Only meaningful where something is authored; a computed band is derived and kept
nowhere, so there is no form to mark.

Which of two forms the model keeps is a fact about the model, so it takes a glyph rather than borrowing the mark
that says a thing is clickable — which is what it did.

### `unknown` — nothing determines it

A hollow dot before the word. Unset and unknown are different: this is a variable no equation reaches and
nobody has valued. The hollow of it is the point, and it pairs with the filled dot that means the opposite kind
of certainty. Chosen over the word alone, because in a list of forty the eye needs something to count.

### `verdict` — what a relationship did

Colour, and the word. Three outcomes, not two: a relationship that could not be judged is not one that failed.

The word is not a caption on the colour, it is the other half of the channel. Chosen over colour alone
(unreadable in three of the places this will be looked at) and over a tick and a cross (two glyphs cannot carry
three outcomes without a third that reads as a question about the reader rather than about the model).

### `at-fault` — something here stops the model building

A bar down the left of the node, and the problem in words beneath it.

**A block-scale mark.** Inline marks and block marks are different scales and may share a channel — a border on
a block is not competing with a border on a control inside it. But scale has to be *named* as a discriminator,
or the next person reads the bar as an affordance. See `block-scale-marks`.

The words matter more than the bar: a `DraftProblem` names the node and says what is wrong in a sentence the
server wrote, and the bar only says which node to read.

---

## Decisions

### `editor-vs-readout` — a readout and an editor look alike, so why do they do opposite things to the number?

**Because they hold different things, and chrome says which is which.**

A readout holds a fixed quantity, and a unit is a lens over it, so choosing a unit **converts**. An editor holds
the pair its author typed — a number *and* the unit it is in — so choosing a unit **restates**, and the number
stays put. An editor that converted would move a number its author had just entered, because filling a control
left to right puts the number in before the unit is picked.

The pair is real rather than a convenient story: `ExpressionDraft.ValueUnit` persists the unit and
`IsStoredAsAbs` persists the band's form.

Chosen over **making both convert** (the original behaviour, and the first thing reported) and over **a rule in
the documentation with no visible difference** — a rule nobody can see is a rule nobody follows, so the chrome
is the rule.

### `no-raised-surfaces` — should pressability be marked by a raised, bas-relief surface?

**No, and the complaint behind the question was answered a different way.**

The instinct was right: a nine-pixel chevron is too quiet for *this edits the model*. But the mark was not
missing, it was the wrong one — the control writes the model, so it is a `slot`. Reaching for a new mark where
an existing one fits is how a vocabulary stops being small, and small is the only property that makes it
learnable.

Against a neumorphic surface specifically:

| | |
| --- | --- |
| Contrast | a same-colour surface signalled by soft shadows lands near 1.1:1 where WCAG 1.4.11 wants 3:1 |
| Themes | the near-white panel has no headroom for the light shadow; dark mode inverts and goes muddy |
| Density | soft shadows need padding to read as depth, and bleed together at forty rows |
| Channels | a fill already means *states the model*; a second surface treatment is the collision above |

### `marks-spent-late` — which marks are worn at rest, and which arrive with the pointer?

**The heaviest arrive late. Status never does.**

A fill is cheap enough to wear always and still read as a slot; a border is not. A chevron on a view control is
cheap individually and expensive in aggregate, so it arrives late too. A disclosure's chevron is the exception,
because it carries state as well as affordance.

Chosen over **everything always visible** (three marks on a single value) and **everything on hover** (a form
with no visible fields is not a form).

---

## Not settled

This is where to start.

**`layout-and-density` — how is a large model arranged?** The marks say what a thing will do. They say nothing
about how to find it: what groups, what collapses, what scrolls, what is worth a column. For forty variables
that is the harder half, and the next thing to need a language.

**`authored-or-computed` — how does a value say whether a person typed it?** An editor and a readout differ in
chrome, so it is clear in a row. In a results panel every value reads the same whether it was authored,
computed, or carried in from provenance — and `IProvenance` already knows which.

**`stale-readings` — what does a reading look like while the server is being asked?** There is one page-level
pulse. Nothing says that the value you are looking at is from the draft as it was two keystrokes ago, which is
exactly when a reader is about to act on it.

**`touch-and-keyboard` — how is a view control found without a pointer?** `views` reveals its chevron on hover
*and on focus*, so the keyboard is covered. Touch is not: the first tap is the discovery.

**`unseen-at-scale` — what does any of this look like at forty rows?** Nothing here has been seen at that size,
on a projector, or in print — the three places the colour rule is written for. The gallery shows one of each
pattern, which is the wrong test for density.

**`block-scale-marks` — what is the vocabulary for block-scale marks?** A node at fault uses a left bar, and
that is allowed because scale discriminates. But panels, sections and groups will want marks of their own, and
nothing says which channels are theirs.

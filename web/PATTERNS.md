# The mark vocabulary

This UI is going to get busy. A model with forty variables, each with a value, a band, a dimensionality, a
verdict and possibly a problem, is a dense page — and the question a reader has to be able to answer at a
glance is not *what kind of widget is this*, it is:

> **What happens if I touch this?**

Every confusion found in this editor so far has been that question going unanswered. So the vocabulary is
organized by **consequence**, not by widget kind.

Everything here is rendered on one page: run the app and open [`#patterns`](http://localhost:5173/#patterns).
Prose about a mark can go stale; a page that renders it cannot.

---

## Two vocabularies, and they never share a channel

| | Answers | Shown |
| --- | --- | --- |
| **Affordance** | what happens if I touch this | on approach, mostly |
| **Status** | what is true about this | always |

**A channel carries one vocabulary only.** This is the rule that matters, and the one already broken once: a
dotted underline meant *hover for an explanation* on a dimension symbol, *click to re-read* on a readout's
unit, and — as a solid underline — *this is the form the model keeps*, which is not an affordance at all. One
channel, three meanings, in ninety lines. At four hundred lines nobody would have untangled it.

| Channel | Carries |
| --- | --- |
| background fill | affordance: states the model |
| border and ring | affordance: a slot being approached or focused |
| chevron | affordance: changes the view, or reveals |
| dotted underline | affordance: explains |
| colour | status: a verdict |
| a glyph | status: a kind |
| weight | neither — a value is bold because it is the value |

---

## Affordance

### `.slot` — states the model

Filled at rest, bordered on hover, ringed on focus.

A border is the heaviest inline mark there is, so it is spent on approach rather than worn all the time: forty
bordered fields down a column is a grid of rectangles, and the fill alone still reads as *something goes
here*.

**Every control inside a slot writes the model** — the number, the unit, the band's form, an operand, an
exponent. The slot is the unit of consequence and its controls inherit it, so they carry no chrome of their
own. A label inside one is still just a label: it may `.tells`, but it does not write.

**A control that writes the model is a slot, whatever it looks like.** The dimensionality editor was built as a
`.reveals` with a chevron, which said *this opens* when the thing a reader needed to know was *this edits*. It
is a slot now. Reaching for a new mark — a raised surface, say — would have been answering the right complaint
with the wrong fix: the mark was not missing, it was the wrong one.

### `.views` — changes this view, and nothing else

A chevron, which appears on approach.

The same reasoning: a readout carrying two permanent chevrons is furniture, and a page of readouts is a
thicket. The space is reserved either way so nothing shifts when it appears. The cost is discoverability
without a pointer, which is accepted because re-reading a value is never the only way to see something — it is
a convenience over a reading that is already correct.

### `.reveals` — shows or hides detail

A chevron that rotates, shown **always**.

Inside a slot it marks the way into the rest of that slot — the dimensionality's exponents, say — and the
panel it opens carries the slot's fill, because it writes the model too.

The exception to the rule above, and the reason for it: a disclosure's chevron reports whether the thing is
*open* as well as that it can be opened. That is information at rest, so it is worth its ink. Not showing it is
how the dimensionality exponents came to have no way of saying they would close.

### `.tells` — explains, and does nothing

A dotted underline and a help cursor.

The only use of either. A reader who learns that a dotted underline means *hover* never has to wonder whether
it also means *click*.

---

## Status

Status is **always visible**, because it is true whether or not anyone is pointing at it.

| State | Mark |
| --- | --- |
| kept this way | `●` on the form the model stores |
| nothing determines it | `○` before *unknown* |
| a verdict | colour, **and the word** — `holds`, `violated`, `not yet judged` |
| at fault | a bar down the left of the node, and the problem in words beneath it |

**Colour is never the only channel.** A verdict carries a word as well, because colour fails on a projector,
in print, and for a reader who cannot tell two of them apart.

**Status never borrows an affordance mark.** Which band form the model keeps is a fact about the model, so it
is a glyph — not a solid underline where a dotted one means *clickable*, which is what it was.

---

---

## An affordance is offered only where it leads somewhere

A mark that does nothing useful is worse than no mark, because a reader who follows it learns to distrust the
next one.

So the dimensionality's exponents are reachable **only in custom mode**. A named combination is already the
answer; offering to edit it underneath is offering to make it something else by accident. Pick *Custom* and the
exponents open, pre-filled with wherever the author was — so starting from a preset and tweaking it still
works, it just has to be asked for.

This needed real state rather than a derived label. While custom mode was inferred from *whether the exponents
happen to match a named combination*, picking Custom while the value still equalled Pressure left the select
reading "Pressure" with the exponents open, and tweaking a custom dimensionality into one that happened to
match would silently rename it.

## Adding a pattern

1. Say which question it answers: *what happens if I touch this*, or *what is true about this*.
2. Use the channel that vocabulary already owns. If it needs a new channel, that is a change to this document
   first.
3. Put it on `#patterns`, in every state it has, and look at it next to the others.
4. If two marks are hard to tell apart on that page, one of them is wrong.

## What this does not cover yet

Layout, density and grouping — how forty variables are arranged, what collapses, what scrolls. The marks say
what a thing will do; they say nothing about how to find it. That is the next thing to need a language.

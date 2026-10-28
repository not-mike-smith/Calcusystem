# Building blocks

The pieces a model editor is assembled from. Each one renders a single domain value and knows nothing about a
draft, a node, or a request.

## What's here

| Component | Writes? | Affordances | Role |
| --- | --- | --- | --- |
| `DimensionalityView` | no | none | What a quantity measures: `M·L/T²`. |
| `DimensionalityEditor` | yes | preset list, exponents in custom mode | Choosing what a quantity measures. |
| `MeasurandView` | no | **none** | A value, exactly as its props say to read it. |
| `MeasurandReadout` | no | unit, band form | A fixed quantity, re-read. **Converts.** |
| `MeasurandEditor` | yes | unit, band form, both numbers | A value being written. **Restates.** |

They rest on three modules with no React in them: `src/format/significance.ts` for how many figures to show and
when to go scientific, `src/format/units.ts` for converting between KMS and a unit, and
`src/format/editing.ts` for what each control in an editor does to the model. The last one is where the rules
below actually live, which is what lets them be exercised without a browser.

## The one thing to understand: converting versus restating

A readout and an editor look alike and do opposite things to the number, and that is not a quirk — **they hold
different things.**

| | Holds | Choosing a unit means | The number |
| --- | --- | --- | --- |
| `MeasurandReadout` | a fixed quantity | *show me this in those units* | changes |
| `MeasurandEditor` | the pair its author typed — a number **and** the unit it is in | *the number I typed is in those units* | stays put |

An editor that converted would move a number its author had just entered: filling the control left to right,
the number goes in before the unit is picked. So in an editor **nothing moves the number but typing** — unit
and band-form controls restate what is already written. Converting a fixed quantity is the readout's job, and
there is one under every variable row.

The pair is real rather than a convenient story: `ExpressionDraft.ValueUnit` persists the unit, so reopening a
draft shows the number that was typed. The band's form is persisted the same way, by `IsStoredAsAbs`.

**Chrome says which is which.** An editor is a bordered field, because every control in it writes to the model.
A readout is plain text whose unit and band symbol are themselves the controls, because nothing in it does.
That is the ordinary rule that editable things look editable, rather than a border language to learn.

## Start here

```tsx
<DimensionalityView encoded="M1,L1,T-2" />              {/* M·L/T² */}
<DimensionalityView encoded="M1,L1,T-2" layout="stacked" />

<MeasurandView value={computed} />                       {/* 6.00 ± 0.06 N */}
<MeasurandView value={computed} band="relative" />       {/* 6.00 N ± 1.0% */}
<MeasurandView value={computed} unit={pounds} notation="scientific" />

<MeasurandReadout value={computed} storedAs="relative" />

<MeasurandEditor
  dimensionality={variable.Dimensionality}
  kmsValue={variable.KmsValue}
  uncertainty={variable.Uncertainty}
  valueUnit={variable.ValueUnit}
  onChange={({ kmsValue, uncertainty, valueUnit }) => …}
/>
```

Every one of them reads the catalogue from context, so a `CatalogueProvider` has to be above them. That is why
none of them takes a list of dimensions or units: a block asks for what it needs rather than having it threaded
through every component in between.

## The marks they use

Which mark means what, and why each channel carries one meaning only, is
[`web/PATTERNS.md`](../../PATTERNS.md) — and `#patterns` in the running app renders all of them. The short
version: a filled slot states the model, a chevron changes the view, a rotating chevron reveals, a dotted
underline explains, and status is always-visible colour and glyphs that never borrow any of those.

## Looking at what they render

```bash
npm run render-check
```

Prints each block as text. It asserts nothing — it is a way to *see* the output, which is the one thing prose
about a component cannot do, and it is how both of the defects below were found.

```text
readout, absolute      → 8.0 Pa ± 0.3 Pa
readout, relative      → 8.0 Pa ± 4%
readout, scientific    → 8.0×10⁶ Pa ± 0.3×10⁶ Pa
view, factored power   → (8.0 ± 0.3)×10⁶ Pa
dimensionality, inline → M/(L·T²)
```

## Guarantees

- **A view with no affordances still takes every choice as a prop.** Unit, band form, notation and figure count
  are all settable on `MeasurandView`; it simply never offers the user a way to change one. A caller that wants
  pounds shows pounds, and the component does not argue.
- **An uncertainty sets the precision of the value it belongs to.** Both are rounded to the same place, so a
  value is never written to more figures than its band justifies.
- **A band converts by the factor alone.** Applying a unit's offset to a band would read ±1 K as −457.87 °F.
  `showBand` is a separate function from `show` for that reason, and `bandSymbol` gives `Δ°C` where the
  distinction shows.
- **Everything is held in KMS.** A unit is a reading, and `format/editing.ts` derives both boxes from the
  quantity rather than keeping a number of its own beside it.
- **In an editor, only typing moves a number.** Choosing a unit or a band form restates what is written.
  Checked by executing `format/editing.ts` over plain and offset units in both directions.
- **Not chosen is not dimensionless.** `DimensionalityEditor` answers `null` for the first and `""` for the
  second, because the domain refuses to guess between them and so does this.
- **The exponents are reachable only in custom mode.** A named combination is already the answer, so offering
  to edit it underneath would be offering to make it something else by accident. Picking *Custom* opens them
  pre-filled, so starting from a preset and tweaking still works — it just has to be asked for.
- **Custom is a mode, not a guess.** It is held, not inferred from whether the exponents happen to match a
  preset, so tweaking a custom dimensionality into one that matches does not silently rename it.
- **A dimensionality shows only what it measures**, in the order the server lists the dimensions, with negative
  exponents turned into a denominator rather than printed with a minus sign.

## Surprises

- **Figures come from the band, two or one.** Two where the band's leading digit is 1 or 2, one otherwise — the
  particle-physics convention. So `±12` keeps both figures while `±51` shows as `±50`, and the value is rounded
  to the band's last place: `1234.5678 ± 51.2` reads `1230 ± 50`, not `1235 ± 50`.
- **With no band at all, four figures.** Which means an authored `2` reads as `2.000`. There is nothing to say
  it is exact, and four figures is the stated default.
- **Scientific notation is chosen by counting zeros, not by size.** More than two leading or trailing zeros in
  the rounded reading and it switches. So `1234567` reads as `1.235×10⁶` — but so does `1000`, while `300`
  stays `300`. The zeros are what is unreadable, not the magnitude.
- **A relative band on a zero value falls back to absolute.** There is nothing for it to be relative to.
- **`storedAs` is only meaningful for an authored value.** A computed value's uncertainty is derived and kept
  nowhere, so there is no form to mark — pass `null` and no marker appears.
- **Switching the band form in an editor keeps the number, not the size.** `0.3` absolute becomes `0.3`
  percent. It is a restatement, like choosing a unit, for the same reason: the author's number is what they
  said. A readout switching form does convert — `±0.02` on a 2 kg value reads as `1%` there.
- **A band typed where there was none is absolute.** The box sits straight after a value in a named unit, and
  `8.0 MPa ± 0.3 MPa` is how that reads. The label says which form it is, and switching costs a click and moves
  no number. The toggle is inert until there is a band to switch, so the order is type, then toggle.
- **The band's label stands in for a long symbol.** It shows the band's own unit — `Δ°C` where that differs
  from the value's `°C` — unless the symbol runs past about five characters, where `″` (same as above) takes its
  place and the symbol goes in the tooltip. Symbols in this library reach seventeen characters.
- **A readout marks the form the model keeps** with a solid underline rather than a dotted one, and says so in
  the tooltip. Only meaningful for an authored value.
- **A typed number is only read when the field is left or Enter is pressed.** Writing back on each keystroke
  would renumber the box under the author, and a half-typed `1.2e` is not a number.
- **A readout has no widget beside its number.** The unit after the value and the symbol after the band are
  the controls, so the reading is not competing with a pair of boxes. They are dotted-underlined to show it.
- **A readout does not factor the power out; `MeasurandView` does.** `(8.0 ± 0.3)×10⁶` needs its two numbers
  adjacent, and a readout has a unit control between them, so there each number carries the power:
  `8.0×10⁶ Pa ± 0.3×10⁶ Pa`. A view has one trailing unit and no controls, so it factors.
- **Each symbol sits next to what it labels.** In a readout the unit follows the value it reads and the band's
  label follows the band, which is why an absolute band repeats the symbol — `8.0 Pa ± 0.3 Pa`. Putting both at
  the end gave `8.0 ± 0.3 Pa Pa`, and only the relative form happened to interleave correctly.

## What does not belong here

- Anything about a draft, a node or a problem → `src/components/`
- Fetching → `src/api/`
- Rounding, conversion, and what an edit does to the model → `src/format/`
- Which units exist → the server, through `CatalogueProvider`

## Related

`src/format/` (`significance`, `units`, `editing`) · `src/model/catalogue.tsx` ·
`src/components/` (what assembles these).
See the [project README](../../README.md) for how the client talks to the server, and the API's
[README](../../../Api/README.md) for where a unit's two conversion numbers come from.

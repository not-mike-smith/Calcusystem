# Building blocks

The pieces a model editor is assembled from. Each one renders a single domain value and knows nothing about a
draft, a node, or a request.

## What's here

| Component | Writes? | Affordances | Role |
| --- | --- | --- | --- |
| `DimensionalityView` | no | none | What a quantity measures: `M·L/T²`. |
| `DimensionalityEditor` | yes | preset list, exponents | Choosing what a quantity measures. |
| `MeasurandView` | no | **none** | A value, exactly as its props say to read it. |
| `MeasurandReadout` | no | unit, band form | A value the reader can re-read. |
| `MeasurandEditor` | yes | unit, band form, both numbers | A value being written. |

They rest on two modules with no React in them: `src/format/significance.ts` for how many figures to show and
when to go scientific, and `src/format/units.ts` for converting between KMS and a unit.

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
  onChange={({ kmsValue, uncertainty }) => …}
/>
```

Every one of them reads the catalogue from context, so a `CatalogueProvider` has to be above them. That is why
none of them takes a list of dimensions or units: a block asks for what it needs rather than having it threaded
through every component in between.

## Guarantees

- **A view with no affordances still takes every choice as a prop.** Unit, band form, notation and figure count
  are all settable on `MeasurandView`; it simply never offers the user a way to change one. A caller that wants
  pounds shows pounds, and the component does not argue.
- **An uncertainty sets the precision of the value it belongs to.** Both are rounded to the same place, so a
  value is never written to more figures than its band justifies.
- **A band converts by the slope alone.** Applying a unit's offset to a band would read ±1 K as −457.87 °F.
  `showBand` is a separate function from `show` for that reason, and `bandSymbol` gives `Δ°C` where the
  distinction shows.
- **Everything is held in KMS.** A unit is a reading. Nothing on the wire records which unit a value was typed
  in, so changing the unit in an editor changes the number shown and not the model.
- **Not chosen is not dimensionless.** `DimensionalityEditor` answers `null` for the first and `""` for the
  second, because the domain refuses to guess between them and so does this.
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
- **Switching the band form in the editor keeps the band's size, not its number.** Going from ±0.02 kg to
  relative on a 2 kg value gives 1%, not 0.02%. The interval the author stated does not move because they
  changed how they want to write it.
- **A typed number is only read when the field is left or Enter is pressed.** Writing back on each keystroke
  would renumber the box under the author, and a half-typed `1.2e` is not a number.
- **`MeasurandReadout` hides the unit its view would print** and shows a unit picker in its place, so the two
  do not both name the unit.

## What does not belong here

- Anything about a draft, a node or a problem → `src/components/`
- Fetching → `src/api/`
- Rounding and conversion arithmetic → `src/format/`
- Which units exist → the server, through `CatalogueProvider`

## Related

`src/format/` · `src/model/catalogue.tsx` · `src/components/` (what assembles these).
See the [project README](../../README.md) for how the client talks to the server, and the API's
[README](../../../Api/README.md) for where the served slope and offset come from.

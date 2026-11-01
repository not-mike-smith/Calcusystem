import { useState, type ReactNode } from "react";
import type { MeasurandReport, Uncertainty } from "../api/client";
import { DimensionalityEditor } from "../blocks/DimensionalityEditor";
import { MeasurandEditor } from "../blocks/MeasurandEditor";
import { MeasurandReadout } from "../blocks/MeasurandReadout";

/**
 * What each mark looks like, and the code that makes it.
 *
 * The render and the snippet sit in one record so that the two drifting apart is visible on sight rather than
 * only on inspection. `DO` holds the correct use of a mark; `WRONG` holds the alternatives it was chosen over,
 * keyed by their id in `language.ts`, so the argument against each one can be looked at rather than taken on
 * trust.
 *
 * The wrong examples use `nono-` classes. Nothing in the app may use one, which is what keeps a deliberate
 * violation from leaking into the UI it is an argument against.
 */
export interface Example {
  render: () => ReactNode;
  /** What a developer or an agent should write — or, for a wrong one, what not to. */
  code: string;
}

export const DO: Record<string, Example> = {
  slot: {
    render: () => <LiveEditor />,
    code: `<MeasurandEditor
  dimensionality={variable.Dimensionality}
  kmsValue={variable.KmsValue}
  uncertainty={variable.Uncertainty}
  valueUnit={variable.ValueUnit}
  onChange={edit}
/>

// and inside it, the shape every slot has:
<span className="slot">
  <input className="number" />
  <select className="affix">…</select>
</span>`,
  },

  views: {
    render: () => <MeasurandReadout value={pressure(8e6, 3e5)} />,
    code: `<MeasurandReadout value={computed} />

// the shape: the reading is the control, and the chevron arrives on approach
<span className="unit views" title="Read this in another unit">
  <select>…</select>
  <span aria-hidden="true">{unit.Symbol}</span>
  <span className="chevron" aria-hidden="true">▾</span>
</span>`,
  },

  reveals: {
    render: () => <DimensionalityEditor encoded="M1,L3,T-2" onChange={() => {}} />,
    code: `<button type="button" className="reveals" aria-expanded={open} onClick={toggle}>
  {reading}
  <span className="chevron" aria-hidden="true">▾</span>
</button>

// aria-expanded is what turns the chevron, in CSS. Do not rotate it from JS.`,
  },

  tells: {
    render: () => (
      <>
        <abbr className="tells" title="Mass">M</abbr>
        <span className="dot">·</span>
        <abbr className="tells" title="Length">L</abbr>
      </>
    ),
    code: `<abbr className="tells" title={dimension.Name}>{dimension.Symbol}</abbr>

// a dotted underline means hover, and never anything else`,
  },

  kept: {
    render: () => <MeasurandReadout value={pressure(8e6, 3e5)} storedAs="relative" />,
    code: `<MeasurandReadout value={computed} storedAs="relative" />

// status takes a glyph, never an affordance channel
{storedAs === band && <span className="mark kept" aria-label="kept this way">●</span>}`,
  },

  unknown: {
    render: () => (
      <span className="unknown">
        <span className="mark unknown" aria-hidden="true">○</span>
        unknown — nothing has given this a value
      </span>
    ),
    code: `<span className="unknown">
  <span className="mark unknown" aria-hidden="true">○</span>
  unknown — nothing has given this a value
</span>`,
  },

  verdict: {
    render: () => (
      <>
        <span className="verdict holds">holds</span>{" · "}
        <span className="verdict broken">violated</span>{" · "}
        <span className="verdict undetermined">not yet judged</span>
      </>
    ),
    code: `<span className="verdict holds">holds</span>
<span className="verdict broken">violated</span>
<span className="verdict undetermined">not yet judged</span>

// the word is the other half of the channel, not a caption on the colour`,
  },

  "at-fault": {
    render: () => (
      <span className="node faulty sheet-node">
        <b>p</b> <span className="quiet">pressure</span>
        <ul className="problems">
          <li><span className="kind">DimensionalityMissing</span> A variable needs a dimensionality.</li>
        </ul>
      </span>
    ),
    code: `<div className={problems.length > 0 ? "node faulty" : "node"}>
  …
  <NodeProblems problems={problems} />
</div>

// the bar says which node to read; the server's sentence says what is wrong`,
  },
};

export const WRONG: Record<string, Example> = {
  "slot-bordered": {
    render: () => (
      <span className="nono-bordered">
        <input className="number" defaultValue="8.0" />
        <span className="affix">MPa</span>
      </span>
    ),
    code: `.nono-bordered { border: 1px solid var(--line); }   /* at rest */`,
  },

  "slot-underlined": {
    render: () => (
      <>
        <span className="nono-underlined">
          <input className="number" defaultValue="8.0" />
        </span>
        <span className="note">and an empty one:</span>
        <span className="nono-underlined"><input className="number" placeholder="" /></span>
      </>
    ),
    code: `.nono-underlined { border-bottom: 1px solid var(--line); }

/* collides with .tells, and an empty one has no area to find */`,
  },

  "slot-raised": {
    render: () => (
      <span className="nono-raised">
        <input className="number" defaultValue="8.0" />
        <span className="affix">MPa</span>
      </span>
    ),
    code: `.nono-raised {
  background: var(--panel);                 /* the same colour as behind it */
  box-shadow: -2px -2px 4px var(--raise), 2px 2px 5px var(--sink);
}

/* ~1.1:1 against the panel where 1.4.11 wants 3:1 */`,
  },

  "views-always": {
    render: () => (
      <span className="measurand nono-loud">
        <b>8.0</b>
        <span className="unit">Pa<span className="chevron">▾</span></span>
        <span className="band"> ± 0.3</span>
        <span className="unit">Pa<span className="chevron">▾</span></span>
        <span className="mark kept">●</span>
      </span>
    ),
    code: `.views .chevron { opacity: 1; }   /* instead of arriving on approach */`,
  },

  "views-dotted": {
    render: () => (
      <span className="measurand">
        <b>8.0</b> <span className="nono-dotted">Pa</span>
        <span className="band"> ± 0.3</span> <span className="nono-dotted">Pa</span>
      </span>
    ),
    code: `.unit { border-bottom: 1px dotted var(--quiet); }

/* now a dotted underline means click here as well as hover */`,
  },

  "reveals-nothing": {
    render: () => (
      <span className="slot">
        <span className="affix leading">Custom</span>
        <button type="button" className="reading nono-bare">M·L³/T²</button>
      </span>
    ),
    code: `<button type="button" onClick={toggle}>{reading}</button>

/* opens, shuts, and never says which it will do */`,
  },

  "tells-shared": {
    render: () => (
      <>
        <span className="nono-dotted">M</span>
        <span className="note">explains</span>
        <span className="nono-dotted">Pa</span>
        <span className="note">re-reads — same mark</span>
      </>
    ),
    code: `/* one channel, two consequences, on the same page */
abbr      { border-bottom: 1px dotted; }   /* hover */
.unit     { border-bottom: 1px dotted; }   /* click */`,
  },

  "kept-underlined": {
    render: () => (
      <>
        <span className="nono-dotted">Pa</span>
        <span className="note">clickable</span>
        <span className="nono-solid">Pa</span>
        <span className="note">kept — told apart by the same channel</span>
      </>
    ),
    code: `.unit.toggle      { border-bottom: 1px dotted; }   /* affordance */
.unit.toggle.kept { border-bottom-style: solid; }  /* status, same channel */`,
  },

  "unknown-word-only": {
    render: () => (
      <span className="nono-list">
        <span className="quiet">p · pressure · unknown</span>
        <span className="quiet">T · temperature · 293.1 K</span>
        <span className="quiet">m · mass · unknown</span>
        <span className="quiet">a · area · 2.000 m²</span>
      </span>
    ),
    code: `<span>unknown</span>   /* nothing to count down a column */`,
  },

  "verdict-colour-only": {
    render: () => (
      <span className="nono-swatches">
        <span className="verdict holds">●</span>
        <span className="verdict broken">●</span>
        <span className="verdict undetermined">●</span>
        <span className="note">print this, or project it</span>
      </span>
    ),
    code: `<span className="verdict holds" />   /* colour and nothing else */`,
  },

  "verdict-two-glyphs": {
    render: () => (
      <>
        <span className="verdict holds">✓</span>{" · "}
        <span className="verdict broken">✗</span>{" · "}
        <span className="verdict undetermined">?</span>
        <span className="note">the third reads as a question about the reader</span>
      </>
    ),
    code: `{satisfied === null ? "?" : satisfied ? "✓" : "✗"}`,
  },

  "at-fault-glyph": {
    render: () => (
      <span className="node sheet-node">
        <b>p</b> <span className="quiet">pressure</span>
        <span className="verdict broken">⚠</span>
        <span className="measurand"><b>8.0</b><span className="unit">MPa</span><span className="mark kept">●</span></span>
        <span className="note">three glyphs, two subjects</span>
      </span>
    ),
    code: `<span className="warning">⚠</span>

/* competes with the glyphs that are about the value rather than the node */`,
  },
};

/** The editor, with its own state, so the fill and border can be approached and focused. */
function LiveEditor() {
  const [written, setWritten] = useState<{
    kmsValue: number | null;
    uncertainty: Uncertainty | null;
    valueUnit: string | null;
  }>({ kmsValue: 8e6, uncertainty: null, valueUnit: "MPa" });

  return (
    <MeasurandEditor
      dimensionality="M1,L-1,T-2"
      kmsValue={written.kmsValue}
      uncertainty={written.uncertainty}
      valueUnit={written.valueUnit}
      onChange={setWritten}
    />
  );
}

export const pressure = (kms: number, upper: number, lower = upper): MeasurandReport => ({
  KmsValue: kms,
  Dimensionality: "M1,L-1,T-2",
  KmsUpperAbsoluteUncertainty: upper,
  KmsLowerAbsoluteUncertainty: lower,
});

export const temperature = (kms: number, band: number): MeasurandReport => ({
  KmsValue: kms,
  Dimensionality: "Θ1",
  KmsUpperAbsoluteUncertainty: band,
  KmsLowerAbsoluteUncertainty: band,
});

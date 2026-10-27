import { useState } from "react";
import type { MeasurandReport } from "../api/client";
import { DimensionalityEditor } from "../blocks/DimensionalityEditor";
import { DimensionalityView } from "../blocks/DimensionalityView";
import { MeasurandEditor } from "../blocks/MeasurandEditor";
import { MeasurandReadout } from "../blocks/MeasurandReadout";
import { MeasurandView } from "../blocks/MeasurandView";

/**
 * Every mark and every block, in every state, on one page.
 *
 * `PATTERNS.md` says what each mark means and why. This says what it looks like — which is the one thing prose
 * is worst at, and the one thing a page cannot get wrong about itself. A new block is checked against the
 * language by putting it here and looking.
 */
export function Gallery() {
  return (
    <main className="gallery">
      <header>
        <span className="title">Patterns</span>
        <span className="subtitle">what each mark means, rendered</span>
        <a className="back" href="#">← the editor</a>
      </header>

      <Affordances />
      <Status />
      <Dimensionalities />
      <Measurands />
    </main>
  );
}

function Affordances() {
  const [open, setOpen] = useState(false);
  const [written, setWritten] = useState<Written>({ kmsValue: 8e6, uncertainty: null, valueUnit: "MPa" });

  return (
    <section className="panel">
      <h2>Affordance — what happens if I touch this</h2>
      <dl>
        <dt>states the model</dt>
        <dd>
          <MeasurandEditor
            dimensionality="M1,L-1,T-2"
            kmsValue={written.kmsValue}
            uncertainty={written.uncertainty}
            valueUnit={written.valueUnit}
            onChange={setWritten}
          />
          <span className="note">filled at rest, bordered on approach, ringed on focus</span>
        </dd>

        <dt>changes this view</dt>
        <dd>
          <MeasurandReadout value={pressure(8e6, 3e5)} storedAs="absolute" />
          <span className="note">a chevron, which appears on approach</span>
        </dd>

        <dt>shows or hides</dt>
        <dd>
          <button type="button" className="reveals" aria-expanded={open} onClick={() => setOpen(!open)}>
            four more of them
            <span className="chevron" aria-hidden="true">▾</span>
          </button>
          {open && <span className="note">shown always, because it reports open as well as openable</span>}
        </dd>

        <dt>explains, and nothing else</dt>
        <dd>
          <abbr className="tells" title="Mass">M</abbr>
          <span className="note">a dotted underline and a help cursor — the only use of either</span>
        </dd>
      </dl>
    </section>
  );
}

function Status() {
  return (
    <section className="panel">
      <h2>Status — what is true about this</h2>
      <dl>
        <dt>kept this way</dt>
        <dd>
          <MeasurandReadout value={pressure(8e6, 3e5)} storedAs="relative" />
          <span className="note">● on the form the model keeps, switch the band to see it move</span>
        </dd>

        <dt>nothing determines it</dt>
        <dd>
          <span className="unknown">
            <span className="mark unknown" aria-hidden="true">○</span>
            unknown — nothing has given this a value
          </span>
        </dd>

        <dt>a verdict</dt>
        <dd>
          <span className="verdict holds">holds</span>
          <span className="note" />
          <span className="verdict broken">violated</span>
          <span className="note" />
          <span className="verdict undetermined">not yet judged</span>
          <span className="note">colour, and a word rather than colour alone</span>
        </dd>

        <dt>at fault</dt>
        <dd>
          <span className="note">a bar down the left of the node, and the problem said in words beneath it</span>
        </dd>
      </dl>
    </section>
  );
}

function Dimensionalities() {
  const [encoded, setEncoded] = useState<string | null>("M1,L-1,T-2");

  return (
    <section className="panel">
      <h2>Dimensionality</h2>
      <dl>
        <dt>inline</dt>
        <dd><DimensionalityView encoded="M1,L-1,T-2" /><span className="note">rows stay level</span></dd>

        <dt>stacked</dt>
        <dd><DimensionalityView encoded="M1,L-1,T-2" layout="stacked" /><span className="note">given a line of its own</span></dd>

        <dt>no denominator</dt>
        <dd><DimensionalityView encoded="M1,L2" /></dd>

        <dt>one factor</dt>
        <dd><DimensionalityView encoded="M1" /></dd>

        <dt>dimensionless</dt>
        <dd><DimensionalityView encoded="" /><span className="note">not the same answer as unchosen</span></dd>

        <dt>being chosen</dt>
        <dd><DimensionalityEditor encoded={encoded} onChange={setEncoded} /></dd>

        <dt>unchosen</dt>
        <dd><DimensionalityEditor encoded={null} onChange={() => {}} /><span className="note">? until the author answers</span></dd>
      </dl>
    </section>
  );
}

function Measurands() {
  return (
    <section className="panel">
      <h2>Measurand, read-only and unconfigurable</h2>
      <dl>
        <dt>absolute band</dt>
        <dd><MeasurandView value={pressure(8, 0.3)} /></dd>

        <dt>relative band</dt>
        <dd><MeasurandView value={pressure(8, 0.3)} band="relative" /></dd>

        <dt>asymmetric band</dt>
        <dd><MeasurandView value={pressure(8, 0.3, 0.12)} /></dd>

        <dt>no band</dt>
        <dd><MeasurandView value={pressure(8, 0)} /><span className="note">four figures, nothing to set them</span></dd>

        <dt>power factored out</dt>
        <dd><MeasurandView value={pressure(8e6, 3e5)} /><span className="note">a view has one unit and no controls</span></dd>

        <dt>power on each number</dt>
        <dd><MeasurandReadout value={pressure(8e6, 3e5)} /><span className="note">a readout has a control between them</span></dd>

        <dt>an offset unit</dt>
        <dd><MeasurandReadout value={temperature(293.15, 0.5)} /><span className="note">read it in °C — the band stays 0.5</span></dd>

        <dt>nothing at all</dt>
        <dd><MeasurandView value={null} /></dd>
      </dl>
    </section>
  );
}

interface Written {
  kmsValue: number | null;
  uncertainty: MeasurandEditorBand;
  valueUnit: string | null;
}

type MeasurandEditorBand = Parameters<typeof MeasurandEditor>[0]["uncertainty"];

const measurand = (
  dimensionality: string, kms: number, upper: number, lower = upper,
): MeasurandReport => ({
  KmsValue: kms,
  Dimensionality: dimensionality,
  KmsUpperAbsoluteUncertainty: upper,
  KmsLowerAbsoluteUncertainty: lower,
});

const pressure = (kms: number, upper: number, lower = upper) =>
  measurand("M1,L-1,T-2", kms, upper, lower);

const temperature = (kms: number, upper: number) => measurand("Θ1", kms, upper, upper);

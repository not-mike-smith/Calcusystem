import { useState, type ReactNode } from "react";
import type { MeasurandReport, Uncertainty } from "../api/client";
import { DimensionalityEditor } from "../blocks/DimensionalityEditor";
import { MeasurandEditor } from "../blocks/MeasurandEditor";
import { MeasurandReadout } from "../blocks/MeasurandReadout";

/**
 * One live example per mark, keyed by its id in `language.ts`.
 *
 * Real components with real state, not pictures of them: a mark that has drifted from what the document says
 * shows here, which a screenshot or a prose description would not.
 */
export const EXAMPLES: Record<string, () => ReactNode> = {
  slot: () => <LiveEditor />,
  views: () => <MeasurandReadout value={pressure(8e6, 3e5)} />,
  reveals: () => <DimensionalityEditor encoded="M1,L3,T-2" onChange={() => {}} />,
  tells: () => <abbr className="tells" title="Mass">M</abbr>,
  kept: () => <MeasurandReadout value={pressure(8e6, 3e5)} storedAs="relative" />,
  unknown: () => (
    <span className="unknown">
      <span className="mark unknown" aria-hidden="true">○</span>
      unknown — nothing has given this a value
    </span>
  ),
  verdict: () => (
    <>
      <span className="verdict holds">holds</span>{" · "}
      <span className="verdict broken">violated</span>{" · "}
      <span className="verdict undetermined">not yet judged</span>
    </>
  ),
  "at-fault": () => (
    <span className="node faulty at-fault-sample">
      <span className="symbol-sample">p</span>
      <ul className="problems">
        <li><span className="kind">DimensionalityMissing</span> A variable needs a dimensionality.</li>
      </ul>
    </span>
  ),
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

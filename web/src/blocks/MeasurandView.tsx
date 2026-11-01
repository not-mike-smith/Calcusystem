import { asNumber, type MeasurandReport, type Unit } from "../api/client";
import { figuresFor, read, readBare, superscript, type Notation } from "../format/significance";
import { baseUnit, show, showBand } from "../format/units";
import { useCatalogue } from "../model/catalogue";

export type BandForm = "absolute" | "relative";

export interface MeasurandViewProps {
  value: MeasurandReport | null | undefined;
  /** What to read it in. Defaults to the unit a KMS value is already in. */
  unit?: Unit | null;
  /** Default `"absolute"`. */
  band?: BandForm;
  /** Default `"auto"` — scientific only where a decimal reading would be mostly zeros. */
  notation?: Notation;
  /** Overrides the figures the band would otherwise set. Used only where there is no band. */
  sigFigs?: number;
}

/**
 * A value, read-only and with no affordances of its own.
 *
 * Every choice about how it reads is a prop, so a component holding one can decide; nothing here offers the
 * user a way to change any of them. `MeasurandReadout` is the one that does.
 *
 * Reads as `2.000 ± 0.020 kg` absolute, or `2.000 kg ± 1.0%` relative — the unit belongs to the value either
 * way, and the band carries the percent.
 */
export function MeasurandView({ value, unit, band = "absolute", notation, sigFigs }: MeasurandViewProps) {
  const { unitsFor } = useCatalogue();

  if (!value) return <span className="measurand none">—</span>;

  const shownIn = unit ?? baseUnit(unitsFor(value.Dimensionality));
  if (!shownIn) return <span className="measurand none">—</span>;

  const kms = asNumber(value.KmsValue);
  const upper = asNumber(value.KmsUpperAbsoluteUncertainty);
  const lower = asNumber(value.KmsLowerAbsoluteUncertainty);

  // The value's precision comes from the absolute band whichever form the band is shown in: a percentage says
  // nothing about which digits of the value are real.
  const reading = read(
    show(kms, shownIn),
    showBand(upper, shownIn),
    showBand(lower, shownIn),
    { notation, sigFigs },
  );

  // Relative needs something to be relative to, so a band on a zero value is only ever absolute.
  const relative = band === "relative" && kms !== 0 && upper + lower > 0;
  const bracketed = reading.exponent !== null && !relative && (reading.band || reading.asymmetric);

  return (
    <span className="measurand">
      {bracketed && "("}
      <b>{reading.value}</b>
      {!relative && reading.band && <span className="band"> ± {reading.band}</span>}
      {!relative && reading.asymmetric && (
        <span className="band"> +{reading.asymmetric.upper} / −{reading.asymmetric.lower}</span>
      )}
      {bracketed && ")"}
      {reading.exponent !== null && <span className="power">×10{superscript(reading.exponent)}</span>}
      <i className="unit">{shownIn.Symbol}</i>
      {relative && <span className="band"> ± {percentages(upper, lower, kms)}</span>}
    </span>
  );
}

/** A band as a percentage of its value, to the figures the band itself justifies. */
function percentages(upper: number, lower: number, kms: number): string {
  const of = (band: number) => {
    const percent = (band / Math.abs(kms)) * 100;

    return readBare(percent, { sigFigs: figuresFor(percent), notation: "decimal" }).value;
  };

  return upper === lower ? `${of(upper)}%` : `+${of(upper)}% / −${of(lower)}%`;
}

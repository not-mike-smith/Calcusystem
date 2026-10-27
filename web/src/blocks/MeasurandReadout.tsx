import { useState, type ReactNode } from "react";
import { asNumber, type MeasurandReport } from "../api/client";
import { figuresFor, read, readBare, superscript, type Notation } from "../format/significance";
import { bandSymbol, baseUnit, show, showBand, unitBySymbol } from "../format/units";
import { useCatalogue } from "../model/catalogue";
import type { BandForm } from "./MeasurandView";

interface Props {
  value: MeasurandReport | null | undefined;
  /**
   * Which form the model keeps, where that is known. Marked on the band's label, so re-reading it never leaves
   * a doubt about which one is stored. Null for a computed value, whose band is derived and kept nowhere.
   */
  storedAs?: BandForm | null;
  notation?: Notation;
  sigFigs?: number;
}

/**
 * A value the reader can re-read: in another unit, and with its band either way.
 *
 * **Nothing here changes the model.** The quantity is fixed and a unit is a lens over it, so selecting one
 * converts — the opposite of `MeasurandEditor`, where the number is what the author stated and a unit says
 * what it means. The two are told apart by their chrome: this one is text, and an editor is a field.
 *
 * The reading is the control. The unit after the value and the symbol after the band are the only two things
 * to click, so there is no widget beside the number competing with it.
 */
export function MeasurandReadout({ value, storedAs = null, notation, sigFigs }: Props) {
  const { unitsFor } = useCatalogue();
  const units = unitsFor(value?.Dimensionality);

  const [symbol, setSymbol] = useState<string | null>(null);
  const [band, setBand] = useState<BandForm>(storedAs ?? "absolute");

  const unit = unitBySymbol(units, symbol) ?? baseUnit(units);

  if (!value || !unit) return <span className="measurand none">—</span>;

  const kms = asNumber(value.KmsValue);
  const upper = asNumber(value.KmsUpperAbsoluteUncertainty);
  const lower = asNumber(value.KmsLowerAbsoluteUncertainty);

  const reading = read(show(kms, unit), showBand(upper, unit), showBand(lower, unit), { notation, sigFigs });

  const banded = upper + lower > 0;
  const relative = band === "relative" && kms !== 0 && banded;
  const choices = units?.Units ?? [];

  /*
   * Each symbol sits next to what it controls: the unit straight after the value it reads, the band's label
   * straight after the band. So an absolute band reads `8.0 MPa ± 0.3 MPa` and a relative one
   * `8.0 MPa ± 1.0 %` — the same order either way.
   *
   * Which is why the power is not factored out here the way `MeasurandView` factors it. `(9.9 ± 0.5)×10⁻⁴`
   * needs the two numbers adjacent, and the unit control has to come between them.
   */
  const shown = (digits: string): ReactNode => (
    <>
      {digits}
      {reading.exponent !== null && <span className="power">×10{superscript(reading.exponent)}</span>}
    </>
  );

  const size = bandOf(reading, relative, () => percentages(upper, lower, kms), shown);

  return (
    <span className="measurand readout">
      <b>{shown(reading.value)}</b>

      {/* The unit itself is the picker, so a reading carries no widget beside its number. */}
      {choices.length > 1 ? (
        <span className="unit pick">
          <select
            value={unit.Symbol ?? ""}
            onChange={(event) => setSymbol(event.target.value)}
            title="Read this in another unit"
          >
            {choices.map((choice) => (
              <option key={choice.Symbol} value={choice.Symbol ?? ""}>{choice.Symbol}</option>
            ))}
          </select>
          <span aria-hidden="true">{unit.Symbol}</span>
        </span>
      ) : (
        <i className="unit">{unit.Symbol}</i>
      )}

      {banded && size && (
        <span className="band">
          {size.symmetric ? " ± " : " "}
          {size.node}
        </span>
      )}

      {banded && (
        <button
          type="button"
          className={storedAs === band ? "unit toggle kept" : "unit toggle"}
          onClick={() => setBand(relative ? "absolute" : "relative")}
          title={relative
            ? `A percentage of the value${storedAs === "relative" ? ", and how it is kept" : ""} — click for an absolute amount`
            : `Absolute${storedAs === "absolute" ? ", and how it is kept" : ""} — click for a percentage`}
        >
          {relative ? "%" : bandSymbol(unit)}
        </button>
      )}
    </span>
  );
}

/** The band's digits, and whether a `±` belongs in front of them. */
function bandOf(
  reading: ReturnType<typeof read>,
  relative: boolean,
  asPercentages: () => { symmetric: boolean; text: string },
  shown: (digits: string) => ReactNode,
): { symmetric: boolean; node: ReactNode } | null {
  if (relative) {
    const percent = asPercentages();

    return { symmetric: percent.symmetric, node: percent.text };
  }

  if (reading.band !== null) return { symmetric: true, node: shown(reading.band) };

  if (reading.asymmetric) {
    return {
      symmetric: false,
      node: <>+{shown(reading.asymmetric.upper)} / −{shown(reading.asymmetric.lower)}</>,
    };
  }

  return null;
}

/** A band as a percentage of its value, to the figures the band itself justifies. */
function percentages(upper: number, lower: number, kms: number): { symmetric: boolean; text: string } {
  const of = (size: number) => {
    const percent = (size / Math.abs(kms)) * 100;

    return readBare(percent, { sigFigs: figuresFor(percent), notation: "decimal" }).value;
  };

  return upper === lower
    ? { symmetric: true, text: of(upper) }
    : { symmetric: false, text: `+${of(upper)} / −${of(lower)}` };
}

import { useState } from "react";
import type { MeasurandReport } from "../api/client";
import type { Notation } from "../format/significance";
import { baseUnit, unitBySymbol } from "../format/units";
import { useCatalogue } from "../model/catalogue";
import { MeasurandView, type BandForm } from "./MeasurandView";

interface Props {
  value: MeasurandReport | null | undefined;
  /**
   * Which form the model actually keeps, where that is known. Marked in the band control, so switching the
   * reading never leaves a doubt about which one is the stored one. Null for a computed value, whose band is
   * derived and stored nowhere.
   */
  storedAs?: BandForm | null;
  notation?: Notation;
  sigFigs?: number;
}

/**
 * A value the reader can re-read: in another unit, and with its band either way.
 *
 * Read-only — nothing here changes the model. Which unit and which band form are this component's own state,
 * so two readouts of the same value can be set differently.
 */
export function MeasurandReadout({ value, storedAs = null, notation, sigFigs }: Props) {
  const { unitsFor } = useCatalogue();
  const units = unitsFor(value?.Dimensionality);

  const [symbol, setSymbol] = useState<string | null>(null);
  const [band, setBand] = useState<BandForm>(storedAs ?? "absolute");

  const unit = unitBySymbol(units, symbol) ?? baseUnit(units);
  const hasBand = !!value && band !== null;

  return (
    <span className="readout">
      <MeasurandView value={value} unit={unit} band={band} notation={notation} sigFigs={sigFigs} />

      {units && (units.Units?.length ?? 0) > 1 && (
        <select
          className="unit-choice"
          value={unit?.Symbol ?? ""}
          onChange={(event) => setSymbol(event.target.value)}
          title="Read this in another unit"
        >
          {(units.Units ?? []).map((choice) => (
            <option key={choice.Symbol} value={choice.Symbol ?? ""}>{choice.Symbol}</option>
          ))}
        </select>
      )}

      {hasBand && (
        <span className="band-choice">
          <Form form="absolute" chosen={band} storedAs={storedAs} onChoose={setBand} label="±" />
          <Form form="relative" chosen={band} storedAs={storedAs} onChoose={setBand} label="%" />
        </span>
      )}
    </span>
  );
}

function Form({
  form, chosen, storedAs, onChoose, label,
}: {
  form: BandForm;
  chosen: BandForm;
  storedAs: BandForm | null;
  onChoose: (form: BandForm) => void;
  label: string;
}) {
  const stored = storedAs === form;

  return (
    <button
      type="button"
      className={form === chosen ? "form chosen" : "form"}
      onClick={() => onChoose(form)}
      title={stored ? `Shown and kept as ${form}` : `Show as ${form}`}
    >
      {label}
      {stored && <span className="kept" aria-label="kept this way">•</span>}
    </button>
  );
}

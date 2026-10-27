import { asNumber, type MeasurandReport } from "../api/client";
import { format } from "../api/dimensionality";

const DIGITS = new Intl.NumberFormat(undefined, { maximumSignificantDigits: 6 });

/** A computed value, in KMS units, with its band where it has one. */
export function Value({ value }: { value: MeasurandReport | null | undefined }) {
  if (!value) return <span className="value none">—</span>;

  const upper = asNumber(value.KmsUpperAbsoluteUncertainty);
  const lower = asNumber(value.KmsLowerAbsoluteUncertainty);

  return (
    <span className="value">
      <b>{DIGITS.format(asNumber(value.KmsValue))}</b>
      {band(upper, lower)}
      <i>{format(value.Dimensionality)}</i>
    </span>
  );
}

function band(upper: number, lower: number) {
  if (upper === 0 && lower === 0) return null;
  if (upper === lower) return <span className="band"> ± {DIGITS.format(upper)}</span>;

  return (
    <span className="band">
      {" "}
      +{DIGITS.format(upper)} / −{DIGITS.format(lower)}
    </span>
  );
}

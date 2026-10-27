import type { DimensionReport } from "../api/client";
import { decode } from "../api/dimensionality";
import { superscript } from "../format/significance";
import { useCatalogue } from "../model/catalogue";

interface Props {
  encoded: string | null | undefined;
  /**
   * `"inline"` keeps every row the same height — `M·L/T²`. `"stacked"` draws a real dividing line, for where
   * it has a line of its own.
   */
  layout?: "inline" | "stacked";
}

/**
 * What a quantity measures. Read-only.
 *
 * Shows only the dimensions with an exponent, in the canonical order the server lists them in, and turns
 * negative exponents into a denominator rather than printing a minus sign.
 */
export function DimensionalityView({ encoded, layout = "inline" }: Props) {
  const { dimensions } = useCatalogue();
  const { over, under } = split(encoded, dimensions);

  if (over.length === 0 && under.length === 0) {
    return <span className="dimensionality-view none" title="Dimensionless">—</span>;
  }

  if (under.length === 0) {
    return <span className="dimensionality-view"><Factors factors={over} /></span>;
  }

  if (layout === "stacked") {
    return (
      <span className="dimensionality-view stacked">
        <span className="numerator">{over.length === 0 ? "1" : <Factors factors={over} />}</span>
        <span className="denominator"><Factors factors={under} /></span>
      </span>
    );
  }

  return (
    <span className="dimensionality-view">
      {over.length === 0 ? "1" : <Factors factors={over} />}
      <span className="solidus">/</span>
      {/* Parenthesised where the denominator has more than one factor, since `M/L·T²` would read as
          `(M/L)·T²`. */}
      {under.length > 1
        ? <>({<Factors factors={under} />})</>
        : <Factors factors={under} />}
    </span>
  );
}

interface Factor {
  symbol: string;
  name: string;
  power: number;
}

function Factors({ factors }: { factors: Factor[] }) {
  return (
    <>
      {factors.map((factor, index) => (
        <span key={factor.symbol}>
          {index > 0 && <span className="dot">·</span>}
          <abbr title={factor.name}>{factor.symbol}</abbr>
          {factor.power !== 1 && superscript(factor.power)}
        </span>
      ))}
    </>
  );
}

/** Numerator and denominator, in the order the server lists the dimensions. */
function split(encoded: string | null | undefined, dimensions: DimensionReport[]) {
  const exponents = decode(encoded);
  const over: Factor[] = [];
  const under: Factor[] = [];

  for (const dimension of dimensions) {
    const symbol = dimension.Symbol ?? "";
    const power = exponents[symbol] ?? 0;
    if (power === 0) continue;

    const factor = { symbol, name: dimension.Name ?? symbol, power: Math.abs(power) };
    (power > 0 ? over : under).push(factor);
  }

  return { over, under };
}

/*
 * Renders each building block to text and prints it, so what they produce can be read without a browser.
 *
 * Not a test — it asserts nothing. It is a way to look at the output, which is the one thing prose about a
 * component cannot do. It found an absolute band printing both unit symbols at the end of the reading, and a
 * floating-point error that silently granted an uncertainty a second significant figure.
 *
 *   npm run render-check
 */
import type { ReactNode } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { DimensionalityView } from "../src/blocks/DimensionalityView";
import { MeasurandReadout } from "../src/blocks/MeasurandReadout";
import { MeasurandView } from "../src/blocks/MeasurandView";
import { CatalogueProvider } from "../src/model/catalogue";

/** Canonical order, as `FundamentalDimension.Order` declares it. */
const Dimensions = [
  { Symbol: "C", Name: "Currency" },
  { Symbol: "N", Name: "Amount of Matter" },
  { Symbol: "M", Name: "Mass" },
  { Symbol: "J", Name: "Luminous Intensity" },
  { Symbol: "I", Name: "Electric Current" },
  { Symbol: "L", Name: "Length" },
  { Symbol: "Θ", Name: "Temperature" },
  { Symbol: "A", Name: "Angle" },
  { Symbol: "T", Name: "Time" },
];

const PRESSURE = "M1,L-1,T-2";
const TEMPERATURE = "Θ1";

const units = {
  Dimensionalities: [
    {
      Dimensionality: PRESSURE,
      BaseSymbol: "Pa",
      Units: [
        { Symbol: "Pa", KmsConversionFactor: 1, ZeroOffset: 0 },
        { Symbol: "MPa", KmsConversionFactor: 1e6, ZeroOffset: 0 },
      ],
    },
    {
      Dimensionality: TEMPERATURE,
      BaseSymbol: "K",
      Units: [
        { Symbol: "K", KmsConversionFactor: 1, ZeroOffset: 0 },
        { Symbol: "°C", KmsConversionFactor: 1, ZeroOffset: 273.15, DeltaSymbol: "Δ°C" },
      ],
    },
  ],
};

/** The rendered text, with the hidden picker markup taken out and whitespace collapsed. */
function text(node: ReactNode): string {
  return renderToStaticMarkup(
    <CatalogueProvider vocabulary={{ Dimensions } as never} units={units as never}>
      {node}
    </CatalogueProvider>,
  )
    .replace(/<select[\s\S]*?<\/select>/g, "")
    .replace(/<[^>]+>/g, "")
    .replace(/\s+/g, " ")
    .trim();
}

const measurand = (dimensionality: string, kms: number, upper: number, lower = upper) => ({
  KmsValue: kms,
  Dimensionality: dimensionality,
  KmsUpperAbsoluteUncertainty: upper,
  KmsLowerAbsoluteUncertainty: lower,
});

const pressure = (kms: number, upper: number, lower = upper) =>
  measurand(PRESSURE, kms, upper, lower);

const rows: [string, ReactNode][] = [
  ["readout, absolute", <MeasurandReadout value={pressure(8, 0.3)} storedAs="absolute" />],
  ["readout, relative", <MeasurandReadout value={pressure(8, 0.3)} storedAs="relative" />],
  ["readout, scientific", <MeasurandReadout value={pressure(8e6, 3e5)} storedAs="absolute" />],
  ["readout, no band", <MeasurandReadout value={pressure(8, 0)} />],
  ["readout, asymmetric", <MeasurandReadout value={pressure(8, 0.3, 0.12)} storedAs="absolute" />],
  ["readout, offset unit", <MeasurandReadout value={measurand(TEMPERATURE, 293.15, 0.5)} storedAs="absolute" />],
  ["view, absolute", <MeasurandView value={pressure(8, 0.3)} />],
  ["view, relative", <MeasurandView value={pressure(8, 0.3)} band="relative" />],
  ["view, factored power", <MeasurandView value={pressure(8e6, 3e5)} />],
  ["view, no band", <MeasurandView value={pressure(8, 0)} />],
  ["dimensionality, inline", <DimensionalityView encoded={PRESSURE} />],
  ["dimensionality, stacked", <DimensionalityView encoded={PRESSURE} layout="stacked" />],
  ["dimensionality, no denominator", <DimensionalityView encoded="M1,L2" />],
  ["dimensionality, dimensionless", <DimensionalityView encoded="" />],
];

/* The unit sits a few pixels off its number by stylesheet rather than by a space, so the gap is absent here. */
for (const [label, node] of rows) console.log(label.padEnd(32), "→", text(node));

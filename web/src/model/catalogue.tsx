import { createContext, useContext, type ReactNode } from "react";
import type { DimensionReport, DimensionalityUnits, UnitCatalogueReport, VocabularyReport } from "../api/client";
import { canonical } from "../format/units";

/**
 * What the server says exists: the dimensions, the kinds, and the units.
 *
 * Held in a context rather than passed down, so a building block can ask for the units of a dimensionality
 * without every component between here and it having to carry a catalogue it does not use.
 */
export interface Catalogue {
  vocabulary: VocabularyReport;
  dimensions: DimensionReport[];
  /** The units that measure this dimensionality, or null where none do. */
  unitsFor: (encoded: string | null | undefined) => DimensionalityUnits | null;
}

const CatalogueContext = createContext<Catalogue | null>(null);

export function CatalogueProvider({
  vocabulary, units, children,
}: { vocabulary: VocabularyReport; units: UnitCatalogueReport; children: ReactNode }) {
  const dimensions = vocabulary.Dimensions ?? [];

  const byDimensionality = new Map(
    (units.Dimensionalities ?? []).map((group) => [
      canonical(group.Dimensionality, dimensions),
      group,
    ]),
  );

  const catalogue: Catalogue = {
    vocabulary,
    dimensions,
    unitsFor: (encoded) =>
      encoded === null || encoded === undefined
        ? null
        : byDimensionality.get(canonical(encoded, dimensions)) ?? null,
  };

  return <CatalogueContext.Provider value={catalogue}>{children}</CatalogueContext.Provider>;
}

/** The catalogue. Throws rather than guessing, since every block here needs it to say anything at all. */
export function useCatalogue(): Catalogue {
  const catalogue = useContext(CatalogueContext);
  if (!catalogue) throw new Error("A CatalogueProvider must be above anything that reads the catalogue.");

  return catalogue;
}

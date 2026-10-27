import { useEffect, useRef, useState } from "react";
import {
  calculate,
  decompose,
  type CalculationReport,
  type DecompositionReport,
  type DraftProblem,
  type SystemDraft,
} from "../api/client";

export interface Analysis {
  /** What stands between the draft and a built system. Empty when it builds. */
  problems: DraftProblem[];
  /** Null while the draft does not build. */
  calculation: CalculationReport | null;
  decomposition: DecompositionReport | null;
  /** A payload the server could not read at all, which names no node. */
  message: string | null;
  pending: boolean;
}

const QUIET: Analysis = {
  problems: [],
  calculation: null,
  decomposition: null,
  message: null,
  pending: false,
};

/**
 * Asks the server what the draft is, shortly after it stops changing.
 *
 * Problems are read off the failed build rather than from a third call to `validation`: `Validate` reads its
 * own answer off a `Build`, so the two cannot disagree, and one of them is already being asked.
 */
export function useAnalysis(draft: SystemDraft, delay = 300): Analysis {
  const [analysis, setAnalysis] = useState<Analysis>(QUIET);

  // Replies can arrive out of order, and the last one asked for is the only one that describes what is on
  // screen. Anything older is dropped.
  const asked = useRef(0);

  useEffect(() => {
    setAnalysis((current) => ({ ...current, pending: true }));

    const timer = setTimeout(async () => {
      const mine = ++asked.current;
      const [calculation, decomposition] = await Promise.all([calculate(draft), decompose(draft)]);
      if (mine !== asked.current) return;

      const failure = !calculation.ok ? calculation : !decomposition.ok ? decomposition : null;

      setAnalysis({
        problems: failure?.problems ?? [],
        calculation: calculation.ok ? calculation.value : null,
        decomposition: decomposition.ok ? decomposition.value : null,
        message: failure && failure.problems.length === 0 ? failure.message : null,
        pending: false,
      });
    }, delay);

    return () => clearTimeout(timer);
  }, [draft, delay]);

  return analysis;
}

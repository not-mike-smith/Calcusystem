import type { DraftProblem, ExpressionDraft, MeasurandReport } from "../api/client";
import { labelFor } from "../model/draft";
import { MeasurandReadout } from "../blocks/MeasurandReadout";
import { NodeProblems } from "./ProblemList";

interface Props {
  node: ExpressionDraft;
  problems: DraftProblem[];
  computed: MeasurandReport | null;
  onRemove: () => void;
}

/**
 * One constant, reference value or scalar, shown and not edited. The editor has no fields for one, and a
 * formula row would offer it operands it ignores and a kind menu that does not hold its kind.
 */
export function FixedValueRow({ node, problems, computed, onRemove }: Props) {
  return (
    <div className={problems.length > 0 ? "node faulty" : "node"}>
      <div className="row">
        <span>{labelFor(node)}</span>
        {node.Name?.trim() && <span className="over">{node.Type}</span>}
        <button type="button" className="ghost remove" onClick={onRemove} title="Remove">×</button>
      </div>

      <div className="row readout">
        <MeasurandReadout value={computed} />
      </div>

      <NodeProblems problems={problems} />
    </div>
  );
}

import type { DraftProblem } from "../api/client";

/** What a node is wrong about, said where the node is. */
export function NodeProblems({ problems }: { problems: DraftProblem[] }) {
  if (problems.length === 0) return null;

  return (
    <ul className="problems">
      {problems.map((problem, index) => (
        <li key={index}>
          <span className="kind">{problem.Kind}</span> {problem.Message}
        </li>
      ))}
    </ul>
  );
}

/** The problems that belong to no node — the ones about the draft as a whole. */
export function DraftProblems({ problems }: { problems: DraftProblem[] }) {
  const loose = problems.filter((problem) => !problem.NodeId);
  if (loose.length === 0) return null;

  return (
    <section className="panel">
      <h2>This model</h2>
      <NodeProblems problems={loose} />
    </section>
  );
}

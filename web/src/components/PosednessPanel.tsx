import { asNumber, type DecompositionReport } from "../api/client";

interface Props {
  decomposition: DecompositionReport | null;
  nameOf: (id: string | null | undefined) => string;
}

/** Whether the model can be solved, and which part of it cannot. */
export function PosednessPanel({ decomposition, nameOf }: Props) {
  if (!decomposition) {
    return (
      <section className="panel">
        <h2>Can this be solved?</h2>
        <p className="quiet">Not until the model builds.</p>
      </section>
    );
  }

  const freedom = asNumber(decomposition.DegreesOfFreedom ?? 0);

  return (
    <section className="panel">
      <h2>Can this be solved?</h2>

      <p className={decomposition.IsWellPosed ? "verdict holds" : "verdict undetermined"}>
        {decomposition.IsWellPosed
          ? "Well posed — every unknown has an equation, and every equation an unknown."
          : "Not well posed."}
      </p>

      <p className="freedom">
        <b>{freedom}</b> {freedom === 1 ? "degree" : "degrees"} of freedom
        <span className="quiet"> — {advice(freedom)}</span>
      </p>

      <Names
        title="Nothing determines these"
        ids={decomposition.UnknownsWithNoEquationIds ?? []}
        nameOf={nameOf}
      />
      <Names
        title="Too few equations for these"
        ids={(decomposition.Underdetermined?.Unknowns ?? []).map((u) => u.Id)}
        nameOf={nameOf}
      />
      <Names
        title="Solvable"
        ids={(decomposition.WellDetermined?.Unknowns ?? []).map((u) => u.Id)}
        nameOf={nameOf}
      />
      <Names
        title="More equations than unknowns"
        ids={(decomposition.Overdetermined?.Unknowns ?? []).map((u) => u.Id)}
        nameOf={nameOf}
      />
      <Names
        title="These check a result rather than determine one"
        ids={decomposition.RedundantEquationIds ?? []}
        nameOf={nameOf}
      />
    </section>
  );
}

/* The count alone can net two opposite failures away, which is why the parts are listed beside it. */
function advice(freedom: number): string {
  if (freedom > 0) return `supply ${freedom} more value${freedom === 1 ? "" : "s"}, or add equations`;
  if (freedom < 0) return "more equations than unknowns to determine";

  return "the counts agree";
}

function Names({
  title, ids, nameOf,
}: { title: string; ids: (string | null | undefined)[]; nameOf: Props["nameOf"] }) {
  if (ids.length === 0) return null;

  return (
    <p className="names">
      <span className="quiet">{title}: </span>
      {ids.map((id) => <code key={id}>{nameOf(id)}</code>)}
    </p>
  );
}

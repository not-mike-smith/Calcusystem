using System.Text;
using Calcusystem.Analysis.Extensions;
using Calcusystem.Analysis.Outcomes;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Units;

namespace Calcusystem.Examples.Presentation;

/// <summary>
/// Prints a <see cref="WorkedExample"/>: what was given, how the model stands, what it computed, and what its
/// relationships did.
/// </summary>
/// <remarks>
/// Returns a string rather than writing to the console, so the shape of a report is something a test can hold
/// to.
/// </remarks>
public static class ExampleReport
{
    private const int Width = 78;

    /// <summary>Renders <paramref name="example"/> against a fresh calculation of its own model.</summary>
    public static string Render(WorkedExample example) =>
        Render(example, example.System.Calculate());

    /// <summary>
    /// Renders <paramref name="example"/> against <paramref name="calculation"/>, which need not be a
    /// calculation of the model as authored — withhold a value or override one and the report follows.
    /// </summary>
    public static string Render(WorkedExample example, Calculation calculation)
    {
        var units = UnitsByNode(example);
        var report = new StringBuilder();

        Heading(report, example);
        Given(report, example);
        Posed(report, example);
        Computed(report, example, calculation);
        Checks(report, calculation, units);
        Outstanding(report, calculation);

        return report.ToString();
    }

    private static void Heading(StringBuilder report, WorkedExample example)
    {
        var rule = new string('═', Width);
        var discipline = $"[{example.Discipline}]";
        var gap = Math.Max(1, Width - example.Title.Length - discipline.Length);

        report.AppendLine(rule);
        report.AppendLine($"{example.Title}{new string(' ', gap)}{discipline}");
        report.AppendLine(rule);
        report.AppendLine();

        foreach (var line in Wrapped(example.Premise, Width)) report.AppendLine(line);
    }

    private static void Given(StringBuilder report, WorkedExample example)
    {
        report.AppendLine();
        report.AppendLine("GIVEN");

        Rows(report, example.Givens.Select(given =>
        {
            var value = example.Leaf(given.Expression.Id).Value;

            return (
                given.Label,
                value is null ? "— withheld —" : Figures.Measure(value, given.Unit),
                Note(example, given));
        }));
    }

    private static string Note(WorkedExample example, Readout given)
    {
        var provenance = example.Leaf(given.Expression.Id).Provenance?.Summary();

        return (given.Note, provenance) switch
        {
            ("", null) => "",
            ("", not null) => provenance,
            (_, null) => given.Note,
            _ => $"{given.Note} — {provenance}",
        };
    }

    private static void Posed(StringBuilder report, WorkedExample example)
    {
        var flat = example.System.Flatten();
        var parts = flat.Decompose();
        var live = flat.Equations.Count - flat.RedundantEquations.Count();

        report.AppendLine();
        report.AppendLine("POSED");
        report.AppendLine(
            $"  {Count(flat.Unknowns.Count, "unknown")} − {Count(live, "determining equation")} " +
            $"= DoF {flat.DegreesOfFreedom}, {flat.Determination}");

        var redundant = flat.RedundantEquations.Count();
        if (redundant > 0)
        {
            report.AppendLine(
                $"  {Count(redundant, "redundancy check")}: an equality whose two sides are both already " +
                "known determines nothing, and corroborates instead.");
        }

        if (parts.IsWellPosed) return;

        if (! parts.Underdetermined.IsEmpty)
        {
            report.AppendLine(
                $"  no equation reaches: {Names(parts.Underdetermined.Unknowns)}");
        }

        if (! parts.Overdetermined.IsEmpty)
        {
            report.AppendLine(
                $"  more equations than unknowns around: {Names(parts.Overdetermined.Unknowns)}");
        }
    }

    private static void Computed(StringBuilder report, WorkedExample example, Calculation calculation)
    {
        report.AppendLine();
        report.AppendLine("COMPUTED");

        Rows(report, example.Computed.Select(result =>
        {
            var value = calculation.ValueOf(result.Expression);

            return (
                result.Label,
                value is null ? "— unresolved —" : Figures.Measure(value, result.Unit),
                result.Note);
        }));
    }

    private static void Checks(
        StringBuilder report,
        Calculation calculation,
        IReadOnlyDictionary<IExpression, UnitOfMeasure> units)
    {
        if (calculation.Outcomes.Count == 0) return;

        report.AppendLine();
        report.AppendLine("CHECKS");

        Rows(report, calculation.Outcomes.Select(outcome =>
        {
            var glyph = outcome.IsSatisfied switch
            {
                true => "✓",
                false => "✗",
                null => "?",
            };

            return (
                $"{glyph} {outcome.Relationship.Id}",
                $"{Side(outcome.Lhs, outcome.Relationship.Lhs, units)} " +
                $"{outcome.Relationship.Symbol} " +
                $"{Side(outcome.Rhs, outcome.Relationship.Rhs, units)}",
                outcome.Relationship.SolvingRole.ToString().ToLowerInvariant());
        }));
    }

    private static void Outstanding(StringBuilder report, Calculation calculation)
    {
        if (calculation.IsComplete) return;

        report.AppendLine();
        report.AppendLine("OUTSTANDING");
        report.AppendLine($"  missing values: {Names(calculation.MissingValues)}");
        report.AppendLine($"  {Count(calculation.Unresolved.Count, "node")} could not be computed");
    }

    /// <summary>
    /// A relationship side in whichever unit the report already shows that node in, or in KMS where it shows
    /// the node nowhere.
    /// </summary>
    /// <remarks>
    /// The fallback prints the dimensionality beside the number. A bare KMS value with no dimension on it is
    /// the one reading an engineer cannot check.
    /// </remarks>
    private static string Side(
        Measurand? value,
        IExpression node,
        IReadOnlyDictionary<IExpression, UnitOfMeasure> units)
    {
        if (value is null) return "—";

        return units.TryGetValue(node, out var unit)
            ? $"{Figures.Significant(value.In(unit), 4)}{Figures.Suffix(unit)}"
            : $"{Figures.Significant(value.KmsValue, 4)} [{value.Dimensionality}]";
    }

    private static IReadOnlyDictionary<IExpression, UnitOfMeasure> UnitsByNode(WorkedExample example)
    {
        var units = new Dictionary<IExpression, UnitOfMeasure>();

        // First listing wins. A node shown twice — a precession rate in rpm and again in °/s — should be
        // compared in the unit it was reported in first, so a check reads against the column above it.
        foreach (var readout in example.Givens.Concat(example.Computed))
        {
            units.TryAdd(readout.Expression, readout.Unit);
        }

        return units;
    }

    /// <summary>
    /// Writes a three-column block, each column widened to its own content.
    /// </summary>
    /// <remarks>
    /// Per block rather than per report: a check's label is a sentence and a given's is two words, and one
    /// width for both leaves whichever lost either truncated or adrift from its value.
    /// </remarks>
    private static void Rows(StringBuilder report, IEnumerable<(string Label, string Value, string Note)> rows)
    {
        var block = rows.ToList();
        if (block.Count == 0) return;

        var labelWidth = block.Max(row => row.Label.Length) + 2;
        var valueWidth = block.Max(row => row.Value.Length) + 2;

        foreach (var (label, value, note) in block)
        {
            report.AppendLine(note.Length == 0
                ? $"  {label.PadRight(labelWidth)}{value}"
                : $"  {label.PadRight(labelWidth)}{value.PadRight(valueWidth)}{note}");
        }
    }

    private static string Count(int many, string noun) =>
        many == 1 ? $"1 {noun}" : $"{many} {noun}s";

    private static string Names<T>(IEnumerable<T> nodes) where T : IExpression =>
        string.Join(", ", nodes.Select(node => node.Id));

    private static IEnumerable<string> Wrapped(string text, int width)
    {
        var line = new StringBuilder();

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                yield return line.ToString();
                line.Clear();
            }

            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }

        if (line.Length > 0) yield return line.ToString();
    }
}

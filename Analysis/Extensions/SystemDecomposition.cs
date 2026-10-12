using Calcusystem.Analysis.Outcomes;
using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Systems;
using Calcusystem.Measurement.Primitives;

namespace Calcusystem.Analysis.Extensions;

/// <summary>
/// Splitting a flat system into the parts that can be solved and the part that cannot, by matching equations to
/// unknowns.
/// </summary>
/// <remarks>This is the Dulmage–Mendelsohn decomposition. The assembly README describes how it works.</remarks>
public static class SystemDecomposition
{
    /// <summary>Flattens <paramref name="system"/> and decomposes the result.</summary>
    /// <param name="system">The system to analyse. Never mutated.</param>
    /// <param name="overrides">Values supplied for this analysis only — see <see cref="SystemFlattener.Flatten"/>.</param>
    /// <remarks>Take the <see cref="FlatSystem"/> yourself if you also want the count, rather than flattening twice.</remarks>
    public static Decomposition Decompose(
        this ExpressionSystem system,
        IReadOnlyDictionary<Variable, Measurand>? overrides = null) =>
        system.Flatten(overrides).Decompose();

    /// <summary>Splits <paramref name="flat"/> into its under-, well-, and over-determined parts.</summary>
    /// <param name="flat">The flattened system to decompose.</param>
    /// <remarks>Equations reaching no unknown take no part in the matching.</remarks>
    public static Decomposition Decompose(this FlatSystem flat)
    {
        var columns = flat.Unknowns;
        var rows = flat.Equations.Where(e => e.Unknowns.Count > 0).ToList();

        if (columns.Count == 0 && rows.Count == 0) return new Decomposition(SystemBlock.Empty, SystemBlock.Empty, SystemBlock.Empty);

        var columnOf = new Dictionary<Variable, int>();
        for (var i = 0; i < columns.Count; i++) columnOf[columns[i]] = i;

        // Every unknown an equation is incident on is one of the system's unknowns: both come from the same
        // filter in `Flatten`, over variables the system reaches. So the lookup cannot miss.
        var incidentColumns = rows
            .Select(e => e.Unknowns.Select(u => columnOf[u]).Distinct().ToArray())
            .ToArray();

        var incidentRows = new List<int>[columns.Count];
        for (var c = 0; c < columns.Count; c++) incidentRows[c] = [];
        for (var r = 0; r < rows.Count; r++)
            foreach (var c in incidentColumns[r])
                incidentRows[c].Add(r);

        var (rowMatch, columnMatch) = Match(incidentColumns, columns.Count);

        // The two tails, each found by following alternating paths out from what the matching could not pair.
        // They are provably disjoint, so what neither reaches is the square part.
        var under = ReachFromUnmatchedColumns(incidentRows, rowMatch, columnMatch, rows.Count);
        var over = ReachFromUnmatchedRows(incidentColumns, rowMatch, columnMatch, columns.Count);

        SystemBlock Block(Func<int, bool> takesColumn, Func<int, bool> takesRow) =>
            new(
                [.. columns.Where((_, c) => takesColumn(c))],
                [.. rows.Where((_, r) => takesRow(r))]);

        return new Decomposition(
            Underdetermined: Block(c => under.Columns[c], r => under.Rows[r]),
            WellDetermined: Block(c => ! under.Columns[c] && ! over.Columns[c], r => ! under.Rows[r] && ! over.Rows[r]),
            Overdetermined: Block(c => over.Columns[c], r => over.Rows[r]));
    }

    /// <summary>
    /// Pairs each equation with an unknown it could determine, as many as can be paired at once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Kuhn's algorithm: one search per equation for an augmenting path — an alternating chain ending at an
    /// unknown nothing holds yet. Flipping every edge along it leaves each equation on the chain still paired
    /// and pairs one more, so the matching grows by exactly one per search and is maximum when none is found.
    /// </para>
    /// <para>
    /// Breadth-first, with the path recorded in <c>reachedBy</c> rather than on a call stack. Nothing bounds how
    /// long an alternating path can be, and a stack frame per step is an avoidable way to fail.
    /// </para>
    /// </remarks>
    /// <returns>For each row, the column it took (or -1); and for each column, the row that took it (or -1).</returns>
    private static (int[] RowMatch, int[] ColumnMatch) Match(int[][] incidentColumns, int columnCount)
    {
        var rowMatch = new int[incidentColumns.Length];
        var columnMatch = new int[columnCount];
        Array.Fill(rowMatch, -1);
        Array.Fill(columnMatch, -1);

        var reachedBy = new int[columnCount];
        var queue = new Queue<int>();

        // Only `start` is newly paired by a search, so a row is never already paired when its turn comes.
        for (var start = 0; start < incidentColumns.Length; start++)
        {
            Array.Fill(reachedBy, -1);
            queue.Clear();
            queue.Enqueue(start);

            var unheld = -1;
            while (unheld < 0 && queue.Count > 0)
            {
                var row = queue.Dequeue();
                foreach (var column in incidentColumns[row])
                {
                    if (reachedBy[column] >= 0) continue;
                    reachedBy[column] = row;

                    // An unknown nothing holds ends the chain; one already held is passed through to the
                    // equation holding it, which is asked to give it up and take another.
                    if (columnMatch[column] < 0) { unheld = column; break; }
                    queue.Enqueue(columnMatch[column]);
                }
            }

            // Walk the chain back to `start`, each row taking the column that reached it and releasing the one
            // it had. The release ends at `start`, which had none.
            for (var column = unheld; column >= 0; )
            {
                var row = reachedBy[column];
                var released = rowMatch[row];
                rowMatch[row] = column;
                columnMatch[column] = row;
                column = released;
            }
        }

        return (rowMatch, columnMatch);
    }

    /// <summary>
    /// Everything an alternating path from an unpaired <i>unknown</i> reaches: the underdetermined part.
    /// </summary>
    /// <remarks>
    /// From an unknown, step to every equation incident on it; from an equation, step to the one unknown it
    /// holds. An equation reached this way is always paired — an unpaired one would be an augmenting path, which
    /// a maximum matching has none of.
    /// </remarks>
    private static (bool[] Rows, bool[] Columns) ReachFromUnmatchedColumns(
        List<int>[] incidentRows, int[] rowMatch, int[] columnMatch, int rowCount)
    {
        var columns = new bool[columnMatch.Length];
        var rows = new bool[rowCount];
        var queue = new Queue<int>();

        for (var c = 0; c < columnMatch.Length; c++)
            if (columnMatch[c] < 0) { columns[c] = true; queue.Enqueue(c); }

        while (queue.Count > 0)
            foreach (var row in incidentRows[queue.Dequeue()])
            {
                if (rows[row]) continue;
                rows[row] = true;

                var held = rowMatch[row];
                if (held >= 0 && ! columns[held]) { columns[held] = true; queue.Enqueue(held); }
            }

        return (rows, columns);
    }

    /// <summary>
    /// Everything an alternating path from an unpaired <i>equation</i> reaches: the overdetermined part.
    /// </summary>
    /// <remarks>The mirror of <see cref="ReachFromUnmatchedColumns"/>, with the two sides swapped.</remarks>
    private static (bool[] Rows, bool[] Columns) ReachFromUnmatchedRows(
        int[][] incidentColumns, int[] rowMatch, int[] columnMatch, int columnCount)
    {
        var rows = new bool[rowMatch.Length];
        var columns = new bool[columnCount];
        var queue = new Queue<int>();

        for (var r = 0; r < rowMatch.Length; r++)
            if (rowMatch[r] < 0) { rows[r] = true; queue.Enqueue(r); }

        while (queue.Count > 0)
            foreach (var column in incidentColumns[queue.Dequeue()])
            {
                if (columns[column]) continue;
                columns[column] = true;

                var holder = columnMatch[column];
                if (holder >= 0 && ! rows[holder]) { rows[holder] = true; queue.Enqueue(holder); }
            }

        return (rows, columns);
    }
}

using Calcusystem.DimensionedExpression.Expressions;
using Calcusystem.DimensionedExpression.Interfaces;
using Calcusystem.DimensionedExpression.Systems;

namespace Calcusystem.Examples.Presentation;

/// <summary>
/// One engineering problem, built as an <see cref="ExpressionSystem"/> and annotated well enough to print.
/// </summary>
/// <remarks>
/// The system alone cannot be reported on: it knows what the quantities are but not which of them a reader
/// came for, nor in which unit. The two <see cref="Readout"/> lists supply that.
/// </remarks>
/// <param name="Title">The problem, in a few words.</param>
/// <param name="Discipline">The branch of engineering it comes from.</param>
/// <param name="Premise">The problem statement, as it would be set.</param>
/// <param name="System">The model. Every value, formula, and check the problem needs.</param>
/// <param name="Givens">What was supplied, each pointing at a <c>Variable</c>.</param>
/// <param name="Computed">What the problem asks for, each pointing at a computed node.</param>
public sealed record WorkedExample(
    string Title,
    string Discipline,
    string Premise,
    ExpressionSystem System,
    IReadOnlyList<Readout> Givens,
    IReadOnlyList<Readout> Computed)
{
    /// <summary>The node with this id.</summary>
    /// <exception cref="KeyNotFoundException">The system holds no node with that id.</exception>
    public IExpression Node(string id) =>
        System.GetAllExpressions().FirstOrDefault(expression => expression.Id == id)
        ?? throw new KeyNotFoundException($"'{Title}' has no node with id '{id}'.");

    /// <summary>The leaf with this id — one whose value a caller may read or withhold.</summary>
    /// <exception cref="KeyNotFoundException">The system holds no variable with that id.</exception>
    public Variable Leaf(string id) =>
        System.Variables.FirstOrDefault(variable => variable.Id == id)
        ?? throw new KeyNotFoundException($"'{Title}' has no variable with id '{id}'.");
}

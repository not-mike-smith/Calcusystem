namespace Calcusystem.Api.Reports;

/// <summary>Every choice a client can offer an author, by the name the wire uses for it.</summary>
/// <param name="ExpressionKinds">Every kind of expression.</param>
/// <param name="LeafExpressionKinds">
/// The kinds among <paramref name="ExpressionKinds"/> that have no operands. Every other kind is computed from
/// its operands.
/// </param>
/// <param name="RelationshipKinds">Every kind of relationship.</param>
/// <param name="SolvingRoles">What a relationship can do to the problem.</param>
/// <param name="AgreementRules">How strictly an equality can read equal.</param>
/// <param name="Landmarks">The landmarks a comparison rule can name.</param>
/// <param name="Comparisons">The comparisons a comparison rule can make.</param>
/// <param name="UncertaintyCorrelations">How a computed node's operands can correlate.</param>
/// <param name="Dimensions">The fundamental dimensions.</param>
public sealed record VocabularyReport(
    IReadOnlyList<string> ExpressionKinds,
    IReadOnlyList<string> LeafExpressionKinds,
    IReadOnlyList<string> RelationshipKinds,
    IReadOnlyList<string> SolvingRoles,
    IReadOnlyList<string> AgreementRules,
    IReadOnlyList<string> Landmarks,
    IReadOnlyList<string> Comparisons,
    IReadOnlyList<string> UncertaintyCorrelations,
    IReadOnlyList<DimensionReport> Dimensions);

/// <summary>A fundamental dimension, by the symbol an encoded dimensionality uses.</summary>
public sealed record DimensionReport(string Symbol, string Name);

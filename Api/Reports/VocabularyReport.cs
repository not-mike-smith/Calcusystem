namespace Calcusystem.Api.Reports;

/// <summary>Every choice a client can offer an author, by the name the wire uses for it.</summary>
public sealed record VocabularyReport(
    IReadOnlyList<string> ExpressionKinds,
    IReadOnlyList<string> RelationshipKinds,
    IReadOnlyList<string> SolvingRoles,
    IReadOnlyList<string> AgreementRules,
    IReadOnlyList<string> Landmarks,
    IReadOnlyList<string> Comparisons,
    IReadOnlyList<string> UncertaintyCorrelations,
    IReadOnlyList<DimensionReport> Dimensions);

/// <summary>A fundamental dimension, by the symbol an encoded dimensionality uses.</summary>
public sealed record DimensionReport(string Symbol, string Name);

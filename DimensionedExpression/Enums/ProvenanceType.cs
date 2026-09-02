using Calcusystem.DimensionedExpression.Snapshots;


namespace Calcusystem.DimensionedExpression.Enums;

/// <summary>Which provenance kind a <see cref="ProvenanceSnapshot"/> describes — the type it rebuilds into.</summary>
public enum ProvenanceType
{
    /// <summary>An instrument or sensor reading.</summary>
    Measured,

    /// <summary>A literature or tabulated value.</summary>
    Reference,

    /// <summary>An engineer-specified value.</summary>
    Design,

    /// <summary>An empirically fitted constant within a constitutive relationship.</summary>
    Model,
}

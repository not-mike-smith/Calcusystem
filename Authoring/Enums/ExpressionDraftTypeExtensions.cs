namespace Calcusystem.Authoring.Enums;

/// <summary>Questions about an <see cref="ExpressionDraftType"/> that more than one layer asks.</summary>
public static class ExpressionDraftTypeExtensions
{
    /// <summary>
    /// Whether a node of this kind has no operands: a variable, a constant, a reference value or a scalar.
    /// </summary>
    /// <remarks>A leaf draft ignores its <c>OperandIds</c>. Every other kind is computed from them.</remarks>
    public static bool IsLeaf(this ExpressionDraftType type) => type is
        ExpressionDraftType.Variable
        or ExpressionDraftType.Constant
        or ExpressionDraftType.ReferenceValue
        or ExpressionDraftType.Scalar;

    /// <summary>
    /// Whether a node of this kind is a quantity of the model: a variable, a constant or a reference value.
    /// </summary>
    /// <remarks>A quantity must have a name and a declared dimensionality, and may have a provenance.</remarks>
    public static bool IsQuantity(this ExpressionDraftType type) => type is
        ExpressionDraftType.Variable
        or ExpressionDraftType.Constant
        or ExpressionDraftType.ReferenceValue;

    /// <summary>Whether a node of this kind is a leaf that is not a quantity of the model: a scalar.</summary>
    /// <remarks>Its draft reads only <c>Value</c>. Its name, declared dimensionality and provenance are ignored.</remarks>
    public static bool ReadsOnlyItsValue(this ExpressionDraftType type) => type.IsLeaf() && ! type.IsQuantity();
}

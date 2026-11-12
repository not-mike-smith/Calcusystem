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
}

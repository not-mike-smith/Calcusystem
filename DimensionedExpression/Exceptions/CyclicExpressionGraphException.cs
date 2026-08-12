using Calcusystem.DimensionedExpression.Interfaces;

namespace Calcusystem.DimensionedExpression.Exceptions;

/// <summary>
/// Thrown when an expression graph contains a cycle — a node reachable from itself through its own operands.
/// </summary>
/// <remarks>
/// <para>
/// Every walk in this library assumes a DAG. A cycle is a malformed model rather than a missing value, so it is
/// raised rather than reported: left undetected it produces a calculation claiming nodes could not be resolved
/// while listing nothing as missing, which reads as "a value is absent" and sends the reader looking for one
/// that does not exist.
/// </para>
/// <para>
/// Ordinary construction cannot produce one — a node is given children that already exist — so reaching this
/// means a child collection was mutated after the fact to close a loop.
/// </para>
/// </remarks>
public class CyclicExpressionGraphException : InvalidOperationException
{
    internal CyclicExpressionGraphException(IExpression node, IExpression operand)
        : base($"Expression graph contains a cycle: '{node.Id}' depends on operand '{operand.Id}', which " +
               $"depends on '{node.Id}' in turn. An expression graph must be acyclic.")
    {
        NodeId = node.Id;
        OperandId = operand.Id;
    }

    /// <summary>Id of the node found to depend on itself.</summary>
    public string NodeId { get; }

    /// <summary>Id of the operand through which the cycle closes.</summary>
    public string OperandId { get; }
}

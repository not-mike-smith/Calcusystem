namespace Calcusystem.Core.Interfaces;

/// <summary>
/// Anything carrying a stable string identity that survives persistence.
/// </summary>
public interface IIdentified
{
    /// <summary>Stable identity, preserved across serialization.</summary>
    string Id { get; }
}

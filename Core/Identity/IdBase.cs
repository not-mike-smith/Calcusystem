using Calcusystem.Core.Interfaces;

namespace Calcusystem.Core.Identity;

/// <summary>
/// Base class for anything carrying a stable string identity that survives persistence.
/// </summary>
/// <remarks>
/// Equality and hashing are by <see cref="Id"/>, not by reference or by field values;
/// IdBase objects can safely be used as key to Dictionary
/// </remarks>
public abstract class IdBase : IIdentified
{
    /// <summary>
    /// Sentinel id meaning "mint a fresh identity for this object". Passing any other non-blank string preserves
    /// that id instead, which is what lets a persisted graph rebuild its references.
    /// </summary>
    public const string CREATE_NEW_ID = "CREATE_NEW";

    private readonly string _id = null!;

    /// <summary>Stable identity, preserved across serialization.</summary>
    /// <remarks>
    /// Passing sentinel value <see cref="CREATE_NEW_ID"/> mints a fresh GUID;
    /// any other non-blank string is adopted verbatim as the <see cref="Id"/>.
    /// A null or blank id causes an exception.
    /// </remarks>
    public string Id
    {
        get => _id;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Expression Id cannot be null or empty");
            }

            _id = value == CREATE_NEW_ID
                ? Guid.NewGuid().ToString("d")
                : value;
        }
    }

    protected IdBase(string id)
    {
        Id = id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is IdBase other && Id == other.Id;
    }
}

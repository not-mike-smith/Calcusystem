namespace Calcusystem.Core;

/// <summary>
/// Base class for anything carrying a stable string identity that survives persistence.
/// </summary>
/// <remarks>
/// Passing <see cref="Constants.CREATE_NEW_ID"/> mints a fresh GUID; passing any other non-blank string adopts
/// it verbatim, which is how a rebuilt graph restores the references between its nodes. A null or blank id
/// throws — an object with no identity cannot be referred to, so there is no useful default.
/// </remarks>
public abstract class IdBase : IIdentified
{
    private readonly string _id = null!;

    /// <summary>Stable identity, preserved across serialization.</summary>
    public string Id
    {
        get => _id;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Expression Id cannot be null or empty");
            }

            _id = value == Constants.CREATE_NEW_ID
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

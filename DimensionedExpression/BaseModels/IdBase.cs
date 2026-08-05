namespace DimensionedExpression.BaseModels;

public abstract class IdBase
{
    private readonly string _id = null!;
    public string Id
    {
        get => _id;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Expression Id cannot be null or empty");
            }

            _id = value == Constants.CREATE_NEW
                ? Guid.NewGuid().ToString("d")
                : value;
        }
    }

    /// <summary>Generates a fresh id, as if <see cref="Constants.CREATE_NEW"/> were passed.</summary>
    protected IdBase() : this(Constants.CREATE_NEW)
    {
    }

    protected IdBase(string id)
    {
        Id = id;
    }
}

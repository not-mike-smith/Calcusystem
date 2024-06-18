namespace DimensionedExpression.BaseModels;

public abstract class IdBase
{
    private readonly string _id = null!;
    public string Id // TODO make this required?
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

    protected IdBase(string id)
    {
        Id = id;
    }
}

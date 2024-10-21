using DimensionedExpression.Interfaces;

namespace Calcusystem.Serialization;

public class DeserializationContext
{
    private readonly Dictionary<string, IExpression> _expressionsById = new();

    public IReadOnlyDictionary<string, IExpression> ExpressionsById => _expressionsById;

    public void AddLoadedExpression(IExpression x)
    {
        _expressionsById.Add(x.Id, x);
    }
}

using Calcusystem.Measurement.Exceptions;
using Calcusystem.Core;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Interfaces;
using Calcusystem.Measurement.State;
using Calcusystem.Measurement;
using ExponentDict = System.Collections.Generic.IReadOnlyDictionary<Calcusystem.Measurement.FundamentalDimension, int>;

namespace Calcusystem.Measurement;

/// <summary>
/// The physical dimension of a quantity, represented as a map from each <see cref="FundamentalDimension"/>
/// to its integer exponent (e.g. velocity is Length¹·Time⁻¹). Exponents of zero are stripped, so any two
/// dimensionally-equal values compare equal regardless of how they were built. Supports dimensional algebra
/// via the <c>*</c> and <c>/</c> operators; integer roots (via <c>/ int</c>) require every exponent to divide
/// evenly, throwing <see cref="NondiscreteDimensionalityException"/> otherwise.
/// </summary>
/// <remarks>
/// A <c>readonly</c> value type: the <c>default</c> value (no backing dictionary) behaves as
/// <see cref="Dimensionless"/>. Instances are constructed through the static fundamental-dimension fields and
/// the algebra operators, not directly. Combine the fields to express derived dimensions, e.g.
/// <c>Mass * Length / (Time * Time)</c> for force.
/// </remarks>
public readonly struct Dimensionality : IStateful<Dimensionality, DimensionalityState>
{
    /// <summary>The empty dimension (all exponents zero) — a pure number such as a ratio or count.</summary>
    public static readonly Dimensionality Dimensionless = new Dimensionality(
        new Dictionary<FundamentalDimension, int>());

    /// <summary>Monetary value (a non-physical fundamental dimension supported for engineering-economics use).</summary>
    public static readonly Dimensionality Currency = new Dimensionality(FundamentalDimension.Currency);

    /// <summary>Amount of substance (mole).</summary>
    public static readonly Dimensionality AmountOfMatter = new Dimensionality(FundamentalDimension.AmountOfMatter);

    /// <summary>Mass (kilogram).</summary>
    public static readonly Dimensionality Mass = new Dimensionality(FundamentalDimension.Mass);

    /// <summary>Luminous intensity (candela).</summary>
    public static readonly Dimensionality LuminousIntensity = new Dimensionality(FundamentalDimension.LuminousIntensity);

    /// <summary>Electric current (ampere).</summary>
    public static readonly Dimensionality ElectricCurrent = new Dimensionality(FundamentalDimension.ElectricCurrent);

    /// <summary>Length (meter).</summary>
    public static readonly Dimensionality Length = new Dimensionality(FundamentalDimension.Length);

    /// <summary>Thermodynamic temperature (kelvin).</summary>
    public static readonly Dimensionality Temperature = new Dimensionality(FundamentalDimension.Temperature);

    /// <summary>Plane angle (radian) — treated as a fundamental dimension so torque stays distinct from energy.</summary>
    public static readonly Dimensionality Angle = new Dimensionality(FundamentalDimension.Angle);

    /// <summary>Time (second).</summary>
    public static readonly Dimensionality Time = new Dimensionality(FundamentalDimension.Time);

    private readonly ExponentDict? _fundamentalDimensions;
    private ExponentDict FundamentalDimensions => _fundamentalDimensions ?? new Dictionary<FundamentalDimension, int>();

    /// <summary>
    /// The smallest magnitude meaningful for this dimension — below it, a value is indistinguishable from zero
    /// on physical grounds alone. Composed from each fundamental dimension's quantum, so a velocity's floor
    /// falls out of length's and time's.
    /// </summary>
    /// <remarks>
    /// A last-resort scale, used only where the measurands supply none of their own. It is far coarser than any
    /// engineering tolerance — the Planck length is some twenty-five orders below a machinist's zero — so it
    /// catches the physically absurd rather than the practically negligible. Uncertainty answers the latter.
    /// </remarks>
    internal readonly double Epsilon;

    /// <summary>
    /// The largest magnitude meaningful for this dimension, composed the same way. The counterpart to
    /// <see cref="Epsilon"/>: past it a value is not large, it is wrong.
    /// </summary>
    internal readonly double MaxValue;

    private Dimensionality(ExponentDict fundamentalDimensions)
    {
        _fundamentalDimensions = Reduce(fundamentalDimensions);
        Epsilon = CalculateEpsilon();
        MaxValue = CalculateMaxValue();
    }

    private Dimensionality(FundamentalDimension fundamentalDimension)
    {
        _fundamentalDimensions = new Dictionary<FundamentalDimension, int>
        {
            {fundamentalDimension, 1}
        };

        Epsilon = CalculateEpsilon();
        MaxValue = CalculateMaxValue();
    }

    private Dimensionality(IEnumerable<KeyValuePair<FundamentalDimension, int>> pairs)
    {
        var dictionary = pairs.Aggregate(
            new Dictionary<FundamentalDimension, int>(),
            (dict, pair) =>
            {
                if (dict.ContainsKey(pair.Key))
                {
                    dict[pair.Key] += pair.Value;
                }
                else
                {
                    dict.Add(pair.Key, pair.Value);
                }

                return dict;
            });

        _fundamentalDimensions = Reduce(dictionary);

        Epsilon = CalculateEpsilon();
        MaxValue = CalculateMaxValue();
    }

    private double CalculateEpsilon()
    {
        if (! FundamentalDimensions.Any()) return double.Epsilon;

        return FundamentalDimensions.Aggregate(
            1d,
            (double x, KeyValuePair<FundamentalDimension, int> pair) =>
                pair.Value < 0
                    ? x / Math.Pow(pair.Key.MaxValue, -pair.Value)
                    : x * Math.Pow(pair.Key.QuantumValue, pair.Value));
    }

    private double CalculateMaxValue()
    {
        if (! FundamentalDimensions.Any()) return double.MaxValue;

        return FundamentalDimensions.Aggregate(
            1d,
            (double x, KeyValuePair<FundamentalDimension, int> pair) =>
                pair.Value < 0
                    ? x / Math.Pow(pair.Key.QuantumValue, -pair.Value)
                    : x * Math.Pow(pair.Key.MaxValue, pair.Value));
    }

    /// <inheritdoc/>
    /// <remarks>Ordered by <see cref="FundamentalDimension.Order"/>, so a consumer that writes the pairs out in
    /// iteration order gets a stable result for dimensionally-equal values without having to sort them itself.
    /// </remarks>
    public DimensionalityState GetState()
    {
        var me = this;
        return new DimensionalityState(OrderedKeys.ToDictionary(key => key, key => me[key]));
    }

    /// <inheritdoc/>
    public static Dimensionality FromState(DimensionalityState state) => new(state.Pairs);

    private static ExponentDict Reduce(ExponentDict fundamentalDimensions)
    {
        return fundamentalDimensions.Where(pair => pair.Value != 0).ToDictionary(
            pair => pair.Key,
            pair => pair.Value);
    }

    /// <summary>
    /// The integer exponent of the given <see cref="FundamentalDimension"/> in this dimension,
    /// or 0 if it is not present.
    /// </summary>
    public int this[FundamentalDimension fundamentalDimension] =>
        FundamentalDimensions.TryGetValue(fundamentalDimension, out var exponent)
            ? exponent
            : 0;

    /// <summary>
    /// Creates a <see cref="Quantity"/> of this dimensionality directly from a raw KMS value. Unlike
    /// <see cref="UnitOfMeasure.Quantity"/>, no unit conversion is applied — the value is taken as already
    /// KMS-normalized.
    /// </summary>
    public Quantity Quantity(double kmsValue) => new Quantity(kmsValue, this);

    private IEnumerable<FundamentalDimension> OrderedKeys => FundamentalDimensions.Keys
        .OrderBy(f => FundamentalDimension.Order[f]);

    /// <summary>Order-independent hash over the (dimension, exponent) pairs, consistent with <see cref="Equals"/>.</summary>
    public override int GetHashCode()
    {
        var i = 23;
        unchecked
        {
            foreach (var fundamentalDimension in OrderedKeys)
            {
                var x = 449 * fundamentalDimension.GetHashCode();
                x += 2467 * FundamentalDimensions[fundamentalDimension].GetHashCode();
                // x is a combination of the FundamentalDimension and exponent
                i = i * x + 17;
            }
        }

        return i;
    }

    /// <summary>
    /// Value equality: two dimensions are equal when they carry the same exponent for every
    /// fundamental dimension (zero-exponent entries having been stripped).
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (! (obj is Dimensionality other)) return false;

        var me = this;
        return FundamentalDimensions.Count == other.FundamentalDimensions.Count &&
               FundamentalDimensions.Keys.All(f => me.FundamentalDimensions[f] == other[f]);
    }

    private string ElementToString(FundamentalDimension f)
    {
        var exponent = Math.Abs(FundamentalDimensions[f]);
        return exponent == 1
            ? f.Symbol
            : $"{f.Symbol}{exponent.ToSuperscript()}";
    }

    /// <summary>
    /// Human-readable form using symbols and superscript exponents, split into a <c>numerator/denominator</c>
    /// around negative exponents (e.g. <c>M·L²/T²</c>). Returns <c>"1"</c> for a dimensionless value.
    /// </summary>
    public override string ToString()
    {
        var me = this;
        var numerators = FundamentalDimensions
            .Where(pair => pair.Value > 0)
            .OrderBy(pair => FundamentalDimension.Order[pair.Key])
            .Select(pair => me.ElementToString(pair.Key));

        var numerator = string.Join('·', numerators);
        numerator = numerator.Length > 0 ? numerator : "1";
        var denominators = FundamentalDimensions
            .Where(pair => pair.Value < 0)
            .OrderBy(pair => FundamentalDimension.Order[pair.Key])
            .Select(pair => me.ElementToString(pair.Key));

        var denominator = string.Join('·', denominators);
        if (denominator.Length == 0) return numerator;

        return $"{numerator}/{denominator}";
    }

    /// <summary>Value equality; see <see cref="Equals"/>.</summary>
    public static bool operator==(Dimensionality lhs, Dimensionality rhs)
    {
        return lhs.Equals(rhs);
    }

    /// <summary>Negation of the equality operator.</summary>
    public static bool operator !=(Dimensionality lhs, Dimensionality rhs)
    {
        return ! (lhs == rhs);
    }

    /// <summary>Multiplies two dimensions by adding their exponents (e.g. Length · Length = Length²).</summary>
    public static Dimensionality operator *(Dimensionality lhs, Dimensionality rhs)
    {
        var pairs = lhs.FundamentalDimensions.ToList();
        pairs.AddRange(rhs.FundamentalDimensions.ToList());
        return new Dimensionality(pairs);
    }

    /// <summary>The multiplicative inverse — every exponent negated (e.g. Time → Time⁻¹).</summary>
    public Dimensionality Reciprocal()
    {
        return new Dimensionality(FundamentalDimensions
            .Select(pair => new KeyValuePair<FundamentalDimension, int>(pair.Key, -pair.Value)));
    }

    /// <summary>Divides two dimensions by subtracting the divisor's exponents from the dividend's.</summary>
    public static Dimensionality operator /(Dimensionality lhs, Dimensionality rhs)
    {
        var pairs = rhs.FundamentalDimensions
            .Select(pair => new KeyValuePair<FundamentalDimension, int>(pair.Key, -pair.Value))
            .ToList();

        pairs.AddRange(lhs.FundamentalDimensions.ToList());
        return new Dimensionality(pairs);
    }

    /// <summary>
    /// Raises the dimension to an integer power by scaling every exponent (e.g. Length <c>* 3</c> = Length³).
    /// Used to express repeated products such as area and volume.
    /// </summary>
    public static Dimensionality operator *(Dimensionality dimensionality, int exponent)
    {
        var dict = dimensionality.FundamentalDimensions.ToDictionary(
            pair => pair.Key,
            pair => pair.Value * exponent);

        return new Dimensionality(dict);
    }

    /// <summary>
    /// Takes the integer <paramref name="root"/> of the dimension by dividing every exponent.
    /// </summary>
    /// <exception cref="DivideByZeroException">The root is 0.</exception>
    /// <exception cref="NondiscreteDimensionalityException">
    /// The root is negative, or an exponent does not divide evenly by it (which would yield a non-integer exponent).
    /// </exception>
    public static Dimensionality operator /(Dimensionality dimensionality, int root)
    {
        if (root == 0) throw new DivideByZeroException("Cannot take 0th root of dimension");
        if (root < 0) throw new NondiscreteDimensionalityException("Cannot take negative root of dimension");
        var dict = dimensionality.FundamentalDimensions.ToDictionary(
            pair => pair.Key,
            pair => pair.Value);

        foreach (var key in dimensionality.FundamentalDimensions.Keys)
        {
            if (dict[key] % root != 0)
            {
                throw new NondiscreteDimensionalityException($"Cannot take {root} root of {dimensionality}");
            }

            dict[key] /= root;
        }

        return new Dimensionality(dict);
    }
}

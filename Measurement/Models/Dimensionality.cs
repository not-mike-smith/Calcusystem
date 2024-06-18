using System;
using System.Collections.Generic;
using System.Linq;
using Measurement.Exceptions;
using Measurement.Extensions;
using ExponentDict = System.Collections.Generic.IReadOnlyDictionary<Measurement.Models.FundamentalDimension, int>;

namespace Measurement.Models;

public readonly struct Dimensionality
{
    public static readonly Dimensionality Dimensionless = new Dimensionality(
        new Dictionary<FundamentalDimension, int>());

    public static readonly Dimensionality Currency = new Dimensionality(FundamentalDimension.Currency);
    public static readonly Dimensionality AmountOfMatter = new Dimensionality(FundamentalDimension.AmountOfMatter);
    public static readonly Dimensionality Mass = new Dimensionality(FundamentalDimension.Mass);
    public static readonly Dimensionality LuminousIntensity = new Dimensionality(FundamentalDimension.LuminousIntensity);
    public static readonly Dimensionality ElectricCurrent = new Dimensionality(FundamentalDimension.ElectricCurrent);
    public static readonly Dimensionality Length = new Dimensionality(FundamentalDimension.Length);
    public static readonly Dimensionality Temperature = new Dimensionality(FundamentalDimension.Temperature);
    public static readonly Dimensionality Angle = new Dimensionality(FundamentalDimension.Angle);
    public static readonly Dimensionality Time = new Dimensionality(FundamentalDimension.Time);

    private readonly ExponentDict _fundamentalDimensions;
    private ExponentDict FundamentalDimensions => _fundamentalDimensions ?? new Dictionary<FundamentalDimension, int>();

    private Dimensionality(ExponentDict fundamentalDimensions)
    {
        _fundamentalDimensions = Reduce(fundamentalDimensions);
    }

    private Dimensionality(FundamentalDimension fundamentalDimension)
    {
        _fundamentalDimensions = new Dictionary<FundamentalDimension, int>
        {
            {fundamentalDimension, 1}
        };
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
    }

    private static ExponentDict Reduce(ExponentDict fundamentalDimensions)
    {
        return fundamentalDimensions.Where(pair => pair.Value != 0).ToDictionary(
            pair => pair.Key,
            pair => pair.Value);
    }

    public int this[FundamentalDimension fundamentalDimension] =>
        FundamentalDimensions.TryGetValue(fundamentalDimension, out var exponent)
            ? exponent
            : 0;

    private IEnumerable<FundamentalDimension> OrderedKeys => FundamentalDimensions.Keys
        .OrderBy(f => FundamentalDimension.Order[f]);

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

    public override bool Equals(object? obj)
    {
        if (!(obj is Dimensionality other)) return false;

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

    public static bool operator==(Dimensionality lhs, Dimensionality rhs)
    {
        return lhs.Equals(rhs);
    }

    public static bool operator !=(Dimensionality lhs, Dimensionality rhs)
    {
        return !(lhs == rhs);
    }

    public static Dimensionality operator *(Dimensionality lhs, Dimensionality rhs)
    {
        var pairs = lhs.FundamentalDimensions.ToList();
        pairs.AddRange(rhs.FundamentalDimensions.ToList());
        return new Dimensionality(pairs);
    }

    public Dimensionality Reciprocal()
    {
        return new Dimensionality(FundamentalDimensions
            .Select(pair => new KeyValuePair<FundamentalDimension, int>(pair.Key, -pair.Value)));
    }

    public static Dimensionality operator /(Dimensionality lhs, Dimensionality rhs)
    {
        var pairs = rhs.FundamentalDimensions
            .Select(pair => new KeyValuePair<FundamentalDimension, int>(pair.Key, -pair.Value))
            .ToList();

        pairs.AddRange(lhs.FundamentalDimensions.ToList());
        return new Dimensionality(pairs);
    }

    public static Dimensionality operator *(Dimensionality dimensionality, int exponent)
    {
        var dict = dimensionality.FundamentalDimensions.ToDictionary(
            pair => pair.Key,
            pair => pair.Value * exponent);

        return new Dimensionality(dict);
    }

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

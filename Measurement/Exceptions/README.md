# Calcusystem.Measurement.Exceptions

What measurement arithmetic throws when an operation is not meaningful.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `IncompatibleDimensionsException` | class | Adding, subtracting, or converting across different dimensionalities. |
| `NondiscreteDimensionalityException` | class | A root that would leave a fractional exponent. |

Both derive from `InvalidOperationException`: the operands are individually valid, and it
is the combination that is not.

## Start here

```csharp
mass.Plus(length);                       // IncompatibleDimensionsException
mass.In(Length.Meter);                   // IncompatibleDimensionsException
(Dimensionality.Length * 3) / 2;         // NondiscreteDimensionalityException — L³ has no square root
```

Where a caller would rather branch than catch, the dimension-tolerant forms return a `NaN`
value instead of throwing: `Quantity.TryAdd`, `TrySubtract`, and `Measurand.TryIn`.

## Guarantees

- **Adding a mass to a length always throws.** There is no configuration that turns this into a
  warning, and no operator overload that silently coerces.
- **Multiplication and division never throw on dimensions**, because every combination of
  exponents is meaningful. Only addition, subtraction, and conversion constrain them.
- **Integer roots only.** `Dimensionality` holds integer exponents, so a root that would leave a
  fraction is refused rather than rounded.

## Surprises

- **`Try*` returns a `NaN`-valued result, not null.** The result is still a `Quantity` or
  `Measurand` and still carries a dimensionality, so a caller that forgets to check propagates a
  `NaN` rather than a null reference.
- **Expression construction throws these too.** The unary expressions check dimensionlessness in
  their constructors and raise `IncompatibleDimensionsException` from a layer above.

## What does not belong here

- Rebuild failures → `Calcusystem.Serialization.Exceptions`
- Cycles in an expression graph → `Calcusystem.DimensionedExpression.Exceptions`

## Related

`Primitives/` (`Quantity`, `Measurand`, `Dimensionality` — all three throw from here).

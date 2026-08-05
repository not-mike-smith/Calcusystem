# Calcusystem

An engineering calculation platform where physical quantities carry their **units, dimensions, and measurement uncertainty** as first-class concerns. You describe a system of variables, formulas, and tolerances; the platform tracks dimensions, propagates uncertainty, and reports which constraints hold — with evaluation and solving on the roadmap.

The goal is calculations that can't silently go wrong: adding a length to a mass throws, converting units happens only at the boundary, and every derived value carries the uncertainty of the measurements it came from.

---

## How to read this codebase

The code is organized so you rarely need to read implementation. **By default, each assembly's `README.md` plus the interfaces in its `Interfaces/` directory contain everything you need to _use_ that assembly. You only need its implementation files to _modify_ it.**

So, depending on your task:

- **Using an assembly** (calling it from another project, or from your own code): read its `README.md`, then the interfaces in `Interfaces/`. The interfaces carry XML docstrings describing each member's contract.
- **Modifying an assembly**: additionally read the implementation files for the types you're changing.

A few assemblies note exceptions at the top of their README — types outside `Interfaces/` that also carry essential contract docstrings (for example, `Measurement` calls out its `Quantity` and `Dimensionality` structs and the `FundamentalDimension` class).

---

## Project structure

Three library assemblies stacked bottom-up, each with a matching test project:

| Assembly | Depends on | What it does |
| --- | --- | --- |
| [`Measurement`](Measurement/README.md) | — | Physical quantities with KMS-normalized units, dimensional algebra, a unified `Measurand` value type, and uncertainty propagation. The foundation. |
| [`DimensionedExpression`](DimensionedExpression/README.md) | `Measurement` | Trees of dimensioned variables and formulas (`IExpression`), binary operators for equality/tolerance/ordering constraints, and the `ExpressionSystem` container. |
| [`Calcusystem.Serialization`](Calcusystem.Serialization/README.md) | `DimensionedExpression` | Maps an `ExpressionSystem` to/from flat, id-referenced DTOs for persistence (object mapping, not byte encoding). |

`Measurement.Test`, `DimensionedExpression.Test`, and `Calcusystem.Serialization.Test` hold the xUnit suites for each layer.

---

## Quick start

Compute with units and uncertainty (the `Measurement` layer):

```csharp
using Measurement;
using Measurement.Units;

// 2 kg ± 1% — supply and read values in whatever unit you like; storage is always KMS
var mass = Mass.Kilogram.Quantity(2).Measurand(SymmetricUncertainty.FromRelErr(0.01));
mass.In(Mass.Pound);   // ≈ 4.409 lb
mass.RelativeError;    // 0.01

// arithmetic enforces dimensions and propagates uncertainty
var accel = new Quantity(9.81, Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time))
    .Measurand(SymmetricUncertainty.FromRelErr(0.005));

var force = mass.Times(accel);   // dimension M·L·t⁻²; uncertainty combines in quadrature
```

Assemble a reusable formula whose leaves get filled in later (the `DimensionedExpression` layer):

```csharp
using DimensionedExpression.Expressions;
using Measurement;

var m = new Variable("m", Dimensionality.Mass);
var a = new Variable("a", Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time));

var f = new ProductExpression();
f.AddFactor(m);
f.AddFactor(a);

f.Dimensionality;      // M·L·t⁻²  — known before any value is supplied
f.DegreesOfFreedom();  // 2  — two unbound leaves
f.IsFullyDescribed;    // false; f.Value is null until both leaves are set
```

See each assembly's README for the full surface.

---

## Build and test

Built on .NET 10.

```bash
dotnet build          # build the whole solution
dotnet test           # run all test suites
```

---

## Status and roadmap

The measurement, expression, and serialization layers are functional; evaluation and solving are the next milestones. The full milestone plan, design decisions, and open questions live in [`project-plan.md`](project-plan.md).

---

## Contributing conventions

- **Every assembly has a `README.md`** at its root, covering purpose, key types, invariants, dependencies, and explicit scope boundaries (what does _not_ belong there).
- **Public interfaces carry XML docstrings** on the interface and each member, articulating the contract — this is what lets a reader use a layer without opening its implementation.
- **Tests live in the matching `*.Test` project** and reference only the layers they cover.
- Prefer small, focused commits; keep behavior changes and documentation legible in the diff.

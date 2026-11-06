# Calcusystem.Examples

Three engineering problems, each built as an `ExpressionSystem` in C# and printed with what it computed,
what it could not, and what its checks did. This is the assembly to read to see what using this platform
actually looks like.

```bash
dotnet run --project Examples
```

Depends on `Analysis`, `DimensionedExpression`, and `Measurement` — the whole stack below the HTTP surface,
which is the point: a worked problem touches units, uncertainty, the expression graph, and the analysis of a
system all at once.

---

## The problems

| | Discipline | Shows |
| --- | --- | --- |
| [`OrificeDrainTime`](Problems/OrificeDrainTime.cs) | chemical | `√`, squaring a measurement, two coherence checks — one an identity, one a reconciliation against a flow meter |
| [`ParallelDcLoads`](Problems/ParallelDcLoads.cs) | electrical | reciprocals and n-ary sums, Kirchhoff's current law as a coherence check, and a **requirement the design violates** |
| [`GyroscopicPrecession`](Problems/GyroscopicPrecession.cs) | mechanical | angle as a dimension, and a requirement that checks the model's own assumption rather than the design |

Each is a `WorkedExample`: a system, plus the labels and units to print it in. `Catalogue.Build()` is all of
them.

---

## Why every example computes forward

**There is no solver yet.** `Analysis` can say whether a system is well posed and which parts of it could be
solved, and it can calculate everything the supplied values determine — but nothing inverts an equation
toward an unknown. So each problem is posed the way an engineer poses one for a spreadsheet: every input is
supplied, the answer is computed by walking the graph, and the relationships in the model are **checks on the
result rather than the means of reaching it.**

That shaped two of the three problems:

- **The draining tank** is a differential equation in its usual form. For a prismatic tank it integrates in
  closed form, and the closed form is what is modeled. Nothing here integrates anything.
- **The DC circuit** is resistive because an LRC circuit needs either a differential equation or complex
  impedance, and a `Measurand` is a real number with an uncertainty on it.

A statics problem with unknown reactions would have needed the solver too, which is why the mechanical
example computes a precession rate rather than balancing a free body.

---

## What a run looks like

```text
COMPUTED
  lighting current I₁     2.000 A ± 1.1 %
  instrument current I₂   2.927 A ± 1.1 %
  heater current I₃       0.5106 A ± 5.0 %
  supply current I_total  5.437 A ± 0.95 %
  branch sum ΣI           5.437 A ± 0.87 %
  total power             130.5 W ± 1.3 %

CHECKS
  ✓ ΣI_branch ·==· I_total  5.437 A ·==· 5.437 A  coherence
  ✗ supply breaker holds    5.437 A ⌜<⌟ 5.000 A   requirement
  ✓ instrument fuse holds   2.927 A ⌜<⌟ 3.000 A   requirement
```

The breaker check fails on purpose. A report where everything passes cannot show you the difference between
a check that held, a check that failed, and a check that never ran.

---

## Three things the examples exist to show

### Correlation belongs to a node, so a repeated input has to be gathered into one

`UncertaintyCorrelation` is a property of a computed node and applies to **all** of its operands. A product
listing one measurement twice beside a second, independent one therefore cannot be written flat: neither
setting describes both pairs.

Gathering the repeat into a node of its own gives each node one rule to state. Rotor inertia, `I = ½·m·r²`:

```csharp
var radiusSquared = new ProductExpression([rotorRadius, rotorRadius])
{
    Id = "r²",
    UncertaintyCorrelation = UncertaintyCorrelation.Correlated,   // one measurement, used twice
};

var rotorInertia = new ProductExpression([half, rotorMass, radiusSquared]);   // independent, so quadrature
```

Written as one flat product, quadrature throughout reports 0.77 % and summing throughout reports 1.33 %. The
answer is 0.97 %, and it is neither.

The same rule decides how power is written in the electrical example. `P = V·I_total` and `P = V²·G` are the
same number; only the second states the uncertainty correctly, because `I_total` already contains the bus
voltage and multiplying by it again presents one measurement as two.

**This is the current shape, not the settled one.** Gathering the repeat into its own node works, and it means
the shape of the tree carries a fact about the inputs — and `P = V·I_total` gives a wrong bar with nothing to
warn the author. Making correlation a property of any *two* expressions, defaulted from a correlation graph
and overridable, is in the hopper: see *Expression correlation as a pairwise property* in
[`project-plan.md`](../project-plan.md).

### Uncertainty is not invariant under algebraic rearrangement

The draining tank reaches its mean discharge two ways. Both reduce to `C_d·A₀·√(gh₀/2)`, and both report the
same number. They report **different uncertainties**, because the longer route passes the
orifice area and the discharge coefficient through twice and nothing can tell the propagator those are one
measurement each.

This is why a coherence check reads its two sides *nominally*. The reported values agree exactly; the bars
around them do not, and demanding that they agree would fail a model that is correct.

### Angle is a dimension here, so a relation that consumes a radian has to say so

`Torque` is `M·L²·A·T⁻²` and angular momentum is `M·L²·A·T⁻¹`, so `τ / (I·ω)` cancels both angles and lands on
`T⁻¹` — a frequency, which cannot be read in rpm or degrees per second. A precession rate is an angular
velocity, so the gyroscope model multiplies by one radian explicitly:

```csharp
Numerator = new ProductExpression([appliedTorque, radian]) { Id = "τ·rad" },
```

It changes no number — `Angle.Radian` converts to KMS by one — and it changes which units the answer can be
expressed in. SI hides this by calling the radian dimensionless; promoting angle to a dimension makes the
bookkeeping explicit, and explicit is the trade this platform takes everywhere else too.

---

## Withholding a value

The runner finishes by calculating the tank model again with the discharge coefficient taken away. Nothing
throws. A half-built model is the normal case, and the report changes in four ways at once:

- the areas and the jet velocity still resolve — only what sits **above** the gap stops;
- every check goes to `?`, which is *outstanding*, not passing and not failing;
- `OUTSTANDING` names the missing value and counts what it blocked;
- degrees of freedom go to **−1**, and `Decompose` names the coefficient as the over-determined one.

That last one is the interesting one. Two coherence checks reach the coefficient once it is unknown, so the
model holds more information about it than determining it would need — which is exactly the surplus that
would let it be backed out of the metered flow. A degrees-of-freedom count alone reports the system as
over-determined and stops; the decomposition says which quantity the surplus is about.

---

## What writing these found

**A watt was declared as a joule times a second rather than per second.** Every unit in `Power` inherited the
dimensionality of energy × time, and four other lists built on a watt inherited it too — so
`Energy.KilowattHour` would not convert to `Energy.Joule`, from the same list.

The error is invisible from inside its own list, because every unit in `Power` was wrong in the same way.
What catches it is asking whether a list's units all measure the same thing, which is now
`ListCompletenessTests.EveryUnitOfOneKindMeasuresOneDimensionality`, and asking whether every readout here
converts into the unit it names, which is `ExampleReportTests.EveryReadoutConvertsIntoTheUnitItNames`. Both
fail if the exponent goes back.

---

## Layout

| Folder | Holds |
| --- | --- |
| [`Problems/`](Problems/README.md) | the three models, and the shared leaf builders |
| [`Presentation/`](Presentation/README.md) | what a worked example is, and how one is printed |

`Program.cs` is at the assembly root, where top-level statements have no namespace to be a folder of. It is
the same exception `Api/Program.cs` is.

---

## Scope boundaries

**What belongs here:** engineering problems worth reading, and the smallest amount of code needed to print
one.

**What does NOT belong here:**

- Anything another assembly could use. Nothing depends on this one, and nothing should.
- A formatting rule the editor already owns. Significant figures are chosen from the uncertainty in
  `web/src/format/significance.ts`; a console report fixes the figure count instead rather than keeping a
  second implementation of that rule in agreement with the first.
- Benchmarks, and problems whose interest is the numerics rather than the modeling.

# Calcusystem.Core.Either

What an operation that might not succeed returns, so that failing needs no exception and succeeding needs
no null.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `Either<TValue>` | abstract record | Either a value or a `Failure`. Carries `Match`, `Map`, `Then`. |
| `Succeeded<TValue>` | sealed record | The case that has a `Value`. |
| `Failed<TValue>` | sealed record | The case that has a `Failure`. |
| `Failure` | sealed record | A `FailureReason` and a message. Not generic, so it can travel. |

## Start here

```csharp
using Calcusystem.Core.Either;
using Calcusystem.Core.Enums;

// Both conversions are implicit, so returning either reads like returning the value.
static Either<int> Parse(string text) =>
    int.TryParse(text, out var n) ? n : new Failure(FailureReason.Invalid, $"'{text}' is not a number");

static Either<int> Halve(int n) =>
    n % 2 == 0 ? n / 2 : new Failure(FailureReason.Unprocessable, $"{n} is odd");

var message = Parse("8")
    .Then(Halve)                       // chains an operation that can itself fail
    .Map(halved => halved + 1)         // transforms a value; a failure passes through
    .Match(v => $"got {v}", f => $"failed: {f}");
```

## Guarantees

- **A failure never produces a value and never throws.** The value lives on `Succeeded<TValue>` and the
  failure on `Failed<TValue>`, so a caller holding `Either<TValue>` cannot read either without first
  establishing which case it has.
- **`Match` is total.** It is abstract, so every case must answer it.
- **`Then` short-circuits.** The first failure skips every later step, and nothing in between tests for one.
- **A failure propagates without being rebuilt.** `Failure` is not generic, so relaying one upward through a
  caller of a different value type hands on the same instance — `AsType<TOther>()`, or the implicit
  conversion. There is a test that the instance is the same one.
- **Cases compare by value** and never across cases, as records.
- **No `FailureReason` is zero**, so one deserialized without a reason is detectably absent.

## Surprises

- **There is deliberately no `Value`, `IsSuccess` or `IsFailure` on `Either<TValue>`.** Any of them would be
  answerable on a failure, and a caller would then reach the value by casting — the runtime exception this
  type exists to replace. A test asserts the base type exposes none of them.
- **The hierarchy is closed by convention, not by the compiler.** The parameterless constructor is
  `private protected`, which blocks the ordinary way of adding a case from outside. But a record's copy
  constructor cannot be narrower than `protected` while the record is unsealed (CS8878), and an outside type
  can reach it with an explicit `base(borrowed)` call. **This is why `Match` is abstract rather than a
  `switch` written over the two known cases** — `Match` stays total whatever exists, and a type switch would
  not.
- **`Failure` carries one message, not a list of findings.** Rich domain output — every dimensional problem in
  a draft, every violated requirement in a calculation — stays its own type, because a caller wants to read
  that rather than branch on it. See *What does not belong here*.
- **`FailureReason` names no transport.** Mapping one to an HTTP status, an exit code or a dialog belongs to
  the outermost layer; nothing here chooses between `400` and `422`.
- **`Map` on a transform returning null** gives a `Succeeded` wrapping null rather than a failure. `Either`
  says whether an operation worked, not whether its answer was non-null.

## What does not belong here

- `FailureReason`, the enum → `Enums/`
- Structured domain findings → the layer that owns them. `Calcusystem.Authoring`'s `DraftProblem` is the
  pattern: a list of problems each naming a node is something to render, where a `Failure` is something to
  branch on.
- Transport mappings — status codes, exit codes, problem-detail documents → the outermost layer.

## Related

`Enums/` (`FailureReason`) · `Identity/` · `Interfaces/`.

See the [assembly README](../README.md) for why this lives in the basement despite having behavior of its own.

# Calcusystem.Core.Either

What an operation that might not succeed returns, so that failing needs no exception and succeeding needs
no null.

## What's here

| Type | Kind | Role |
| --- | --- | --- |
| `Either<TValue>` | abstract record | Either a value or a `Failure`. Carries `DidFail`, `Match`, `Map`, `Then`, `TryGetFailure`, `TryGetValue`. |
| `Succeeded<TValue>` | sealed record | The case that has a `Value`. |
| `Failed<TValue>` | sealed record | The case that has a `Failure`. |
| `Failure` | record | A `FailureReason` and a message. Not generic, so it can travel. |
| `Failure<TDetail>` | sealed record | A failure with structured findings attached. |

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

## Giving up early, without writing in a functional style

```csharp
// relay a dependency's failure; the implicit conversion retypes it
var attempt = _dependency.DoSomething(parameters);
if (attempt.TryGetFailure(out var failure)) return failure;

// relay, then use the value — no cast on either path
if (! attempt.TryGetValue(out var value, out var failure)) return failure;
Use(value);
```

`DidFail` answers the same question where the failure itself is not wanted — logging a count, say. Matching
`Failed<T>` directly does the same job with nothing added to the type.

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

- **There is deliberately no `Value` on `Either<TValue>`.** It would be answerable on a failure, and a caller
  would then reach it by casting — the runtime exception this type exists to replace. A test asserts the base
  type exposes no such member. `DidFail` is not the same hazard: a boolean cannot be used to reach a value it
  does not carry. Asking whether something failed is free; *answering with a value that is not there* is what
  had to go.
- **Nothing hands back a retyped `Either`.** `Failure.AsType<T>()` exists, but there is no
  `Either<T>.AsType<TOther>()` — on a success it would have nothing to answer with but an exception. To relay
  a dependency's failure, take the `Failure` out first (`TryGetFailure`, or matching `Failed<T>`) and return
  that; the implicit conversion retypes it.
- **The hierarchy is closed by convention, not by the compiler.** The parameterless constructor is
  `private protected`, which blocks the ordinary way of adding a case from outside. But a record's copy
  constructor cannot be narrower than `protected` while the record is unsealed (CS8878), and an outside type
  can reach it with an explicit `base(borrowed)` call. **This is why `Match` is abstract rather than a
  `switch` written over the two known cases** — `Match` stays total whatever exists, and a type switch would
  not.
- **Detail is a subtype, not a second type parameter.** `Failure<TDetail>` carries structured findings — the
  list of problems behind a failed build, say — and everything that takes a `Failure` takes one of these, so a
  caller that only relays or reports a failure needs to know nothing about them. Recover them with a type test:
  `if (failure is Failure<IReadOnlyList<DraftProblem>> d) Render(d.Detail);`
- **The cost of that shape: a signature does not advertise what its failures carry.** `Either<T>` says only
  that it can fail. Where the detail matters, the member's own documentation has to say so — a second type
  parameter would have said it in the type, at the price of two arguments everywhere and failures that no
  longer chain across vocabularies.
- **`FailureReason` names no transport.** Mapping one to an HTTP status, an exit code or a dialog belongs to
  the outermost layer; nothing here chooses between `400` and `422`.
- **`Map` on a transform returning null** gives a `Succeeded` wrapping null rather than a failure. `Either`
  says whether an operation worked, not whether its answer was non-null.

## What does not belong here

- `FailureReason`, the enum → `Enums/`
- The findings' own types → the layer that owns them. `Calcusystem.Authoring` defines `DraftProblem`; this
  assembly only carries a list of them as opaque detail, and never knows what one is.
- Transport mappings — status codes, exit codes, problem-detail documents → the outermost layer.

## Related

`Enums/` (`FailureReason`) · `Identity/` · `Interfaces/`.

See the [assembly README](../README.md) for why this lives in the basement despite having behavior of its own.

# Calcusystem.Core.Enums

The closed sets the basement names.

## What's here

| Type | Role |
| --- | --- |
| `FailureReason` | Why an operation did not succeed, at a level a caller can branch on. |

## Guarantees

- **No member is zero**, so a reason deserialized without one is detectably absent rather than silently
  `NotFound`. Same rule as `SolvingRole` and `AgreementRule`.

## Surprises

- **It names no transport.** There is no HTTP status here, no exit code, no dialog severity. Nothing in the
  domain should have to choose between `400` and `422`, and a console or a test has no use for either — so the
  mapping lives in the outermost layer, which is the only place that knows what it is mapping *to*.
- **`Unprocessable` and `Invalid` are different.** `Invalid` means the request itself does not make sense — a
  malformed id, a missing argument. `Unprocessable` means the request is fine but the subject will not support
  it, such as a model that does not yet build.

## What does not belong here

- The types that carry a reason → `Either/`
- Reconstruction discriminators, which belong with the types they rebuild → each assembly's own `Enums/`

## Related

`Either/` (`Failure`, which carries one).

See the [assembly README](../README.md) for what makes something basement vocabulary.

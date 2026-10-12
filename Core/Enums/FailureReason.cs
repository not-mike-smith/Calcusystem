namespace Calcusystem.Core.Enums;

/// <summary>
/// Why an operation did not succeed, at a level a caller can branch on.
/// </summary>
/// <remarks>
/// Deliberately a small, transport-free set. Mapping one of these to an HTTP status, an exit code, or a dialog
/// is the outermost layer's business — nothing here should have to decide between <c>400</c> and <c>422</c>, and
/// a console or a test has no use for either.
/// </remarks>
public enum FailureReason
{
    /// <summary>Nothing was found under the identity given.</summary>
    NotFound = 1,

    /// <summary>The request itself does not make sense — a malformed id, a missing argument.</summary>
    Invalid,

    /// <summary>
    /// The request makes sense but the subject will not support it — a model that does not yet build, a system
    /// too underdetermined to solve.
    /// </summary>
    Unprocessable,

    /// <summary>The subject has changed underneath, or the operation contradicts its current state.</summary>
    Conflict,

    /// <summary>The operation is not implemented for this subject.</summary>
    Unsupported,

    /// <summary>Something failed that the caller is not expected to be able to correct.</summary>
    Unexpected,
}

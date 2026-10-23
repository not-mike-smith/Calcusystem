using Calcusystem.Authoring.Drafts;
using Calcusystem.Core.Either;
using Calcusystem.Core.Enums;
using Calcusystem.Serialization.Mappers;
using Dtos = Calcusystem.Serialization.Dtos;

namespace Calcusystem.Api.Http;

/// <summary>Reads a posted draft, answering with a failure rather than throwing.</summary>
public static class DraftIntake
{
    /// <summary>
    /// The draft <paramref name="wire"/> describes, or <c>Invalid</c> if it describes none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A draft is freely invalid, so almost nothing is refused here — a modelling mistake belongs in the
    /// problems a build reports, not in a status code. What is refused is a payload that cannot be held as a
    /// draft at all; <c>DraftMapper.FromWire</c> lists those.
    /// </para>
    /// <para>
    /// The exceptions caught are the ones that mapper raises for a malformed payload. They are caught around
    /// that one call, so nothing else's failure can arrive dressed as a bad request.
    /// </para>
    /// </remarks>
    public static Either<SystemDraft> Read(Dtos.SystemDraft? wire)
    {
        if (wire is null) return new Failure(FailureReason.Invalid, "No draft was posted.");

        try
        {
            return DraftMapper.FromWire(wire);
        }
        catch (Exception e) when (e is FormatException or InvalidOperationException or NotImplementedException
                                      or ArgumentException)
        {
            return new Failure(FailureReason.Invalid, e.Message);
        }
    }
}

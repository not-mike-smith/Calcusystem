using Calcusystem.Authoring.Drafts;
using Calcusystem.Core.Either;
using Calcusystem.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Calcusystem.Api.Http;

/// <summary>
/// Turns a <see cref="Failure"/> into a response. The only place in Calcusystem that speaks HTTP about one.
/// </summary>
public static class FailureTranslation
{
    /// <summary>The status code a <paramref name="reason"/> is reported as.</summary>
    /// <remarks>
    /// Total over <see cref="FailureReason"/>, and a member with no case here is a build error rather than a
    /// silent 500.
    /// </remarks>
    public static int StatusFor(FailureReason reason) => reason switch
    {
        FailureReason.NotFound => StatusCodes.Status404NotFound,
        FailureReason.Invalid => StatusCodes.Status400BadRequest,
        FailureReason.Unprocessable => StatusCodes.Status422UnprocessableEntity,
        FailureReason.Conflict => StatusCodes.Status409Conflict,
        FailureReason.Unsupported => StatusCodes.Status501NotImplemented,
        FailureReason.Unexpected => StatusCodes.Status500InternalServerError,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "No status code for this reason."),
    };

    /// <summary>The value as a 200, or the failure as a problem document.</summary>
    /// <param name="controller">The controller answering.</param>
    /// <param name="outcome">What the work produced.</param>
    public static IActionResult Respond<TValue>(this ControllerBase controller, Either<TValue> outcome) =>
        outcome.Match<IActionResult>(value => controller.Ok(value), controller.AsProblem);

    /// <summary>
    /// <paramref name="failure"/> as an <c>application/problem+json</c> document.
    /// </summary>
    /// <remarks>
    /// A failure carrying <see cref="DraftProblem"/>s puts every one of them under the <c>problems</c>
    /// extension member. A client marking up an editor needs all of them, not the first.
    /// </remarks>
    public static IActionResult AsProblem(this ControllerBase controller, Failure failure)
    {
        var status = StatusFor(failure.Reason);

        var details = new ProblemDetails
        {
            Status = status,
            Title = TitleFor(failure.Reason),
            Detail = failure.Message,
            Instance = controller.Request.Path,
        };

        // The domain reason as well as the status, because several reasons share a code and the enum is what a
        // client can branch on without reading prose.
        details.Extensions["reason"] = failure.Reason.ToString();

        if (failure is Failure<IReadOnlyList<DraftProblem>> detailed)
        {
            details.Extensions["problems"] = detailed.Detail;
        }

        var result = new ObjectResult(details) { StatusCode = status };
        result.ContentTypes.Add("application/problem+json");

        return result;
    }

    private static string TitleFor(FailureReason reason) => reason switch
    {
        FailureReason.NotFound => "Not found.",
        FailureReason.Invalid => "The request could not be read.",
        FailureReason.Unprocessable => "The request was read but could not be acted on.",
        FailureReason.Conflict => "That conflicts with the current state.",
        FailureReason.Unsupported => "Not supported.",
        FailureReason.Unexpected => "Something went wrong.",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "No title for this reason."),
    };
}

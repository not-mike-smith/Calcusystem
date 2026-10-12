using Calcusystem.Api.Http;
using Calcusystem.Core.Enums;
using FluentAssertions;

namespace Calcusystem.Api.Test;

/* The one place the domain's failures become HTTP. */
public class FailureTranslationTests
{
    /* The guard that matters: a reason added to the enum and forgotten here would otherwise be served as a 500
       with no sign anything was missed. */
    [Fact]
    public void EveryFailureReasonHasAStatusCode()
    {
        var translating = () =>
        {
            foreach (var reason in Enum.GetValues<FailureReason>()) FailureTranslation.StatusFor(reason);
        };

        translating.Should().NotThrow();
    }

    [Fact]
    public void NoFailureReasonIsServedAsSuccess()
    {
        Enum.GetValues<FailureReason>().Select(FailureTranslation.StatusFor)
            .Should().OnlyContain(status => status >= 400);
    }

    /* A draft that cannot be built is not a malformed request, and the two have to be told apart by status: a
       client retries one and fixes the other. */
    [Fact]
    public void AnUnbuildableDraftAndAnUnreadablePayloadDoNotShareAStatus()
    {
        FailureTranslation.StatusFor(FailureReason.Unprocessable)
            .Should().NotBe(FailureTranslation.StatusFor(FailureReason.Invalid));
    }
}

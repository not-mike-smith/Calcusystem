using Calcusystem.Core.Either;
using Calcusystem.Core.Enums;
using FluentAssertions;
using Xunit;

/* The snippets printed in this assembly's READMEs, compiled. Documentation that no longer builds is worse than
   none, and prose alone cannot show that a shown `using` list is sufficient. */
namespace Calcusystem.Core.Test;

public class ReadmeSnippets
{
    [Fact]
    public void EitherReadmeStartHere()
    {
        static Either<int> Parse(string text) =>
            int.TryParse(text, out var n) ? n : new Failure(FailureReason.Invalid, $"'{text}' is not a number");

        static Either<int> Halve(int n) =>
            n % 2 == 0 ? n / 2 : new Failure(FailureReason.Unprocessable, $"{n} is odd");

        var message = Parse("8")
            .Then(Halve)
            .Map(halved => halved + 1)
            .Match(v => $"got {v}", f => $"failed: {f}");

        message.Should().Be("got 5");

        // The two ways it can fail, each short-circuiting at a different step.
        Parse("eight").Then(Halve).Map(h => h + 1).Match(v => $"got {v}", f => $"failed: {f}")
            .Should().Be("failed: Invalid: 'eight' is not a number");

        Parse("7").Then(Halve).Map(h => h + 1).Match(v => $"got {v}", f => $"failed: {f}")
            .Should().Be("failed: Unprocessable: 7 is odd");
    }
}

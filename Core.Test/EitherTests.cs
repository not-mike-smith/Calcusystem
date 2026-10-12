using Calcusystem.Core.Either;
using Calcusystem.Core.Enums;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Core.Test;

public class EitherTests
{
    private static Failure NotFound(string what = "nope") => new(FailureReason.NotFound, what);

    // ── The two cases ────────────────────────────────────────────────────────

    [Fact]
    public void ASuccessCarriesItsValue()
    {
        Either<int> either = 42;

        either.Should().BeOfType<Succeeded<int>>()
            .Which.Value.Should().Be(42);
    }

    [Fact]
    public void AFailureCarriesItsReasonAndMessage()
    {
        Either<int> either = NotFound("no such variable");

        var failure = either.Should().BeOfType<Failed<int>>().Which.Failure;
        failure.Reason.Should().Be(FailureReason.NotFound);
        failure.Message.Should().Be("no such variable");
    }

    /* Both conversions are implicit so a method returning Either<T> reads like one returning T — `return value;`
       and `return failure;` both work, with no wrapper at the call site. */
    [Fact]
    public void BothConversionsAreImplicit()
    {
        static Either<string> Lookup(bool found) => found ? "found it" : NotFound();

        Lookup(true).Should().BeOfType<Succeeded<string>>();
        Lookup(false).Should().BeOfType<Failed<string>>();
    }

    // ── Matching ─────────────────────────────────────────────────────────────

    [Fact]
    public void MatchAnswersFromWhicheverCaseApplies()
    {
        Either<int> ok = 7;
        Either<int> bad = NotFound();

        ok.Match(v => $"got {v}", f => $"failed {f.Reason}").Should().Be("got 7");
        bad.Match(v => $"got {v}", f => $"failed {f.Reason}").Should().Be("failed NotFound");
    }

    [Fact]
    public void SwitchRunsOnlyTheBranchThatApplies()
    {
        var seen = new List<string>();
        Either<int> ok = 7;

        ok.Switch(v => seen.Add($"value {v}"), f => seen.Add($"failure {f.Reason}"));
        NotFound().AsType<int>().Switch(v => seen.Add($"value {v}"), f => seen.Add($"failure {f.Reason}"));

        seen.Should().Equal("value 7", "failure NotFound");
    }

    [Fact]
    public void ValueOrSubstitutesOnlyForAFailure()
    {
        Either<int> ok = 7;
        Either<int> bad = NotFound();

        ok.ValueOr(-1).Should().Be(7);
        bad.ValueOr(-1).Should().Be(-1);
    }

    // ── Composing dependent operations ───────────────────────────────────────

    [Fact]
    public void MapTransformsAValueAndLeavesAFailureAlone()
    {
        Either<int> ok = 7;
        Either<int> bad = NotFound();

        ok.Map(v => v * 2).Should().BeOfType<Succeeded<int>>().Which.Value.Should().Be(14);
        bad.Map(v => v * 2).Should().BeOfType<Failed<int>>();
    }

    /* The point of the whole type: dependent operations chain, and the first failure short-circuits the rest, so
       nothing in between has to test for one. */
    [Fact]
    public void ThenChainsAndTheFirstFailureStopsTheRest()
    {
        var ran = new List<string>();

        Either<int> Parse(string s)
        {
            ran.Add("parse");
            return int.TryParse(s, out var n) ? n : new Failure(FailureReason.Invalid, $"'{s}' is not a number");
        }

        Either<int> Halve(int n)
        {
            ran.Add("halve");
            return n % 2 == 0 ? n / 2 : new Failure(FailureReason.Unprocessable, $"{n} is odd");
        }

        Parse("8").Then(Halve).Should().BeOfType<Succeeded<int>>().Which.Value.Should().Be(4);
        ran.Should().Equal("parse", "halve");

        ran.Clear();
        var failed = Parse("eight").Then(Halve);

        failed.Should().BeOfType<Failed<int>>()
            .Which.Failure.Reason.Should().Be(FailureReason.Invalid);
        ran.Should().Equal(["parse"], "the continuation never runs after a failure");
    }

    /* A caller propagating a dependency's failure up through its own, different, payload type hands the same
       failure on rather than rebuilding it — so nothing is lost or restated in the relay. */
    [Fact]
    public void AFailurePropagatesAcrossPayloadTypesUnchanged()
    {
        var original = NotFound("the variable");
        Either<string> upstream = original;

        Either<int> propagated = upstream is Failed<string> f ? f.Failure : 0;

        propagated.Should().BeOfType<Failed<int>>()
            .Which.Failure.Should().BeSameAs(original);
    }

    [Fact]
    public void AsTypeIsTheExplicitFormOfThatPropagation()
    {
        var original = NotFound();

        original.AsType<decimal>().Should().BeOfType<Failed<decimal>>()
            .Which.Failure.Should().BeSameAs(original);
    }

    // ── What the type refuses to let you do ──────────────────────────────────

    /* Deliberately no Value on the base: it would be answerable on a failure, and a caller would reach it by
       casting — the runtime exception this type replaces. The sketch it grew from had the payload on its shared
       interface, so `failure.Result` compiled and threw.

       DidFail is not the same hazard and is expected to be here. A boolean cannot be used to reach a value it
       does not carry. */
    [Fact]
    public void TheBaseTypeAnswersWhetherItFailedButNeverWithAValue()
    {
        var members = typeof(Either<int>).GetMembers().Select(m => m.Name).ToList();

        members.Should().NotContain("Value");
        members.Should().Contain("DidFail");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DidFailAnswersFromTheCase(bool shouldFail)
    {
        Either<int> either = shouldFail ? NotFound() : 1;

        either.DidFail.Should().Be(shouldFail);
    }

    /* The non-functional way to relay a dependency's failure. The failure converts implicitly, so a caller
       returning Either<Other> writes `return failure;` with no mention of its own value type. */
    [Fact]
    public void TryGetFailureHandsOutTheFailureForAnEarlyReturn()
    {
        var original = NotFound("the dependency");

        Either<int> bad = original;
        bad.TryGetFailure(out var relayed).Should().BeTrue();
        relayed.Should().BeSameAs(original);

        Either<int> ok = 1;
        ok.TryGetFailure(out var none).Should().BeFalse();
        none.Should().BeNull();
    }

    /* Giving up early and then using the value, with no cast on either path — which is the gap TryGetFailure
       alone leaves, since after that guard the compiler still does not know a value is there. */
    [Fact]
    public void TryGetValueHandsOutWhicheverThereIs()
    {
        Either<int> ok = 7;
        ok.TryGetValue(out var value, out var noFailure).Should().BeTrue();
        value.Should().Be(7);
        noFailure.Should().BeNull();

        Either<int> bad = NotFound();
        bad.TryGetValue(out var noValue, out var failure).Should().BeFalse();
        noValue.Should().Be(default(int));
        failure!.Reason.Should().Be(FailureReason.NotFound);
    }

    /* The ordinary way of adding a case is blocked: the parameterless constructor is private protected, so a
       record declared outside this assembly cannot make its implicit base call.

       It is not airtight. A record's copy constructor must stay protected while the record is unsealed (CS8878),
       and an outside type can reach it with an explicit `base(borrowed)` call — verified, it compiles. That is
       why Match is abstract rather than a switch written here over the two known cases: Match is total for any
       case that can exist, so the gap costs correctness nothing. */
    [Fact]
    public void TheOrdinaryWayOfAddingACaseIsBlocked()
    {
        var constructors = typeof(Either<>).GetConstructors(
            System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic);

        var parameterless = constructors.Single(c => c.GetParameters().Length == 0);
        parameterless.IsFamilyAndAssembly.Should().BeTrue("private protected keeps derivation inside Core");

        var copy = constructors.Single(c => c.GetParameters().Length == 1);
        copy.IsFamily.Should().BeTrue("a record's copy constructor cannot be narrower than protected");
    }

    // ── Equality ─────────────────────────────────────────────────────────────

    [Fact]
    public void CasesCompareByValueAndNeverAcrossCases()
    {
        ((Either<int>)7).Should().Be((Either<int>)7);
        ((Either<int>)7).Should().NotBe((Either<int>)8);

        new Failed<int>(NotFound()).Should().Be(new Failed<int>(NotFound()));
        new Failed<int>(NotFound()).Should().NotBe(new Succeeded<int>(0));
    }

    // ── Failures with structured detail ──────────────────────────────────────

    /* The motivating case: a failed build has a list of findings, not one message. Subtyping carries them
       without Either gaining a second type parameter. */
    [Fact]
    public void AFailureCanCarryStructuredDetail()
    {
        string[] problems = ["m has no dimensionality", "the sum mixes mass and length"];
        Either<int> either = new Failure<string[]>(FailureReason.Unprocessable, "2 problems", problems);

        either.TryGetFailure(out var failure).Should().BeTrue();
        failure.Should().BeOfType<Failure<string[]>>()
            .Which.Detail.Should().BeSameAs(problems);
    }

    /* Everything that takes a Failure takes one of these, so a caller that only relays or reports is untouched
       by the detail being there. */
    [Fact]
    public void ADetailBlindCallerTreatsItAsAnyOtherFailure()
    {
        Either<int> either = new Failure<string[]>(FailureReason.Conflict, "clashed", ["a", "b"]);

        either.DidFail.Should().BeTrue();
        either.Match(v => "value", f => $"{f.Reason}: {f.Message}").Should().Be("Conflict: clashed");
        either.ValueOr(-1).Should().Be(-1);
    }

    /* Detail attached deep in a call stack has to survive the relay to the top, or the shape is useless: the
       whole point is that the outermost layer renders it. */
    [Fact]
    public void RelayingPreservesTheDetailAcrossPayloadTypes()
    {
        var original = new Failure<string[]>(FailureReason.Unprocessable, "2 problems", ["one", "two"]);
        Either<int> deep = original;

        deep.TryGetFailure(out var relayed).Should().BeTrue();
        Either<string> middle = relayed!;
        middle.TryGetFailure(out var atTheTop).Should().BeTrue();

        atTheTop.Should().BeSameAs(original);
        atTheTop.Should().BeOfType<Failure<string[]>>()
            .Which.Detail.Should().Equal("one", "two");
    }

    [Fact]
    public void ThenPassesDetailedFailuresThroughUntouched()
    {
        var original = new Failure<int[]>(FailureReason.Invalid, "bad", [1, 2, 3]);
        Either<int> start = original;

        start.Then(v => (Either<string>)v.ToString())
            .Should().BeOfType<Failed<string>>()
            .Which.Failure.Should().BeSameAs(original);
    }

    /* Record equality includes the runtime type, so a detailed failure never compares equal to the plain one it
       would flatten to. Otherwise a test could pass while the detail was silently dropped. */
    [Fact]
    public void ADetailedFailureIsNotEqualToAPlainOne()
    {
        var detailed = new Failure<string[]>(FailureReason.Invalid, "bad", ["x"]);
        var plain = new Failure(FailureReason.Invalid, "bad");

        detailed.Should().NotBe(plain);
        ((Failure)detailed).Should().NotBe(plain);
    }

    /* No zero member, so a failure deserialized without a reason is detectably absent rather than silently
       NotFound. Same rule as SolvingRole and AgreementRule. */
    [Fact]
    public void NoFailureReasonIsZero()
    {
        Enum.GetValues<FailureReason>().Should().NotContain(default(FailureReason));
    }
}

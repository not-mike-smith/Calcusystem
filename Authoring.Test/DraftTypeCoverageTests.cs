using Calcusystem.Authoring.Enums;
using Calcusystem.DimensionedExpression.Enums;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Authoring.Test;

/* ExpressionDraftType is one flat set over the node kinds that the snapshots group by arity across three enums.
   Two lists of the same closed set drift, so these hold them in step: a node kind added to the domain and not
   here would be unauthorable, and one removed from the domain but left here would be offerable and unbuildable.

   Relationships need no such test — RelationshipDraft.Type is BinaryOperatorType itself. */
public class DraftTypeCoverageTests
{
    private static IEnumerable<string> DomainExpressionKinds() =>
        Enum.GetNames<UnaryExpressionType>()
            .Concat(Enum.GetNames<NaryExpressionType>())
            .Concat(Enum.GetNames<BinaryExpressionType>());

    [Fact]
    public void EveryDomainExpressionKindCanBeDrafted()
    {
        var draftable = Enum.GetNames<ExpressionDraftType>();

        DomainExpressionKinds().Should().BeSubsetOf(draftable);
    }

    [Fact]
    public void EveryDraftableKindExistsInTheDomain()
    {
        // Variable is the exception: it has a snapshot of its own rather than a kind within a grouped one,
        // because it is the only node rebuilt without resolving references.
        var expected = DomainExpressionKinds().Append(nameof(ExpressionDraftType.Variable));

        Enum.GetNames<ExpressionDraftType>().Should().BeSubsetOf(expected);
    }

    /* No member is zero, so a draft deserialized without a kind is detectably untyped rather than silently a
       Variable. Same reasoning as SolvingRole and AgreementRule. */
    [Fact]
    public void NoDraftTypeIsZero()
    {
        Enum.GetValues<ExpressionDraftType>().Should().NotContain(default(ExpressionDraftType));
        Enum.GetValues<DraftProblemKind>().Should().NotContain(default(DraftProblemKind));
    }
}

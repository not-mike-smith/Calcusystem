using Calcusystem.Authoring.Drafts;
using Calcusystem.Authoring.Enums;
using Calcusystem.Measurement.Extensions;
using Calcusystem.Measurement.Primitives;
using Calcusystem.Measurement.Units;
using FluentAssertions;
using Xunit;

namespace Calcusystem.Authoring.Test;

/* The snippets printed in this assembly's READMEs, compiled. Documentation that no longer builds is worse than
   none, and prose alone cannot show that a shown `using` list is sufficient.

   The snippets stop at TryBuild. Carrying one through to system.Calculate() would mean referencing
   Calcusystem.Analysis from here, and a test project references only the layers it covers. */
public class ReadmeSnippets
{
    [Fact]
    public void AssemblyReadmeUsingIt()
    {
        var draft = SystemDraft.Create("Newton's second law", "F = m·a");

        var m = draft.AddVariable("m", Dimensionality.Mass);
        m.Value = Mass.Kilogram.Quantity(2).WithUncertainty(1.0.Percent());
        var a = draft.AddVariable("a", Dimensionality.Length / (Dimensionality.Time * Dimensionality.Time));

        var f = draft.AddExpression(ExpressionDraftType.Product);
        f.OperandIds.Add(m.Id);
        f.OperandIds.Add(a.Id);

        foreach (var problem in draft.Validate())
        {
            problem.NodeId.Should().NotBeNull();
            problem.Message.Should().NotBeEmpty();
        }

        draft.TryBuild(out var system, out var problems).Should().BeTrue();
        problems.Should().BeEmpty();
        system!.Variables.Should().HaveCount(2);
    }

    [Fact]
    public void DraftsReadmeStartHere()
    {
        var draft = SystemDraft.Create("a model", "");

        var m = draft.AddVariable("m", Dimensionality.Mass);
        var doubled = draft.AddExpression(ExpressionDraftType.Sum);
        doubled.OperandIds.Add(m.Id);
        doubled.OperandIds.Add(m.Id);

        draft.TryBuild(out var system, out var problems).Should().BeTrue();

        problems.Should().BeEmpty();

        // The same operand twice is one variable in the built system: Add absorbs by identity.
        system!.Variables.Should().ContainSingle();
        system.DerivedExpressions.Should().ContainSingle();
    }
}

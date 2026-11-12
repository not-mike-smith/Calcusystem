using Calcusystem.DimensionedExpression.Exceptions;
using Calcusystem.DimensionedExpression.Names;
using FluentAssertions;
using Xunit;

namespace Calcusystem.DimensionedExpression.Test.Names;

/* Spec A §4.1 rule 4: what a valid name is. One rule, asked by the domain when a node is built and by a draft
   when it reports, so the two cannot come to disagree. */
public class NodeNameTests
{
    [Theory]
    [InlineData("m")]
    [InlineData("h₀")]
    [InlineData("A_tank")]
    [InlineData("π/4")]
    [InlineData("D²")]
    [InlineData("drain time")]
    [InlineData("F-max")]
    [InlineData("🚀")]
    [InlineData("eq-1")]
    public void AnOrdinaryNameIsValid(string name) => NodeName.Problem(name).Should().BeNull();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(" m")]
    [InlineData("m ")]
    [InlineData("m\n")]
    public void AnEmptyOrPaddedNameIsNot(string? name) => NodeName.Problem(name).Should().NotBeNull();

    /* Cf and Cc: characters a reader cannot see. Two names differing only by one of these would look identical
       and be different nodes. */
    [Theory]
    [InlineData("a​b")]   // zero-width space
    [InlineData("a‍b")]   // zero-width joiner
    [InlineData("a‮b")]   // right-to-left override
    [InlineData("a­b")]   // soft hyphen
    [InlineData("a\u0007b")]   // bell
    [InlineData("a\tb")]       // a tab inside is a control character
    [InlineData("a\U000E0041b")] // a tag character, outside the basic plane
    public void ANameHoldingAnInvisibleCharacterIsNot(string name) =>
        NodeName.Problem(name).Should().NotBeNull();

    /* The family emoji is four emoji joined by zero-width joiners, and the joiner is a format character. */
    [Fact]
    public void AnEmojiJoinedByAZeroWidthJoinerIsNot() =>
        NodeName.Problem("👨‍👩‍👧").Should().NotBeNull();

    /* A lone surrogate is not text, and cannot be normalized to be stored. */
    [Fact]
    public void TextThatIsNotValidUnicodeIsNot()
    {
        NodeName.Problem("a\uD800b").Should().NotBeNull();

        var act = () => NodeName.Checked("a\uD800b");
        act.Should().Throw<InvalidNameException>();
    }

    /* Latin a and Cyrillic а. Nothing guards against mixing scripts, so these are two valid, different names. */
    [Fact]
    public void LookAlikeCharactersAreAllowedAndStayDistinct()
    {
        NodeName.Problem("а").Should().BeNull();
        NodeName.Normalized("а").Should().NotBe(NodeName.Normalized("a"));
    }

    /* é as one code point and as e + combining acute: the same characters, encoded differently. */
    [Fact]
    public void TwoEncodingsOfTheSameCharactersNormalizeToOneName()
    {
        NodeName.Normalized("é").Should().Be("é");
        NodeName.Normalized("é").Should().Be("é");
    }

    [Fact]
    public void NamesAreCaseSensitive() =>
        NodeName.Normalized("Re").Should().NotBe(NodeName.Normalized("re"));

    [Fact]
    public void CheckedReturnsTheStoredFormOrThrows()
    {
        NodeName.Checked("é").Should().Be("é");

        var act = () => NodeName.Checked(" padded ");
        act.Should().Throw<InvalidNameException>().WithMessage("*padded*");

        var none = () => NodeName.Checked(null);
        none.Should().Throw<InvalidNameException>();
    }
}

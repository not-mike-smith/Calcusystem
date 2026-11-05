using Calcusystem.Examples.Presentation;

namespace Calcusystem.Examples.Problems;

/// <summary>Every worked example in this assembly.</summary>
/// <remarks>
/// One list, so the runner and the test suite cannot cover different sets. An example absent from here is an
/// example nothing prints and nothing checks.
/// </remarks>
public static class Catalogue
{
    /// <summary>Builds all of them, each one fresh.</summary>
    /// <remarks>
    /// Fresh because a <c>Variable</c>'s value is mutable: a caller that withholds one to see what stalls must
    /// not be editing a model something else is reading.
    /// </remarks>
    public static IReadOnlyList<WorkedExample> Build() =>
    [
        OrificeDrainTime.Build(),
        ParallelDcLoads.Build(),
        GyroscopicPrecession.Build(),
    ];
}

// The runner. Top-level statements have no namespace to be a folder of, so this sits at the assembly root
// alongside Api/Program.cs — the same exception, for the same reason.

using Calcusystem.Analysis.Extensions;
using Calcusystem.Examples.Presentation;
using Calcusystem.Examples.Problems;

Console.OutputEncoding = System.Text.Encoding.UTF8;

foreach (var example in Catalogue.Build())
{
    Console.WriteLine(ExampleReport.Render(example));
    Console.WriteLine();
}

// The same model with one value taken away. A half-built model is the normal case while someone is still
// authoring, so nothing throws: the degrees of freedom go up, the nodes downstream of the gap report as
// unresolved, and every check that needed one of them comes back outstanding rather than passing or failing.
var halfBuilt = OrificeDrainTime.Build();
halfBuilt.Leaf(OrificeDrainTime.DischargeCoefficientId).Value = null;

Console.WriteLine("The same model, with the discharge coefficient withheld");
Console.WriteLine(new string('─', 78));
Console.WriteLine();
Console.WriteLine(ExampleReport.Render(halfBuilt, halfBuilt.System.Calculate()));

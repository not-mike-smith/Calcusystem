using Calcusystem.Api.Http;
using Calcusystem.Api.Reports;
using Calcusystem.Api.Workflows;
using Calcusystem.Authoring.Drafts;
using Microsoft.AspNetCore.Mvc;
using Dtos = Calcusystem.Serialization.Dtos;

namespace Calcusystem.Api.Controllers;

/// <summary>
/// Everything a client can ask about a draft it holds. Each endpoint takes the whole draft and keeps nothing.
/// </summary>
[ApiController]
[Route("api/drafts")]
public sealed class DraftsController(DraftWorkflow workflow) : ControllerBase
{
    /// <summary>What stands between the posted draft and a built system.</summary>
    /// <remarks>
    /// Always a 200, with the problems in the body. The request is well formed either way, and a draft with
    /// problems is the answer rather than a refusal — a 4xx here would make an editor read failures as errors.
    /// </remarks>
    [HttpPost("validation")]
    [ProducesResponseType<IReadOnlyList<DraftProblem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Validate([FromBody] Dtos.SystemDraft draft) =>
        this.Respond(DraftIntake.Read(draft).Map(workflow.Problems));

    /// <summary>Builds the posted draft, and answers with it in the form a client can save.</summary>
    /// <remarks>A draft that cannot be built is a 422 carrying every problem under <c>problems</c>.</remarks>
    [HttpPost("build")]
    [ProducesResponseType<Dtos.ExpressionSystem>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Build([FromBody] Dtos.SystemDraft draft) =>
        this.Respond(DraftIntake.Read(draft).Then(workflow.Build));

    /// <summary>Which of the posted draft's unknowns can be solved, and which cannot.</summary>
    [HttpPost("decomposition")]
    [ProducesResponseType<DecompositionReport>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Decompose([FromBody] Dtos.SystemDraft draft) =>
        this.Respond(DraftIntake.Read(draft).Then(workflow.Decompose));

    /// <summary>What the posted draft computes from the values it carries.</summary>
    [HttpPost("calculation")]
    [ProducesResponseType<CalculationReport>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult Calculate([FromBody] Dtos.SystemDraft draft) =>
        this.Respond(DraftIntake.Read(draft).Then(workflow.Calculate));
}

using Calcusystem.Api.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Calcusystem.Api.Controllers;

/// <summary>The choices an author can be offered, so a client does not hold its own copy of them.</summary>
[ApiController]
[Route("api/vocabulary")]
public sealed class VocabularyController : ControllerBase
{
    /// <summary>Every kind, role, rule, landmark and dimension, by the name the wire uses.</summary>
    [HttpGet]
    [ProducesResponseType<VocabularyReport>(StatusCodes.Status200OK)]
    public VocabularyReport Get() => Reporting.Vocabulary();
}

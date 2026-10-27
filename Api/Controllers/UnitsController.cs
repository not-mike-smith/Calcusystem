using Calcusystem.Api.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Calcusystem.Api.Controllers;

/// <summary>The units an author can read and write a value in.</summary>
/// <remarks>
/// Apart from <c>/api/vocabulary</c> because it is far larger and changes even less often, so a client can
/// fetch it once and keep it.
/// </remarks>
[ApiController]
[Route("api/units")]
public sealed class UnitsController : ControllerBase
{
    /// <summary>Every unit, grouped by what it measures, with the two numbers that convert a KMS value into it.</summary>
    [HttpGet]
    [ProducesResponseType<UnitCatalogueReport>(StatusCodes.Status200OK)]
    public UnitCatalogueReport Get() => Reporting.Units();
}

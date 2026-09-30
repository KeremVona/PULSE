using System.Security.Claims;
using HeartCDSS.Application.Encounters.Commands.MakeEncounter;
using HeartCDSS.Application.Encounters.Queries.GetEncounterById;
using HeartCDSS.Application.Encounters.Queries.GetPatientEncounters;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeartCDSS.Api.Controllers;

/// <summary>
/// API contract for making a clinical encounter request via HTTP.
/// </summary>
public sealed record MakeEncounterApiRequest(
    string EncounterNumber,
    EncounterType EncounterType,
    string? ChiefComplaint,
    ClinicalIndicatorsInputDto Indicators,
    Guid? ClinicianId = null
);

/// <summary>
/// Clinical encounter controller orchestrating patient consultation lifecycle,
/// physiological intake, and automated machine learning risk assessment.
/// </summary>
[ApiController]
[Route("api/v1/patients/{patientId:guid}/encounters")]
[Authorize(Roles = $"{Role.Physician},{Role.ClinicalStaff},{Role.Admin}")]
public sealed class EncountersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EncountersController> _logger;

    public EncountersController(IMediator mediator, ILogger<EncountersController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Makes a new clinical encounter, records physiological indicators, triggers automated ML risk evaluation, and stores SHAP attributions.
    /// </summary>
    [HttpPost("")]
    [ProducesResponseType(typeof(MakeEncounterResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> MakeEncounter(
        [FromRoute] Guid patientId,
        [FromBody] MakeEncounterApiRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var madeClinicianId = ResolveClinicianId(request.ClinicianId);

        var madeCommand = new MakeEncounterCommand(
            PatientId: patientId,
            ClinicianId: madeClinicianId,
            EncounterNumber: request.EncounterNumber,
            EncounterType: request.EncounterType,
            ChiefComplaint: request.ChiefComplaint,
            Indicators: request.Indicators
        );

        _logger.LogInformation(
            "Clinician {ClinicianId} initiating encounter {EncounterNumber} for Patient {PatientId}",
            madeClinicianId,
            request.EncounterNumber,
            patientId);

        var madeResult = await _mediator.Send(madeCommand, cancellationToken);

        return CreatedAtAction(
            nameof(GetEncounterById),
            new { patientId = patientId, encounterId = madeResult.EncounterId },
            madeResult
        );
    }

    /// <summary>
    /// Retrieves a historical clinical encounter by ID, including recorded indicators, calibrated risk probability, and SHAP feature attributions.
    /// </summary>
    [HttpGet("{encounterId:guid}")]
    [Authorize(Roles = $"{Role.Physician},{Role.ClinicalStaff},{Role.Admin},{Role.Auditor}")]
    [ProducesResponseType(typeof(EncounterDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEncounterById(
        [FromRoute] Guid patientId,
        [FromRoute] Guid encounterId,
        CancellationToken cancellationToken)
    {
        var madeQuery = new GetEncounterByIdQuery(patientId, encounterId);
        var madeResult = await _mediator.Send(madeQuery, cancellationToken);

        if (madeResult is null)
        {
            return NotFound(new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Encounter Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Clinical encounter '{encounterId}' for patient '{patientId}' was not found.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(madeResult);
    }

    /// <summary>
    /// Retrieves all historical encounters for a specified patient.
    /// </summary>
    [HttpGet("")]
    [Authorize(Roles = $"{Role.Physician},{Role.ClinicalStaff},{Role.Admin},{Role.Auditor}")]
    [ProducesResponseType(typeof(IReadOnlyList<EncounterDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPatientEncounters(
        [FromRoute] Guid patientId,
        CancellationToken cancellationToken)
    {
        var madeQuery = new GetPatientEncountersQuery(patientId);
        var madeEncounters = await _mediator.Send(madeQuery, cancellationToken);
        return Ok(madeEncounters);
    }

    private Guid ResolveClinicianId(Guid? requestClinicianId)
    {
        if (requestClinicianId.HasValue && requestClinicianId.Value != Guid.Empty)
        {
            return requestClinicianId.Value;
        }

        var claimVal = User.FindFirst("clinician_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(claimVal, out var cid))
        {
            return cid;
        }

        return Guid.NewGuid();
    }
}

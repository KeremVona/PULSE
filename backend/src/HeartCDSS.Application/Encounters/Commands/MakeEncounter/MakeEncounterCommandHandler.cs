using System.Text.Json;
using HeartCDSS.Application.Common.Exceptions;
using HeartCDSS.Application.Common.Interfaces;
using HeartCDSS.Application.Common.Models;
using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Exceptions;
using HeartCDSS.Domain.Repositories;
using HeartCDSS.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HeartCDSS.Application.Encounters.Commands.MakeEncounter;

/// <summary>
/// MediatR Command Handler orchestrating clinical encounter initialization,
/// physiological intake, resilient ML inference dispatch, persistence, and audit logging.
/// </summary>
public sealed class MakeEncounterCommandHandler : IRequestHandler<MakeEncounterCommand, MakeEncounterResultDto>
{
    private static readonly Guid DefaultModelId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly IPatientRepository _patientRepository;
    private readonly IClinicalEncounterRepository _encounterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMlInferenceClient? _mlInferenceClient;
    private readonly IRiskAssessmentRepository? _riskAssessmentRepository;
    private readonly IPredictionAuditRepository? _auditRepository;
    private readonly ICorrelationContext? _correlationContext;
    private readonly ILogger<MakeEncounterCommandHandler>? _logger;

    public MakeEncounterCommandHandler(
        IPatientRepository patientRepository,
        IClinicalEncounterRepository encounterRepository,
        IUnitOfWork unitOfWork,
        IMlInferenceClient? mlInferenceClient = null,
        IRiskAssessmentRepository? riskAssessmentRepository = null,
        IPredictionAuditRepository? auditRepository = null,
        ICorrelationContext? correlationContext = null,
        ILogger<MakeEncounterCommandHandler>? logger = null)
    {
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
        _encounterRepository = encounterRepository ?? throw new ArgumentNullException(nameof(encounterRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mlInferenceClient = mlInferenceClient;
        _riskAssessmentRepository = riskAssessmentRepository;
        _auditRepository = auditRepository;
        _correlationContext = correlationContext;
        _logger = logger;
    }

    public async Task<MakeEncounterResultDto> Handle(
        MakeEncounterCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Verify Patient existence
        var madePatient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (madePatient is null)
        {
            throw new KeyNotFoundException($"Patient with ID '{request.PatientId}' was not found.");
        }

        // 2. Check for duplicate Encounter Number
        var madeExistingEncounter = await _encounterRepository.GetByEncounterNumberAsync(request.EncounterNumber, cancellationToken);
        if (madeExistingEncounter is not null)
        {
            throw new DomainException($"Clinical encounter with number '{request.EncounterNumber}' already exists.");
        }

        // 3. Make physiological Value Objects
        var madeBloodPressure = BloodPressure.Make(
            request.Indicators.RestingBp,
            request.Indicators.DiastolicBp
        );

        var madeCholesterol = CholesterolReading.Make(
            request.Indicators.SerumCholesterol,
            request.Indicators.FastingBloodSugar
        );

        var madeEcg = EcgResult.Make(
            request.Indicators.RestingEcg,
            request.Indicators.MaxHeartRate,
            request.Indicators.ExerciseAngina,
            request.Indicators.OldpeakStDepression,
            request.Indicators.Slope
        );

        // 4. Make Encounter Aggregate Root
        var madeEncounter = ClinicalEncounter.Make(
            patientId: request.PatientId,
            clinicianId: request.ClinicianId,
            encounterNumber: request.EncounterNumber,
            encounterType: request.EncounterType,
            chiefComplaint: request.ChiefComplaint
        );

        // 5. Make and attach Clinical Indicators
        var madeIndicators = ClinicalIndicators.Make(
            encounterId: madeEncounter.Id,
            ageAtEncounter: request.Indicators.Age,
            sex: request.Indicators.Sex,
            chestPainType: (ChestPainType)request.Indicators.ChestPainType,
            bloodPressure: madeBloodPressure,
            cholesterol: madeCholesterol,
            ecg: madeEcg,
            fluoroscopyVessels: request.Indicators.FluoroscopyVessels,
            thalassemia: (ThalassemiaType)request.Indicators.Thalassemia
        );

        madeEncounter.AttachClinicalIndicators(madeIndicators);
        madePatient.AddEncounter(madeEncounter);
        await _encounterRepository.AddAsync(madeEncounter, cancellationToken);

        // 6. Extract physiological clinical alerts
        var madeAlerts = madeIndicators.GetClinicalWarnings();

        MlInferenceResultDto? madeMlResult = null;

        // 7. If ML Inference Client is available, dispatch prediction
        if (_mlInferenceClient is not null)
        {
            var madeCorrelationId = _correlationContext?.CorrelationId ?? Guid.NewGuid();
            var madePayloadDto = MlIndicatorsPayloadDto.MakeFromDomain(madeIndicators);
            var madeInferenceRequest = MlInferenceRequestDto.Make(
                indicators: madePayloadDto,
                encounterId: madeEncounter.Id.ToString(),
                correlationId: madeCorrelationId.ToString(),
                patientMrn: madePatient.Mrn
            );

            var madeStopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                madeMlResult = await _mlInferenceClient.MakePredictionAsync(madeInferenceRequest, cancellationToken);
                madeStopwatch.Stop();

                // Map to domain RiskAssessmentResult and attach to encounter
                var madeAssessment = madeMlResult.MakeRiskAssessmentResult(madeEncounter.Id, DefaultModelId);
                madeEncounter.AddRiskAssessment(madeAssessment);

                if (_riskAssessmentRepository is not null)
                {
                    await _riskAssessmentRepository.AddAsync(madeAssessment, cancellationToken);
                }

                // Record successful inference audit
                if (_auditRepository is not null)
                {
                    var madeSuccessAudit = PredictionAudit.RecordSuccess(
                        encounterId: madeEncounter.Id,
                        userId: request.ClinicianId,
                        correlationId: madeCorrelationId,
                        requestPayload: JsonSerializer.Serialize(madeInferenceRequest),
                        responsePayload: JsonSerializer.Serialize(madeMlResult),
                        executionTimeMs: (int)madeStopwatch.ElapsedMilliseconds,
                        explainingFeatures: JsonSerializer.Serialize(madeMlResult.Explanations),
                        riskAssessmentResultId: madeAssessment.Id,
                        clientIp: _correlationContext?.ClientIp,
                        userAgent: _correlationContext?.UserAgent
                    );

                    await _auditRepository.AddAsync(madeSuccessAudit, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                madeStopwatch.Stop();

                // Record failure audit for clinical traceability
                if (_auditRepository is not null)
                {
                    var failureStatus = ex is MlInferenceUnavailableException un && un.DownstreamReason == "CIRCUIT_BREAKER_OPEN"
                        ? InferenceExecutionStatus.CircuitBreakerOpen
                        : ex is TimeoutException or MlInferenceUnavailableException { DownstreamReason: "TIMEOUT" }
                            ? InferenceExecutionStatus.Timeout
                            : InferenceExecutionStatus.InternalError;

                    var madeFailureAudit = PredictionAudit.RecordFailure(
                        encounterId: madeEncounter.Id,
                        userId: request.ClinicianId,
                        correlationId: madeCorrelationId,
                        requestPayload: JsonSerializer.Serialize(madeInferenceRequest),
                        executionTimeMs: (int)madeStopwatch.ElapsedMilliseconds,
                        failureStatus: failureStatus,
                        errorDetails: ex.Message,
                        clientIp: _correlationContext?.ClientIp,
                        userAgent: _correlationContext?.UserAgent
                    );

                    await _auditRepository.AddAsync(madeFailureAudit, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                _logger?.LogError(ex, "ML Inference failed during encounter orchestration for {EncounterNumber}", request.EncounterNumber);
                throw;
            }
        }

        // 8. Persist through Unit of Work
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MakeEncounterResultDto(
            EncounterId: madeEncounter.Id,
            PatientId: madeEncounter.PatientId,
            ClinicianId: madeEncounter.ClinicianId,
            EncounterNumber: madeEncounter.EncounterNumber,
            EncounterType: madeEncounter.EncounterType,
            Status: madeEncounter.Status,
            ChiefComplaint: madeEncounter.ChiefComplaint,
            EncounterDate: madeEncounter.EncounterDate,
            ClinicalAlerts: madeAlerts,
            RiskAssessment: madeMlResult
        );
    }
}

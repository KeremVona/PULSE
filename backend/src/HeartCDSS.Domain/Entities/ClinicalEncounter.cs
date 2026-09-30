using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Events;
using HeartCDSS.Domain.Exceptions;

namespace HeartCDSS.Domain.Entities;

/// <summary>
/// Clinical Encounter Aggregate Root managing patient consultation lifecycle, physiological intake, and risk assessment history.
/// </summary>
public sealed class ClinicalEncounter : AggregateRoot<Guid>
{
    private readonly List<RiskAssessmentResult> _riskAssessments = [];

    public Guid PatientId { get; private set; }
    public Guid ClinicianId { get; private set; }
    public string EncounterNumber { get; private set; } = string.Empty;
    public EncounterType EncounterType { get; private set; }
    public EncounterStatus Status { get; private set; } = EncounterStatus.InProgress;
    public string? ChiefComplaint { get; private set; }
    public DateTimeOffset EncounterDate { get; private set; }

    public ClinicalIndicators? Indicators { get; private set; }
    public IReadOnlyCollection<RiskAssessmentResult> RiskAssessments => _riskAssessments.AsReadOnly();

    private ClinicalEncounter() { }

    private ClinicalEncounter(
        Guid id,
        Guid patientId,
        Guid clinicianId,
        string encounterNumber,
        EncounterType encounterType,
        string? chiefComplaint,
        DateTimeOffset encounterDate)
        : base(id)
    {
        PatientId = patientId;
        ClinicianId = clinicianId;
        EncounterNumber = encounterNumber;
        EncounterType = encounterType;
        ChiefComplaint = chiefComplaint;
        EncounterDate = encounterDate;
        Status = EncounterStatus.InProgress;

        AddDomainEvent(new EncounterMadeEvent(Id, PatientId, ClinicianId, EncounterNumber, EncounterType));
    }

    /// <summary>
    /// Factory method instantiating a new clinical encounter.
    /// </summary>
    public static ClinicalEncounter Make(
        Guid patientId,
        Guid clinicianId,
        string encounterNumber,
        EncounterType encounterType,
        string? chiefComplaint = null,
        DateTimeOffset? encounterDate = null,
        Guid? id = null)
    {
        if (patientId == Guid.Empty)
            throw new DomainException("Patient ID cannot be empty.");

        if (clinicianId == Guid.Empty)
            throw new DomainException("Clinician ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(encounterNumber))
            throw new DomainException("Encounter number cannot be empty.");

        var date = encounterDate ?? DateTimeOffset.UtcNow;

        return new ClinicalEncounter(
            id ?? Guid.NewGuid(),
            patientId,
            clinicianId,
            encounterNumber.Trim(),
            encounterType,
            chiefComplaint?.Trim(),
            date
        );
    }

    /// <summary>
    /// Binds physiological indicators to this encounter.
    /// </summary>
    public void AttachClinicalIndicators(ClinicalIndicators indicators)
    {
        ArgumentNullException.ThrowIfNull(indicators);

        if (Status == EncounterStatus.Cancelled || Status == EncounterStatus.Signed)
        {
            throw new InvalidEntityStateException(
                nameof(ClinicalEncounter),
                Id,
                nameof(AttachClinicalIndicators),
                $"Cannot update indicators on an encounter with status '{Status}'."
            );
        }

        Indicators = indicators;
        SetUpdatedAt();

        AddDomainEvent(new ClinicalIndicatorsRecordedEvent(
            indicators.Id,
            Id,
            indicators.AgeAtEncounter,
            indicators.BloodPressure.Systolic,
            indicators.Cholesterol.SerumCholesterolMgDl
        ));
    }

    /// <summary>
    /// Adds a completed risk assessment evaluation to the encounter history.
    /// </summary>
    public void AddRiskAssessment(RiskAssessmentResult assessment)
    {
        ArgumentNullException.ThrowIfNull(assessment);

        if (Status == EncounterStatus.Cancelled)
        {
            throw new InvalidEntityStateException(
                nameof(ClinicalEncounter),
                Id,
                nameof(AddRiskAssessment),
                "Cannot attach a risk assessment to a cancelled encounter."
            );
        }

        _riskAssessments.Add(assessment);
        Status = EncounterStatus.AssessmentComplete;
        SetUpdatedAt();

        AddDomainEvent(new RiskAssessmentCompletedEvent(
            assessment.Id,
            Id,
            PatientId,
            assessment.RiskProbability,
            assessment.RiskBand,
            assessment.IsCadPositive
        ));
    }

    /// <summary>
    /// Clinician signs off and finalizes the encounter.
    /// </summary>
    public void Sign()
    {
        if (Status == EncounterStatus.Signed)
            return;

        if (Status == EncounterStatus.Cancelled)
            throw new InvalidEntityStateException(nameof(ClinicalEncounter), Id, nameof(Sign), "Cannot sign a cancelled encounter.");

        if (Indicators is null)
            throw new InvalidEntityStateException(nameof(ClinicalEncounter), Id, nameof(Sign), "Cannot sign an encounter without clinical indicators.");

        Status = EncounterStatus.Signed;
        SetUpdatedAt();
    }

    /// <summary>
    /// Cancels the encounter with an audit rationale.
    /// </summary>
    public void Cancel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Cancellation reason must be specified.");

        if (Status == EncounterStatus.Signed)
            throw new InvalidEntityStateException(nameof(ClinicalEncounter), Id, nameof(Cancel), "Cannot cancel a signed encounter.");

        Status = EncounterStatus.Cancelled;
        SetUpdatedAt();
    }
}

using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Enums;

namespace HeartCDSS.Domain.Events;

/// <summary>
/// Dispatched when machine learning inference finishes evaluating cardiovascular risk for an encounter.
/// </summary>
public sealed record RiskAssessmentCompletedEvent(
    Guid RiskAssessmentId,
    Guid EncounterId,
    Guid PatientId,
    decimal RiskProbability,
    RiskBand RiskBand,
    bool IsCadPositive,
    DateTimeOffset OccurredOn,
    Guid EventId) : IDomainEvent
{
    public RiskAssessmentCompletedEvent(
        Guid riskAssessmentId,
        Guid encounterId,
        Guid patientId,
        decimal riskProbability,
        RiskBand riskBand,
        bool isCadPositive)
        : this(riskAssessmentId, encounterId, patientId, riskProbability, riskBand, isCadPositive, DateTimeOffset.UtcNow, Guid.NewGuid())
    {
    }
}

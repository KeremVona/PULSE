using FluentAssertions;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Events;
using HeartCDSS.Domain.Exceptions;
using HeartCDSS.Domain.ValueObjects;
using Xunit;

namespace HeartCDSS.Domain.Tests.Entities;

public class ClinicalEncounterTests
{
    private static ClinicalIndicators MakeDummyIndicators(Guid encounterId)
    {
        return ClinicalIndicators.Make(
            encounterId: encounterId,
            ageAtEncounter: 58,
            sex: 1,
            chestPainType: ChestPainType.Asymptomatic,
            bloodPressure: BloodPressure.Make(145, 90),
            cholesterol: CholesterolReading.Make(268, true),
            ecg: EcgResult.Make(RestingEcgType.StTAbnormality, 115, true, 2.8m, StSlope.Flat),
            fluoroscopyVessels: 2,
            thalassemia: ThalassemiaType.ReversibleDefect
        );
    }

    [Fact]
    public void Make_ShouldEmitEncounterMadeEvent()
    {
        var patientId = Guid.NewGuid();
        var clinicianId = Guid.NewGuid();

        var encounter = ClinicalEncounter.Make(
            patientId: patientId,
            clinicianId: clinicianId,
            encounterNumber: "ENC-1001",
            encounterType: EncounterType.OutpatientScreening,
            chiefComplaint: "Exertional retrosternal chest pressure"
        );

        encounter.PatientId.Should().Be(patientId);
        encounter.ClinicianId.Should().Be(clinicianId);
        encounter.EncounterNumber.Should().Be("ENC-1001");
        encounter.Status.Should().Be(EncounterStatus.InProgress);
        encounter.ChiefComplaint.Should().Be("Exertional retrosternal chest pressure");

        encounter.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EncounterMadeEvent>()
            .Which.EncounterId.Should().Be(encounter.Id);
    }

    [Fact]
    public void AttachClinicalIndicators_ShouldSetIndicatorsAndEmitEvent()
    {
        var encounter = ClinicalEncounter.Make(Guid.NewGuid(), Guid.NewGuid(), "ENC-1002", EncounterType.CardiologyFollowUp);
        encounter.ClearDomainEvents();

        var indicators = MakeDummyIndicators(encounter.Id);
        encounter.AttachClinicalIndicators(indicators);

        encounter.Indicators.Should().Be(indicators);
        encounter.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ClinicalIndicatorsRecordedEvent>()
            .Which.IndicatorsId.Should().Be(indicators.Id);
    }

    [Fact]
    public void AddRiskAssessment_ShouldUpdateStatusAndEmitEvent()
    {
        var encounter = ClinicalEncounter.Make(Guid.NewGuid(), Guid.NewGuid(), "ENC-1003", EncounterType.EmergencyTriage);
        encounter.ClearDomainEvents();

        var ci = ConfidenceInterval.Make(0.60m, 0.72m);
        var assessment = RiskAssessmentResult.Make(
            encounterId: encounter.Id,
            modelId: Guid.NewGuid(),
            riskProbability: 0.6650m,
            riskBand: RiskBand.Elevated,
            confidenceInterval: ci,
            decisionThreshold: 0.2900m,
            baseExpectedValue: -0.4200m,
            cdssAdvisoryText: "Clinical evaluation advisory notice."
        );

        encounter.AddRiskAssessment(assessment);

        encounter.Status.Should().Be(EncounterStatus.AssessmentComplete);
        encounter.RiskAssessments.Should().ContainSingle().Which.Should().Be(assessment);
        encounter.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RiskAssessmentCompletedEvent>()
            .Which.RiskProbability.Should().Be(0.6650m);
    }

    [Fact]
    public void Sign_WithIndicators_ShouldTransitionToSigned()
    {
        var encounter = ClinicalEncounter.Make(Guid.NewGuid(), Guid.NewGuid(), "ENC-1004", EncounterType.OutpatientScreening);
        var indicators = MakeDummyIndicators(encounter.Id);
        encounter.AttachClinicalIndicators(indicators);

        encounter.Sign();

        encounter.Status.Should().Be(EncounterStatus.Signed);
    }

    [Fact]
    public void Sign_WithoutIndicators_ShouldThrowInvalidEntityStateException()
    {
        var encounter = ClinicalEncounter.Make(Guid.NewGuid(), Guid.NewGuid(), "ENC-1005", EncounterType.OutpatientScreening);

        var act = () => encounter.Sign();

        act.Should().Throw<InvalidEntityStateException>()
            .WithMessage("*without clinical indicators*");
    }

    [Fact]
    public void Cancel_WhenInProgress_ShouldTransitionToCancelled()
    {
        var encounter = ClinicalEncounter.Make(Guid.NewGuid(), Guid.NewGuid(), "ENC-1006", EncounterType.OutpatientScreening);

        encounter.Cancel("Patient left before evaluation");

        encounter.Status.Should().Be(EncounterStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadySigned_ShouldThrow()
    {
        var encounter = ClinicalEncounter.Make(Guid.NewGuid(), Guid.NewGuid(), "ENC-1007", EncounterType.OutpatientScreening);
        encounter.AttachClinicalIndicators(MakeDummyIndicators(encounter.Id));
        encounter.Sign();

        var act = () => encounter.Cancel("Attempted cancellation after sign-off");

        act.Should().Throw<InvalidEntityStateException>()
            .WithMessage("*Cannot cancel a signed encounter*");
    }
}

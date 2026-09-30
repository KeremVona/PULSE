using System.Text.Json;
using FluentAssertions;
using HeartCDSS.Application.Common.Models;
using HeartCDSS.Application.Encounters.Commands.MakeEncounter;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.ValueObjects;
using Xunit;

namespace HeartCDSS.Domain.Tests.Infrastructure;

public class MlInferenceDtosTests
{
    private static ClinicalIndicators MakeSampleDomainIndicators(Guid encounterId)
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

    private static ClinicalIndicatorsInputDto MakeSampleInputDto()
    {
        return new ClinicalIndicatorsInputDto(
            Age: 62,
            Sex: 0,
            ChestPainType: 3,
            RestingBp: 130,
            DiastolicBp: 80,
            SerumCholesterol: 240,
            FastingBloodSugar: 0,
            RestingEcg: 0,
            MaxHeartRate: 155,
            ExerciseAngina: 0,
            OldpeakStDepression: 1.2m,
            Slope: 1,
            FluoroscopyVessels: 1,
            Thalassemia: 3
        );
    }

    [Fact]
    public void MakeFromDomain_ShouldMapAllClinicalPropertiesAccurately()
    {
        // Arrange
        var madeEncounterId = Guid.NewGuid();
        var madeDomainIndicators = MakeSampleDomainIndicators(madeEncounterId);

        // Act
        var madeDto = MlIndicatorsPayloadDto.MakeFromDomain(madeDomainIndicators);

        // Assert
        madeDto.Age.Should().Be(58);
        madeDto.Sex.Should().Be(1);
        madeDto.Cp.Should().Be(4);
        madeDto.Trestbps.Should().Be(145);
        madeDto.Chol.Should().Be(268);
        madeDto.Fbs.Should().Be(1);
        madeDto.Restecg.Should().Be(1);
        madeDto.Thalach.Should().Be(115);
        madeDto.Exang.Should().Be(1);
        madeDto.Oldpeak.Should().Be(2.8);
        madeDto.Slope.Should().Be(2);
        madeDto.Ca.Should().Be(2);
        madeDto.Thal.Should().Be(7);
    }

    [Fact]
    public void MakeFromInputDto_ShouldMapAllFieldsAccurately()
    {
        // Arrange
        var madeInput = MakeSampleInputDto();

        // Act
        var madeDto = MlIndicatorsPayloadDto.MakeFromInputDto(madeInput);

        // Assert
        madeDto.Age.Should().Be(62);
        madeDto.Sex.Should().Be(0);
        madeDto.Cp.Should().Be(3);
        madeDto.Trestbps.Should().Be(130);
        madeDto.Chol.Should().Be(240);
        madeDto.Fbs.Should().Be(0);
        madeDto.Restecg.Should().Be(0);
        madeDto.Thalach.Should().Be(155);
        madeDto.Exang.Should().Be(0);
        madeDto.Oldpeak.Should().Be(1.2);
        madeDto.Slope.Should().Be(1);
        madeDto.Ca.Should().Be(1);
        madeDto.Thal.Should().Be(3);
    }

    [Fact]
    public void MlInferenceRequestDto_ShouldSerializeToFastApiSnakeCaseJson()
    {
        // Arrange
        var madeIndicators = MlIndicatorsPayloadDto.Make(
            age: 55,
            sex: 1,
            cp: 2,
            trestbps: 140,
            chol: 250,
            fbs: 1,
            restecg: 0,
            thalach: 160,
            exang: 0,
            oldpeak: 1.5,
            slope: 1,
            ca: 0,
            thal: 3
        );
        var madeRequest = MlInferenceRequestDto.Make(
            indicators: madeIndicators,
            encounterId: "enc-9876",
            correlationId: "trace-4321",
            patientMrn: "MRN-1122"
        );

        // Act
        var madeJson = JsonSerializer.Serialize(madeRequest);

        // Assert
        madeJson.Should().Contain("\"encounter_id\":\"enc-9876\"");
        madeJson.Should().Contain("\"correlation_id\":\"trace-4321\"");
        madeJson.Should().Contain("\"patient_mrn\":\"MRN-1122\"");
        madeJson.Should().Contain("\"clinical_indicators\":{");
        madeJson.Should().Contain("\"trestbps\":140");
        madeJson.Should().Contain("\"oldpeak\":1.5");
    }

    [Fact]
    public void MlInferenceResultDto_ShouldDeserializeFromFastApiResponseJson()
    {
        // Arrange
        var sampleResponseJson = """
        {
            "encounter_id": "8d39c094-0a37-4f6c-8a03-7cb529bfa5e1",
            "correlation_id": "trace-49a8123-bc94-432a",
            "risk_probability": 0.684,
            "risk_band": "Elevated",
            "is_cad_positive": true,
            "decision_threshold": 0.29,
            "confidence_interval": {
                "lower": 0.621,
                "upper": 0.74,
                "confidence_level": 0.95,
                "margin_percentage": 5.9
            },
            "top_risk_drivers": [
                {
                    "feature_key": "oldpeak",
                    "feature_label": "ST Depression (mm)",
                    "patient_value": "2.8 mm",
                    "shap_value": 0.142,
                    "impact_direction": "RISK_INCREASING",
                    "rank": 1
                }
            ],
            "top_protective_factors": [
                {
                    "feature_key": "thalach",
                    "feature_label": "Max Heart Rate (bpm)",
                    "patient_value": "165 bpm",
                    "shap_value": -0.085,
                    "impact_direction": "PROTECTIVE",
                    "rank": 1
                }
            ],
            "all_attributions": [],
            "explanations": [
                {
                    "feature_key": "oldpeak",
                    "feature_label": "ST Depression (mm)",
                    "patient_value": "2.8 mm",
                    "shap_value": 0.142,
                    "impact_direction": "RISK_INCREASING",
                    "rank": 1
                }
            ],
            "clinical_warnings": [
                "Critical Hypertension: Resting BP (210 mmHg) > 200 mmHg."
            ],
            "recommended_action": "Urgent cardiology referral for angiography evaluation.",
            "clinical_disclaimer": "Clinical Decision Support Notice: Screening aid only.",
            "inference_latency_ms": 14.8
        }
        """;

        // Act
        var madeResult = JsonSerializer.Deserialize<MlInferenceResultDto>(
            sampleResponseJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        // Assert
        madeResult.Should().NotBeNull();
        madeResult!.EncounterId.Should().Be("8d39c094-0a37-4f6c-8a03-7cb529bfa5e1");
        madeResult.CorrelationId.Should().Be("trace-49a8123-bc94-432a");
        madeResult.RiskProbability.Should().Be(0.684);
        madeResult.RiskBand.Should().Be("Elevated");
        madeResult.IsCadPositive.Should().BeTrue();
        madeResult.DecisionThreshold.Should().Be(0.29);
        madeResult.ConfidenceInterval.Lower.Should().Be(0.621);
        madeResult.ConfidenceInterval.Upper.Should().Be(0.74);
        madeResult.ConfidenceInterval.MarginPercentage.Should().Be(5.9);
        madeResult.TopRiskDrivers.Should().HaveCount(1);
        madeResult.TopRiskDrivers[0].FeatureKey.Should().Be("oldpeak");
        madeResult.TopRiskDrivers[0].ImpactDirection.Should().Be("RISK_INCREASING");
        madeResult.TopProtectiveFactors.Should().HaveCount(1);
        madeResult.ClinicalWarnings.Should().ContainSingle().Which.Should().Contain("Hypertension");
        madeResult.RecommendedAction.Should().Contain("Urgent cardiology referral");
        madeResult.InferenceLatencyMs.Should().Be(14.8);
    }

    [Fact]
    public void MakeRiskAssessmentResult_ShouldMapToDomainEntityAndAttributions()
    {
        // Arrange
        var madeEncounterId = Guid.NewGuid();
        var madeModelId = Guid.NewGuid();

        var madeResultDto = new MlInferenceResultDto
        {
            EncounterId = madeEncounterId.ToString(),
            CorrelationId = "trace-test",
            RiskProbability = 0.72,
            RiskBand = "High",
            IsCadPositive = true,
            DecisionThreshold = 0.29,
            ConfidenceInterval = MlConfidenceIntervalDto.Make(0.65, 0.79, 0.95, 7.0),
            RecommendedAction = "Immediate invasive catheterization.",
            ClinicalDisclaimer = "Legal clinical disclaimer text.",
            Explanations =
            [
                MlFeatureAttributionDto.Make("oldpeak", "ST Depression", "3.0 mm", 0.18, "RISK_INCREASING", 1),
                MlFeatureAttributionDto.Make("thalach", "Max HR", "170 bpm", -0.06, "PROTECTIVE", 2)
            ]
        };

        // Act
        var madeDomainResult = madeResultDto.MakeRiskAssessmentResult(madeEncounterId, madeModelId);

        // Assert
        madeDomainResult.EncounterId.Should().Be(madeEncounterId);
        madeDomainResult.ModelId.Should().Be(madeModelId);
        madeDomainResult.RiskProbability.Should().Be(0.72m);
        madeDomainResult.RiskBand.Should().Be(RiskBand.High);
        madeDomainResult.DecisionThreshold.Should().Be(0.29m);
        madeDomainResult.ConfidenceInterval.LowerBound.Should().Be(0.65m);
        madeDomainResult.ConfidenceInterval.UpperBound.Should().Be(0.79m);
        madeDomainResult.CdssAdvisoryText.Should().Be("Legal clinical disclaimer text.");
        madeDomainResult.RecommendedAction.Should().Be("Immediate invasive catheterization.");
        madeDomainResult.Attributions.Should().HaveCount(2);

        var firstAttr = madeDomainResult.Attributions.First(a => a.FeatureKey == "oldpeak");
        firstAttr.ImpactDirection.Should().Be(ImpactDirection.RiskIncreasing);
        firstAttr.AttributionValue.Should().Be(0.18m);

        var secondAttr = madeDomainResult.Attributions.First(a => a.FeatureKey == "thalach");
        secondAttr.ImpactDirection.Should().Be(ImpactDirection.Protective);
        secondAttr.AttributionValue.Should().Be(-0.06m);
    }
}

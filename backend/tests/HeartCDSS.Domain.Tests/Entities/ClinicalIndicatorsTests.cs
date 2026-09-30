using FluentAssertions;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Exceptions;
using HeartCDSS.Domain.ValueObjects;
using Xunit;

namespace HeartCDSS.Domain.Tests.Entities;

public class ClinicalIndicatorsTests
{
    [Fact]
    public void Make_WithValidParameters_ShouldInstantiate()
    {
        var encounterId = Guid.NewGuid();
        var bp = BloodPressure.Make(140, 85);
        var chol = CholesterolReading.Make(245, true);
        var ecg = EcgResult.Make(RestingEcgType.Normal, 135, false, 1.2m, StSlope.Flat);

        var indicators = ClinicalIndicators.Make(
            encounterId: encounterId,
            ageAtEncounter: 55,
            sex: 1,
            chestPainType: ChestPainType.TypicalAngina,
            bloodPressure: bp,
            cholesterol: chol,
            ecg: ecg,
            fluoroscopyVessels: 1,
            thalassemia: ThalassemiaType.Normal
        );

        indicators.EncounterId.Should().Be(encounterId);
        indicators.AgeAtEncounter.Should().Be(55);
        indicators.Sex.Should().Be(1);
        indicators.ChestPainType.Should().Be(ChestPainType.TypicalAngina);
        indicators.BloodPressure.Should().Be(bp);
        indicators.Cholesterol.Should().Be(chol);
        indicators.Ecg.Should().Be(ecg);
        indicators.FluoroscopyVessels.Should().Be(1);
        indicators.Thalassemia.Should().Be(ThalassemiaType.Normal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(111)]
    public void Make_WithInvalidAge_ShouldThrow(int invalidAge)
    {
        var act = () => ClinicalIndicators.Make(
            Guid.NewGuid(),
            invalidAge,
            1,
            ChestPainType.TypicalAngina,
            BloodPressure.Make(120),
            CholesterolReading.Make(200, false),
            EcgResult.Make(RestingEcgType.Normal, 120, false, 0.0m, StSlope.Upsloping),
            0,
            ThalassemiaType.Normal
        );

        act.Should().Throw<InvalidPhysiologicalValueException>()
            .WithMessage("*AgeAtEncounter*");
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    public void Make_WithInvalidSex_ShouldThrow(int invalidSex)
    {
        var act = () => ClinicalIndicators.Make(
            Guid.NewGuid(),
            50,
            invalidSex,
            ChestPainType.TypicalAngina,
            BloodPressure.Make(120),
            CholesterolReading.Make(200, false),
            EcgResult.Make(RestingEcgType.Normal, 120, false, 0.0m, StSlope.Upsloping),
            0,
            ThalassemiaType.Normal
        );

        act.Should().Throw<InvalidPhysiologicalValueException>()
            .WithMessage("*Sex*");
    }

    [Theory]
    [InlineData(4)]
    [InlineData(-1)]
    public void Make_WithInvalidFluoroscopyVessels_ShouldThrow(int invalidVessels)
    {
        var act = () => ClinicalIndicators.Make(
            Guid.NewGuid(),
            50,
            1,
            ChestPainType.TypicalAngina,
            BloodPressure.Make(120),
            CholesterolReading.Make(200, false),
            EcgResult.Make(RestingEcgType.Normal, 120, false, 0.0m, StSlope.Upsloping),
            invalidVessels,
            ThalassemiaType.Normal
        );

        act.Should().Throw<InvalidPhysiologicalValueException>()
            .WithMessage("*FluoroscopyVessels*");
    }

    [Fact]
    public void GetClinicalWarnings_ShouldGenerateExpectedAlerts()
    {
        var bp = BloodPressure.Make(210); // Critical hypertension
        var chol = CholesterolReading.Make(450, true); // Severe hypercholesterolemia & diabetic
        var ecg = EcgResult.Make(RestingEcgType.Normal, 215, true, 4.0m, StSlope.Downsloping); // Tachycardia & Severe ST depression

        var indicators = ClinicalIndicators.Make(
            Guid.NewGuid(),
            98, // Age alert
            1,
            ChestPainType.Asymptomatic,
            bp,
            chol,
            ecg,
            2, // Multi-vessel
            ThalassemiaType.ReversibleDefect
        );

        var warnings = indicators.GetClinicalWarnings();

        warnings.Should().Contain(w => w.Contains("Critical Hypertension"));
        warnings.Should().Contain(w => w.Contains("Severe Hypercholesterolemia"));
        warnings.Should().Contain(w => w.Contains("Extreme Tachycardia"));
        warnings.Should().Contain(w => w.Contains("Severe ST Depression"));
        warnings.Should().Contain(w => w.Contains("Multi-Vessel Calcification"));
        warnings.Should().Contain(w => w.Contains("Diabetic Risk Factor"));
        warnings.Should().Contain(w => w.Contains("Age Alert"));
    }
}

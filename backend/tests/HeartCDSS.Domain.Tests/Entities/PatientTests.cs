using FluentAssertions;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Exceptions;
using Xunit;

namespace HeartCDSS.Domain.Tests.Entities;

public class PatientTests
{
    [Fact]
    public void Make_WithValidParameters_ShouldInstantiatePatient()
    {
        var dob = new DateOnly(1968, 5, 14);
        var patient = Patient.Make(
            mrn: "MRN-123456",
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: dob,
            biologicalSex: BiologicalSex.Male,
            phoneNumber: "+1-555-0199"
        );

        patient.Id.Should().NotBeEmpty();
        patient.Mrn.Should().Be("MRN-123456");
        patient.FirstName.Should().Be("John");
        patient.LastName.Should().Be("Doe");
        patient.DateOfBirth.Should().Be(dob);
        patient.BiologicalSex.Should().Be(BiologicalSex.Male);
        patient.PhoneNumber.Should().Be("+1-555-0199");
        patient.Status.Should().Be(PatientStatus.Active);
        patient.Encounters.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "John", "Doe")]
    [InlineData("MRN-1", "", "Doe")]
    [InlineData("MRN-1", "John", "")]
    public void Make_WithMissingRequiredFields_ShouldThrowDomainException(string mrn, string first, string last)
    {
        var act = () => Patient.Make(mrn, first, last, new DateOnly(1980, 1, 1), BiologicalSex.Female);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Make_WithFutureDateOfBirth_ShouldThrowDomainException()
    {
        var futureDob = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var act = () => Patient.Make("MRN-FUTURE", "Baby", "Doe", futureDob, BiologicalSex.Female);

        act.Should().Throw<DomainException>()
            .WithMessage("*cannot be in the future*");
    }

    [Fact]
    public void CalculateAge_ShouldAccuratelyComputeAge()
    {
        var dob = new DateOnly(1970, 6, 15);
        var patient = Patient.Make("MRN-AGE", "Alice", "Smith", dob, BiologicalSex.Female);

        // Target before birthday in 2026
        patient.CalculateAge(new DateOnly(2026, 6, 14)).Should().Be(55);

        // Target on birthday in 2026
        patient.CalculateAge(new DateOnly(2026, 6, 15)).Should().Be(56);

        // Target after birthday in 2026
        patient.CalculateAge(new DateOnly(2026, 6, 16)).Should().Be(56);
    }

    [Fact]
    public void UpdateDemographics_ShouldModifyDetailsAndTimestamp()
    {
        var patient = Patient.Make("MRN-UPDATE", "Alice", "Smith", new DateOnly(1990, 1, 1), BiologicalSex.Female);
        var originalUpdate = patient.UpdatedAt;

        patient.UpdateDemographics("Alicia", "Johnson", "+1-555-9999", "Bob Johnson", "+1-555-8888");

        patient.FirstName.Should().Be("Alicia");
        patient.LastName.Should().Be("Johnson");
        patient.PhoneNumber.Should().Be("+1-555-9999");
        patient.EmergencyContactName.Should().Be("Bob Johnson");
        patient.UpdatedAt.Should().BeOnOrAfter(originalUpdate);
    }

    [Fact]
    public void UpdateStatus_ToDeceasedAndAttemptReactivation_ShouldThrow()
    {
        var patient = Patient.Make("MRN-DEC", "Jane", "Doe", new DateOnly(1950, 1, 1), BiologicalSex.Female);
        patient.UpdateStatus(PatientStatus.Deceased);

        var act = () => patient.UpdateStatus(PatientStatus.Active);
        act.Should().Throw<InvalidEntityStateException>()
            .WithMessage("*Cannot reactivate a deceased patient*");
    }

    [Fact]
    public void AddEncounter_WithMismatchedPatientId_ShouldThrow()
    {
        var patient = Patient.Make("MRN-ENC", "Jane", "Doe", new DateOnly(1985, 1, 1), BiologicalSex.Female);
        var wrongPatientId = Guid.NewGuid();
        var encounter = ClinicalEncounter.Make(wrongPatientId, Guid.NewGuid(), "ENC-001", EncounterType.OutpatientScreening);

        var act = () => patient.AddEncounter(encounter);
        act.Should().Throw<DomainException>()
            .WithMessage("*does not match patient*");
    }
}

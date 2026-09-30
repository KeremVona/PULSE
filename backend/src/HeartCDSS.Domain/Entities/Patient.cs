using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Exceptions;

namespace HeartCDSS.Domain.Entities;

/// <summary>
/// Patient Aggregate Root managing demographics, medical record identity, and encounter history.
/// </summary>
public sealed class Patient : AggregateRoot<Guid>
{
    private readonly List<ClinicalEncounter> _encounters = [];

    public string Mrn { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateOnly DateOfBirth { get; private set; }
    public BiologicalSex BiologicalSex { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? EmergencyContactName { get; private set; }
    public string? EmergencyContactPhone { get; private set; }
    public PatientStatus Status { get; private set; } = PatientStatus.Active;

    public IReadOnlyCollection<ClinicalEncounter> Encounters => _encounters.AsReadOnly();

    // Required for ORM reflection
    private Patient() { }

    private Patient(
        Guid id,
        string mrn,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        BiologicalSex biologicalSex,
        string? phoneNumber,
        string? emergencyContactName,
        string? emergencyContactPhone)
        : base(id)
    {
        Mrn = mrn;
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        BiologicalSex = biologicalSex;
        PhoneNumber = phoneNumber;
        EmergencyContactName = emergencyContactName;
        EmergencyContactPhone = emergencyContactPhone;
        Status = PatientStatus.Active;
    }

    /// <summary>
    /// Factory method instantiating a validated Patient aggregate.
    /// </summary>
    public static Patient Make(
        string mrn,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        BiologicalSex biologicalSex,
        string? phoneNumber = null,
        string? emergencyContactName = null,
        string? emergencyContactPhone = null,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(mrn))
            throw new DomainException("Medical Record Number (MRN) cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("Patient first name cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Patient last name cannot be null or empty.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dateOfBirth > today)
            throw new DomainException($"Patient date of birth ({dateOfBirth}) cannot be in the future.");

        if (!Enum.IsDefined(typeof(BiologicalSex), biologicalSex))
            throw new DomainException($"Invalid biological sex code: {biologicalSex}");

        return new Patient(
            id ?? Guid.NewGuid(),
            mrn.Trim().ToUpperInvariant(),
            firstName.Trim(),
            lastName.Trim(),
            dateOfBirth,
            biologicalSex,
            phoneNumber?.Trim(),
            emergencyContactName?.Trim(),
            emergencyContactPhone?.Trim()
        );
    }

    public int CalculateAge(DateOnly? asOfDate = null)
    {
        var targetDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var age = targetDate.Year - DateOfBirth.Year;
        if (DateOfBirth > targetDate.AddYears(-age))
            age--;
        return Math.Max(0, age);
    }

    public void UpdateDemographics(
        string firstName,
        string lastName,
        string? phoneNumber,
        string? emergencyContactName,
        string? emergencyContactPhone)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name cannot be empty.");

        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name cannot be empty.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber?.Trim();
        EmergencyContactName = emergencyContactName?.Trim();
        EmergencyContactPhone = emergencyContactPhone?.Trim();
        SetUpdatedAt();
    }

    public void UpdateStatus(PatientStatus newStatus)
    {
        if (Status == PatientStatus.Deceased && newStatus != PatientStatus.Deceased)
            throw new InvalidEntityStateException(nameof(Patient), Id, nameof(UpdateStatus), "Cannot reactivate a deceased patient record.");

        Status = newStatus;
        SetUpdatedAt();
    }

    public void AddEncounter(ClinicalEncounter encounter)
    {
        ArgumentNullException.ThrowIfNull(encounter);
        if (encounter.PatientId != Id)
            throw new DomainException($"Encounter patient ID ({encounter.PatientId}) does not match patient ({Id}).");

        _encounters.Add(encounter);
        SetUpdatedAt();
    }
}

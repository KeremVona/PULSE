using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Exceptions;

namespace HeartCDSS.Domain.Entities;

/// <summary>
/// Domain entity representing authenticated clinical personnel, physicians, and administrative users.
/// </summary>
public sealed class User : AggregateRoot<Guid>
{
    private readonly List<Role> _roles = [];

    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? MedicalLicenseNumber { get; private set; }
    public string? Department { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();

    private User() { }

    private User(
        Guid id,
        string username,
        string email,
        string passwordHash,
        string fullName,
        string? medicalLicenseNumber,
        string? department,
        bool isActive)
        : base(id)
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        MedicalLicenseNumber = medicalLicenseNumber;
        Department = department;
        IsActive = isActive;
    }

    public static User Make(
        string username,
        string email,
        string passwordHash,
        string fullName,
        string? medicalLicenseNumber = null,
        string? department = null,
        bool isActive = true,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Username cannot be empty.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("A valid email address is required.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash cannot be empty.");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Full name cannot be empty.");

        return new User(
            id ?? Guid.NewGuid(),
            username.Trim().ToLowerInvariant(),
            email.Trim().ToLowerInvariant(),
            passwordHash,
            fullName.Trim(),
            medicalLicenseNumber?.Trim(),
            department?.Trim(),
            isActive
        );
    }

    public void AddRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        if (!_roles.Any(r => r.Id == role.Id || r.RoleName == role.RoleName))
        {
            _roles.Add(role);
            SetUpdatedAt();
        }
    }

    public void RemoveRole(Guid roleId)
    {
        var existing = _roles.FirstOrDefault(r => r.Id == roleId);
        if (existing is not null)
        {
            _roles.Remove(existing);
            SetUpdatedAt();
        }
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdatedAt();
    }
}

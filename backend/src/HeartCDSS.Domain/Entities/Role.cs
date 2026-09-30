using HeartCDSS.Domain.Common;
using HeartCDSS.Domain.Exceptions;

namespace HeartCDSS.Domain.Entities;

/// <summary>
/// Domain entity representing an authorization role in the clinical system.
/// </summary>
public sealed class Role : Entity<Guid>
{
    public const string Physician = "Physician";
    public const string ClinicalStaff = "ClinicalStaff";
    public const string Admin = "Admin";
    public const string Auditor = "Auditor";

    public string RoleName { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Role() { }

    public Role(Guid id, string roleName, string? description = null) : base(id)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            throw new DomainException("Role name cannot be empty.");

        RoleName = roleName.Trim();
        Description = description?.Trim();
    }
}

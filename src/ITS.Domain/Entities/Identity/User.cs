using ITS.Domain.Common;

namespace ITS.Domain.Entities.Identity;

public class User : AuditableEntity<Guid>
{
    private User() { }

    public static User ProvisionFromAd(
        string adObjectId,
        string userPrincipalName,
        string email,
        string displayName,
        string? firstName,
        string? lastName,
        int? departmentId,
        string? jobTitle,
        string? phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adObjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrincipalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new User
        {
            Id = Guid.CreateVersion7(),
            AdObjectId = adObjectId,
            UserPrincipalName = userPrincipalName.ToLowerInvariant(),
            Email = email.ToLowerInvariant(),
            DisplayName = displayName,
            FirstName = firstName,
            LastName = lastName,
            DepartmentId = departmentId,
            JobTitle = jobTitle,
            PhoneNumber = phoneNumber,
            IsActive = true,
            TimeZoneId = "UTC",
            Locale = "en-US"
        };
    }

    public string AdObjectId { get; private set; } = default!;
    public string UserPrincipalName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public int? DepartmentId { get; private set; }
    public Guid? ManagerUserId { get; private set; }
    public string? JobTitle { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? AvatarUrl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? LastAdSyncAt { get; private set; }
    public string TimeZoneId { get; private set; } = "UTC";
    public string Locale { get; private set; } = "en-US";

    // Navigation properties
    public Department? Department { get; private set; }
    public User? Manager { get; private set; }
    public ICollection<UserGlobalRole> GlobalRoles { get; private set; } = [];

    public void SyncFromAd(
        string userPrincipalName,
        string email,
        string displayName,
        string? firstName,
        string? lastName,
        int? departmentId,
        Guid? managerUserId,
        string? jobTitle,
        string? phoneNumber,
        bool isActive,
        DateTime syncedAt)
    {
        UserPrincipalName = userPrincipalName.ToLowerInvariant();
        Email = email.ToLowerInvariant();
        DisplayName = displayName;
        FirstName = firstName;
        LastName = lastName;
        DepartmentId = departmentId;
        ManagerUserId = managerUserId;
        JobTitle = jobTitle;
        PhoneNumber = phoneNumber;
        IsActive = isActive;
        LastAdSyncAt = syncedAt;
    }

    public void UpdatePreferences(string timeZoneId, string locale)
    {
        TimeZoneId = timeZoneId;
        Locale = locale;
    }

    public void UpdateAvatar(string? avatarUrl)
        => AvatarUrl = avatarUrl;

    public void RecordLogin(DateTime loginAt)
        => LastLoginAt = loginAt;

    public void Deactivate()
        => IsActive = false;

    public void Activate()
        => IsActive = true;
}

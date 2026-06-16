namespace ITS.Domain.Enums;

public enum AuditOperation
{
    Login = 1,
    Logout = 2,
    LoginFailed = 3,
    Create = 4,
    Update = 5,
    Delete = 6,
    Restore = 7,
    Archive = 8,
    PermissionGrant = 9,
    PermissionRevoke = 10,
    WorkflowTransition = 11,
    Export = 12,
    AdminAction = 13,
    AdSync = 14
}

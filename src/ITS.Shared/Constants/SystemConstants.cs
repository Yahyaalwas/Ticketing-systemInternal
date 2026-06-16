namespace ITS.Shared.Constants;

public static class SystemConstants
{
    public static class Pagination
    {
        public const int DefaultPageSize = 25;
        public const int MaxPageSize = 100;
    }

    public static class Tickets
    {
        public const int MaxTitleLength = 500;
        public const int MaxDescriptionLength = 65536;
        public const int MaxLabelsPerTicket = 20;
    }

    public static class Attachments
    {
        public const long DefaultMaxFileSizeBytes = 25L * 1024 * 1024;
        public const int DefaultRetentionDays = 90;
    }

    public static class Comments
    {
        public const int MaxBodyLength = 65536;
        public const int MaxReplyDepth = 1;
    }

    public static class Projects
    {
        public const int MaxKeyLength = 10;
        public const string KeyPattern = @"^[A-Z0-9]{2,10}$";
    }

    public static class Jwt
    {
        public const int DefaultExpiryHours = 8;
        public const int MinKeyLength = 32;
    }

    public static class AdSync
    {
        public const int DefaultIntervalMinutes = 15;
        public const int MaxRetries = 3;
    }

    // Sentinel user ID for system/background job operations
    public static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");
}

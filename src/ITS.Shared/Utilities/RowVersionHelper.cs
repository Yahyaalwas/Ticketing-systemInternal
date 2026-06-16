namespace ITS.Shared.Utilities;

public static class RowVersionHelper
{
    public static string ToETag(byte[] rowVersion)
        => $"\"{Convert.ToBase64String(rowVersion)}\"";

    public static byte[] FromETag(string etag)
        => Convert.FromBase64String(etag.Trim('"'));

    public static bool TryFromETag(string? etag, out byte[] rowVersion)
    {
        rowVersion = [];
        if (string.IsNullOrWhiteSpace(etag)) return false;
        try
        {
            rowVersion = Convert.FromBase64String(etag.Trim('"'));
            return rowVersion.Length == 8; // SQL Server ROWVERSION is always 8 bytes
        }
        catch
        {
            return false;
        }
    }
}

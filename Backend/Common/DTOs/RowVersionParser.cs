namespace Common.DTOs;

public static class RowVersionParser
{
    public static bool TryParse(string? value, out byte[] rowVersion)
    {
        rowVersion = [];
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            rowVersion = Convert.FromBase64String(value);
            return rowVersion.Length == 8; 
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
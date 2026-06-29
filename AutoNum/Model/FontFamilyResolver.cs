using System.Diagnostics;
using System.Drawing;

namespace AutoNumber.Model;

internal static class FontFamilyResolver
{
    public static FontFamily Resolve(string? familyName, FontFamily fallback)
    {
        if (string.IsNullOrWhiteSpace(familyName))
        {
            return fallback;
        }

        try
        {
            return new FontFamily(familyName);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Failed to restore font family '{familyName}', using fallback '{fallback.Name}': {ex.Message}");
            return fallback;
        }
    }
}

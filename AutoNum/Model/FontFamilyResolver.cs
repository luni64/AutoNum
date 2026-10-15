using AutoNumber.Infrastructure;
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
            AppLog.Warn($"Font family '{familyName}' is not available on this system, using '{fallback.Name}' instead: {ex.Message}");
            return fallback;
        }
    }
}

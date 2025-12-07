using System.Globalization;

internal static class StringExtensions
{
    internal static bool TryParseScpiBool(this string s, out bool value)
    {
        if (s is null)
        {
            value = default;
            return false;
        }

        var t = s.Trim().ToUpperInvariant();

        switch (t)
        {
            case "1":
            case "ON":
            case "TRUE":
                value = true;
                return true;

            case "0":
            case "OFF":
            case "FALSE":
                value = false;
                return true;

            default:
                value = default;
                return false;
        }
    }

    internal static T ConvertScpiTo<T>(this string input)
    {
        if (typeof(T) == typeof(string))
            return (T)(object)input;

        if (typeof(T) == typeof(bool) && input.TryParseScpiBool(out var b))
            return (T)(object)b;
        
        return (T)Convert.ChangeType(input, typeof(T), CultureInfo.InvariantCulture)!;
    }
}

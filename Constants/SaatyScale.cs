namespace SupplierSelectionDSS.Constants;

public static class SaatyScale
{
    public readonly record struct Item(
        string Code,
        decimal DirectValue,
        decimal ReciprocalValue,
        string ReciprocalCode,
        string Intensity,
        string Description);

    public static readonly IReadOnlyList<Item> Entries = new List<Item>
    {
        new("1/9", 0.1111m, 9.0000m, "9",   "Extremely",                   "Extreme preference for column criterion"),
        new("1/8", 0.1250m, 8.0000m, "8",   "Very strongly to extremely",  "Very strong to extreme preference for column criterion"),
        new("1/7", 0.1429m, 7.0000m, "7",   "Very strongly",               "Very strong preference for column criterion"),
        new("1/6", 0.1667m, 6.0000m, "6",   "Strongly to very strongly",   "Strong to very strong preference for column criterion"),
        new("1/5", 0.2000m, 5.0000m, "5",   "Strongly",                    "Strong preference for column criterion"),
        new("1/4", 0.2500m, 4.0000m, "4",   "Moderately to strongly",      "Moderate to strong preference for column criterion"),
        new("1/3", 0.3333m, 3.0000m, "3",   "Moderately",                  "Moderate preference for column criterion"),
        new("1/2", 0.5000m, 2.0000m, "2",   "Equally to moderately",       "Equal to moderate preference for column criterion"),
        new("1",   1.0000m, 1.0000m, "1",   "Equally",                     "Equal importance"),
        new("2",   2.0000m, 0.5000m, "1/2", "Equally to moderately",       "Equal to moderate preference for row criterion"),
        new("3",   3.0000m, 0.3333m, "1/3", "Moderately",                  "Moderate preference for row criterion"),
        new("4",   4.0000m, 0.2500m, "1/4", "Moderately to strongly",      "Moderate to strong preference for row criterion"),
        new("5",   5.0000m, 0.2000m, "1/5", "Strongly",                    "Strong preference for row criterion"),
        new("6",   6.0000m, 0.1667m, "1/6", "Strongly to very strongly",   "Strong to very strong preference for row criterion"),
        new("7",   7.0000m, 0.1429m, "1/7", "Very strongly",               "Very strong preference for row criterion"),
        new("8",   8.0000m, 0.1250m, "1/8", "Very strongly to extremely",  "Very strong to extreme preference for row criterion"),
        new("9",   9.0000m, 0.1111m, "1/9", "Extremely",                   "Extreme preference for row criterion")
    }.AsReadOnly();

    private static readonly Dictionary<string, Item> ByCode =
        Entries.ToDictionary(e => e.Code, StringComparer.OrdinalIgnoreCase);

    public static readonly HashSet<decimal> AllowedDecimals =
        Entries.Select(e => e.DirectValue).ToHashSet();

    public static bool TryParse(string? code, out Item item)
    {
        item = default;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = code.Trim();
        return ByCode.TryGetValue(normalized, out item);
    }

    public static bool TryFromDecimal(decimal value, out Item item)
    {
        var rounded = Math.Round(value, 4, MidpointRounding.AwayFromZero);
        foreach (var entry in Entries)
        {
            if (Math.Abs(entry.DirectValue - rounded) <= 0.0002m)
            {
                item = entry;
                return true;
            }
        }

        item = default;
        return false;
    }

    public static decimal GetReciprocalDecimal(decimal directValue)
    {
        if (TryFromDecimal(directValue, out var item))
        {
            return item.ReciprocalValue;
        }

        return Math.Round(1m / directValue, 4, MidpointRounding.AwayFromZero);
    }

    public static string GetReciprocalCode(string code)
    {
        if (TryParse(code, out var item))
        {
            return item.ReciprocalCode;
        }

        return "—";
    }

    public static string FormatCanonicalFraction(decimal value)
    {
        if (TryFromDecimal(value, out var item))
        {
            return item.Code;
        }

        return value.ToString("0.####");
    }

    public static string GetComparisonExplanation(string rowName, string colName, string code)
    {
        if (!TryParse(code, out var item))
        {
            return string.Empty;
        }

        if (item.Code == "1")
        {
            return $"'{rowName}' and '{colName}' are equally important (1)";
        }

        if (item.DirectValue > 1m)
        {
            return $"'{rowName}' is {item.Intensity.ToLowerInvariant()} more important than '{colName}' ({item.Code})";
        }

        return $"'{colName}' is {item.Intensity.ToLowerInvariant()} more important than '{rowName}' ({item.ReciprocalCode})";
    }
}

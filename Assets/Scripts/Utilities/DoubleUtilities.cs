using System;

public static class DoubleUtilities
{
    private static readonly string[] abbreviations = {
        "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc"
    };

    public static string ToIdleNotation(double value)
    {
        if (value < 1000)
            return Math.Round(value).ToString("N0", System.Globalization.CultureInfo.CurrentCulture);

        double tValue = value;
        int abbreviationIndex = -1;

        while (tValue >= 1000 && abbreviationIndex < abbreviations.Length - 1)
        {
            tValue /= 1000;
            abbreviationIndex++;
        }

        if (abbreviationIndex == -1)
            return value.ToString("N0", System.Globalization.CultureInfo.CurrentCulture);

        else if (abbreviationIndex >= abbreviations.Length)
            return ToScientificNotation(value);

        string abbreviation = abbreviations[abbreviationIndex];
        return $"{tValue:F1}{abbreviation}";
    }

    public static string ToScientificNotation(double value)
    {
        int exponent = 0;
        double tValue = value;

        if (value < 10)
            return value.ToString("F2");

        while (tValue >= 10)
        {
            tValue /= 10;
            exponent++;
        }

        return tValue.ToString("F2") + "e" + exponent;
    }

    /// <summary>
    /// Display total time in HH:MM:SS format.
    /// </summary>
    public static string UpdateTime(int totalSeconds)
    {
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        // Show only minutes and seconds (e.g., 05:23)
        if (hours == 0)
            return $"{minutes:D2}:{seconds:D2}";

        // Show only hours and minutes (e.g., 02:45)
        return $"{hours:D2}:{minutes:D2}";
    }
}

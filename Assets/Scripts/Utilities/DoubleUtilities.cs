using System;

public static class DoubleUtilities
{
    private static readonly string[] abbreviations = {
        "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc"
    };

    public static string ToIdleNotation(double value)
    {
        if (value < 1000)
            return Math.Round(value).ToString();

        double tValue = value;
        int abbreviationIndex = -1;

        while (tValue >= 1000 && abbreviationIndex < abbreviations.Length - 1)
        {
            tValue /= 1000;
            abbreviationIndex++;
        }

        // Nếu vượt quá giới hạn abbreviation có sẵn thì dùng ScientificNotation
        if (abbreviationIndex == -1)
            return value.ToString();

        else if (abbreviationIndex >= abbreviations.Length)
            return ToScientificNotation(value);

        string abbreviation = abbreviations[abbreviationIndex];
        return tValue.ToString("F2") + abbreviation;
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
}

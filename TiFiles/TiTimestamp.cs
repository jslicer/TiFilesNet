// <copyright file="TiTimestamp.cs" company="Always Elucidated Solution Pioneers, LLC">
// Copyright (c) Always Elucidated Solution Pioneers, LLC. All rights reserved.
// </copyright>

namespace TiFiles;

/// <summary>
/// Methods to extract timestamps from TIFILES headers.
/// </summary>
internal static class TiTimestamp
{
    private const int HourShift = 11;

    private const int MinuteShift = 5;

    private const int YearShift = 9;

    private const int MonthShift = 5;

    private const int HourMask = 0x1F;

    private const int MinuteMask = 0x3F;

    private const int SecondMask = 0x1F;

    private const int YearMask = 0x7F;

    private const int MonthMask = 0x0F;

    private const int DayMask = 0x1F;

    private const int SecondsPerStoredUnit = 2;

    private const int LegacyYearThreshold = 70;

    private const int LegacyCenturyBaseYear = 1900;

    private const int CurrentCenturyBaseYear = 2000;

    private const int MinimumSupportedYear = 1970;

    private const int MaximumSupportedYear = 2069;

    private const int MaximumHour = 23;

    private const int MaximumMinuteOrSecond = 59;

    private const int MinimumMonthOrDay = 1;

    private const int MaximumMonth = 12;

    /// <summary>
    /// Decodes the specified TIFILES timestamp words into a date/time value.
    /// </summary>
    /// <param name="timeWord">The time word.</param>
    /// <param name="dateWord">The date word.</param>
    /// <returns>The date/time value.</returns>
    //// ReSharper disable once MethodTooLong
    public static DateTime? Decode(ushort timeWord, ushort dateWord)
    {
        // ReSharper disable once ComplexConditionExpression
        if (timeWord == 0 && dateWord == 0)
        {
            return null;
        }

        int hour = (timeWord >> HourShift) & HourMask;
        int minute = (timeWord >> MinuteShift) & MinuteMask;
        int second = (timeWord & SecondMask) * SecondsPerStoredUnit;
        int yearValue = (dateWord >> YearShift) & YearMask;
        int month = (dateWord >> MonthShift) & MonthMask;
        int day = dateWord & DayMask;

        // ReSharper disable once ComplexConditionExpression
        int year = yearValue >= LegacyYearThreshold
            ? LegacyCenturyBaseYear + yearValue
            : CurrentCenturyBaseYear + yearValue;

        // ReSharper disable once ComplexConditionExpression
        if (hour > MaximumHour
            || minute > MaximumMinuteOrSecond
            || second > MaximumMinuteOrSecond
            || month is < MinimumMonthOrDay or > MaximumMonth
            || day < MinimumMonthOrDay)
        {
            return null;
        }

        try
        {
            // ReSharper disable once RedundantArgument
            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Unspecified);
        }
        //// ReSharper disable once UncatchableException
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>
    /// Encodes the specified date/time value into TIFILES timestamp words.
    /// </summary>
    /// <param name="value">The date/time value.</param>
    /// <returns>The TIFILES timestamp words.</returns>
    /// <exception cref="ArgumentOutOfRangeException">value - TIFILES timestamps support years 1970 through
    /// 2069.</exception>
    public static (ushort TimeWord, ushort DateWord) Encode(DateTime? value)
    {
        if (value is null)
        {
            return (0, 0);
        }

        DateTime dateTime = (DateTime)value;

        if (dateTime.Year is < MinimumSupportedYear or > MaximumSupportedYear)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "TIFILES timestamps support years 1970 through 2069.");
        }

        // ReSharper disable once ComplexConditionExpression
        int yearValue = dateTime.Year >= CurrentCenturyBaseYear
            ? dateTime.Year - CurrentCenturyBaseYear
            : dateTime.Year - LegacyCenturyBaseYear;

        // ReSharper disable once ComplexConditionExpression
        ushort timeWord = (ushort)((dateTime.Hour << HourShift)
            | (dateTime.Minute << MinuteShift)
            | (dateTime.Second / SecondsPerStoredUnit));

        // ReSharper disable once ComplexConditionExpression
        ushort dateWord = (ushort)((yearValue << YearShift)
            | (dateTime.Month << MonthShift)
            | dateTime.Day);

        return (timeWord, dateWord);
    }
}
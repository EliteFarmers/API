﻿using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Utilities;

public static class FormatUtils
{
    public static long GetTimeFromContestKey(string contestKey)
    {
        var split = contestKey.Split(":");
        if (split.Length < 3) return 0;

        var year = int.Parse(split[0]);

        var monthDay = split[1].Split("_").Select(int.Parse).ToArray();
        if (monthDay.Length != 2) return 0;

        var month = monthDay[0] - 1;
        var day = monthDay[1] - 1;

        return GetTimeFromSkyblockDate(year, month, day);
    }

    /// <summary>
    /// Zero indexed year, month, and day
    /// </summary>
    /// <param name="skyblockYear"></param>
    /// <param name="skyblockMonth"></param>
    /// <param name="skyblockDay"></param>
    /// <returns></returns>
    public static long GetTimeFromSkyblockDate(int skyblockYear, int skyblockMonth, int skyblockDay)
    {
        var days = skyblockYear * 372 + skyblockMonth * 31 + skyblockDay;
        var seconds = days * 1200; // 1200 (60 * 20) seconds per day

        return SkyblockDate.SkyblockEpochSeconds + seconds;
    }

    public static SkyblockDate GetSkyblockDate(DateTime dateTime) => new(dateTime);

    public static string GetReadableSkyblockDate(DateTime dateTime) => GetSkyblockDate(dateTime).ToString();

    public static Crop? GetCropFromContestKey(string contestKey)
    {
        var split = contestKey.Split(":");
        if (split.Length < 3) return null;

        return GetCropFromItemId(split[2]);
    }

    public static Crop? GetCropFromItemId(string itemId) {
        return itemId.TryGetCrop(out var crop) ? crop : null;
    }

    [Pure]
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? str) => string.IsNullOrEmpty(str);
    [Pure]
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? str) => string.IsNullOrWhiteSpace(str);

    public static string GetFormattedCropName(Crop crop) => crop switch
    {
        Crop.Cactus => "Cactus",
        Crop.Carrot => "Carrot",
        Crop.CocoaBeans => "Cocoa Beans",
        Crop.Melon => "Melon",
        Crop.Mushroom => "Mushroom",
        Crop.NetherWart => "Nether Wart",
        Crop.Potato => "Potato",
        Crop.Pumpkin => "Pumpkin",
        Crop.SugarCane => "Sugar Cane",
        Crop.Wheat => "Wheat",
        _ => "Invalid Crop"
    };
    
    public static Crop? FormattedCropNameToCrop(string cropName) => cropName switch
    {
        "Cactus" => Crop.Cactus,
        "Carrot" => Crop.Carrot,
        "Cocoa Beans" => Crop.CocoaBeans,
        "Melon" => Crop.Melon,
        "Mushroom" => Crop.Mushroom,
        "Nether Wart" => Crop.NetherWart,
        "Potato" => Crop.Potato,
        "Pumpkin" => Crop.Pumpkin,
        "Sugar Cane" => Crop.SugarCane,
        "Wheat" => Crop.Wheat,
        _ => null
    };

    public static string? GetFormattedCropName(string itemId)
    {
        var crop = GetCropFromItemId(itemId);
        return crop is null ? null : GetFormattedCropName((Crop) crop);
    }

    public static string GetSkyblockMonthName(int month) => month switch
    {
        0 => "Early Spring",
        1 => "Spring",
        2 => "Late Spring",
        3 => "Early Summer",
        4 => "Summer",
        5 => "Late Summer",
        6 => "Early Autumn",
        7 => "Autumn",
        8 => "Late Autumn",
        9 => "Early Winter",
        10 => "Winter",
        11 => "Late Winter",
        _ => "Invalid Month"
    };

    public static string GetMedalName(ContestMedal medal) => medal switch
    {
        ContestMedal.Bronze => "bronze",
        ContestMedal.Silver => "silver",
        ContestMedal.Gold => "gold",
        ContestMedal.Platinum => "platinum",
        ContestMedal.Diamond => "diamond",
        _ => "none"
    };

    public static string AppendOrdinalSuffix(int number)
    {
        var j = number % 10;
        var k = number % 100;

        if (j == 1 && k != 11) return $"{number}st";
        if (j == 2 && k != 12) return $"{number}nd";
        if (j == 3 && k != 13) return $"{number}rd";

        return $"{number}th";
    }
}
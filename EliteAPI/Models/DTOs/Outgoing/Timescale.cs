﻿namespace EliteAPI.Models.DTOs.Outgoing; 

public class CollectionDataPointDto {
    public long Timestamp { get; set; }
    public long Value { get; set; }
}

public class CropCollectionsDataPointDto {
    public long Timestamp { get; set; }
    public string CropWeight { get; set; } = "0";
    public required Dictionary<string, long> Crops { get; set; }
    public required Dictionary<string, int> Pests { get; set; }
}

public class SkillDataPointDto {
    public long Timestamp { get; set; }
    public double Value { get; set; }
}

public class SkillsDataPointDto {
    public long Timestamp { get; set; }
    public required Dictionary<string, double> Skills { get; set; }
}
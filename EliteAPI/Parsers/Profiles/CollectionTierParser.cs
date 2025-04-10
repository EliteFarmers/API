﻿using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Parsers.Profiles;

public static class CollectionTierParser
{
    public static void ParseCollectionTiers(this ProfileMember member, string[]? collectionStrings)
    {
        member.CollectionTiers = ParseCollectionTiers(collectionStrings);
    }

    public static Dictionary<string, int> ParseCollectionTiers(string[]? collectionStrings)
    {
        var collections = new Dictionary<string, int>();

        if (collectionStrings is null) return collections;

        foreach (var collection in collectionStrings)
        {
            // Split at last underscore of multiple underscores
            var lastUnderscore = collection.LastIndexOf("_", StringComparison.Ordinal);

            var collectionType = collection[..lastUnderscore];
            var collectionTier = collection[(lastUnderscore + 1)..];

            if (!int.TryParse(collectionTier, out var tier)) continue;

            if (collections.ContainsKey(collectionType))
            {
                collections[collectionType] = Math.Max(collections[collectionType], tier);
                continue;
            }

            collections[collectionType] = tier;
        }

        return collections;
    }

    public static long GetCollection(this ProfileMember member, string collectionKey) {
        var collection = member.Collections.RootElement.TryGetProperty(collectionKey, out var value)
            ? value.GetInt64() 
            : 0;
        
        return collection;
    }

    public static bool TryGetCollection(this ProfileMember member, string collectionKey, out long value) {
        var found = member.Collections.RootElement.TryGetProperty(collectionKey, out var val);
        value = found ? val.GetInt64() : 0;
        return found;
    }
}
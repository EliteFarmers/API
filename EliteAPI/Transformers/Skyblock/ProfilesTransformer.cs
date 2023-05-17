using EliteAPI.Data;
using EliteAPI.Data.Models.Hypixel;
using EliteAPI.Services;
using EliteAPI.Services.ContestService;
using System.Text.Json.Nodes;
using System.Xml.Serialization;

namespace EliteAPI.Transformers.Skyblock;

public class ProfilesTransformer
{

    private readonly DataContext context;
    private readonly IContestService contestService;
    public ProfilesTransformer(DataContext context, IContestService contests)
    {
        this.context = context;
        this.contestService = contests;
    }

    public void TransformProfilesResponse(RawProfilesResponse data)
    {
        if (!data.Success || data.Profiles == null || data.Profiles.Length <= 0) return;
        
        foreach (var profile in data.Profiles)
        {
            if (profile == null) continue;

            var profileObj = new Profile
            {
                ProfileUUID = profile.ProfileId,
                ProfileName = profile.CuteName,
                GameMode = profile.GameMode,
            };
            context.Profiles.Add(profileObj);

            MetricsService.IncrementProfilesTransformedCount(profileId?.ToString() ?? "Unknown");


            var membersArray = members?.AsArray();
            if (membersArray == null || membersArray.Count == 0) continue;

            foreach (var member in membersArray)
            {
                if (member == null) continue;

                TransformMemberResponse(member.AsObject(), profileObj, isSelected);
            }
        }
    }

    public void TransformMemberResponse(JsonObject data, Profile profile, bool selected)
    {
        // Get key of the data object
        if (data == null) return;
        
        data.TryGetPropertyValue("pets", out var pets);
        data.TryGetPropertyValue("collection", out var collection);
        data.TryGetPropertyValue("jacob2", out var jacob);
        data.TryGetPropertyValue("fairy_souls_collected", out var fairySoulsCollected);
        data.TryGetPropertyValue("fairy_souls", out var fairySouls);
        data.TryGetPropertyValue("coin_purse", out var coinPurse);

        var member = new ProfileMember
        {
            IsSelected = selected,
            Profile = profile,
        };

        if (collection != null) 
        {
            ProcessCollections(collection.AsObject(), member);
        }

        if (pets != null) 
        {
            ProcessPets(pets.AsObject(), member);
        }

        if (jacob != null)
        {
            ProcessJacob(jacob.AsObject(), member);
        }
    }

    private void ProcessCollections(JsonObject collections, ProfileMember member)
    {
        if (collections == null) return;
        foreach (var collection in collections)
        {
            if (collection.Value == null) continue;

            var collectionName = collection.Key;
            var hasAmount = collection.Value.AsValue().TryGetValue(out long amount);
            if (!hasAmount) continue;

            var collectionObj = new Collection
            {
                Name = collectionName,
                Amount = amount,
                ProfileMember = member,
            };

            context.Collections.Add(collectionObj);
            member.Collections.Add(collectionObj);
        }

        context.SaveChanges();
    }

    private void ProcessPets(JsonObject pets, ProfileMember member)
    {
        if (pets == null) return;
        foreach (var pet in pets)
        {
            if (pet.Value == null) continue;
            var petData = pet.Value.AsObject();

            petData.TryGetPropertyValue("uuid", out var uuid);
            petData.TryGetPropertyValue("type", out var type);
            petData.TryGetPropertyValue("tier", out var tier);
            petData.TryGetPropertyValue("exp", out var exp);
            petData.TryGetPropertyValue("active", out var active);
            petData.TryGetPropertyValue("heldItem", out var heldItem);
            petData.TryGetPropertyValue("candyUsed", out var candyUsed);
            petData.TryGetPropertyValue("skin", out var skin);

            var petObj = new Pet
            {
                UUID = uuid?.ToString(),
                Type = type?.ToString(),
                Tier = tier?.ToString(),
                Exp = double.Parse(exp?.ToString() ?? "0"),
                Active = active?.ToString() == "true",
                HeldItem = heldItem?.ToString(),
                CandyUsed = short.Parse(candyUsed?.ToString() ?? "0"),
                Skin = skin?.ToString(),
                ProfileMemberId = member.Id,
            };
            context.Pets.Add(petObj);
            member.Pets.Add(petObj);
        }
        context.SaveChanges();  
    }

    private void ProcessJacob(JsonObject jacob, ProfileMember member)
    {
        if (jacob == null) return;

        var jacobData = member.JacobData;

        jacob.TryGetPropertyValue("medals_inv", out var medalsInv);
        jacob.TryGetPropertyValue("perks", out var perks);
        jacob.TryGetPropertyValue("contests", out var contests);

        if (medalsInv != null)
        {
            var medalsInvData = medalsInv.AsObject();
            medalsInvData.TryGetPropertyValue("gold", out var gold);
            medalsInvData.TryGetPropertyValue("silver", out var silver);
            medalsInvData.TryGetPropertyValue("bronze", out var bronze);

            jacobData.Medals.Gold = int.Parse(gold?.ToString() ?? "0");
            jacobData.Medals.Silver = int.Parse(silver?.ToString() ?? "0");
            jacobData.Medals.Bronze = int.Parse(bronze?.ToString() ?? "0");
        }

        if (perks != null)
        {
            var perksData = perks.AsObject();
            perksData.TryGetPropertyValue("double_drops", out var doubleDrops);
            perksData.TryGetPropertyValue("farming_level_cap", out var levelCap);

            jacobData.Perks.DoubleDrops = int.Parse(doubleDrops?.ToString() ?? "0");
            jacobData.Perks.LevelCap = int.Parse(levelCap?.ToString() ?? "0");
        }

        if (contests != null)
        {
            ProcessContests(contests.AsObject(), jacobData, member.LastUpdated);
        }

        member.JacobData = jacobData;
        context.SaveChanges();
    }

    private void ProcessContests(JsonObject contests, JacobData jacobData, DateTime LastUpdated)
    {

        foreach (var contest in contests)
        {
            var key = contest.Key;
            var value = contest.Value?.AsObject();
            if (value == null) continue;

            var crop = FormatTransformer.GetCropFromContestKey(key);
            var timestamp = FormatTransformer.GetTimeFromContestKey(key);

            if (crop == null || timestamp == DateTime.MinValue) continue;

            if (timestamp > LastUpdated)
            {
                // Check to see if the contest is already in the database
                var existingContest = jacobData.Contests.FirstOrDefault(c => c.JacobContest.Crop == crop && c.JacobContest.Timestamp == timestamp);

                // Create a new contest if it doesn't exist
                if (existingContest == null)
                {
                    var newContest = new ContestParticipation
                    {
                        
                    };
                    context.JacobContests.Add(newContest);
                    jacobData.Contests.Add(newContest);
                }
                else
                {
                    existingContest.JacobData = jacobData;
                    jacobData.Contests.Add(existingContest);
                }
            }
        }
    }
}

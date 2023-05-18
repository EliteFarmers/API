using EliteAPI.Data;
using EliteAPI.Data.Models.Hypixel;
using EliteAPI.Services;
using EliteAPI.Services.ContestService;
using EliteAPI.Services.MojangService;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Xml.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EliteAPI.Transformers.Skyblock;

public class ProfilesTransformer
{

    private readonly DataContext context;
    private readonly IContestService contestService;
    private readonly IMojangService mojangService;

    public ProfilesTransformer(DataContext context, IContestService contestService, IMojangService mojangService)
    {
        this.context = context;
        this.contestService = contestService;
        this.mojangService = mojangService;
    }

    public async void TransformProfilesResponse(RawProfilesResponse data)
    {
        if (!data.Success || data.Profiles == null || data.Profiles.Length <= 0) return;
        
        foreach (var profile in data.Profiles)
        {
            if (profile == null) continue;

            var newProfile = await TransformSingleProfile(profile);
            if (newProfile == null) continue;

            // Check if profile already exists, if so, update it
            var oldProfile = await context.Profiles.FindAsync(newProfile.ProfileId);
            if (oldProfile == null) continue;

            // Updating profile persists togglable fields if they've been disabled (ex: collections)
            UpdateProfile(oldProfile, newProfile);
        }
    }

    public async Task<Profile?> TransformSingleProfile(RawProfileData profile)
    {
        var profileId = profile.ProfileId.Replace("-", "");

        var profileObj = new Profile
        {
            ProfileId = profileId,
            ProfileName = profile.CuteName,
            GameMode = profile.GameMode,
        };
        context.Profiles.Add(profileObj);

        MetricsService.IncrementProfilesTransformedCount(profileId?.ToString() ?? "Unknown");

        var members = profile.Members;
        if (members == null || members.Count == 0) return null;

        foreach (var member in members)
        {
            // Hyphens shouldn't be included anyways, but just in case Hypixel pulls another fast one
            var memberId = member.Key.Replace("-", "");
            var memberData = member.Value;

            await TransformMemberResponse(memberId, memberData, profileObj, profile.Selected);
        }

        await context.SaveChangesAsync();

        return profileObj;
    }

    public async Task TransformMemberResponse(string memberId, RawMemberData memberData, Profile profile, bool selected)
    {
        if (memberData == null) return;

        var minecraftAccount = await mojangService.GetMinecraftAccountByUUID(memberId);
        if (minecraftAccount == null) return;

        var jacob = ProcessJacob(memberData);

        var member = new ProfileMember
        {
            MinecraftAccount = minecraftAccount,
            IsSelected = selected,
            Profile = profile,
            JacobData = ProcessJacob(memberData)
        };

        member.Collections = await ProcessCollections(memberData, member);
        /*
        if (pets != null) 
        {
            ProcessPets(pets.AsObject(), member);
        }

        if (jacob != null)
        {
            ProcessJacob(jacob.AsObject(), member);
        }*/
    }

    public void UpdateProfile(Profile oldProfile, Profile newProfile)
    {
        oldProfile.ProfileName = newProfile.ProfileName;
        oldProfile.GameMode = newProfile.GameMode;
        //oldProfile.Members = newProfile.Members;
        context.SaveChanges();
    }

    private async Task<List<Collection>> ProcessCollections(RawMemberData member, ProfileMember profileMember)
    {
        if (member.Collection == null)
        {
            var oldCollections = context.Collections.Where(c => c.ProfileMemberId == profileMember.Id);
            return oldCollections.ToList() ?? new();
        };

        var list = new List<Collection>();

        foreach (var collection in member.Collection)
        {
            var collectionName = collection.Key;
            var amount = collection.Value;

            var old = context.Collections.FirstOrDefault(c => c.Name == collectionName && c.ProfileMemberId == profileMember.Id);

            if (old != null)
            {
                old.Amount = amount;
                continue;
            }

            try
            {
                var collectionObj = new Collection
                {
                    Name = collectionName,
                    Amount = amount,
                    ProfileMember = profileMember,
                };

                await context.Collections.AddAsync(collectionObj);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        await context.SaveChangesAsync();
        return list;
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

    private JacobData ProcessJacob(RawMemberData member)
    {
        var jacob = new JacobData();
        var jacobData = member.Jacob;

        if (jacobData == null) return jacob;

        if (jacobData.MedalsInventory != null)
        {
            jacob.Medals.Gold = jacobData.MedalsInventory.Gold;
            jacob.Medals.Silver = jacobData.MedalsInventory.Silver;
            jacob.Medals.Bronze = jacobData.MedalsInventory.Bronze;
        }

        if (jacobData.Perks != null)
        {
            jacob.Perks.DoubleDrops = jacobData.Perks.DoubleDrops ?? 0;
            jacob.Perks.LevelCap = jacobData.Perks.FarmingLevelCap ?? 0;
        }

        if (jacobData.Contests != null)
        {
            // TODO: Figure out how to get the last updated time
            ProcessContests(jacob, jacobData.Contests, DateTime.MinValue);
        }

        return jacob;
    }

    private void ProcessContests(JacobData jacobData, RawJacobContest[] contests, DateTime LastUpdated)
    {
        /*
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
        }*/
    }
}

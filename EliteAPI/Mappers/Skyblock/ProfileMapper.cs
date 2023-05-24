using EliteAPI.Data;
using EliteAPI.Data.Models.Hypixel;
using EliteAPI.Services;
using EliteAPI.Services.ContestService;
using EliteAPI.Services.MojangService;
using System.Text.Json.Nodes;
using EliteAPI.Services.ProfileService;

namespace EliteAPI.Mappers.Skyblock;

public class ProfileMapper
{

    private readonly DataContext _context;
    private readonly IContestService _contestService;
    private readonly IMojangService _mojangService;

    public ProfileMapper(DataContext context, IContestService contestService, IMojangService mojangService)
    {
        _context = context;
        _contestService = contestService;
        _mojangService = mojangService;
    }

    public async Task TransformProfilesResponse(RawProfilesResponse data)
    {
        if (!data.Success || data.Profiles is not { Length: > 0 }) return;
        
        foreach (var profile in data.Profiles)
        {
            var newProfile = await TransformSingleProfile(profile);
            if (newProfile == null) continue;

            // Check if profile already exists, if so, update it
            var oldProfile = await _context.Profiles.FindAsync(newProfile.ProfileId);
            if (oldProfile == null) continue;

            // Updating profile persists toggleable fields if they've been disabled (ex: collections)
            UpdateProfile(oldProfile, newProfile);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<Profile?> TransformSingleProfile(RawProfileData profile)
    {
        var profileId = profile.ProfileId.Replace("-", "");

        var existing = await _context.Profiles.FindAsync(profileId);

        var profileObj = existing ?? new Profile
        {
            ProfileId = profileId,
            ProfileName = profile.CuteName,
            GameMode = profile.GameMode,
        };

        if (existing == null) await _context.Profiles.AddAsync(profileObj);

        MetricsService.IncrementProfilesTransformedCount(profileId ?? "Unknown");

        var members = profile.Members;
        if (members.Count == 0) return null;

        foreach (var (key, memberData) in members)
        {
            // Hyphens shouldn't be included anyways, but just in case Hypixel pulls another fast one
            var memberId = key.Replace("-", "");

            await TransformMemberResponse(memberId, memberData, profileObj, profile.Selected);
        }

        await _context.SaveChangesAsync();

        return profileObj;
    }

    public async Task TransformMemberResponse(string memberId, RawMemberData memberData, Profile profile, bool selected)
    {
        var minecraftAccount = await _mojangService.GetMinecraftAccountByUUID(memberId);
        if (minecraftAccount == null) return;

        var member = new ProfileMember
        {
            PlayerUuid = memberId,
            MinecraftAccount = minecraftAccount,
            IsSelected = selected,
            Profile = profile,
            ProfileId = profile.ProfileId,
            JacobData = ProcessJacob(memberData)
        };

        member.Collections = await ProcessCollections(memberData, member);
        member.Pets = await ProcessPets(memberData.Pets, member);
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
        _context.SaveChanges();
    }

    private async Task<List<Collection>> ProcessCollections(RawMemberData member, ProfileMember profileMember)
    {
        if (member.Collection == null)
        {
            var oldCollections = _context.Collections.Where(c => c.ProfileMemberId == profileMember.Id);
            return oldCollections.ToList() ?? new List<Collection>();
        };

        var list = new List<Collection>();

        foreach (var (collectionName, amount) in member.Collection)
        {
            var old = _context.Collections.FirstOrDefault(c => c.Name == collectionName && c.ProfileMemberId == profileMember.Id);

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

                await _context.Collections.AddAsync(collectionObj);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        await _context.SaveChangesAsync();
        return list;
    }

    private async Task<List<Pet>> ProcessPets(RawPetData[]? pets, ProfileMember member)
    {
        if (pets is not { Length: > 0 }) return new List<Pet>();

        var list = new List<Pet>();
        foreach (var pet in pets)
        {
            var petObj = new Pet
            {
                UUID = pet.Uuid,
                Type = pet.Type,
                Tier = pet.Tier,
                Exp = pet.Exp,
                Active = pet.Active,
                HeldItem = pet.HeldItem,
                CandyUsed = (short) pet.CandyUsed,
                Skin = pet.Skin,

                ProfileMember = member,
            };

            await _context.Pets.AddAsync(petObj);
            list.Add(petObj);
        }

        await _context.SaveChangesAsync();
        return list;
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

        if (jacobData.Contests.Count > 0)
        {
            // TODO: Figure out how to get the last updated time
            ProcessContests(jacob, jacobData.Contests, DateTime.MinValue);
        }

        return jacob;
    }

    private void ProcessContests(JacobData jacobData, Dictionary<string, RawJacobContest> contests, DateTime lastUpdated)
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

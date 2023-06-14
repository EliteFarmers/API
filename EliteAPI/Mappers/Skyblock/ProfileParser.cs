using EliteAPI.Data;
using EliteAPI.Services;
using EliteAPI.Services.MojangService;
using AutoMapper;
using EliteAPI.Models.DTOs.Incoming;
using Microsoft.EntityFrameworkCore;
using EliteAPI.Models.Entities.Hypixel;
using Profile = EliteAPI.Models.Entities.Hypixel.Profile;
using EliteAPI.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EliteAPI.Mappers.Skyblock;

public class ProfileParser
{
    private readonly DataContext _context;
    private readonly IMojangService _mojangService;
    private readonly IMapper _mapper;

    private readonly Func<DataContext, long, Task<JacobContest?>> _fetchJacobContest = 
        EF.CompileAsyncQuery((DataContext context, long key) =>            
            context.JacobContests
                .FirstOrDefault(j => j.Id == key)
        );

    private readonly Func<DataContext, string, string, Task<ProfileMember?>> _fetchProfileMemberData = 
        EF.CompileAsyncQuery((DataContext context, string profileUuid, string playerUuid) =>            
            context.ProfileMembers
                .Include(p => p.Profile)
                .Include(p => p.Skills)
                .Include(p => p.Pets)
                .Include(p => p.JacobData)
                .ThenInclude(j => j.Contests)
                .ThenInclude(c => c.JacobContest)
                .AsSplitQuery()
                .FirstOrDefault(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
        );

    public ProfileParser(DataContext context, IMojangService mojangService, IMapper mapper)
    {
        _context = context;
        _mojangService = mojangService;
        _mapper = mapper;
    }

    public async Task<List<ProfileMember>> TransformProfilesResponse(RawProfilesResponse data, string? playerUuid)
    {
        var profiles = new List<ProfileMember>();
        if (!data.Success || data.Profiles is not { Length: > 0 }) return profiles;
        
        foreach (var profile in data.Profiles)
        {
            var transformed = await TransformSingleProfile(profile, playerUuid);

            if (transformed != null)
            {
                profiles.AddRange(transformed.Members.Where(member => member.MinecraftAccount.Id.Equals(playerUuid)));
            }
        }

        return profiles;
    }

    public async Task<Profile?> TransformSingleProfile(RawProfileData profile, string? playerUuid)
    {
        var members = profile.Members;
        if (members.Count == 0) return null;

        var profileId = profile.ProfileId.Replace("-", "");
        var existing = await _context.Profiles.FindAsync(profileId);

        var profileObj = existing ?? new Profile
        {
            ProfileId = profileId,
            ProfileName = profile.CuteName,
            GameMode = profile.GameMode,
            Members = new List<ProfileMember>(),
            IsDeleted = false
        };

        if (existing is not null)
        {
            profileObj.GameMode = profile.GameMode;
            profileObj.ProfileName = profile.CuteName;
            profileObj.IsDeleted = false;
        }
        else
        {
            try
            {
                _context.Profiles.Add(profileObj);
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        foreach (var (key, memberData) in members)
        {
            // Hyphens shouldn't be included anyways, but just in case Hypixel pulls another fast one
            var memberId = key.Replace("-", "");

            var selected = playerUuid?.Equals(memberId) == true && profile.Selected;
            await TransformMemberResponse(key, memberData, profileObj, selected);
        }

        MetricsService.IncrementProfilesTransformedCount(profileId ?? "Unknown");

        if (existing is null)
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            return profileObj;
        }
        
        return profileObj;
    }

    public async Task TransformMemberResponse(string memberId, RawMemberData memberData, Profile profile, bool selected)
    {
        var minecraftAccount = await _mojangService.GetMinecraftAccountByUUID(memberId);
        if (minecraftAccount == null) return;

        var existing = await _fetchProfileMemberData(_context, memberId, profile.ProfileId);

        if (existing is not null)
        {
            existing.IsSelected = selected;
            existing.LastUpdated = DateTime.UtcNow;
            existing.WasRemoved = false;

            existing.JacobData = await ProcessJacob(memberData, existing, existing.JacobData);
            existing.Skills = ProcessSkills(memberData, existing);
            existing.Pets = ProcessPets(memberData.Pets, existing);
            existing.Collections = ProcessCollections(memberData, existing);
            existing.SkyblockXp = memberData.Leveling?.Experience ?? 0;
            existing.Purse = memberData.CoinPurse ?? 0;
            existing.CollectionTiers = CombineCollectionTiers(memberData.UnlockedCollTiers);

            // Add CraftedMinions to profile
            if (memberData.CraftedGenerators is not { Length: 0 })
            {
                CombineMinions(profile, memberData.CraftedGenerators);
            }

            _context.ProfileMembers.Update(existing);
            await _context.SaveChangesAsync();

            return;
        }

        var member = new ProfileMember
        {
            Id = Guid.NewGuid(),
            PlayerUuid = memberId,
            
            Profile = profile,
            ProfileId = profile.ProfileId,
            MinecraftAccountId = minecraftAccount.Id,
            MinecraftAccount = minecraftAccount,

            LastUpdated = DateTime.UtcNow,
            IsSelected = selected,
            WasRemoved = false
        };

        _context.ProfileMembers.Add(member);

        try
        {
            await _context.SaveChangesAsync();
            await _context.Entry(member).GetDatabaseValuesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        member.Collections = ProcessCollections(memberData, member);
        member.Pets = ProcessPets(memberData.Pets, member);
        member.JacobData = await ProcessJacob(memberData, member, member.JacobData);
        member.Skills = ProcessSkills(memberData, member);
        member.SkyblockXp = memberData.Leveling?.Experience ?? 0;
        member.Purse = memberData.CoinPurse ?? 0;
        member.CollectionTiers = CombineCollectionTiers(memberData.UnlockedCollTiers);

        // Add CraftedMinions to profile
        if (memberData.CraftedGenerators is not { Length: 0 })
        {
            CombineMinions(profile, memberData.CraftedGenerators);
        }
    }

    private Dictionary<string, long> ProcessCollections(RawMemberData member, ProfileMember profileMember)
    {
        var oldCollections = profileMember.Collections;

        if (member.Collection == null)
        {
            return oldCollections;
        };

        return member.Collection;
    }

    private List<Pet> ProcessPets(RawPetData[]? pets, ProfileMember member)
    {
        if (pets is not { Length: > 0 }) return new List<Pet>();

        _context.Pets.RemoveRange(member.Pets);

        var list = new List<Pet>();
        foreach (var pet in pets)
        {
            var petObj = new Pet
            {
                Uuid = pet.Uuid,
                Type = pet.Type,
                Tier = pet.Tier,
                Exp = pet.Exp,
                Active = pet.Active,
                HeldItem = pet.HeldItem,
                CandyUsed = (short) pet.CandyUsed,
                Skin = pet.Skin,

                ProfileMember = member,
            };

            list.Add(petObj);
        }

        _context.Pets.AddRange(list);

        return list;
    }

    private async Task<JacobData> ProcessJacob(RawMemberData member, ProfileMember profileMember, JacobData? existing)
    {
        var jacob = existing ?? new JacobData()
        {
            ProfileMember = profileMember,
            ProfileMemberId = profileMember.Id
        };
        var jacobData = member.Jacob;

        jacob.EarnedMedals.Gold = 0;
        jacob.EarnedMedals.Silver = 0;
        jacob.EarnedMedals.Bronze = 0;

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

        if (existing == null)
        {
            _context.JacobData.Add(jacob);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(jacob);
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex) { Console.WriteLine(ex); }

        if (jacobData.Contests.Count > 0)
        {
            await ProcessContests(jacob, jacobData.Contests);
        }

        return jacob;
    }

    private async Task ProcessContests(JacobData jacobData, Dictionary<string, RawJacobContest> contests)
    {
        var lastUpdatedTime = 0;//((DateTimeOffset) jacobData.ContestsLastUpdated).ToUnixTimeSeconds();
        var existingContests = new Dictionary<long, ContestParticipation>();

        foreach (var contest in jacobData.Contests)
        {
            var key = contest.JacobContest.Timestamp + (int) contest.JacobContest.Crop;

            if (existingContests.ContainsKey(key)) continue;

            existingContests.Add(key, contest);
        }

        foreach (var (key, contest) in contests)
        {
            await ProcessContest(jacobData, key, contest, lastUpdatedTime, existingContests);
        }

        jacobData.ContestsLastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private async Task ProcessContest(JacobData jacob, string contestKey, RawJacobContest contest, long lastUpdatedTime, Dictionary<long, ContestParticipation> existingContests)
    {
        if (contest.Collected < 100) return;

        var timestamp = ((DateTimeOffset) FormatUtils.GetTimeFromContestKey(contestKey)).ToUnixTimeSeconds();
        var crop = FormatUtils.GetCropFromContestKey(contestKey);
        if (crop == null) return;

        // Only process if the contest is newer than the last updated time
        // or if the contest has not been claimed (in case it got claimed after the last update)
        var existing = existingContests.GetValueOrDefault(timestamp + (int) crop);

        if (existing is not null && existing.MedalEarned != ContestMedal.None)
        {
            if (existing.MedalEarned == ContestMedal.Bronze) jacob.EarnedMedals.Bronze++;
            else if (existing.MedalEarned == ContestMedal.Silver) jacob.EarnedMedals.Silver++;
            else if (existing.MedalEarned == ContestMedal.Gold) jacob.EarnedMedals.Gold++;
        }

        if (existing is not null && timestamp < lastUpdatedTime && existing.Position >= 0) return;

        if (existing != null)
        {
            existing.Collected = contest.Collected;
            existing.MedalEarned = GetContestMedal(contest);
            existing.Position = contest.Position ?? -1;

            if (existing.MedalEarned == ContestMedal.None) return;

            jacob.EarnedMedals.Gold += existing.MedalEarned == ContestMedal.Gold ? 1 : 0;
            jacob.EarnedMedals.Silver += existing.MedalEarned == ContestMedal.Silver ? 1 : 0;
            jacob.EarnedMedals.Bronze += existing.MedalEarned == ContestMedal.Bronze ? 1 : 0;

            return;
        }

        var jacobContest = await _fetchJacobContest(_context, timestamp + (int) crop);

        if (jacobContest is null)
        {
            jacobContest = new JacobContest
            {
                Id = timestamp + (int) crop,
                Timestamp = timestamp,
                Crop = (Crop) crop,
                Participants = contest.Participants ?? -1,
            };

            _context.JacobContests.Add(jacobContest);
        }
        else
        {
            var participants = contest.Participants ?? -1;
            if (contest.Participants > jacobContest.Participants)
            {
                jacobContest.Participants = participants;
            }
        }

        var participation = new ContestParticipation
        {
            Collected = contest.Collected,
            MedalEarned = GetContestMedal(contest),
            Position = contest.Position ?? -1,

            ProfileMemberId = jacob.ProfileMemberId,
            ProfileMember = jacob.ProfileMember!,
            JacobContestId = jacobContest.Id,
            JacobContest = jacobContest,
        };

        if (participation.MedalEarned != ContestMedal.None)
        {
            if (participation.MedalEarned == ContestMedal.Bronze) jacob.EarnedMedals.Bronze++;
            else if (participation.MedalEarned == ContestMedal.Silver) jacob.EarnedMedals.Silver++;
            else if (participation.MedalEarned == ContestMedal.Gold) jacob.EarnedMedals.Gold++;
        }

        jacob.Contests.Add(participation);
        jacobContest.Participations.Add(participation);

        _context.ContestParticipations.Add(participation);
    }

    public ContestMedal GetContestMedal(RawJacobContest contest)
    {
        if (contest.Medal is not null)
        {
            return contest.Medal switch
            { 
                "gold" => ContestMedal.Gold,
                "silver" => ContestMedal.Silver,
                "bronze" => ContestMedal.Bronze,
                _ => ContestMedal.None
            };
        }

        var participants = contest.Participants;
        var position = contest.Position;
        if (position is null || participants is null) return ContestMedal.None;

        if (position <= (participants * 0.05) + 1) {
            return ContestMedal.Gold;
        }
        
        if (position <= (participants * 0.25) + 1) {
            return ContestMedal.Silver;
        }
        
        if (position <= (participants * 0.6) + 1) {
            return ContestMedal.Bronze;
        }

        return ContestMedal.None;
    }

    private void CombineMinions(Profile profile, string[]? minionStrings)
    {
        if (minionStrings is null) return;

        var craftedMinions = profile.CraftedMinions;

        // Ex: "WHEAT_1", "SUGAR_CANE_1"
        foreach (var minion in minionStrings)
        {
            // Split at last underscore of multiple underscores
            var lastUnderscore = minion.LastIndexOf("_", StringComparison.Ordinal);

            var minionType = minion[..lastUnderscore];
            var minionLevel = minion[(lastUnderscore + 1)..];

            if (!int.TryParse(minionLevel, out var level)) continue;

            craftedMinions.TryGetValue(minionType, out var current);
            // Set the bit at the level to 1
            craftedMinions[minionType] = current | (1 << level);
        }

        profile.CraftedMinions = craftedMinions;
    }

    private Skills ProcessSkills(RawMemberData data, ProfileMember member)
    {
        var skills = member.Skills;
        
        if (data.ExperienceSkillCombat is null) return skills;

        skills.Combat = data.ExperienceSkillCombat ?? skills.Combat;
        skills.Mining = data.ExperienceSkillMining ?? skills.Mining;
        skills.Foraging = data.ExperienceSkillForaging ?? skills.Foraging;
        skills.Fishing = data.ExperienceSkillFishing ?? skills.Fishing;
        skills.Enchanting = data.ExperienceSkillEnchanting ?? skills.Enchanting;
        skills.Alchemy = data.ExperienceSkillAlchemy ?? skills.Alchemy;
        skills.Taming = data.ExperienceSkillTaming ?? skills.Taming;
        skills.Carpentry = data.ExperienceSkillCarpentry ?? skills.Carpentry;
        skills.Runecrafting = data.ExperienceSkillRunecrafting ?? skills.Runecrafting;
        skills.Social = data.ExperienceSkillSocial ?? skills.Social;
        skills.Farming = data.ExperienceSkillFarming ?? skills.Farming;

        return skills;
    }

    private Dictionary<string, int> CombineCollectionTiers(string[]? collectionStrings)
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
}

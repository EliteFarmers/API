using EliteAPI.Data;
using EliteAPI.Services;
using EliteAPI.Services.MojangService;
using AutoMapper;
using EliteAPI.Models.DTOs.Incoming;
using Microsoft.EntityFrameworkCore;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Profiles;
using Profile = EliteAPI.Models.Entities.Hypixel.Profile;
using EliteAPI.Utilities;

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

        var existingProfileIds = new List<string>();
        
        foreach (var profile in data.Profiles)
        {
            var transformed = await TransformSingleProfile(profile, playerUuid);

            if (transformed == null) continue;

            var owned = transformed.Members
                .Where(member => member.PlayerUuid.Equals(playerUuid));

            profiles.AddRange(owned);
            existingProfileIds.Add(transformed.ProfileId);
        }

        var deletedMemberIds = await _context.ProfileMembers               
            .Where(p => !p.WasRemoved && !existingProfileIds.Contains(p.Profile.ProfileId))
            .Select(p => p.Id)
            .ToListAsync();

        if (deletedMemberIds.Count == 0) return profiles;

        // Mark all members that were not returned as deleted
        foreach (var memberId in deletedMemberIds)
        {
            var member = await _context.ProfileMembers.FindAsync(memberId);
            if (member is null) continue;

            member.WasRemoved = true;
        }

        await _context.SaveChangesAsync();

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
            await TransformMemberResponse(memberId, memberData, profileObj, selected);
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
            //if (existing.WasRemoved) return;

            existing.IsSelected = selected;
            existing.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await UpdateProfileMember(profile, existing, memberData);
            await _context.SaveChangesAsync();

            return;
        }

        var member = new ProfileMember
        {
            Id = Guid.NewGuid(),
            PlayerUuid = memberId,
            
            Profile = profile,
            ProfileId = profile.ProfileId,
            MinecraftAccount = minecraftAccount,

            LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            IsSelected = selected,
            WasRemoved = false
        };

        _context.ProfileMembers.Add(member);
        profile.Members.Add(member);

        try
        {
            await _context.SaveChangesAsync();
            await _context.Entry(member).GetDatabaseValuesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        await UpdateProfileMember(profile, member, memberData);
    }

    private async Task UpdateProfileMember(Profile profile, ProfileMember member, RawMemberData incomingData)
    {
        member.Collections = incomingData.Collection ?? member.Collections;
        member.SkyblockXp = incomingData.Leveling?.Experience ?? 0;
        member.Purse = incomingData.CoinPurse ?? 0;

        member.Pets = ProcessPets(incomingData.Pets, member);
        member.JacobData = await ProcessJacob(incomingData, member, member.JacobData);
        
        member.ParseJacob(incomingData.Jacob);
        await _context.SaveChangesAsync();
        await member.ParseJacobContests(incomingData.Jacob);

        member.ParseSkills(incomingData);
        member.ParseCollectionTiers(incomingData.UnlockedCollTiers);

        profile.CraftedMinions = CraftedMinionParser.Combine(profile.CraftedMinions, incomingData.CraftedGenerators);
    }

    private List<Pet> ProcessPets(RawPetData[]? pets, ProfileMember member)
    {
        if (pets is not { Length: > 0 }) return new List<Pet>();

        return pets.Select(pet => new Pet
            {
                Uuid = pet.Uuid,
                Type = pet.Type,
                Tier = pet.Tier,
                Exp = pet.Exp,
                Active = pet.Active,
                HeldItem = pet.HeldItem,
                CandyUsed = (short)pet.CandyUsed,
                Skin = pet.Skin,
            }).ToList();
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
        jacob.Participations = 0;

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

            existingContests.Add(key, contest);
        }


        var newParticipations = new List<ContestParticipation>();
        foreach (var (key, contest) in contests)
        {
            var contestParticipation = await ProcessContest(jacobData, key, contest, lastUpdatedTime, existingContests);

            if (contestParticipation is null) continue;
            
            newParticipations.Add(contestParticipation);
        }

        _context.ContestParticipations.AddRange(newParticipations);

        jacobData.ContestsLastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private async Task<ContestParticipation?> ProcessContest(JacobData jacob, string contestKey, RawJacobContest contest, long lastUpdatedTime, Dictionary<long, ContestParticipation> existingContests)
    {
        if (contest.Collected < 100) return null;

        var crop = FormatUtils.GetCropFromContestKey(contestKey);
        if (crop == null) return null;

        jacob.Participations++;

        var medal = GetContestMedal(contest);

        if (medal != ContestMedal.None)
        {
            switch (medal)
            {
                case ContestMedal.Bronze:
                    jacob.EarnedMedals.Bronze++;
                    break;
                case ContestMedal.Silver:
                    jacob.EarnedMedals.Silver++;
                    break;
                case ContestMedal.Gold:
                    jacob.EarnedMedals.Gold++;
                    break;
            }
        }

        var timestamp = ((DateTimeOffset) FormatUtils.GetTimeFromContestKey(contestKey)).ToUnixTimeSeconds();
        
        // Only process if the contest is newer than the last updated time
        // or if the contest has not been claimed (in case it got claimed after the last update)
        var existing = existingContests.GetValueOrDefault(timestamp + (int) crop);

        if (existing is not null && timestamp < lastUpdatedTime && existing.Position >= 0) return null;

        if (existing is not null)
        {
            existing.Collected = contest.Collected;
            existing.MedalEarned = GetContestMedal(contest);
            existing.Position = contest.Position ?? -1;

            return null;
        }

        var key = timestamp + (int) crop;
        var jacobContest = await _fetchJacobContest(_context, key);

        if (jacobContest is null)
        {
            jacobContest = new JacobContest
            {
                Id = key,
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
            MedalEarned = medal,
            Position = contest.Position ?? -1,

            ProfileMemberId = jacob.ProfileMemberId,
            ProfileMember = jacob.ProfileMember!,
            JacobContestId = jacobContest.Id,
            JacobContest = jacobContest,
        };

        jacobContest.Participations.Add(participation);
        return participation;
    }

    public ContestMedal GetContestMedal(RawJacobContest contest)
    {
        // Respect given medal if it exists
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

        // Calculate medal based on position
        if (position <= (participants * 0.05) + 1) return ContestMedal.Gold;
        if (position <= (participants * 0.25) + 1) return ContestMedal.Silver;
        if (position <= (participants * 0.6) + 1) return ContestMedal.Bronze;
        
        return ContestMedal.None;
    }
}

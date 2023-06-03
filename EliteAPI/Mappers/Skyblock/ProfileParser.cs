using EliteAPI.Data;
using EliteAPI.Services;
using EliteAPI.Services.ContestService;
using EliteAPI.Services.MojangService;
using AutoMapper;
using EliteAPI.Models.DTOs.Incoming;
using Microsoft.EntityFrameworkCore;
using EliteAPI.Models.Entities.Hypixel;
using Profile = EliteAPI.Models.Entities.Hypixel.Profile;
using EliteAPI.Utilities;

namespace EliteAPI.Mappers.Skyblock;

public class ProfileParser
{

    private readonly DataContext _context;
    private readonly IContestService _contestService;
    private readonly IMojangService _mojangService;
    private readonly IMapper _mapper;

    private readonly Func<DataContext, string, string, Task<ProfileMember?>> _fetchProfileMemberData = 
        EF.CompileAsyncQuery((DataContext context, string profileUuid, string playerUuid) =>            
            context.ProfileMembers
                .Include(p => p.Profile)
                .Include(p => p.Collections)
                .Include(p => p.Skills)
                .Include(p => p.Pets)
                .Include(p => p.JacobData)
                .ThenInclude(j => j.Contests)
                .ThenInclude(c => c.JacobContest)
                .AsSplitQuery()
                .FirstOrDefault(p => p.Profile.ProfileId.Equals(profileUuid) && p.PlayerUuid.Equals(playerUuid))
        );

    public ProfileParser(DataContext context, IContestService contestService, IMojangService mojangService, IMapper mapper)
    {
        _context = context;
        _contestService = contestService;
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

        foreach (var (key, memberData) in members)
        {
            // Hyphens shouldn't be included anyways, but just in case Hypixel pulls another fast one
            var memberId = key.Replace("-", "");

            var selected = playerUuid?.Equals(memberId) == true && profile.Selected;
            await TransformMemberResponse(key, memberData, profileObj, selected);
        }

        MetricsService.IncrementProfilesTransformedCount(profileId ?? "Unknown");

        if (existing == null)
        {
            _context.Profiles.Add(profileObj);
            await _context.SaveChangesAsync();
            return profileObj;
        }

        _context.Entry(existing).CurrentValues.SetValues(profileObj);
        await _context.SaveChangesAsync();

        return profileObj;
    }

    public async Task TransformMemberResponse(string memberId, RawMemberData memberData, Profile profile, bool selected)
    {
        var minecraftAccount = await _mojangService.GetMinecraftAccountByUUID(memberId);
        if (minecraftAccount == null) return;

        var existing = await _fetchProfileMemberData(_context, memberId, profile.ProfileId);
        /*_context.ProfileMembers
            .Include(m => m.Collections)
            .Include(m => m.Pets)
            .Include(m => m.JacobData)
            .ThenInclude(j => j.Contests)
            .ThenInclude(c => c.JacobContest)
            .Include(m => m.Skills)
            .AsSplitQuery()
            .FirstOrDefault(m => m.PlayerUuid.Equals(memberId) && m.ProfileId.Equals(profile.ProfileId));*/

        if (existing != null)
        {
            existing.IsSelected = selected;
            existing.LastUpdated = DateTime.UtcNow;
            existing.WasRemoved = false;

            existing.JacobData = await ProcessJacob(memberData, existing, existing.JacobData);
            existing.Skills = ProcessSkills(memberData, existing);
            existing.Pets = ProcessPets(memberData.Pets, existing);
            existing.Collections = ProcessCollections(memberData, existing);

            // Add CraftedMinions to profile
            if (memberData.CraftedGenerators is not { Length: 0 })
            {
                CombineMinions(profile, existing, memberData.CraftedGenerators);
            }

            _context.ProfileMembers.Update(existing);
            await _context.SaveChangesAsync();

            return;
        }

        var member = new ProfileMember
        {
            PlayerUuid = memberId,
            MinecraftAccount = minecraftAccount,
            IsSelected = selected,
            Profile = profile,
            ProfileId = profile.ProfileId,
            LastUpdated = DateTime.UtcNow,
            WasRemoved = false
        };

        _context.ProfileMembers.Add(member);
        await _context.SaveChangesAsync();
        await _context.Entry(member).GetDatabaseValuesAsync();

        member.Collections = ProcessCollections(memberData, member);
        member.Pets = ProcessPets(memberData.Pets, member);
        member.JacobData = await ProcessJacob(memberData, member, member.JacobData);
        member.Skills = ProcessSkills(memberData, member);

        // Add CraftedMinions to profile
        if (memberData.CraftedGenerators is not { Length: 0 })
        {
            CombineMinions(profile, member, memberData.CraftedGenerators);
        }
    }

    public void UpdateProfile(Profile oldProfile, Profile newProfile)
    {
        oldProfile.ProfileName = newProfile.ProfileName;
        oldProfile.GameMode = newProfile.GameMode;
        //oldProfile.Members = newProfile.Members;
        _context.SaveChanges();
    }

    private List<Collection> ProcessCollections(RawMemberData member, ProfileMember profileMember)
    {
        var oldCollections = profileMember.Collections;

        if (member.Collection == null)
        {
            return oldCollections;
        };

        var list = new List<Collection>();

        foreach (var (collectionName, amount) in member.Collection)
        {
            var old = oldCollections.Find(c => c.Name.Equals(collectionName));

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
                    ProfileMemberId = profileMember.Id,
                };

                _context.Collections.Add(collectionObj);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
        
        return list;
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

        await _context.SaveChangesAsync();

        if (jacobData.Contests.Count > 0)
        {
            ProcessContests(jacob, jacobData.Contests);
        }

        return jacob;
    }

    private void ProcessContests(JacobData jacobData, Dictionary<string, RawJacobContest> contests)
    {
        foreach (var (key, contest) in contests)
        { 
            ProcessContest(jacobData, key, contest);
        }

        jacobData.ContestsLastUpdated = DateTime.UtcNow;
    }

    private void ProcessContest(JacobData jacob, string contestKey, RawJacobContest contest)
    {
        if (contest.Collected < 100) return;

        var lastUpdatedTime = jacob.ContestsLastUpdated;

        var timestamp = FormatUtils.GetTimeFromContestKey(contestKey);
        var crop = FormatUtils.GetCropFromContestKey(contestKey);
        if (crop == null) return;

        // Only process if the contest is either newer than the last updated time or if the contest has not been collected
        var existing = jacob.Contests.Find(c => c.Crop == crop && c.JacobContest?.Timestamp == timestamp);
        if (existing is not null && timestamp < lastUpdatedTime && existing.Collected > 0) return;

        if (existing != null)
        {
            existing.Collected = contest.Collected;
            existing.MedalEarned = GetContestMedal(contest);
            existing.Position = contest.Position ?? -1;

            jacob.EarnedMedals.Gold += existing.MedalEarned == ContestMedal.Gold ? 1 : 0;
            jacob.EarnedMedals.Silver += existing.MedalEarned == ContestMedal.Silver ? 1 : 0;
            jacob.EarnedMedals.Bronze += existing.MedalEarned == ContestMedal.Bronze ? 1 : 0;

            return;
        }

        var jacobContest = existing?.JacobContest;
        var jacobContestEvent = jacobContest?.JacobContestEvent;

        if (jacobContestEvent == null)
        {
            jacobContestEvent = new JacobContestEvent
            {
                Timestamp = timestamp,
            };

            _context.JacobContestEvents.Add(jacobContestEvent);
        }

        if (jacobContest == null)
        {
            jacobContest = new JacobContest
            {
                Timestamp = timestamp,
                JacobContestEvent = jacobContestEvent,
                Crop = (Crop) crop,
            };

            jacobContestEvent.JacobContests.Add(jacobContest);
            _context.JacobContests.Add(jacobContest);
        }

        var participation = new ContestParticipation
        {
            Collected = contest.Collected,
            MedalEarned = GetContestMedal(contest),
            Position = contest.Position ?? -1,
            Crop = (Crop) crop,
            JacobContest = jacobContest,

            ProfileMember = jacob.ProfileMember!,
            ProfileMemberId = jacob.ProfileMemberId,
        };

        jacob.EarnedMedals.Gold += participation.MedalEarned == ContestMedal.Gold ? 1 : 0;
        jacob.EarnedMedals.Silver += participation.MedalEarned == ContestMedal.Silver ? 1 : 0;
        jacob.EarnedMedals.Bronze += participation.MedalEarned == ContestMedal.Bronze ? 1 : 0;

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

        if (position <= participants * 0.05 + 1) {
            return ContestMedal.Gold;
        }
        
        if (position <= participants * 0.25 + 1) {
            return ContestMedal.Silver;
        }
        
        if (position <= participants * 0.6 + 1) {
            return ContestMedal.Bronze;
        }

        return ContestMedal.None;
    }

    private void CombineMinions(Profile profile, ProfileMember member, string[]? minionStrings)
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

            var level = int.TryParse(minionLevel, out var l) ? l : 0;

            var existing = craftedMinions.FirstOrDefault(m => m.Type == minionType);

            if (existing != null)
            {
                existing.RegisterTier(level);
                _context.Update(existing);
            }
            else
            { 
                var newMinion = new CraftedMinion 
                {
                    Type = minionType,
                    ProfileMember = member,
                    ProfileMemberId = member.Id
                };

                newMinion.RegisterTier(level);

                _context.Add(newMinion);
            }
        }
    }

    private List<Skill> ProcessSkills(RawMemberData data, ProfileMember member)
    {
        var skills = new List<Skill>();
        
        if (data.ExperienceSkillCombat is null) return skills;

        skills.Add(ProcessSkill(SkillName.Combat, data.ExperienceSkillCombat, member));
        skills.Add(ProcessSkill(SkillName.Foraging, data.ExperienceSkillForaging, member));
        skills.Add(ProcessSkill(SkillName.Mining, data.ExperienceSkillMining, member));
        skills.Add(ProcessSkill(SkillName.Farming, data.ExperienceSkillFarming, member));
        skills.Add(ProcessSkill(SkillName.Fishing, data.ExperienceSkillFishing, member));
        skills.Add(ProcessSkill(SkillName.Enchanting, data.ExperienceSkillEnchanting, member));
        skills.Add(ProcessSkill(SkillName.Alchemy, data.ExperienceSkillAlchemy, member));
        skills.Add(ProcessSkill(SkillName.Taming, data.ExperienceSkillTaming, member));
        skills.Add(ProcessSkill(SkillName.Carpentry, data.ExperienceSkillCarpentry, member));
        skills.Add(ProcessSkill(SkillName.RuneCrafting, data.ExperienceSkillRunecrafting, member));
        skills.Add(ProcessSkill(SkillName.Social, data.ExperienceSkillSocial, member));

        return skills;
    }

    private Skill ProcessSkill(string name, double? exp, ProfileMember member)
    {
        var existing = member.Skills.FirstOrDefault(s => s.Type == name);

        if (existing != null)
        {
            existing.Exp = exp ?? 0.0;
            return existing;
        }

        var skill = new Skill
        {
            Type = name,
            Exp = exp ?? 0.0,
            ProfileMember = member,
            ProfileMemberId = member.Id
        };

        return skill;
    }
}

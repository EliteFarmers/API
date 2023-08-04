using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Mappers.Profiles;

public static class JacobDataParser
{
    public static void ParseJacob(this ProfileMember member, RawJacobData? incomingJacob)
    {
        member.JacobData = ParseJacobData(member.JacobData, incomingJacob);
        member.JacobData.ProfileMember = member;
        member.JacobData.ProfileMemberId = member.Id;
    }

    public static JacobData ParseJacobData(JacobData jacob, RawJacobData? incomingJacob)
    {
        if (incomingJacob is null) return jacob;

        if (incomingJacob.MedalsInventory is not null)
        {
            jacob.Medals.Gold = incomingJacob.MedalsInventory.Gold;
            jacob.Medals.Silver = incomingJacob.MedalsInventory.Silver;
            jacob.Medals.Bronze = incomingJacob.MedalsInventory.Bronze;
        }

        if (incomingJacob.Perks is not null)
        {
            jacob.Perks.DoubleDrops = incomingJacob.Perks.DoubleDrops ?? 0;
            jacob.Perks.LevelCap = incomingJacob.Perks.FarmingLevelCap ?? 0;
        }

        return jacob;
    }
}

namespace EliteAPI.Data.Models.Hypixel;

public class Profile
{
    public int Id { get; set; }
    public string? ProfileUUID { get; set; }
    public required string ProfileName { get; set; }
    public string? GameMode { get; set; }
    public List<Member> Members { get; set; } = new();
}

public class Member
{
    public int Id { get; set; }
    public required Profile Profile { get; set; }
    public List<Collection> Collections { get; set; } = new();
}

public class Collection
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required long Amount { get; set; }
}
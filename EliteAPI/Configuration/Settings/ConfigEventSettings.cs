namespace EliteAPI.Configuration.Settings;

public class ConfigEventSettings 
{
    public EventTeamsWordListDto TeamsWordList { get; set; } = new();
}

public class EventTeamsWordListDto
{
    public List<string> Adjectives { get; set; } = [];
    public List<string> Nouns { get; set; } = [];
    public List<string> Verbs { get; set; } = [];
}


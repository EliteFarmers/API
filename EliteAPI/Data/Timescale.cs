namespace EliteAPI.Data;

public class HypertableColumnAttribute : Attribute {
}

public interface ITimeScale {
	public DateTimeOffset Time { get; set; }
}
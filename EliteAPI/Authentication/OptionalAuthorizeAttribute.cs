namespace EliteAPI.Authentication;

/// <summary>
/// Only used to add the bearer token to the swagger UI
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class OptionalAuthorizeAttribute : Attribute;
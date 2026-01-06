using System.IO.Hashing;
using System.Text.Json;
using System.Text.Json.Serialization;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Features.Profiles.Utilities;

public static class MemberDataHasher
{
	private static readonly JsonSerializerOptions Options = new() {
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public static long ComputeHash(ProfileMemberResponse data) {
		var bytes = JsonSerializer.SerializeToUtf8Bytes(data, Options);
		return (long)XxHash3.HashToUInt64(bytes);
	}
	
	public static long ComputeHash(ProfileResponse data) {
		var bytes = JsonSerializer.SerializeToUtf8Bytes(data, Options);
		return (long)XxHash3.HashToUInt64(bytes);
	}
}

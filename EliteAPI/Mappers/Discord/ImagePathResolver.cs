using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Images;

namespace EliteAPI.Mappers.Discord;

public class ImagePathResolver(IConfiguration configuration) : IValueResolver<Image, ImageAttachmentDto, string> {
	public string Resolve(Image source, ImageAttachmentDto destination, string destMember, ResolutionContext context) {
		var prefix = configuration["S3:PublicUrl"];
		return string.IsNullOrEmpty(prefix) ? source.Path : $"{prefix}/{source.Path}";
	}
}
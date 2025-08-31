using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Images.Models;

public class Image {
	[MaxLength(48)]
	public string Id { get; set; } = Guid.NewGuid().ToString();

	[MaxLength(512)]
	public required string Path { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	
	[MaxLength(64)]
	public string? Title { get; set; } 
	
	[MaxLength(512)]
	public string? Description { get; set; }
	
	public int? Order { get; set; }
	
	[MaxLength(64)]
	public string? Hash { get; set; }

	[Column(TypeName = "jsonb")] 
	public Dictionary<string, string> Metadata { get; set; } = new();
}

public class ImageEntityConfiguration : IEntityTypeConfiguration<Image>
{
	public void Configure(EntityTypeBuilder<Image> builder)
	{
		builder.HasKey(image => image.Id);
		builder.HasIndex(image => image.Path);
	}
}

public class AllowedFileExtensions : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
	    if (value is null) return ValidationResult.Success;
        if (value is not IFormFile file) return new ValidationResult("File is required");
        
        if (!IsFileValid(file)) {
            return new ValidationResult($"File type is not allowed.");
        }

        return ValidationResult.Success;
    }
    
    public static bool IsFileValid(IFormFile file) {
	    if (file.Length == 0) {
		    return false;
	    }

	    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
	    if (string.IsNullOrEmpty(extension)) {
		    return false;
	    }

	    // A using declaration ensures the stream is disposed
	    using var stream = file.OpenReadStream();
	    
	    // Special handling for WebP files
	    if (extension == ".webp") {
		    if (stream.Length < 12) return false;
        
		    var buffer = new byte[12];
		    stream.ReadExactly(buffer, 0, 12);

		    // Check for "RIFF" at the beginning and "WEBP" at offset 8
		    return buffer.Take(4).SequenceEqual("RIFF"u8.ToArray()) &&
		           buffer.Skip(8).Take(4).SequenceEqual("WEBP"u8.ToArray());
	    }

	    // General handling for other file types
	    if (!FileSignatures.TryGetValue(extension, out var signatures)) {
		    return false;
	    }

	    // Read the maximum number of bytes required for any signature
	    var headerBytes = new byte[signatures.Max(s => s.Length)];
	    var bytesRead = stream.Read(headerBytes, 0, headerBytes.Length);
	    if (bytesRead == 0) return false;

	    // Check if the read header matches any of the valid signatures
	    return signatures.Any(sig => headerBytes.Take(sig.Length).SequenceEqual(sig));
    }

    private static readonly Dictionary<string, List<byte[]>> FileSignatures = new()
    {
	    { ".jpg", [
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 },
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 },
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 },
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xDB },
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xEE }
		    ]
	    },
	    { ".jpeg", [
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xE2 },
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xE3 },
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 },
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xDB },
			    new byte[] { 0xFF, 0xD8, 0xFF, 0xEE }
		    ]
	    },
	    { ".png", [new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }] },
	    { ".gif", [new byte[] { 0x47, 0x49, 0x46, 0x38 }] },
    };
}
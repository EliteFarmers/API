using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Models.Entities.Images;

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
		// Make image path unique
		builder.HasIndex(image => image.Path).IsUnique();
	}
}

public class AllowedFileExtensions : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file) return new ValidationResult("File is required");
        
        if (!IsFileValid(file)) {
            return new ValidationResult($"File type is not allowed.");
        }

        return ValidationResult.Success;
    }
    
    public static bool IsFileValid(IFormFile file) {
	    var extension = Path.GetExtension(file.FileName)?.ToLower();
	    if (string.IsNullOrEmpty(extension)) return false;
	    
	    FileSignatures.TryGetValue(extension, out var signatures);
	    if (signatures is null) return false;
	    
	    using var reader = new BinaryReader(file.OpenReadStream());
	    
	    var headerBytes = reader.ReadBytes(signatures.Max(signature => signature.Length));
	    var result = signatures.Any(signature => headerBytes.Take(signature.Length).SequenceEqual(signature));
	    
	    return result;
    }

    private static readonly Dictionary<string, List<byte[]>> FileSignatures = new()
    {
        // { ".jpeg", [
        //         [0xFF, 0xD8, 0xFF, 0xE0],
        //         [0xFF, 0xD8, 0xFF, 0xE2],
        //         [0xFF, 0xD8, 0xFF, 0xE3],
        //         [0xFF, 0xD8, 0xFF, 0xEE],
        //         [0xFF, 0xD8, 0xFF, 0xDB]
        //     ]
        // },
        // { ".jpg", [
        //         [0xFF, 0xD8, 0xFF, 0xE0],
        //         [0xFF, 0xD8, 0xFF, 0xE1],
        //         [0xFF, 0xD8, 0xFF, 0xE8],
        //         [0xFF, 0xD8, 0xFF, 0xEE],
        //         [0xFF, 0xD8, 0xFF, 0xDB]
        //     ]
        // },
        // {
	       //  ".webp", [
		      //   [0x52, 0x49, 0x46, 0x46], 
		      //   [0x00, 0x00, 0x00, 0x00],
		      //   [0x57, 0x45, 0x42, 0x50]
	       //  ]
        // },
        { ".png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]] },
        // { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
    };
}
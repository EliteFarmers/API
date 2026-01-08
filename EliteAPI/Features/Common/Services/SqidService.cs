using Sqids;

namespace EliteAPI.Features.Common.Services;

public static class SqidService
{
    private static readonly SqidsEncoder<int> Encoder = new(new SqidsOptions
    {
        MinLength = 6,
        Alphabet = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ123456789"
    });

    public static string Encode(int id) => Encoder.Encode(id);
    
    public static int? Decode(string slug)
    {
        try 
        {
            var decoded = Encoder.Decode(slug);
            if (decoded.Count > 0) return decoded[0];
            
            // Handle hyphenated slugs
            var parts = slug.Split('-');
            if (parts.Length > 0)
            {
                var potentialSqid = parts.Last();
                decoded = Encoder.Decode(potentialSqid);
                if (decoded.Count > 0) return decoded[0];
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
}

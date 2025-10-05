using EliteAPI.Parsers.Inventories;
using FastEndpoints;
using MinecraftRenderer;
using MinecraftRenderer.TexturePacks;
using SixLabors.ImageSharp;
using SkyblockRepo;

namespace EliteAPI.Features.Textures.Services;

[RegisterService<ItemTextureResolver>(LifeTime.Singleton)]
public class ItemTextureResolver
{
    public static MinecraftBlockRenderer Renderer { get; set; } = null!;
    private static MinecraftBlockRenderer.BlockRenderOptions Options { get; set; } = null!;
    private ILogger Logger { get; }
    private ISkyblockRepoClient RepoClient { get; }

    public ItemTextureResolver(
        ILogger<ItemTextureResolver> logger, 
        ISkyblockRepoClient repoClient,
        IConfiguration configuration)
    {
        Logger = logger;
        RepoClient = repoClient;
        
        var assetsPath = configuration["MinecraftRenderer:AssetsPath"] 
                         ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EliteAPI");
        var texturePacksPath = Path.Combine(assetsPath, "texturepacks");
        var assetsPathRoot = Path.Combine(assetsPath, "minecraft", "assets", "minecraft");
        
        var texturePackRegistry = TexturePackRegistry.Create();
        texturePackRegistry.RegisterAllPacks(texturePacksPath);
        
        Renderer = MinecraftBlockRenderer.CreateFromMinecraftAssets(
            assetsDirectory: assetsPathRoot,
            texturePackRegistry: texturePackRegistry);
        
        Options = MinecraftBlockRenderer.BlockRenderOptions.Default with
        {
            Size = 128,
            SkullTextureResolver = (customDataId, profile) =>
            {
                if (profile is not null || customDataId is null) return null;
                var item = RepoClient.FindItem(customDataId);
                return item?.Data?.Skin?.Value ?? null;
            },
            PackIds = ["hypixelplus"]
        };
        
        Renderer.PreloadRegisteredPacks();
        
        NbtParser.SetRenderer(Renderer);
        
        Logger.LogInformation("MinecraftBlockRenderer initialized successfully");
    }
    
    public async Task<byte[]> RenderItemAsync(string itemId, int size = 128)
    {
        try
        {
            using var image = Renderer.RenderGuiItemFromTextureId(itemId, Options with { Size = size });
            
            using var ms = new MemoryStream();
            await image.SaveAsPngAsync(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to render item {ItemId}", itemId);
            throw;
        }
    }
}
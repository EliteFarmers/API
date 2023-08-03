using AutoMapper;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Parsers.Inventories;
using EliteAPI.Services.MemberService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Controllers.Member; 

[Route("[controller]")]
[ApiController]
public class InventoryController : Controller {
    
    private readonly IMemberService _memberService;
    private readonly IMapper _mapper;

    public InventoryController(IMemberService memberService, IMapper mapper) {
        _memberService = memberService;
        _mapper = mapper;
    }
    
    // GET <InventoryController>/7da0c47581dc42b4962118f8049147b7/
    [HttpGet("{playerUuid}")]
    public async Task<ActionResult<DecodedInventoriesDto>> GetSelectedInventory(string playerUuid) {
        var uuid = playerUuid.Replace("-", "");
        await _memberService.UpdatePlayerIfNeeded(uuid);

        var query = await _memberService.ProfileMemberQuery(uuid);
        if (query is null) return NotFound("Inventory not found.");
        
        var inventory = await query
            .Where(p => p.IsSelected)
            .Include(p => p.Inventories)
            .Select(p => p.Inventories)
            .AsNoTracking()
            .FirstOrDefaultAsync();
        
        if (inventory is null) return NotFound("Inventory not found.");
        
        var decoded = await inventory.DecodeToNbt();
        
        return Ok(decoded);
    }
}
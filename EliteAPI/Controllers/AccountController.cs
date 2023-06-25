using EliteAPI.Authentication;
using EliteAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    // GET api/<ValuesController>
    [HttpGet]
    [ServiceFilter(typeof(DiscordAuthFilter))]
    public ActionResult<Account> Get()
    {
        if (HttpContext.Items["Account"] is not Account result)
        {
            return NotFound("Account not found.");
        }

        return Ok(result);
    }
}

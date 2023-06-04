using AutoMapper;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EliteAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContestsController : ControllerBase
    {

        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public ContestsController(DataContext dataContext, IMapper mapper)
        {
            _context = dataContext;
            _mapper = mapper;
        }

        // GET: api/<ContestsController>
        [HttpGet]
        public async Task<IEnumerable<JacobContestDto>> Get()
        {
            var data = await _context.JacobContests
                .Include(j => j.Participations)
                .ThenInclude(p => p.ProfileMember)
                .ThenInclude(p => p == null ? null : p.MinecraftAccount)
                .Where(j => j.Participations.Count > 1)
                .ToListAsync();

            return _mapper.Map<IEnumerable<JacobContestDto>>(data);
        }

        // GET api/<ContestsController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<ContestsController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ContestsController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ContestsController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Exceptions;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VisitsController : ControllerBase
    {
        private readonly IDbService _dbService;

        public VisitsController(IDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpPost]
        public async Task<IActionResult> AddNewRental(int id, CreateVisitDTO createVisitRequest)
            {
                if (!createVisitRequest.Services.Any())
                {
                    return BadRequest("At least one item is required.");
                }
                try
                {
                    await _dbService.AddNewVisitAsync(createVisitRequest);
                }
                catch (ConflictException e)
                {
                    return Conflict(e.Message);
                }
                catch (NotFoundException e)
                {
                    return NotFound(e.Message);
                }
            
                return CreatedAtAction(nameof(GetVisit), new { id }, createVisitRequest);
              
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetVisit(int id)
        {
            var visit = await _dbService.GetVisitAsync(id);
            return Ok(visit);
        }
    }
}

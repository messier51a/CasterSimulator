using CasterSimulator.Models;
using Microsoft.AspNetCore.Mvc;

namespace CasterSimulator.WebAPI.Controllers
{
    [ApiController]
    [Route("api/heatschedule")]
    public class HeatScheduleController : ControllerBase
    {
        private static List<Heat> _heats = new();
        
        [HttpGet]
        public IActionResult GetHeatSchedule()
        {
            return Ok(_heats);
        }
        
        [HttpPost]
        public IActionResult UpdateHeatSchedule([FromBody] List<Heat> heats)
        {
            if (heats == null || heats.Count == 0)
                return BadRequest("Heat list cannot be empty.");

            _heats = heats;
            return Ok(new { Message = "Heat list updated successfully." });
        }
    }
}
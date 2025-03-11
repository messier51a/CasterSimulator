using CasterSimulator.Models;
using Microsoft.AspNetCore.Mvc;

namespace CasterSimulator.WebAPI.Controllers
{
    [ApiController]
    [Route("api/cutschedule")]
    public class CutScheduleController : ControllerBase
    {
        private static List<Product> _cutSchedule = new();

        [HttpGet]
        public IActionResult GetCutSchedule()
        {
            return Ok(_cutSchedule);
        }

        [HttpPost]
        public IActionResult UpdateCutSchedule([FromBody] List<Product> cutSchedule)
        {
            _cutSchedule = cutSchedule;
            return Ok(new { Message = "Cut schedule updated successfully." });
        }
    }
}
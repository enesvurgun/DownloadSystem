using Microsoft.AspNetCore.Mvc;
using backend_api.Services;
using StackExchange.Redis;


namespace backend_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadController : ControllerBase
    {
        private readonly MessagePublisher _publisher;

        public DownloadController()
        {
            _publisher = new MessagePublisher();
        }

        [HttpPost("enqueue")]
        public IActionResult Enqueue([FromBody] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("URL is required.");

            _publisher.Publish(url);
            return Ok("URL enqueued successfully.");
        }
        [HttpGet("status")]
        public IActionResult GetStatus([FromQuery] string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return BadRequest("Filename is required.");

            try
            {
                var redis = ConnectionMultiplexer.Connect("localhost:6379");
                var db = redis.GetDatabase();

                var key = $"download:{filename}";
                var status = db.StringGet(key);

                if (status.IsNullOrEmpty)
                    return NotFound("No status found for the given file.");

                return Ok(status.ToString());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error reading status: {ex.Message}");
            }
        }


    }

}

using Microsoft.AspNetCore.Mvc;
using backend_api.Services;

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
    }
}

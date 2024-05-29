using Microsoft.AspNetCore.Mvc;
using Voicer.Service;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using Microsoft.Extensions.Logging;

namespace Voicer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoiceController : Controller
    {
        private readonly IVoiceService _voiceService;
        private readonly ILogger<VoiceController> _logger;

        public VoiceController(IVoiceService voiceService, ILogger<VoiceController> logger)
        {
            _voiceService = voiceService;
            _logger = logger;
        }

        [HttpPost("ProcessVoiceQuery")]
        public async Task<IActionResult> ProcessVoiceQuery([FromBody] string query)
        {
            _logger.LogInformation("Received query: {query}", query);
            var response = await _voiceService.ProcessQuery(query);
            _logger.LogInformation("Processed response: {response}", response);
            SpeakResponse(response);
            return Ok(response);
        }

        private void SpeakResponse(string response)
        {
            using (SpeechSynthesizer synth = new SpeechSynthesizer())
            {
                // synth.SelectVoice("Microsoft David Desktop");
                synth.Rate = 5;

                synth.SpeakAsync(response);
            }
        }

        [HttpPost("ChangeUsername")]
        public async Task<IActionResult> ChangeUsername([FromBody] ChangeUsernameRequest request)
        {
            _logger.LogInformation("Received ChangeUsername request: {request}", request);
            if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.NewUsername))
            {
                return BadRequest("UserId and NewUsername are required.");
            }

            var result = await _voiceService.ChangeUsernameAsync(request.UserId, request.NewUsername);
            if (result)
            {
                _logger.LogInformation("Username changed successfully.");
                return Ok("Username changed successfully.");
            }
            _logger.LogWarning("User not found: {UserId}", request.UserId);
            return NotFound("User not found.");
        }

        [HttpGet("Voicer")]
        public IActionResult Voicer()
        {
            return View();
        }
    }

    public class ChangeUsernameRequest
    {
        public string UserId { get; set; }
        public string NewUsername { get; set; }
    }
}

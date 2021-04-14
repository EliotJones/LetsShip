using System.Text.Json;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PriceFalcon.App.Jobs;
using PriceFalcon.Domain;
using PriceFalcon.Web.ViewModels.Home;

namespace PriceFalcon.Web.Controllers
{
    [Route("jobs")]
    public class JobsController : Controller
    {
        private readonly IMediator _mediator;

        public JobsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [ValidateAntiForgeryToken]
        [HttpPost("create")]
        public async Task<IActionResult> Create(CreateJobViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var selection = JsonSerializer.Deserialize<HtmlElementSelection>(model.SelectionJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (selection == null)
            {
                return BadRequest("No selection provided.");
            }

            var result = await _mediator.Send(
                new CreateJob
                {
                    DraftJobToken = model.Token,
                    Selector = selection
                });

            if (result.Status == CreateJobResult.ResultStatus.DraftNotFound)
            {
                return NotFound();
            }

            if (result.Status == CreateJobResult.ResultStatus.LimitReached)
            {
                return BadRequest("You have reached your limit of free jobs.");
            }

            if (result.Status == CreateJobResult.ResultStatus.SelectionInvalid)
            {
                return BadRequest("Selection not found.");
            }

            return RedirectToAction("Created", new {token = result.Token});
        }

        [HttpGet("created/{token}")]
        public IActionResult Created(string token)
        {
            return View();
        }

        [HttpGet("{token}")]
        public async Task<IActionResult> Index(string token)
        {
            ViewData["token"] = token;

            var data = await _mediator.Send(new GetJobData(token));

            return View(data);
        }

        [ValidateAntiForgeryToken]
        [HttpPost("cancel/{token}")]
        public async Task<IActionResult> CancelJob(string token)
        {
            await _mediator.Send(new CancelJob(token));

            return RedirectToAction("Index", "Home");
        }
    }
}
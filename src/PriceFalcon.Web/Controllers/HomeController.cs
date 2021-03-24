using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PriceFalcon.App;
using PriceFalcon.App.DraftJobs;
using PriceFalcon.App.Registration;
using PriceFalcon.Web.Services;
using PriceFalcon.Web.ViewModels;

namespace PriceFalcon.Web.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMediator _mediator;

        public HomeController(ILogger<HomeController> logger,
            IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(IndexViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _mediator.Send(
                new GetUserByEmail
                {
                    Email = model.Email
                });

            if (user == null || !user.IsVerified)
            {
                await _mediator.Send(new SendEmailInvite(model.Email));

            return RedirectToAction("CheckEmail");
            }

            await _mediator.Send(
                new RequestNewJobToken
                {
                    Email = model.Email
                });

            return RedirectToAction("CheckEmailNewJob");
        }

        [HttpGet("invited")]
        public IActionResult CheckEmail()
        {
            return View();
        }

        [HttpGet("job-emailed")]
        public IActionResult CheckEmailNewJob()
        {
            return View();
        }

        [HttpGet("register/{token}")]
        public async Task<IActionResult> Register(string token)
        {
            var validated = await _mediator.Send(
                new ValidateEmailToken
                {
                    Token = token
                });

            if (!validated.IsSuccess)
            {
                return BadRequest();
            }

            var jobToken = await _mediator.Send(new CreateValidatedNewJobToken
            {
                Email = validated.Email!
            });

            return RedirectToAction("CreateJob", new {token = WebUtility.UrlEncode(jobToken)});
        }

        [HttpGet("create/new/{token}")]
        public async Task<IActionResult> CreateJob(string token)
        {
            ViewData["token"] = token;

            var validationResult = await _mediator.Send(new ValidateNewJobToken(token));

            if (!validationResult.IsValid)
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost("create/new/{token}")]
        public async Task<IActionResult> CreateJobStart(string token, CreateJobViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var jobToken = await _mediator.Send(
                new CreateDraftJob
                {
                    Token = token,
                    Website = new Uri(model.Url)
                });

            if (jobToken == null)
            {
                // TODO: errors
                return RedirectToAction("Index");
            }

            return RedirectToAction("TrackDraftJob", new {token = jobToken});
        }

        [HttpGet("create/status/{token}")]
        public async Task<IActionResult> TrackDraftJob(string token)
        {
            var metadata = await _mediator.Send(new GetDraftJobTrackingMetadata(token));

            if (metadata == null)
            {
                return NotFound();
            }

            return View(new TrackDraftJobViewModel
            {
                Token = token,
                Website = metadata.Url.ToString()
            });
        }

        [HttpGet("create/track/{token}")]
        public async Task<IActionResult> TrackDraftJobStatuses(string token)
        {
            var items = await _mediator.Send(new GetDraftJobStatusesByToken(token));

            return Ok(items.Select(x => new TrackDraftJobLogViewModel
            {
                Status = x.Status,
                Created = x.Created,
                Message = x.Message
            }).ToList());
        }

        [HttpGet("create/select/{token}")]
        public async Task<IActionResult> SelectDraftJobItem(string token)
        {
            return View("SelectDraftJobItem", token);
        }

        [HttpGet("create/iframe/token")]
        public async Task<IActionResult> GetIframeContent(string token)
        {
            token = WebUtility.UrlDecode(token);

            var content = await _mediator.Send(new GetDraftJobHtmlByToken(token));

            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest();
            }

            var metadata = await _mediator.Send(new GetDraftJobTrackingMetadata(token));

            if (metadata == null)
            {
                return BadRequest();
            }

            var result = IframeHtmlPreparer.PrepareHtml(content, metadata.Url);

            return Content(result, "text/html");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

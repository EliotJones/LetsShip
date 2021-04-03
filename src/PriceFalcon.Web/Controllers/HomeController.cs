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
using PriceFalcon.App.Jobs;
using PriceFalcon.App.Registration;
using PriceFalcon.Domain;
using PriceFalcon.Web.Services;
using PriceFalcon.Web.ViewModels;
using PriceFalcon.Web.ViewModels.Home;

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

        [HttpGet("about")]
        public IActionResult About()
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
                    Email = model.Email!
                });

            if (user == null || !user.IsVerified)
            {
                var result = await _mediator.Send(new SendEmailInvite(model.Email!));

                if (result == SendEmailInviteResult.Invalid)
                {
                    return BadRequest($"Invalid or unrecognized email address: {model.Email}.");
                }

                if (result == SendEmailInviteResult.QuotaExceeded)
                {
                    return BadRequest("You have sent too many emails to this address.");
                }

                return RedirectToAction("CheckEmail");
            }

            var newJobTokenResult = await _mediator.Send(
                new RequestNewJobToken
                {
                    Email = model.Email!
                });

            if (newJobTokenResult.Status == RequestNewJobTokenResult.StatusReason.NoVerifiedUser)
            {
                return BadRequest("Your account isn't verified yet, check your email for an invite.");
            }

            if (newJobTokenResult.Status == RequestNewJobTokenResult.StatusReason.TooManyRequests)
            {
                return BadRequest("You've sent too many new job requests to this email address in the past few minutes, take a break for a bit.");
            }

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

            return RedirectToAction("CreateDraftJob", new { token = WebUtility.UrlEncode(jobToken) });
        }

        [HttpGet("create/new/{token}")]
        public async Task<IActionResult> CreateDraftJob(string token)
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
        public async Task<IActionResult> CreateDraftJobStart(string token, CreateDraftJobViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var jobToken = await _mediator.Send(
                new CreateDraftJob
                {
                    Token = token,
                    Website = new Uri(model.Url!)
                });

            if (jobToken == null)
            {
                // TODO: errors
                return RedirectToAction("Index");
            }

            return RedirectToAction("TrackDraftJob", new { token = jobToken });
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
            var jobForDraftJob = await _mediator.Send(new GetJobTokenByDraftJobToken(token));

            if (jobForDraftJob != null)
            {
                return RedirectToAction("Index", "Jobs", new { token = jobForDraftJob });
            }

            return View("SelectDraftJobItem", new CreateJobViewModel
            {
                Token = token
            });
        }

        [HttpGet("create/iframe/token")]
        public async Task<IActionResult> GetIframeContent(string token)
        {
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

        [HttpPost("create/calculate/token")]
        public async Task<IActionResult> CalculateSelectValidity(string token, SelectTrackItemViewModel model)
        {
            var selection = new HtmlElementSelection
            {
                Element = model.Element,
                Text = model.Text,
                Lineage = model.Lineage.Select(x => new HtmlElementSummary
                {
                    Id = x.Id,
                    Classes = x.Classes,
                    Tag = x.Tag,
                    Name = x.Name
                }).ToList()
            };

            var validationResult = await _mediator.Send(new ValidateDraftJobSelection(token, selection));

            return Ok(
                new SelectTrackItemResponseViewModel
                {
                    IsValid = validationResult.IsValid,
                    Price = validationResult.Price,
                    Reason = validationResult.Reason
                });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

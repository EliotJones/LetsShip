using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PriceFalcon.App;
using PriceFalcon.App.DraftJobs;
using PriceFalcon.App.Registration;
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
            token = WebUtility.UrlDecode(token);
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

        [HttpGet("create/{token}")]
        public async Task<IActionResult> CreateJob(string token)
        {
            ViewData["token"] = token;

            token = WebUtility.UrlDecode(token);

            var validationResult = await _mediator.Send(new ValidateNewJobToken(token));

            if (!validationResult.IsValid)
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost("create/{token}")]
        public async Task<IActionResult> CreateJobStart(string token, CreateJobViewModel model)
        {
            token = WebUtility.UrlDecode(token);

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



            return Ok();
        }

        [HttpGet("create/monitor/{token}")]
        public async Task<IActionResult> MonitorJobStart(string token)
        {
            token = WebUtility.UrlDecode(token);

            return NotFound();
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PriceFalcon.App;
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

        [HttpGet]
        [Route("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Route("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(IndexViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _mediator.Send(new SendEmailInvite(model.Email));

            return RedirectToAction("CheckEmail");
        }

        [HttpGet]
        [Route("invited")]
        public IActionResult CheckEmail()
        {
            return View();
        }

        [HttpGet]
        [Route("register/{token}")]
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

            return RedirectToAction("CreateJob", new {token = jobToken});
        }

        [HttpGet]
        [Route("create/{token}")]
        public IActionResult CreateJob(string token)
        {
            return View();
        }

        [HttpPost]
        [Route("create/{token}")]
        public async Task<IActionResult> CreateJobStart(string token, [FromBody] CreateJobViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var jobId = await _mediator.Send(
                new CreateJob
                {
                    Token = token,
                    Website = new Uri(model.Url)
                });

            return Ok();
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

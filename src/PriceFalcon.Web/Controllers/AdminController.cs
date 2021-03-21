using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PriceFalcon.App;
using PriceFalcon.Infrastructure;
using PriceFalcon.Web.ViewModels;

namespace PriceFalcon.Web.Controllers
{
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly PriceFalconConfig _config;
        private readonly IMediator _mediator;

        public AdminController(PriceFalconConfig config, IMediator mediator)
        {
            _config = config;
            _mediator = mediator;
        }

        [HttpGet("emails")]
        public async Task<IActionResult> ListEmails()
        {
            if (_config.Environment == EnvironmentType.Production)
            {
                return RedirectToAction("Index", "Home");
            }

            var emails = await _mediator.Send(new GetAllEmails());

            return View(
                new AdminEmailsListViewModel
                {
                    Emails = emails.Select(x => new EmailViewModel
                    {
                        Created = x.Created,
                        Body = x.Body,
                        Recipient = x.Recipient,
                        Subject = x.Subject
                    }).ToList()
                });
        }
    }
}
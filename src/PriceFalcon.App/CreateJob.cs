using System;
using MediatR;

namespace PriceFalcon.App
{
    public class CreateJob : IRequest<string?>
    {
        public string Email { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public Uri Website { get; set; } = new Uri("about:blank");
    }
}

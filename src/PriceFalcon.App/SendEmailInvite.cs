﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PriceFalcon.Domain;
using PriceFalcon.Infrastructure;
using PriceFalcon.Infrastructure.DataAccess;

namespace PriceFalcon.App
{
    public class SendEmailInvite : IRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    internal class SendEmailInviteHandler : IRequestHandler<SendEmailInvite>
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;

        public SendEmailInviteHandler(IUserRepository userRepository, ITokenService tokenService, IEmailService emailService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _emailService = emailService;
        }

        public async Task<Unit> Handle(SendEmailInvite request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
            {
                return Unit.Value;
            }

            var user = await _userRepository.GetByEmail(request.Email);

            if (user != null && user.IsVerified)
            {
                return Unit.Value;
            }

            if (user == null)
            {
                user = await _userRepository.CreateUser(request.Email);
            }

            var token = await _tokenService.GenerateToken(user.Id, Token.Purpose.ValidateEmail, DateTime.UtcNow.AddDays(10));

            var message = $@"<p>Hi there,</p>
                <p>In order to begin using PriceFalcon you need to validate your email. Use the link below to validate. If you didn't sign up you can safely ignore this email.</p>
                <p><a href='http://localhost:5220/register/{token}'>Sign me up!</a></p>";
            
            await _emailService.Send(user.Email, "Verify your email", message);

            return Unit.Value;
        }
    }
}

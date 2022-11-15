using ApiFluentValidator.Models;
using ApiFluentValidator.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace ApiFluentValidator.Security
{
    public class CustomBasicAuthenticationHandler : AuthenticationHandler<CustomBasicAuthenticationSchemeOptions>
    {
        private readonly IUserService _userService;

        public CustomBasicAuthenticationHandler(IUserService userService, 
                                                IOptionsMonitor<CustomBasicAuthenticationSchemeOptions> options, 
                                                ILoggerFactory logger, 
                                                UrlEncoder encoder, 
                                                ISystemClock clock) : base(options, logger, encoder, clock)
        {
            _userService = userService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // validation comes in here
            if (!Request.Headers.ContainsKey(Constants.AuthorizationHeaderName))
            {
                return AuthenticateResult.Fail("Header Not Found.");
            }

            var headerValue = Request.Headers[Constants.AuthorizationHeaderName];

            User? user = null;
            try
            {
                var authHeader = AuthenticationHeaderValue.Parse(headerValue);
                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
                var username = credentials[0];
                var password = credentials[1];
                user = await _userService.Authenticate(username, password);
            }
            catch
            {
                return AuthenticateResult.Fail("Invalid Authorization Header");
            }

            if (user == null)
                return AuthenticateResult.Fail("Invalid Username or Password");

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}

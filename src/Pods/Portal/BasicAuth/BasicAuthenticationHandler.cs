using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Portal
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IUserService _userService;

        public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
            UrlEncoder encoder, ISystemClock clock,IUserService userService) : base(options, logger, encoder, clock)
        {
            _userService = userService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            UserIdentity userIdentity;
 
            try
            {
                var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
                var username = credentials[0];
                var password = credentials[1];
                userIdentity = await _userService.Authenticate(username, password);
            }
            catch
            {
                return AuthenticateResult.Fail("Error Occured.Authorization failed.");
            }
 
            if (userIdentity == null)
                return AuthenticateResult.Fail("Invalid Credentials");
            var claims = new[]
            {
                new Claim(ClaimTypes.Role, userIdentity.Role),
                new Claim(ClaimTypes.Name, userIdentity.PartitionKey),
            };
 
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
 
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
 
            return AuthenticateResult.Success(ticket);
        }
    }
}
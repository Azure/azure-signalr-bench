using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Portal
{
    public class ValidationMiddleware
    {
        private readonly RequestDelegate _nextMiddleware;

        public ValidationMiddleware(RequestDelegate nextMiddleware)
        {
            _nextMiddleware = nextMiddleware;
        }

        public async Task Invoke(HttpContext context)
        {
            var method = context.Request.Method;
            if (method.ToLower().Equals("options")&&context.Request.Headers.ContainsKey("WebHook-Request-Origin"))
            {
                context.Response.Headers.Add("WebHook-Allowed-Origin", "*");
                context.Response.StatusCode = 200;
                context.Response.Headers.ContentLength = 0;
                await context.Response.CompleteAsync();
                return;
            }
            await _nextMiddleware(context);
        }
    }
}
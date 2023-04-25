using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Portal.Auth;

public class SecretMaskingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Regex _secretRegex;

    public SecretMaskingMiddleware(RequestDelegate next)
    {
        _next = next;
        _secretRegex = new Regex(@";AccessKey=(.*?);Version", RegexOptions.Compiled);
    }

    public async Task InvokeAsync(HttpContext context)
    {

        var user = context.User;
        bool canReadSecret = AuthUtil.CanReadSecret(user);
        if (!canReadSecret)
        {
            // Todo: use a custom stream to handle large response body
            using (var responseBody = new MemoryStream())
            {
                var originalBodyStream = context.Response.Body;
                context.Response.Body = responseBody;
                await _next(context);
                responseBody.Seek(0, SeekOrigin.Begin);
                context.Response.Body = originalBodyStream;
                // TODO: optimize
                var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
                var maskedBody = _secretRegex.Replace(responseBodyText, ";AccessKey=XXX;Version");
                if (maskedBody == responseBodyText)
                {
                    responseBody.Seek(0, SeekOrigin.Begin);
                    await responseBody.CopyToAsync(originalBodyStream);
                }
                else
                {
                    context.Response.ContentLength = null;
                    await context.Response.WriteAsync(maskedBody);
                }
            }
        }
        else
        {
            await _next(context);
        }
    }
}
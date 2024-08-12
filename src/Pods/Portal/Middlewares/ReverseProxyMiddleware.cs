using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Portal.Auth;

namespace Portal
{
    public class ReverseProxyMiddleware
    {
        private static HttpClient _httpClient;
        private readonly RequestDelegate _nextMiddleware;
        private readonly string token;
        private readonly PerfState _perfState;
        private ILogger<ReverseProxyMiddleware> _logger;

        public ReverseProxyMiddleware(RequestDelegate nextMiddleware,  ILogger<ReverseProxyMiddleware> logger, PerfState perfState)
        {
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                return true;
            };
            _httpClient = new HttpClient(httpClientHandler);
            _nextMiddleware = nextMiddleware;
            _perfState = perfState;
            _logger = logger;
            token = File.ReadAllText("/var/run/secrets/kubernetes.io/serviceaccount/token");
        }

        public async Task Invoke(HttpContext context)
        {
            var targetUri = BuildTargetUri(context.Request);

            if (targetUri != null)
            {
                var user = context.User;
                if (!AuthUtil.CanAccessCluster(user))
                {
                    context.Response.StatusCode = 403;
                    return;
                }
                var targetRequestMessage = CreateTargetMessage(context, targetUri);
                AddAuthorizationHeader(context.Request, targetRequestMessage);

                using (var responseMessage = await _httpClient.SendAsync(targetRequestMessage,
                    HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
                {
                    context.Response.StatusCode = (int) responseMessage.StatusCode;
                    _logger.LogInformation($"Proxying request to {targetUri} ,Received response with status code {responseMessage.StatusCode}");

                    CopyFromTargetResponseHeaders(context, responseMessage);

                    await ProcessResponseContent(context, responseMessage);
                }

                return;
            }

            await _nextMiddleware(context);
        }

        private async Task ProcessResponseContent(HttpContext context, HttpResponseMessage responseMessage)
        {
            var content = await responseMessage.Content.ReadAsByteArrayAsync();
            await context.Response.Body.WriteAsync(content);
        }


        private HttpRequestMessage CreateTargetMessage(HttpContext context, Uri targetUri)
        {
            var requestMessage = new HttpRequestMessage();
            CopyFromOriginalRequestContentAndHeaders(context, requestMessage);
            requestMessage.RequestUri = targetUri;
            requestMessage.Headers.Host = targetUri.Host;
            requestMessage.Method = GetMethod(context.Request.Method);

            return requestMessage;
        }

        private void CopyFromOriginalRequestContentAndHeaders(HttpContext context, HttpRequestMessage requestMessage)
        {
            var requestMethod = context.Request.Method;

            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(context.Request.Body);
                requestMessage.Content = streamContent;
            }

            foreach (var header in context.Request.Headers)
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        private void CopyFromTargetResponseHeaders(HttpContext context, HttpResponseMessage responseMessage)
        {
            foreach (var header in responseMessage.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();

            foreach (var header in responseMessage.Content.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();
            context.Response.Headers.Remove("transfer-encoding");
        }

        private static HttpMethod GetMethod(string method)
        {
            if (HttpMethods.IsDelete(method)) return HttpMethod.Delete;
            if (HttpMethods.IsGet(method)) return HttpMethod.Get;
            if (HttpMethods.IsHead(method)) return HttpMethod.Head;
            if (HttpMethods.IsOptions(method)) return HttpMethod.Options;
            if (HttpMethods.IsPost(method)) return HttpMethod.Post;
            if (HttpMethods.IsPut(method)) return HttpMethod.Put;
            if (HttpMethods.IsTrace(method)) return HttpMethod.Trace;
            return new HttpMethod(method);
        }

        private Uri BuildTargetUri(HttpRequest request)
        {
            string targetUri = null;
            PathString remainingPath;
            if (request.Path.StartsWithSegments("/k8s", out remainingPath))
                targetUri = "https://kubernetes-dashboard.kubernetes-dashboard.svc.cluster.local" + remainingPath;
            else if(request.Path.StartsWithSegments("/grafana", out remainingPath))
                targetUri = "http://grafana:3000" + remainingPath;
            foreach (var kv in _perfState.K8sProxy)
            {
                if (request.Path.StartsWithSegments($"/{kv.Key}", out remainingPath))
                {
                    targetUri = kv.Value + remainingPath;
                    break;
                }
            }
            if (targetUri == null)
                return null;
            if(request.QueryString.HasValue)
                targetUri +=  request.QueryString;
            return new Uri(targetUri);
        }

        private void AddAuthorizationHeader(HttpRequest request, HttpRequestMessage requestMessage)
        {
            if (request.Path.StartsWithSegments("/k8s"))
                requestMessage.Headers.Add("Authorization", $"Bearer {token}");
        }
    }
}
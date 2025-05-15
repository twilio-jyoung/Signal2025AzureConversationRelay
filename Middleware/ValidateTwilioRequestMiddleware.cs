using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio.Security;

namespace Signal2025AzureConversationRelay.Middleware
{
    public class ValidateTwilioRequestMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<ValidateTwilioRequestMiddleware> _logger;
        private readonly RequestValidator _requestValidator;
        private const string WebhookSignatureValidationFailureMessage = "Twilio Webhook Signature Validation - Failed";
        private const string WebhookSignatureValidationSuccessMessage = "Twilio Webhook Signature Validation - Success";

        public ValidateTwilioRequestMiddleware(ILogger<ValidateTwilioRequestMiddleware> logger, IConfiguration configuration)
        {
            _logger = logger;
            _requestValidator = new RequestValidator(configuration["Twilio:AuthToken"] ?? throw new Exception("'Twilio:AuthToken' not configured."));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            _logger.LogTrace("Twilio Webhook Received.  Starting Signature Validation.");

            var requestData = await context.GetHttpRequestDataAsync();
            var requestUrl = $"{requestData.Url.Scheme}://{requestData.Url.Host}{requestData.Url.PathAndQuery}";

            // twilio sends to https, but if using ngrok, it will redirect to http, causing signature validation to fail.
            if(requestUrl.Contains("http://") && (requestUrl.Contains("ngrok.io") || requestUrl.Contains("ngrok.app")))
                requestUrl = requestUrl.Replace("http://", "https://");

            var signature = GetHeaderValue(requestData, "X-Twilio-Signature");

            var contentType = GetHeaderValue(requestData, "Content-Type");
            if (!contentType.Contains("application/x-www-form-urlencoded"))
                throw new FailedWebhookSignatureValidationException(WebhookSignatureValidationFailureMessage);

            var body = await new StreamReader(requestData.Body).ReadToEndAsync();

            var parameters = ParseFormUrlEncodedBody(body);
            var isValid = _requestValidator.Validate(requestUrl, parameters, signature);

            if (!isValid){
                _logger.LogError(WebhookSignatureValidationFailureMessage);
                throw new FailedWebhookSignatureValidationException(WebhookSignatureValidationFailureMessage);
            }

            foreach (var param in parameters)
            {
                // _logger.LogTrace($"Twilio Webhook Parameter: {param.Key} = {param.Value}");
                context.Items.Add(param.Key, param.Value);
            }
            
            _logger.LogTrace(WebhookSignatureValidationSuccessMessage);
            await next(context);
        }

        private Dictionary<string, string> ParseFormUrlEncodedBody(string body)
        {
            return body.Split('&')
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]));
        }

        private string GetHeaderValue(HttpRequestData httpRequestData, string headerName)
        {
            return httpRequestData.Headers.TryGetValues(headerName, out var values) ? values.FirstOrDefault() : null;
        }
    }
}
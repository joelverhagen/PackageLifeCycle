using Microsoft.Extensions.Logging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NuGet.PackageLifeCycle;

public class DeprecationService
{
    private static readonly IReadOnlySet<string> HeadersToRedact = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "X-NuGet-ApiKey", "Authorization", "Cookie", "Set-Cookie"
    };

    private readonly ILogger<DeprecationService> _logger;
    private readonly Common.ILogger _httpLogger;

    public DeprecationService(ILogger<DeprecationService> logger)
    {
        _logger = logger;
        _httpLogger = logger.ToNuGetLogger(mapInformationToDebug: true);
    }

    public async Task<bool> DeprecateAsync(
        string packagePublishUrl,
        string packageId,
        string? apiKey,
        DeprecationRequest request,
        bool confirm,
        SourceRepository sourceRepository,
        CancellationToken token)
    {
        var httpSourceResource = await sourceRepository.GetResourceAsync<HttpSourceResource>(token);
        var httpSource = httpSourceResource.HttpSource;

        var requestUrl = $"{packagePublishUrl.TrimEnd('/')}/{packageId}/deprecations";

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
        });

        if (confirm)
        {
            Console.WriteLine(
                $"The deprecation request will be for package '{packageId}' and have the following content:" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"{json}" +
                $"{Environment.NewLine}");

            var confirmed = false;
            while (!confirmed)
            {
                Console.Write($"Do you want to proceed? (y/n) ");
                var input = Console.ReadLine()?.Trim().ToLowerInvariant();
                switch (input)
                {
                    case "y":
                        confirmed = true;
                        break;
                    case "n":
                        return false;
                }
            }
        }

        var contentFactory = () => new StringContent(json, Encoding.UTF8, "application/json");

        while (true)
        {
            var result = await httpSource.ProcessResponseAsync(
                new HttpSourceRequest(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Put, requestUrl)
                    {
                        Content = contentFactory()
                    };

                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        request.Headers.Add("X-NuGet-ApiKey", apiKey);
                    }

                    return request;
                }),
                async response =>
                {
                    _logger.LogDebug("The full HTTP session is below:" + Environment.NewLine + "{HttpSession}", await GetHttpSessionStringAsync(response, contentFactory()));

                    if ((response.StatusCode == HttpStatusCode.Forbidden && response.Headers.RetryAfter is not null)
                        || response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        var now = DateTimeOffset.UtcNow;
                        var retryAfter = now.AddSeconds(60);
                        if (response.Headers.RetryAfter is not null)
                        {
                            if (response.Headers.RetryAfter.Delta.HasValue)
                            {
                                retryAfter = now + response.Headers.RetryAfter.Delta.Value;
                            }
                            else if (response.Headers.RetryAfter.Date.HasValue)
                            {
                                retryAfter = response.Headers.RetryAfter.Date.Value.ToLocalTime();
                            }
                        }

                        _logger.LogWarning("The deprecation request was rate limited: {StatusCode} {ReasonPhrase}", (int)response.StatusCode, response.ReasonPhrase);
                        return (Success: false, Retry: true, RetryAfter: retryAfter - now);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("The deprecation request failed: {StatusCode} {ReasonPhrase}", (int)response.StatusCode, response.ReasonPhrase);
                        return (Success: false, Retry: false, RetryAfter: TimeSpan.Zero);
                    }

                    return (Success: true, Retry: false, RetryAfter: TimeSpan.Zero);
                },
                _httpLogger,
                token);

            if (result.Success)
            {
                return true;
            }

            if (result.Retry)
            {
                _logger.LogWarning("Retry will occur in {RetrySeconds} seconds.", (int)Math.Round(result.RetryAfter.TotalSeconds, 0));
                await Task.Delay(result.RetryAfter);
                continue;
            }

            return false;
        }
        
    }

    private async Task<string> GetHttpSessionStringAsync(HttpResponseMessage response, HttpContent? requestContentOverride)
    {

        var sb = new StringBuilder();

        var request = response.RequestMessage;
        if (request is not null)
        {
            sb.AppendLine("=== REQUEST ===");
            sb.AppendLine(await request.GetDebugStringAsync(HeadersToRedact, requestContentOverride));
            sb.AppendLine();
        }

        sb.AppendLine("=== RESPONSE ===");
        sb.AppendLine(await response.GetDebugStringAsync(HeadersToRedact));

        return sb.ToString();
    }
}

using System.Text;

public static class HttpMessageExtensions
{
    private enum HeaderDisplayMode
    {
        JoinSpace,
        JoinComma,
        MultipleLines,
    }

    private static readonly IReadOnlyDictionary<string, HeaderDisplayMode> HeaderDisplayModes = new Dictionary<string, HeaderDisplayMode>(StringComparer.OrdinalIgnoreCase)
    {
        { "User-Agent", HeaderDisplayMode.JoinSpace },
        { "Accept-Encoding", HeaderDisplayMode.JoinComma },
        { "Set-Cookie", HeaderDisplayMode.MultipleLines },
    };

    public static async Task<string> GetDebugStringAsync(this HttpRequestMessage request, IReadOnlySet<string> headersToRedact, HttpContent? requestContentOverride = null)
    {
        var builder = new StringBuilder();

        builder.AppendFormat("{0} {1} HTTP/{2}\r\n", request.Method, request.RequestUri?.AbsoluteUri, request.Version);

        await AppendHeadersAndBody(builder, request.Headers, headersToRedact, requestContentOverride ?? request.Content);

        return builder.ToString();
    }

    public static async Task<string> GetDebugStringAsync(this HttpResponseMessage response, IReadOnlySet<string> headersToRedact)
    {
        var builder = new StringBuilder();

        builder.AppendFormat("HTTP/{0} {1} {2}\r\n", response.Version, (int)response.StatusCode, response.ReasonPhrase);

        await AppendHeadersAndBody(builder, response.Headers, headersToRedact, response.Content);

        return builder.ToString();
    }

    private static async Task AppendHeadersAndBody(
        StringBuilder builder,
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers,
        IReadOnlySet<string> headersToRedact,
        HttpContent? content)
    {
        if (content != null)
        {
            headers = headers.Concat(content.Headers);
        }

        // Write the headers.
        foreach (var header in headers)
        {
            if (!HeaderDisplayModes.TryGetValue(header.Key, out var mode))
            {
                mode = HeaderDisplayMode.MultipleLines;
            }

            if (mode == HeaderDisplayMode.MultipleLines)
            {
                foreach (var value in header.Value)
                {
                    builder.AppendFormat("{0}: {1}\r\n", header.Key, headersToRedact.Contains(header.Key) ? "[REDACTED]" : value);
                }
            }
            else
            {
                var delimiter = mode switch
                {
                    HeaderDisplayMode.JoinSpace => " ",
                    HeaderDisplayMode.JoinComma => ", ",
                    _ => throw new NotImplementedException(),
                };

                builder.AppendFormat("{0}: {1}\r\n", header.Key, headersToRedact.Contains(header.Key) ? "[REDACTED]" : string.Join(delimiter, header.Value));
            }
        }

        builder.Append("\r\n");

        // Write the request or response body.
        if (content != null)
        {
            using (var stream = await content.ReadAsStreamAsync())
            {
                var buffer = new byte[1024 * 32 + 1];
                var totalRead = 0;
                int read;
                do
                {
                    read = await stream.ReadAsync(buffer, totalRead, buffer.Length - totalRead);
                    totalRead += read;
                }
                while (totalRead < buffer.Length && read > 0);

                var hasMore = totalRead == buffer.Length;
                var dataLength = Math.Min(totalRead, buffer.Length - 1);

                try
                {
                    using (var memoryStream = new MemoryStream(buffer, 0, dataLength))
                    using (var reader = new StreamReader(memoryStream))
                    {
                        // Write the response body as a string.
                        builder.Append(reader.ReadToEnd());
                    }
                }
                catch (Exception)
                {
                    // Write the response body as base64 bytes.
                    builder.Append("<base64>\r\n");
                    builder.Append(Convert.ToBase64String(buffer, 0, dataLength));
                }

                if (hasMore)
                {
                    builder.Append("\r\n<truncated>");
                }
            }
        }
    }
}
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace GlobomaticsHelpdesk.Web.Services;

public record ApiResponse(
    int StatusCode,
    string ReasonPhrase,
    bool IsSuccess,
    string Body,
    string? ContentType,
    IReadOnlyDictionary<string, string> Headers,
    TimeSpan Elapsed);

public class HelpdeskApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions PrettyJson = new()
    {
        WriteIndented = true,
    };

    public Uri BaseAddress => http.BaseAddress ?? new Uri("about:blank");

    public Task<ApiResponse> SendAsync(
        HttpMethod method,
        string path,
        string? apiKey,
        object? jsonBody = null,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(method, path);

        if (jsonBody is not null)
        {
            var json = JsonSerializer.Serialize(jsonBody);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return SendAsync(request, apiKey, ct);
    }

    public Task<ApiResponse> SendRawAsync(
        HttpMethod method,
        string path,
        string? apiKey,
        string? rawJsonBody,
        CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(method, path);

        if (!string.IsNullOrWhiteSpace(rawJsonBody))
        {
            request.Content = new StringContent(rawJsonBody, Encoding.UTF8, "application/json");
        }

        return SendAsync(request, apiKey, ct);
    }

    public async Task<ApiResponse> SendAsync(
        HttpRequestMessage request,
        string? apiKey,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Remove("X-Api-Key");
            request.Headers.Add("X-Api-Key", apiKey);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ApiResponse(
                StatusCode: 0,
                ReasonPhrase: ex.GetType().Name,
                IsSuccess: false,
                Body: ex.Message,
                ContentType: null,
                Headers: new Dictionary<string, string>(),
                Elapsed: sw.Elapsed);
        }

        sw.Stop();

        var body = await response.Content.ReadAsStringAsync(ct);
        var contentType = response.Content.Headers.ContentType?.MediaType;

        if (!string.IsNullOrEmpty(body)
            && contentType is not null
            && contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                body = JsonSerializer.Serialize(doc.RootElement, PrettyJson);
            }
            catch
            {
                // Leave body as-is when it isn't valid JSON.
            }
        }

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in response.Headers)
        {
            headers[h.Key] = string.Join(", ", h.Value);
        }
        foreach (var h in response.Content.Headers)
        {
            headers[h.Key] = string.Join(", ", h.Value);
        }

        return new ApiResponse(
            StatusCode: (int)response.StatusCode,
            ReasonPhrase: response.ReasonPhrase ?? string.Empty,
            IsSuccess: response.IsSuccessStatusCode,
            Body: body,
            ContentType: contentType,
            Headers: headers,
            Elapsed: sw.Elapsed);
    }

    /// <summary>
    /// Sends a request to an SSE endpoint and yields each data event as a string.
    /// </summary>
    public async IAsyncEnumerable<string> StreamAsync(
        string path,
        string? apiKey,
        string? rawJsonBody,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);

        if (!string.IsNullOrWhiteSpace(rawJsonBody))
        {
            request.Content = new StringContent(rawJsonBody, Encoding.UTF8, "application/json");
        }

        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Add("X-Api-Key", apiKey);
        }

        using var response = await http.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, ct);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        // An SSE event can span multiple `data:` lines. Per the spec, each
        // `data:` line is concatenated with a "\n" separator and the event is
        // dispatched on a blank line. We must reconstruct those newlines here,
        // otherwise multi-line deltas from the model get flattened.
        var dataLines = new List<string>();

        while (true)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;

            if (line.Length == 0)
            {
                // Blank line: dispatch the buffered event.
                if (dataLines.Count > 0)
                {
                    yield return string.Join("\n", dataLines);
                    dataLines.Clear();
                }
                continue;
            }

            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                // A single optional space after the colon is part of the framing.
                var value = line["data:".Length..];
                if (value.StartsWith(' '))
                {
                    value = value[1..];
                }
                dataLines.Add(value);
            }
        }

        // Flush any trailing event not terminated by a blank line.
        if (dataLines.Count > 0)
        {
            yield return string.Join("\n", dataLines);
        }
    }
}

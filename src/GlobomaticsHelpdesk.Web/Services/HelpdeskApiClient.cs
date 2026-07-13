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
}

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Part3.Services;

/// <summary>
/// HTTP client for the existing RaceDay Part 2 API.
/// Business rules remain in the API.
/// </summary>
public sealed class RaceDayApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _context;

    private readonly JsonSerializerOptions _json =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    public RaceDayApiClient(
        HttpClient http,
        IConfiguration config,
        IHttpContextAccessor context)
    {
        _http = http;
        _context = context;

        var baseUrl = config["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl) ||
            !Uri.TryCreate(baseUrl, UriKind.Absolute, out var apiUri) ||
            (apiUri.Scheme != Uri.UriSchemeHttp &&
             apiUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "ApiSettings:BaseUrl is missing or invalid. " +
                "Set it to the root URL of the Part 2 API in appsettings.json.");
        }

        _http.BaseAddress = new Uri(
            apiUri.AbsoluteUri.EndsWith('/')
                ? apiUri.AbsoluteUri
                : apiUri.AbsoluteUri + "/");

        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<ApiResponse<T>> GetAsync<T>(
        string path,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Get, path);
        return await SendAsync<T>(request, ct);
    }

    public async Task<ApiResponse<TResponse>> PostAsync<TBody, TResponse>(
        string path,
        TBody body,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Post, path);

        request.Content = new StringContent(
            JsonSerializer.Serialize(body, _json),
            Encoding.UTF8,
            "application/json");

        return await SendAsync<TResponse>(request, ct);
    }

    public async Task<ApiResponse<TResponse>> PutAsync<TBody, TResponse>(
        string path,
        TBody body,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Put, path);

        request.Content = new StringContent(
            JsonSerializer.Serialize(body, _json),
            Encoding.UTF8,
            "application/json");

        return await SendAsync<TResponse>(request, ct);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(
        string path,
        CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Delete, path);

        try
        {
            using var response = await _http.SendAsync(request, ct);

            var message = await ReadMessage(response, ct);

            return new ApiResponse<bool>(
                response.IsSuccessStatusCode,
                response.IsSuccessStatusCode,
                message,
                response.StatusCode);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new ApiResponse<bool>(
                false,
                false,
                "The RaceDay API did not respond in time. Check that the Part 2 API is running.",
                HttpStatusCode.RequestTimeout);
        }
        catch (HttpRequestException)
        {
            return new ApiResponse<bool>(
                false,
                false,
                "Could not connect to the RaceDay API. Check its address and make sure it is running.",
                HttpStatusCode.ServiceUnavailable);
        }
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string path)
    {
        var request = new HttpRequestMessage(
            method,
            path.TrimStart('/'));

        var apiCookie = _context.HttpContext?
            .Session.GetString("RaceDayApiCookie");

        if (!string.IsNullOrWhiteSpace(apiCookie))
        {
            request.Headers.TryAddWithoutValidation(
                "Cookie",
                apiCookie);
        }

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return request;
    }

    private async Task<ApiResponse<T>> SendAsync<T>(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        try
        {
            using var response = await _http.SendAsync(request, ct);

            var raw = await response.Content.ReadAsStringAsync(ct);

            var cookies = response.Headers.TryGetValues(
                    "Set-Cookie",
                    out var setCookies)
                ? setCookies
                    .Select(cookie => cookie.Split(';', 2)[0])
                    .Where(cookie => cookie.Contains('='))
                    .ToArray()
                : Array.Empty<string>();

            if (cookies.Length > 0)
            {
                _context.HttpContext?.Session.SetString(
                    "RaceDayApiCookie",
                    string.Join("; ", cookies));
            }

            T? data = default;

            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    data = JsonSerializer.Deserialize<T>(raw, _json);
                }
                catch (JsonException)
                {
                    // The response was not valid JSON for the requested type.
                }
            }

            var message = ExtractMessage(raw)
                ?? (response.IsSuccessStatusCode
                    ? "Request completed."
                    : "The request could not be completed.");

            return new ApiResponse<T>(
                response.IsSuccessStatusCode,
                data,
                message,
                response.StatusCode);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new ApiResponse<T>(
                false,
                default,
                "The RaceDay API did not respond in time. Check that the Part 2 API is running.",
                HttpStatusCode.RequestTimeout);
        }
        catch (HttpRequestException)
        {
            return new ApiResponse<T>(
                false,
                default,
                "Could not connect to the RaceDay API. Check its address and make sure it is running.",
                HttpStatusCode.ServiceUnavailable);
        }
    }

    private static async Task<string> ReadMessage(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        var raw = await response.Content.ReadAsStringAsync(ct);

        return ExtractMessage(raw)
            ?? (response.IsSuccessStatusCode
                ? "Request completed."
                : "The request could not be completed.");
    }

    private static string? ExtractMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(raw);
            var root = document.RootElement;

            // TryGetProperty is valid only when the JSON root is an object.
            // A normal API collection such as [...] is an array.
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            string[] propertyNames =
            {
                "message", "Message",
                "title", "Title",
                "detail", "Detail",
                "error", "Error"
            };

            foreach (var name in propertyNames)
            {
                if (!root.TryGetProperty(name, out var value))
                {
                    continue;
                }

                if (value.ValueKind == JsonValueKind.String)
                {
                    var message = value.GetString();

                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        return message;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // A non-JSON response uses the safe fallback message.
        }

        return null;
    }
}

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string Message,
    HttpStatusCode StatusCode);

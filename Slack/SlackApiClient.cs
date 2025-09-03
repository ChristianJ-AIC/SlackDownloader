using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using SlackConsoleApp.Slack.Models;

namespace SlackConsoleApp.Slack;

public class SlackApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions;

    public SlackApiClient(string accessToken, HttpMessageHandler? handler = null)
    {
        _http = handler is null ? new HttpClient() : new HttpClient(handler);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task<ConversationsListResponse> ListConversationsPageAsync(
        string? cursor,
        IEnumerable<string> types,
        int limit = 200,
        CancellationToken ct = default)
    {
        var typesParam = string.Join(",", types);
        var url = $"https://slack.com/api/users.conversations?limit={limit}&types={Uri.EscapeDataString(typesParam)}";
        if (!string.IsNullOrEmpty(cursor))
            url += $"&cursor={Uri.EscapeDataString(cursor)}";

        return await SendSlackRequestAsync<ConversationsListResponse>(url, ct);
    }

    public async IAsyncEnumerable<SlackMessage> GetConversationHistoryAsync(
        string channelId,
        int? maxMessages = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        string? cursor = null;
        int count = 0;
        do
        {
            var url = $"https://slack.com/api/conversations.history?channel={Uri.EscapeDataString(channelId)}&limit=200";
            if (!string.IsNullOrEmpty(cursor))
                url += $"&cursor={Uri.EscapeDataString(cursor)}";

            var resp = await SendSlackRequestAsync<ConversationHistoryResponse>(url, ct);

            if (resp.Messages is not null)
            {
                foreach (var m in resp.Messages)
                {
                    yield return m;
                    count++;
                    if (maxMessages.HasValue && count >= maxMessages.Value)
                        yield break;
                }
            }

            cursor = resp.ResponseMetadata?.NextCursor;
        } while (!string.IsNullOrEmpty(cursor) && !ct.IsCancellationRequested);
    }

    private async Task<T> SendSlackRequestAsync<T>(string url, CancellationToken ct)
    {
        while (true)
        {
            using var httpResp = await _http.GetAsync(url, ct);

            if ((int)httpResp.StatusCode == 429)
            {
                var retryAfter = httpResp.Headers.TryGetValues("Retry-After", out var vals) &&
                                 int.TryParse(vals.FirstOrDefault(), out int seconds)
                    ? seconds
                    : 5;
                Console.WriteLine($"Rate limited. Waiting {retryAfter}s...");
                await Task.Delay(TimeSpan.FromSeconds(retryAfter), ct);
                continue;
            }

            if (!httpResp.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Slack API HTTP {(int)httpResp.StatusCode} {httpResp.ReasonPhrase}");
            }

            var payload = await httpResp.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("ok", out var okElem) || !okElem.GetBoolean())
            {
                throw new InvalidOperationException($"Slack API error: {payload}");
            }

            return JsonSerializer.Deserialize<T>(payload, _jsonOptions)!;
        }
    }
}
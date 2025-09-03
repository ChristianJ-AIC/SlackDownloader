using SlackConsoleApp.Slack;
using SlackConsoleApp.Slack.Models;

namespace SlackConsoleApp.Services;

public class ConversationService
{
    private readonly SlackApiClient _api;

    public ConversationService(SlackApiClient api)
    {
        _api = api;
    }

    public async Task<List<SlackConversation>> ListAllConversationsAsync(CancellationToken ct = default)
    {
        var types = new[] { "public_channel", "private_channel", "im", "mpim" };
        string? cursor = null;
        var all = new List<SlackConversation>();

        do
        {
            var page = await _api.ListConversationsPageAsync(cursor, types, 200, ct);
            if (page.Channels is not null)
                all.AddRange(page.Channels);

            cursor = page.ResponseMetadata?.NextCursor;
        } while (!string.IsNullOrEmpty(cursor) && !ct.IsCancellationRequested);

        return all
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToList();
    }
}
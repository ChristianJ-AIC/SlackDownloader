using SlackConsoleApp.Slack;
using SlackConsoleApp.Slack.Models;

namespace SlackConsoleApp.Services;

public class MessageExportService
{
    private readonly SlackApiClient _api;

    public MessageExportService(SlackApiClient api)
    {
        _api = api;
    }

    public async Task<List<SlackMessage>> GetConversationHistoryAsync(string channelId, int? maxMessages = null, CancellationToken ct = default)
    {
        var results = new List<SlackMessage>();
        await foreach (var msg in _api.GetConversationHistoryAsync(channelId, maxMessages, ct))
        {
            results.Add(msg);
        }
        return results;
    }
}
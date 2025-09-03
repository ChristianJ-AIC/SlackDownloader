namespace SlackConsoleApp.Slack.Models;

public class ConversationHistoryResponse
{
    public bool Ok { get; set; }
    public List<SlackMessage>? Messages { get; set; }
    public bool HasMore { get; set; }
    public ResponseMetadata? ResponseMetadata { get; set; }
}
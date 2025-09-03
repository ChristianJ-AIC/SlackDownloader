namespace SlackConsoleApp.Slack.Models;

public class ConversationsListResponse
{
    public bool Ok { get; set; }
    public List<SlackConversation>? Channels { get; set; }
    public ResponseMetadata? ResponseMetadata { get; set; }
}
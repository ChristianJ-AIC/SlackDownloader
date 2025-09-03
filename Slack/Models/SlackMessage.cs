using System.Text.Json.Serialization;

namespace SlackConsoleApp.Slack.Models;

public class SlackMessage
{
    public string? Type { get; set; }
    public string? User { get; set; }
    public string? Text { get; set; }
    public string? Ts { get; set; }

    [JsonPropertyName("thread_ts")]
    public string? ThreadTs { get; set; }

    [JsonPropertyName("parent_user_id")]
    public string? ParentUserId { get; set; }

    [JsonPropertyName("subtype")]
    public string? Subtype { get; set; }

    public List<SlackMessageAttachment>? Attachments { get; set; }
    public List<SlackMessageBlock>? Blocks { get; set; }
}

public class SlackMessageAttachment
{
    public string? Fallback { get; set; }
    public string? Text { get; set; }
    public string? Color { get; set; }
}

public class SlackMessageBlock
{
    public string? Type { get; set; }
}
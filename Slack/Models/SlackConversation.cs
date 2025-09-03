using System.Text.Json.Serialization;

namespace SlackConsoleApp.Slack.Models;

public class SlackConversation
{
    public string? Id { get; set; }
    public string? Name { get; set; }

    [JsonPropertyName("is_private")]
    public bool IsPrivate { get; set; }

    [JsonPropertyName("is_im")]
    public bool IsIM { get; set; }

    [JsonPropertyName("is_mpim")]
    public bool IsMpim { get; set; }

    [JsonPropertyName("is_channel")]
    public bool IsChannel { get; set; }

    [JsonPropertyName("is_group")]
    public bool IsGroup { get; set; }

    public string? Creator { get; set; }

    public string TypeLabel =>
        IsIM ? "DM" :
        IsMpim ? "Group DM" :
        IsPrivate ? "Private Channel" :
        "Public Channel";
}
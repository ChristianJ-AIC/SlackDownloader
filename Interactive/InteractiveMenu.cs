using System.Text;
using ConsoleSlackDownloader.Interactive;
using SlackConsoleApp.Services;
using SlackConsoleApp.Slack;

namespace SlackConsoleApp.Interactive;

public class InteractiveMenu
{
    private string? _accessToken; // = Environment.GetEnvironmentVariable("SLACK_ACCESS_TOKEN");
    private string _exportDir = "export";
    private int? _maxMessagesPerConversation = null;
    private bool _showIdsInList = true;

    private SlackApiClient? _api;
    private ConversationService? _conversationService;
    private MessageExportService? _exportService;
    private AppConfig appConfig;

    public InteractiveMenu(AppConfig appConfig)
    {
        this.appConfig = appConfig;
        _accessToken = appConfig.Slack.AccessToken;
    }

    public async Task RunAsync()
    {
        bool exit = false;
        while (!exit)
        {
            try
            {
                PrintHeader();
                PrintStatus();
                PrintMenu();

                Console.Write("Select an option: ");
                var key = Console.ReadLine()?.Trim();

                switch (key)
                {
                    case "1":
                        await SetOrShowTokenAsync();
                        break;
                    case "2":
                        await RunOAuthAsync();
                        break;
                    case "3":
                        await ListConversationsAsync();
                        break;
                    case "4":
                        await ExportHistoriesAsync();
                        break;
                    case "5":
                        SetExportDirectory();
                        break;
                    case "6":
                        SetMaxMessages();
                        break;
                    case "7":
                        ToggleShowIds();
                        break;
                    case "8":
                        exit = true;
                        Console.WriteLine("Exiting...");
                        break;
                    default:
                        Console.WriteLine("Unknown selection. Try again.");
                        break;
                }

                if (!exit)
                {
                    Console.WriteLine();
                    Console.Write("Press ENTER to continue...");
                    Console.ReadLine();
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    private void PrintHeader()
    {
        Console.Clear();
        Console.WriteLine("=================================================");
        Console.WriteLine("  Slack Interactive Utility");
        Console.WriteLine("=================================================\n");
    }

    private void PrintStatus()
    {
        Console.WriteLine("Current Settings:");
        Console.WriteLine($"  Token set: {(!string.IsNullOrEmpty(_accessToken) ? "Yes" : "No")}");
        Console.WriteLine($"  Export Directory: {_exportDir}");
        Console.WriteLine($"  Max Messages / Conversation: {(_maxMessagesPerConversation.HasValue ? _maxMessagesPerConversation.ToString() : "Unlimited")}");
        Console.WriteLine($"  Show IDs in List: {_showIdsInList}");
        Console.WriteLine();
    }

    private void PrintMenu()
    {
        Console.WriteLine("Menu:");
        Console.WriteLine("  1) Set / Show Token");
        Console.WriteLine("  2) Run OAuth Flow to Acquire Token");
        Console.WriteLine("  3) List Conversations");
        Console.WriteLine("  4) Export Histories");
        Console.WriteLine("  5) Set Export Directory");
        Console.WriteLine("  6) Set Max Messages Per Conversation");
        Console.WriteLine("  7) Toggle Show IDs in Conversation List");
        Console.WriteLine("  8) Quit");
        Console.WriteLine();
    }

    private async Task EnsureClientsAsync()
    {
        if (string.IsNullOrWhiteSpace(_accessToken))
            throw new InvalidOperationException("No access token set. Use option 1 or 2 first.");

        if (_api is null)
        {
            _api = new SlackApiClient(_accessToken);
            _conversationService = new ConversationService(_api);
            _exportService = new MessageExportService(_api);
        }
    }

    private async Task SetOrShowTokenAsync()
    {
        Console.WriteLine();
        if (!string.IsNullOrEmpty(_accessToken))
        {
            Console.WriteLine("A token is currently set (not displayed for safety).");
            Console.Write("Do you want to replace it? (y/N): ");
            var ans = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (ans != "y" && ans != "yes")
                return;
        }

        Console.Write("Enter token (paste, input hidden): ");
        var newToken = ReadSecretFromConsole();
        if (string.IsNullOrWhiteSpace(newToken))
        {
            Console.WriteLine("No token entered. Keeping existing.");
            return;
        }

        _accessToken = newToken.Trim();
        _api = null; // force re-init
        Console.WriteLine("Token set.");
    }

    private async Task RunOAuthAsync()
    {
        Console.WriteLine("Starting OAuth flow...");
        var oauth = new SlackOAuth();
        var token = await oauth.RunInteractiveOAuthAsync(appConfig);
        if (token is null)
        {
            Console.WriteLine("OAuth failed or was cancelled.");
            return;
        }
        _accessToken = token;
        _api = null;
        Console.WriteLine("Token stored in memory (NOT persisted).");
    }

    private async Task ListConversationsAsync()
    {
        await EnsureClientsAsync();
        Console.WriteLine("Fetching conversations (all types)...");
        var conversations = await _conversationService!.ListAllConversationsAsync();

        Console.WriteLine($"Found {conversations.Count} conversations:\n");
        int width = conversations.Select(c => (c.Name ?? c.Id ?? "").Length).DefaultIfEmpty(0).Max();
        width = Math.Clamp(width, 10, 40);

        foreach (var c in conversations.OrderBy(c => c.Name ?? c.Id))
        {
            var name = (c.Name ?? c.Id ?? "").PadRight(width);
            var idDisplay = _showIdsInList ? $" (ID={c.Id})" : "";
            Console.WriteLine($"{name}  [{c.TypeLabel}]  Private={c.IsPrivate} IM={c.IsIM}{idDisplay}");
        }
    }

    private async Task ExportHistoriesAsync()
    {
        await EnsureClientsAsync();
        Directory.CreateDirectory(_exportDir);

        Console.WriteLine("Fetching conversation list for export...");
        var conversations = await _conversationService!.ListAllConversationsAsync();

        Console.WriteLine($"Will export {conversations.Count} conversation histories to '{_exportDir}'.");

        Console.Write("Continue? (y/N): ");
        var input = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (input != "y" && input != "yes")
        {
            Console.WriteLine("Aborted by user.");
            return;
        }

        foreach (var conversation in conversations)
        {
            var displayName = conversation.Name ?? conversation.Id ?? "(unknown)";
            Console.WriteLine($"\nExporting: {displayName}  ({conversation.TypeLabel})");
            try
            {
                var msgs = await _exportService!.GetConversationHistoryAsync(conversation.Id!, _maxMessagesPerConversation);
                var fileName = SanitizeFileName(displayName) + ".json";
                var path = Path.Combine(_exportDir, fileName);

                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    conversation = conversation,
                    exportedUtc = DateTime.UtcNow,
                    messageCount = msgs.Count,
                    messages = msgs
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                await File.WriteAllTextAsync(path, json);
                Console.WriteLine($"  -> Wrote {msgs.Count} messages to {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR exporting {displayName}: {ex.Message}");
            }
        }

        Console.WriteLine("\nExport complete.");
    }

    private void SetExportDirectory()
    {
        Console.Write($"Current directory: {_exportDir}. Enter new (blank to keep): ");
        var dir = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(dir))
        {
            Console.WriteLine("Keeping existing.");
            return;
        }

        _exportDir = dir.Trim();
        Console.WriteLine($"Export directory set to '{_exportDir}'.");
    }

    private void SetMaxMessages()
    {
        Console.Write($"Current max messages per conversation: {(_maxMessagesPerConversation?.ToString() ?? "Unlimited")}. Enter new integer or blank for unlimited: ");
        var val = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(val))
        {
            _maxMessagesPerConversation = null;
            Console.WriteLine("Set to Unlimited.");
            return;
        }

        if (int.TryParse(val.Trim(), out int parsed) && parsed > 0)
        {
            _maxMessagesPerConversation = parsed;
            Console.WriteLine($"Max messages set to {parsed}.");
        }
        else
        {
            Console.WriteLine("Invalid number. No change made.");
        }
    }

    private void ToggleShowIds()
    {
        _showIdsInList = !_showIdsInList;
        Console.WriteLine($"Show IDs in lists: now {_showIdsInList}");
    }

    private static string SanitizeFileName(string raw)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            raw = raw.Replace(c, '_');
        return raw;
    }

    private static string ReadSecretFromConsole()
    {
        var sb = new StringBuilder();
        ConsoleKeyInfo key;
        while (true)
        {
            key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0)
                {
                    sb.Length--;
                    Console.Write("\b \b");
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                sb.Append(key.KeyChar);
                Console.Write("*");
            }
        }
        Console.WriteLine();
        return sb.ToString();
    }
}
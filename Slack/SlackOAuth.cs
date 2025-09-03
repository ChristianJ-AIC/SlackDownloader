using ConsoleSlackDownloader.Interactive;
using System.Net;
using System.Text.Json;

namespace SlackConsoleApp.Slack;

public class SlackOAuth
{
    //private readonly string _clientId = Environment.GetEnvironmentVariable("SLACK_CLIENT_ID") ?? "<YOUR_CLIENT_ID>";
    //private readonly string _clientSecret = Environment.GetEnvironmentVariable("SLACK_CLIENT_SECRET") ?? "<YOUR_CLIENT_SECRET>";
    //private readonly string _scopes = Environment.GetEnvironmentVariable("SLACK_OAUTH_SCOPES") ??
    //                                  "channels:read,groups:read,im:read,mpim:read,channels:history,groups:history,im:history,mpim:history";
    private readonly int _port = 53682;

    public async Task<string?> RunInteractiveOAuthAsync(AppConfig appConfig, CancellationToken cancellationToken = default)
    {
        if (appConfig.Slack.ClientId.StartsWith("<YOUR_"))
        {
            Console.WriteLine("Set SLACK_CLIENT_ID / SLACK_CLIENT_SECRET before OAuth.");
            return null;
        }

        var redirectUri = $"https://localhost:{_port}/slack/oauth/callback";
        var state = Guid.NewGuid().ToString("N");
        var url =
            $"https://slack.com/oauth/v2/authorize?client_id={Uri.EscapeDataString(appConfig.Slack.ClientId)}" +
            $"&scope={Uri.EscapeDataString(appConfig.Slack.Scopes)}" +
            $"&user_scope={Uri.EscapeDataString(appConfig.Slack.Scopes)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&state={Uri.EscapeDataString(state)}";

        using var listener = new HttpListener();
        listener.Prefixes.Add($"https://localhost:{_port}/slack/oauth/");
        listener.Start();

        Console.WriteLine("Opening browser for OAuth...");
        TryOpenBrowser(url);
        Console.WriteLine("Waiting for redirect...");

        var ctx = await listener.GetContextAsync();

        var query = ctx.Request.QueryString;
        string? code = query["code"];
        string? receivedState = query["state"];
        string? error = query["error"];

        var responseHtml = "<html><body>OAuth complete. Return to the console.</body></html>";
        var buffer = System.Text.Encoding.UTF8.GetBytes(responseHtml);
        ctx.Response.ContentLength64 = buffer.Length;
        await ctx.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
        ctx.Response.OutputStream.Close();

        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine("OAuth error: " + error);
            return null;
        }

        if (receivedState != state)
        {
            Console.WriteLine("State mismatch.");
            return null;
        }

        if (code is null)
        {
            Console.WriteLine("No code returned.");
            return null;
        }

        Console.WriteLine("Exchanging code for token...");

        using var http = new HttpClient();
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = appConfig.Slack.ClientId,
            ["client_secret"] = appConfig.Slack.ClientSecret,
            ["redirect_uri"] = redirectUri
        });

        var resp = await http.PostAsync("https://slack.com/api/oauth.v2.access", form, cancellationToken);
        var json = await resp.Content.ReadAsStringAsync(cancellationToken);

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.GetProperty("ok").GetBoolean())
        {
            Console.WriteLine("OAuth exchange failed: " + json);
            return null;
        }

        string? token = doc.RootElement.TryGetProperty("authed_user", out var authed) &&
                        authed.TryGetProperty("access_token", out var userTokenElem)
            ? userTokenElem.GetString()
            : doc.RootElement.GetProperty("access_token").GetString();

        Console.WriteLine("Token acquired (not printed).");
        return token;
    }

    private void TryOpenBrowser(string url)
    {
        try
        {
            // Windows
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            // Mac
            // System.Diagnostics.Process.Start("open", url);
            // Linux...
            //System.Diagnostics.Process.Start("xdg-open", url);
        }
        catch
        {
            Console.WriteLine("Please manually open: " + url);
        }
    }
}
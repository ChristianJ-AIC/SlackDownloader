using ConsoleSlackDownloader.Interactive;
using Microsoft.Extensions.Configuration;
using SlackConsoleApp.Interactive;

namespace SlackConsoleApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Slack Console App (Interactive Version)");
        Console.WriteLine("NOTE: Uses Slack's official Web API. No web scraping.");
        Console.WriteLine("Press Ctrl+C at any time to attempt to exit gracefully.\n");

        var configurationRoot = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.dev.json", optional: true, reloadOnChange: true)
            .Build();
        var appConfig = configurationRoot.Get<AppConfig>() ?? throw new ArgumentNullException();

        var menu = new InteractiveMenu(appConfig);
        await menu.RunAsync();
    }
}
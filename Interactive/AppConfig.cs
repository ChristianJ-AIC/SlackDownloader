namespace ConsoleSlackDownloader.Interactive
{
    public class AppConfig
    {
        public SlackSettings Slack { get; set; }
        public class SlackSettings
        {
            public string? AccessToken { get; set; }
            public string? ClientId { get; set; }
            public string? ClientSecret { get; set; }
            public string? Scopes { get; set; }
        }
    }
}

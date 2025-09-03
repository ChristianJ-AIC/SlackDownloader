# Slack Console App (Interactive Version)

This tool only uses Slack’s **official Web API**. 


## Features

- Interactive menu (set token, run OAuth, list conversations, export histories)
- Pagination with `next_cursor` for complete coverage
- Optional limit on messages per conversation
- Simple JSON export structure
- In-memory token (not persisted)

- 
## Requirements

1. .NET 9 SDK
2. A Slack App and a User OAuth token (https://api.slack.com/apps - See below)


## Configuring Slack to allow extraction

1. Create a Slack App in your workspace
- Go to https://api.slack.com/apps and click "Create New App" => "From scratch", and give it a name and select your workspace (AIC), and finally click "Create App"
- In the "Install App" section, click "Install to Workspace" and authorize the app. You will get to OAuth tokens (user & bot), copy the user token.
- The following may not be needed, but if you have issues: In the "OAuth & Permissions" section, add the following User Token Scopes: channels:history,channels:read,files:read,groups:history,groups:read,im:history,im:read,mpim:history,mpim:read


## Running

```
dotnet run
```

Then follow the menu:
1. Set or obtain a token (options 1 or 2)
2. List conversations (option 3)
3. Export histories (option 4)

Exported files appear in `export/` by default (change via option 5).


## Extending

Ideas to enhance:
- Fetch thread replies (`conversations.replies`)
- File downloads (requires additional scopes)
- Parallelization with rate-limit management

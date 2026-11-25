# ChatApp

ChatApp is a single shared chat room. Open the app in two browser sessions to see messages and presence update live.

## Run locally

Install the .NET 10 SDK. Visual Studio 2026 is optional. From the repository root, run:

```powershell
dotnet run --project ChatApp.csproj --launch-profile http
```

Open `http://localhost:5103/`. The first run restores NuGet packages and creates `chat.db` in the project root. Later runs reuse that file. To change the SQLite filename, edit `Database:FileName` in `appsettings.json`. Relative paths resolve from the app's content root. The repository includes the pinned SignalR browser client, version 10.0.11, so a fresh clone needs no browser asset restore step.

## Assumptions

Names that differ only in letter case or surrounding whitespace identify the same user. For example, `DJ`, ` dj `, and `dj` all match. The first saved spelling supplies the display name.

The server caps display names at 50 characters and messages at 2,000 characters after trimming surrounding whitespace.

## Design

EF Core manages the SQLite schema. A user has a display name, a unique normalized name, and a creation timestamp. A message has an author, text, and a send timestamp. SQLite generates IDs for both. The presence tracker keeps each user's active connections in memory. The first connection marks the user online, and the last removal marks them offline.

SignalR clients can request a roster and receive presence updates before joining. Joining binds the user's identity to the connection. The hub saves each message through the chat service before broadcasting it to joined connections, including the sender. A crash between those steps can leave a saved message available only through history. [Implementation decisions](DECISIONS.md)

The browser remembers the joined name for the current tab. Refresh reloads the newest 50 messages. After a dropped connection, the browser rejoins and pages forward from the last covered message. **Load older messages** pages backward. Live messages and history share one list keyed by the database ID, so the sender's echo appears once.

The server pages older history, and loaded messages accumulate in the browser for the session. On each new message, `renderMessages()` sorts by ID and rebuilds the list. This keeps rendering straightforward for the exercise. Incremental rendering or a windowed list could reduce the work in long-running sessions.

## AI usage

Codex implemented the scaffold, persistence, chat service, presence tracker, hub, browser client, and tests. Ramesh verified milestone 1 startup and approved milestones 2 through 5. [Prompt log](AI-PROMPTS.md)

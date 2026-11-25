# ChatApp

ChatApp is a single shared chat room in development. The server exposes a SignalR hub at `/chat`. The browser serves the static start page pending the chat UI.

## Run locally

Install the .NET 10 SDK. Visual Studio 2026 is optional. From the repository root, run:

```powershell
dotnet run --project ChatApp.csproj --launch-profile http
```

Open `http://localhost:5103/`. The first run restores NuGet packages and creates `chat.db` in the project root. Later runs reuse that file. To change the SQLite filename, edit `Database:FileName` in `appsettings.json`. Relative paths resolve from the app's content root. There is no browser asset restore step yet.

## Assumptions

Names that differ only in letter case or surrounding whitespace identify the same user. For example, `DJ`, ` dj `, and `dj` all match. The first saved spelling supplies the display name.

The server caps display names at 50 characters and messages at 2,000 characters after trimming surrounding whitespace.

## Design

EF Core manages the SQLite schema. A user has a display name, a unique normalized name, and a creation timestamp. A message has an author, text, and a send timestamp. SQLite generates IDs for both. The presence tracker keeps each user's active connections in memory. The first connection marks the user online, and the last removal marks them offline.

SignalR clients can request a roster and receive presence updates before joining. Joining binds the user's identity to the connection. The hub saves each message through the chat service before broadcasting it to joined connections, including the sender. A crash between those steps can leave a saved message available only through history. [Implementation decisions](DECISIONS.md)

## AI usage

Codex prepared the scaffold, project notes, SQLite persistence layer, and chat service. Ramesh verified server startup in milestone 1 and reviewed milestone 2. [Prompt log](AI-PROMPTS.md)

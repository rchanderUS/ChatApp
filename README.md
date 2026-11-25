# ChatApp

ChatApp is a single shared chat room in development. Milestone 2 serves the static start page and initializes the SQLite database.

## Run locally

Install the .NET 10 SDK. Visual Studio 2026 is optional. From the repository root, run:

```powershell
dotnet run --project ChatApp.csproj --launch-profile http
```

Open `http://localhost:5103/`. The first run restores NuGet packages and creates `chat.db` in the project root. Later runs reuse that file. To change the SQLite filename, edit `Database:FileName` in `appsettings.json`. Relative paths resolve from the app's content root. There is no browser asset restore step yet.

## Assumptions

Names that differ only in letter case or surrounding whitespace identify the same user. For example, `DJ`, ` dj `, and `dj` all match. The first saved spelling supplies the display name.

## Design

EF Core manages the SQLite schema. A user has a display name, a unique normalized name, and a creation timestamp. A message has an author, text, and a send timestamp. SQLite generates IDs for both. Visitors will see the roster before joining by name, and ASP.NET Core SignalR will deliver messages and updates based on active connections. [Implementation decisions](DECISIONS.md)

## AI usage

Codex prepared the scaffold, project notes, and SQLite persistence layer. Ramesh verified server startup in milestone 1 and reviewed milestone 2. [Prompt log](AI-PROMPTS.md)

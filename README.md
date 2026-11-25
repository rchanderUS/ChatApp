# ChatApp

ChatApp is a single shared chat room in development. Milestone 1 serves a static start page.

## Run locally

Install the .NET 10 SDK. Visual Studio 2026 is optional. From the repository root, run:

```powershell
dotnet run --project ChatApp.csproj --launch-profile http
```

Open `http://localhost:5103/`. Database setup and browser asset restoration begin in later milestones.

## Design

Visitors will see the roster before joining by name. ASP.NET Core SignalR will deliver messages and presence updates. EF Core will store users and messages in a local SQLite file. Online status will come from active connections in server memory. [Implementation decisions](DECISIONS.md)

## AI usage

Codex prepared the milestone 1 scaffold and project notes. Ramesh verified that the server starts. [Prompt log](AI-PROMPTS.md)

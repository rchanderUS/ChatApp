# ChatApp

ChatApp is a single shared chat room built with ASP.NET Core, SignalR, EF Core, and SQLite. Visitors can see who has joined and who is online before entering the room.

## Run locally

Install the .NET 10 SDK (or later) and clone or extract the repository. Visual Studio 2026 is optional. Run the following from the repository root.

```powershell
dotnet run --project ChatApp.csproj --launch-profile http
```

Open `http://localhost:5103/`. The first run restores NuGet packages and creates `chat.db` in the project root. Later runs reuse that file. To change the SQLite filename, edit `Database:FileName` in `appsettings.json`. Relative paths resolve from the app's content root. The repository includes the pinned SignalR browser client, version 10.0.11, so a fresh clone needs no browser asset restore step.

To try live updates, open the URL in two independent browser sessions, such as a regular window and a private window. Join with different names and send a message. Join under the same name from two tabs to try multi-tab presence. Stop the server with Ctrl+C.

Run the automated service and presence tests from the repository root.

```powershell
dotnet test ChatApp.slnx
```

## Assumptions

There is one shared room on a single server. Joining by name has no password or account ownership check. Anyone who enters a known name can join as that user.

Names that differ only in letter case or surrounding whitespace identify the same user. For example, `DJ`, ` dj `, and `dj` all match. The first saved spelling supplies the display name.

The server caps display names at 50 characters and messages at 2,000 characters after trimming surrounding whitespace.

## Design

The static HTML and vanilla JavaScript handle forms, rendering, paging, and reconnection. SignalR provides hub calls and broadcasts within ASP.NET Core. `ChatHub` coordinates connection identity and message delivery. `ChatService` validates input and handles user lookup, message persistence, and history queries.

`ChatDbContext` maps the SQLite tables. A user has an integer ID, display name, unique normalized name, and UTC creation timestamp. A message has an integer ID, author ID, text, and UTC send timestamp. SQLite generates both IDs and enforces the author relationship. SQLite gives this single-server exercise persistence with one local file. The app creates the initial schema at startup with `EnsureCreatedAsync`.

`PresenceTracker` maintains connection bindings and each user's active connection set in memory. The first connection marks the user online, and the last removal marks them offline. Presence revisions let the browser reconcile roster snapshots with live updates. Only users and messages are persisted.

SignalR clients can request a roster and receive presence updates before joining. Joining binds the user's identity to the connection. The hub saves each message through the chat service before broadcasting it to joined connections, including the sender. A crash between those steps can leave a saved message available only through history.

The browser remembers the joined name for the current tab. Refresh reloads the newest 50 messages. After a dropped connection, the browser rejoins and pages forward from the last covered message. **Load older messages** pages backward. Live messages and history share one list keyed by the database ID, so the sender's echo appears once.

## Assignment scenarios

| Scenario | Verified behavior |
| --- | --- |
| Same user opens two tabs or devices | Two tabs under the same name share one user. Closing one keeps the user online until the final connection closes. |
| Page refresh | The tab rejoins using the saved display name and reloads the latest 50 messages. Presence can briefly flip if the old connection closes before the new one joins. |
| Abrupt network loss | A silent WebSocket interruption left presence online until SignalR detected the timeout, about 30 seconds in the test. The browser retries automatically and offers **Reconnect** if retries run out. A returning client recovered 65 missed messages while receiving a live message. |
| Two people join with the same name at once | A controlled test with separate database contexts hit the unique index. The losing call reloaded the winning row, and both calls returned the same user ID. |
| Case or surrounding whitespace differs | `DJ`, `dj`, and ` DJ ` resolved to one row with the first saved casing. |
| Empty, whitespace-only, or very long messages | The server rejected empty and whitespace-only input and a 2,001-character message. A 2,000-character message was accepted. |
| Server restarts during chat | Users and messages survived. Presence started empty, then rebuilt as browsers rejoined. Sending resumed after reconnect. |
| History grows to 100,000 messages | A service check read all 100,000 messages in 2,000 pages without gaps or duplicates. The browser was checked with 67 messages. |

The solution passed 22 xUnit tests. Browser verification used independent Chrome sessions. Clean-machine setup with the .NET 10 SDK and browser performance with all 100,000 messages loaded remain unverified. [Verification details](DECISIONS.md)

## Tradeoffs and next steps

- **Presence and scale.** Connection state is local to one server and resets on restart. A multi-server version could use a SignalR backplane for broadcasts and a shared presence store with expiring connection records.
- **Message delivery.** Saving before broadcasting makes history the recovery path if the app stops between the two operations. A durable outbox could retry delivery after that gap if stronger delivery guarantees were needed.
- **Browser history.** The server pages older messages, while loaded messages accumulate in the browser. On each new message, `renderMessages()` sorts by ID and rebuilds the list. Incremental rendering or a windowed list could make long-running sessions cheaper.
- **Operational limits.** Rate limiting for joins and sends would reduce abuse. If the schema evolves, EF Core migrations would let the app update existing databases while preserving their data.

## AI usage

Codex helped implement the scaffold, persistence, chat service, presence tracker, hub, browser client, and tests. It also ran the automated and browser verification recorded in the decision log. Ramesh independently verified startup, verified milestones 2 and 5, approved the intermediate commits through milestone 6, and reviewed implementation choices such as name normalization, SQLite configuration and constraint handling, presence revisions, and sender echo deduplication. [AI instructions](AGENTS.md) · [Prompt log](AI-PROMPTS.md)

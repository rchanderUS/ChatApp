## Objective

Build one shared real-time chat room. Visitors see every known user and their live status before joining. Users join by name, browse persisted history, and send messages. Users and messages survive restarts. Presence comes from active connections. Aim for a runnable core in 2–3 hours.

## Source brief

Read `ASSIGNMENT.md` in the repository root. It defines the submission requirements. This file records implementation decisions. Flag any conflict before implementing the affected behavior. Keep the full brief in its own file.

## Stack

- .NET 10, ASP.NET Core, and C# with Visual Studio 2026.
- ASP.NET Core SignalR for real-time updates.
- EF Core and a file-based SQLite database. Create the schema at startup with `EnsureCreatedAsync` for this exercise. Ignore generated database files in Git.
- Static HTML and vanilla JavaScript. Pin the SignalR browser client version and serve it locally. Keep styling minimal.

## Architecture

- `ChatHub` coordinates hub calls and broadcasts. Delegate validation and database operations to `ChatService`.
- `ChatService` handles user lookup or creation, message validation and persistence, and history queries.
- `PresenceTracker` is a singleton. Maintain `connectionId → userId` and `userId → active connections` under one lock. A user is online while the user's connection set is nonempty. Keep the indexes consistent and take snapshots under the lock. Perform async work outside it.
- `ChatDbContext` manages `Users` and `Messages`. Enforce a unique index on `Users.NormalizedName`.
- `wwwroot/index.html` and `chat.js` provide a join form, user roster, message list, and send form. Keep rendering, connection lifecycle, and form handling in separate functions. Render names and messages with `textContent`.

## Behavior

1. **Roster on load.** Register SignalR handlers before connecting. Fetch the full roster after connection starts, including before joining, and keep it live. Include the user ID and display name in presence events so visitors can see newly created users. Make sure an initial or reconnect snapshot cannot overwrite newer updates. Use a presence revision with snapshots and events, or an equivalent ordering strategy.
2. **Join by name.** Trim the name, reject blank names and names longer than 50 characters, and normalize with `ToLowerInvariant()`. Preserve the casing of the first persisted display name. Look up by normalized name and insert if absent. On a violation of that unique index, detach the failed insert and reload the winning row. Let unrelated database errors surface.
3. **Bind identity to the connection.** Use the same `Join(name)` method for first join, refresh, and reconnect. Resolve the user on the server and bind that user ID to the current connection. Repeating the same join is idempotent. Reject attempts to switch identity on an already joined connection. Resolve the sender from the server-side binding and require a joined connection to send or read chat history.
4. **Track presence in memory.** Broadcast online on the first connection and offline when the last connection is removed in `OnDisconnectedAsync`. Closing one of two tabs keeps the user online. An abrupt network loss can leave presence stale until SignalR detects the connection timeout. Order or version updates so a delayed event cannot undo a newer state for that user.
5. **Persist before broadcasting.** Trim message text and reject blank messages or more than 2,000 characters after trimming. Save the message with a database-generated ID and server UTC timestamp, then broadcast the saved message to joined connections, including the sender. Include the author and timestamp. A crash can happen between saving and broadcasting. Merge fetched and live messages by ID and display them in ID order.
6. **Page history.** Initially show the newest 50 messages in ascending ID order. Provide **Load older messages** using an exclusive `beforeId` cursor. Make all older messages accessible without reading the full table at once.
7. **Recover on refresh and reconnect.** Remember the successfully joined display name in `sessionStorage`. On refresh, rejoin and load the latest page. Use `withAutomaticReconnect` for dropped connections and refresh the roster after reconnect. For a joined user, capture the last covered history position before processing new live events and fetch missed messages forward in ascending ID order using an exclusive `afterId` cursor. Advance the catch-up cursor from fetched pages, even if a newer live event arrives while catching up. Merge and deduplicate by ID. Disable sending until rejoin succeeds. If startup fails or retries run out, show the disconnected state and a **Reconnect** action.
8. **Restart behavior.** Reopen the same SQLite file on restart. Start with empty in-memory presence and rebuild it as clients rejoin.

## Data model

- User — `Id` (int primary key), `DisplayName`, `NormalizedName` (unique), `CreatedAtUtc`.
- Message — `Id` (int primary key), `UserId` (foreign key), `Text`, `SentAtUtc`.
- Persist users and messages only.

## Build and commit order

Use `Milestone <i>: <milestone objective>` for commit messages. After verification, stop any server you started and confirm its port is free before handing control back.

1. `chore: scaffold solution and project notes`
2. `feat: add SQLite user and message persistence`
3. `feat: implement normalized joins and paged history`
4. `feat: track active connections and presence transitions`
5. `feat: wire SignalR chat and presence events`
6. `feat: add browser UI and reconnect recovery`
7. `test: verify chat scenarios and record results`
8. `docs: describe setup, design decisions, and tradeoffs`

Work only on the requested milestone. Keep the application runnable after each milestone. Report what changed, what you verified, and what remains unverified. Pause for Ramesh to review before committing.

## Verification

- Add unit tests for future service classes as part of the milestone that introduces them.

- Verify that `DJ`, ` dj `, and `dj` resolve to one user and preserve the original display casing. Test overlapping joins with separate DbContext instances against the same SQLite database and assert one row and one returned identity.
- Check the message size boundaries and whitespace-only rejection. Verify multi-connection presence, including that removing the last connection emits the offline transition.
- In two independent browser sessions, verify that an unjoined visitor sees the initial roster and live presence changes. Join both sessions and check live messages with author and timestamp.
- Verify refresh, abrupt disconnect detection, and restart with the same database. Confirm that a reconnecting client catches up when more than 50 messages were sent during its absence. Check that older history loads without duplicate messages.
- Record actual results in `DECISIONS.md`. Distinguish verified behavior from behavior that remains untested.

## README and AI records

- Cover setup on a clean machine with the .NET 10 SDK, the exact `dotnet run` command, how to open the app, and where the database is created. List any client asset restore step required for a fresh clone.
- Explain assumptions, code boundaries, the data model, persist-before-broadcast, and in-memory connection tracking.
- Cover all eight scenarios in assignment section 5. State what works today and how remaining gaps could be handled. Mark scenarios as handled only after implementation and verification.
- Discuss single-server presence, timeout and refresh behavior, and the gap between saving and broadcasting. With more time, discuss a SignalR backplane and shared presence with expiring connection records, along with rate limiting. Keep deployment and other out-of-scope features at the discussion level.
- Keep this file and `AI-PROMPTS.md` in Git. Log the prompts actually used. In the README, describe Codex's contributions and what Ramesh reviewed and verified. Use `DECISIONS.md` to explain why design choices were made.

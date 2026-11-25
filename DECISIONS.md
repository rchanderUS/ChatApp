# Project decisions

## Milestone 1

- Keep the existing .NET 10 web project and solution. Serve a static start page from `wwwroot` so the browser client has an entry point as features are added.
- Use the existing HTTP launch profile for local setup. It gives reviewers a fixed address and requires no HTTPS certificate setup.
- Use one shared room. Persist users and messages in SQLite, derive presence from active connections, and use SignalR to deliver updates. The persistence layer is milestone 2.
- Make all history accessible through pages as it grows. This satisfies the assignment's history requirement while keeping the initial load bounded.

### Verification

- `dotnet build ChatApp.slnx` succeeded with zero warnings and errors. This machine used the .NET 11 SDK to build the `net10.0` target.
- The documented run command started the app on port 5103. A separate loopback smoke test of the built executable returned HTTP 200 with the expected page heading.
- Ramesh independently verified server startup. The verification instance was stopped, and port 5103 was confirmed free on IPv4 and IPv6 loopback.
- Git ignored the SQLite database and its three expected sidecar filenames.
- Setup on a clean machine with the .NET 10 SDK and visual browser rendering remain unverified. The eight chat scenarios in the assignment are pending implementation and verification.

## Milestone 2

- Use EF Core's SQLite provider and a scoped `ChatDbContext`. Configure the database filename in `appsettings.json` and resolve relative paths from the content root.
- Create the schema with `EnsureCreatedAsync` before serving requests. This suits the exercise's initial schema. Future schema changes that must preserve existing rows will need migrations.
- Store users and messages with database-generated integer IDs. SQLite enforces the unique normalized name and the message's foreign key to its author. Timestamp properties use `DateTimeOffset` so a UTC offset survives a round trip.

### Verification

- `dotnet build ChatApp.slnx` passed with zero warnings and errors.
- An isolated startup created an empty database and served the start page with HTTP 200. A temporary EF Core harness verified generated IDs and author linkage, duplicate normalized-name rejection, and orphan message rejection.
- The app restarted against the same SQLite file, and a fresh context retrieved the original user and message. Both test server instances were stopped, and ports 5103 and 15103 were free on IPv4 and IPv6 loopback.
- Browser-driven restart recovery, normalized joins, and history paging remain pending. The restart check exercised persistence through EF Core.
- A startup test with `Database:FileName` set to `configured.db` created that file under a temporary content root and returned HTTP 200. The test server was stopped and both ports were free on IPv4 loopback.
- Ramesh verified the milestone and approved the commit.

## Milestone 3

- Keep name matching, message validation and persistence, and history queries in the scoped `ChatService`. The first persisted display name supplies the casing for later joins.
- Enforce length limits on the server after trimming surrounding whitespace. Display names are capped at 50 characters and messages at 2,000 characters.
- Let SQLite arbitrate concurrent inserts. An extended error code of `2067` signals a uniqueness violation. The normalized-name index is the only unique index on `Users`. On that error, detach the failed entity and reload the winning row.
- Use exclusive message ID cursors and fetch at most 51 rows to determine whether another 50-message page is available. Return every page in ascending ID order.

### Verification

- The solution built with zero warnings and errors. The start page returned HTTP 200.
- A temporary integration harness paused one join after lookup, let a second context insert `DJ` into the same SQLite file, then resumed the first join. `DJ`, ` dj `, and `dj` returned the same ID and display casing, with one persisted row.
- Verified whitespace rejection, the 50-character name limit, and the 2,000-character trimmed message limit. Saved messages included the author and a UTC timestamp.
- With 121 saved messages, verified newest, older, and forward pages, including cursor exclusion, ascending order, empty results, and the flag indicating more rows.
- Stopped the verification server and confirmed that ports 5103 and 15103 were free on IPv4 and IPv6 loopback.
- Browser-driven joins, live delivery, and reconnect recovery remain pending. History paging was verified at the service level with 121 rows.

## Milestone 4

- Track presence per server in memory using each user's active connection count. Maintain both connection-to-user and user-to-connections indexes under one lock. Once SignalR is wired up, clients will receive transitions only when a user goes online or offline, each with a monotonically increasing revision. A snapshot includes the current revision and the set of online user IDs so a client can resync if it detects a gap.
- Presence resets on server restart by design. It is derived from live connections, never persisted, and rebuilt as clients rejoin.

### Verification

- All eight PresenceTracker unit tests passed with `dotnet test ChatApp.slnx`.
- Built with zero warnings and errors. The start page returned HTTP 200.
- A temporary harness verified that closing one of two connections keeps the user online and that removing the last connection returns an offline transition. It also checked duplicate joins, identity switch rejection, and snapshot isolation.
- Adding and removing 100 connections in parallel produced exactly one online and one offline transition for the user, with increasing revisions and no remaining connection bindings.
- Stopped the verification server and confirmed ports 5103 and 15103 were free on IPv4 and IPv6 loopback.
- SignalR broadcasts, browser presence updates, and abrupt disconnect recovery remain pending.

## ChatService test follow-up

- Each test creates its schema in a fresh context over an open in-memory SQLite connection. The race test shares that connection across two contexts and pauses the first save so SQLite's unique index rejects the second insert attempt for the same name.
- All 21 tests passed, including 13 ChatService cases and 8 PresenceTracker cases. The service tests cover normalized and overlapping joins, validation, persisted message metadata, and history cursors.

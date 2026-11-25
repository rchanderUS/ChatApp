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

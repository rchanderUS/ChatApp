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

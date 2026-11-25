# AI prompt log

## Milestone 1

Ramesh supplied the repository instructions in [AGENTS.md](AGENTS.md) and asked:

> I want to build a simple real time chat-app as per ASSIGNMENT.md. I want to work on 1 milestone at a time, Keep the application runnable at the end of each milestone. Report what changed, what you verified, and what remains unverified. Pause for my review before committing. You can start by completing milestone 1

> Looks like you started the app in 5103 and I am unable to verify the startup. Stop the ChatApp instance you started on port 5103. Confirm that port 5103 is free. For future milestones, stop any server you start for verification before handing control back to me.

> Verified that I am also able to run the server. You can commit the changes. For commit massages make a note to use style, "Milestone <i>: <milestone objective>. After commit proceed to implement milestone 2

## Milestone 2

> Can you add in Readme.md the assumption that we made about normalizing name. We made a decision to treat names that different only in case or space as same user. Also in the Data definition, add a brief comment about why we are using normalized name

> Can we also have the db name information in appsettings, rather than hardcoded in program.cs

## Milestone 3

> Verified. Go ahead and commit. Implement Milestone 3

> Extract the validation limits into public constants on ChatService: MaxDisplayNameLength = 50 and MaxMessageLength = 2000, alongside the existing PageSize. Use them in the length checks and interpolate them into the exception messages so the text can never drift from the values. Then add both limits to the Assumptions section of the README and to DECISIONS.md as explicit decisions:
> Names capped at 50 chars, Messages at 2,000 chars, enforced server-side

> In JoinAsync, I would prefer to rely only on SqliteExtendedErrorCode == 2067 (SQLITE_CONSTRAINT_UNIQUE). Remove the sqlite.Message.Contains(...) check — matching on error-message text is fragile across SQLite versions, and the extended error code already says exactly which constraint fired. Keep the detach-and-reread fallback as is.

> In SaveMessageAsync, SingleAsync on the author throws a InvalidOperationException if the userId doesn't exist. Replace it with a lookup that throws ArgumentException("Unknown user.") when the author is missing, so the hub can surface a clean client error instead of a 500.

## Milestone 4

> Go ahead and commit milestone 3. Implement milestone 4

> I see you implemented the presence revision. Add a short entry to DECISIONS.md: presence is tracked as connection counts per user in memory; clients receive transitions only on status flips, each carrying a monotonically increasing revision; a snapshot (revision + online set) lets a client resync if it detects a gap. Note the tradeoff: state is per-server and lost on restart, by design — presence is derived from live connections, never persisted.

> Write unit tests for PresenceTracker covering: first connection for a user returns an online transition; a second connection for the same user returns null; removing one of two connections returns null and the user stays in the snapshot; removing the last connection returns an offline transition; removing an unknown connection returns null; re-adding the same connection for the same user is a no-op; adding the same connection for a different user throws; snapshot revision increases only on transitions.

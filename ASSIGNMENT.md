# Take-Home Exercise: Simple Chat Application

## Objective

Build a small real-time chat application that demonstrates your understanding of client/server communication, state management, persistence, and reactive UI updates.

**Use whatever language, framework, and tooling you're most comfortable with.** We care about how you think and how you structure code, not which stack you picked. You will not be penalized for choosing a stack we don't use.

The core of the exercise should be completable in roughly **2–3 hours**. You're welcome to spend more time if you want to, but please don't feel obligated to polish it — we'd rather see a clear, honest, unfinished-in-places solution than a rushed complete one.

---

## 1. Technology

- **Language and framework:** your choice.
- **Datastore:** any persistent datastore. A relational database (Postgres, MySQL, SQLite) is the easiest fit. If you choose something else, explain why in your README.
- **Real-time updates:** your choice of mechanism — WebSockets, Server-Sent Events, your framework's built-in real-time layer, or long polling. Tell us in the README what you picked and why.
- **One constraint:** please don't build on a fully managed real-time backend (Firebase Realtime Database, Supabase Realtime, Pusher, Ably, etc.). Those implement the most interesting part of this exercise for you. Real-time features that ship *with* a web framework are completely fine — Rails Action Cable, Django Channels, ASP.NET SignalR, Socket.IO, Laravel Echo/Reverb, Spring WebSocket, Phoenix Channels, and so on.

---

## 2. Functionality

**a. On load, the app shows:**
   - A list of all users who have ever joined
   - Each user's current online/offline status

**b. A visitor can enter a name to join the chat** — no authentication, no password.
   - If a user with that name doesn't exist, create it
   - If the user already exists, they rejoin as that user
   - Once joined, they enter a single shared chat room with everyone else

**c. Inside the chat room:**
   - Show all past chat messages, with author and timestamp
   - Show all users and their online/offline status
   - Allow sending new messages

**d. Updates must be live.** When someone sends a message, it appears for every other connected user *without them refreshing the page*. The same goes for presence — when someone joins or leaves, everyone else sees it.

---

## 3. Persistence

- **Users and messages must survive a restart.** Stop the app, start it again, and the user list and message history are still there.
- **Online/offline status must *not* be persisted.** It should be derived from who is actually connected right now. In other words: don't store an `is_online` flag and flip it to `true` when someone logs in — think about what happens when a user closes their laptop lid instead of clicking a logout button.

---

## 4. Focus

The emphasis is on **functionality, correctness, and design clarity — not aesthetics.** Unstyled HTML is completely fine. A minimal UI is fine as long as it demonstrates proper flow and live updates.

We're specifically interested in:

- How you separate concerns — user interface, application logic, and data access
- How you break the UI into reusable pieces
- How an event gets fanned out to every connected client
- How you handle the messy edge cases below

---

## 5. Scenarios to consider

Real chat apps live and die on these. You don't have to solve all of them — but we'd like evidence you thought about them. **Handle what you can in the time you have, and document the rest in your README:** what happens today, and what you'd do about it.

1. The same user opens the app in two tabs, or on a phone and a laptop. Then closes one of them.
2. A user refreshes the page.
3. A user closes their browser or loses network connectivity without disconnecting cleanly.
4. Two people try to join with the same name at the same moment.
5. Names that differ only by case or surrounding whitespace: `dj`, `DJ`, `" DJ "`.
6. Empty or whitespace-only messages. Extremely long messages.
7. The server restarts while users are connected.
8. The chat history grows to 100,000 messages.

---

## 6. Out of scope

Please don't spend time on: authentication or passwords, private/direct messages, multiple chat rooms, editing or deleting messages, typing indicators, read receipts, file or image uploads, visual design, deployment or hosting, or exhaustive test coverage. (A few meaningful tests are welcome; we're not looking for 100%.)

---

## 7. Assumptions

Make any assumptions you need in order to finish, but **write them down in the README.** Ambiguity in the requirements above is intentional in places — we're interested in the calls you make and why.

---

## 8. Show your work

We want to see how you arrived at the final product, and your thought process, as much as possible.

- **Commit incrementally.** A single "initial commit" containing the finished application tells us nothing about how you work. Small, labeled commits are much more useful to us — and give us better things to talk about in your interview.
- Notes, scratch files, or a decision log in the repo are all welcome.

---

## 9. Use of AI

**Use of AI is encouraged, but not required.**

- If you used an AI assistant, please include your instruction/prompt files in the repo — `CLAUDE.md`, `.cursorrules`, `AGENTS.md`, a prompt log, whatever you have.
- Be prepared to explain any part of the code in your interview, including parts an assistant wrote. "I'd have to look at that again" is a fine answer once; it's not a fine answer for the whole codebase.
- The code will be evaluated on its own merits.

---

## 10. Submission

Submit a link to a Git repository containing your work, including the commit history. You can also send your submission as an email attachment.

**Please send your submission at least 24 hours before your technical interview.**

Your submission should include:

- **Commit history** showing incremental progress
- **A `README.md`** with:
  - **Setup instructions** — dependencies, database setup, how to run the app. Assume the reader has a clean machine and knows nothing about your stack.
  - **Assumptions** you made
  - **Architecture and design approach** — a few paragraphs. How is the code organized? How do live updates travel from one user's browser to another's? Where does online/offline status live? What does your data model look like?
  - **Known limitations and tradeoffs** — including which of the scenarios in section 5 you didn't get to, and what you'd do about them
  - **What you'd do with more time**
  - **A note on AI usage**, if any

---

## What happens next

Your technical interview will be a conversation about this code. Expect us to ask why you made certain choices, what would break under load or across multiple servers, and how you'd extend it. **Being able to talk clearly about the tradeoffs in an incomplete solution counts for more than a complete solution you can't explain.**

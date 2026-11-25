"use strict";

const ui = Object.fromEntries([
    "connection-status", "reconnect", "join-form", "display-name", "join", "error",
    "users", "chat", "load-older", "messages", "send-form", "message-text", "send"
].map(id => [id, document.getElementById(id)]));

const nameKey = "chatApp.displayName";
const users = new Map();
const messages = new Map();
const dateFormat = new Intl.DateTimeFormat(undefined, { dateStyle: "short", timeStyle: "short" });
let displayName = sessionStorage.getItem(nameKey);
let phase = "connecting";
let generation = 0;
let rosterRevision = -1;
let highestPresenceRevision = -1;
let coveredId = null;
let olderCursor = null;
let hasOlder = false;
let joining = false;
let sending = false;
let loadingOlder = false;

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chat")
    .withAutomaticReconnect()
    .build();

connection.on("PresenceChanged", onPresenceChanged);
connection.on("MessageReceived", onMessageReceived);
connection.onreconnecting(() => resetConnection("reconnecting"));
connection.onreconnected(() => restoreConnection(generation));
connection.onclose(() => resetConnection("disconnected"));

ui["join-form"].addEventListener("submit", join);
ui["send-form"].addEventListener("submit", send);
ui["load-older"].addEventListener("click", loadOlder);
ui.reconnect.addEventListener("click", start);
void start();

function setPhase(next) {
    phase = next;
    renderControls();
}

function renderControls() {
    const canJoin = phase === "visitor" && !joining;
    const canSend = phase === "joined";
    ui["connection-status"].textContent = {
        connecting: "Connecting…",
        visitor: "Connected",
        joining: "Joining chat…",
        joined: `Joined as ${displayName}`,
        reconnecting: "Reconnecting…",
        disconnected: "Disconnected"
    }[phase];
    ui.reconnect.hidden = phase !== "disconnected";
    ui["join-form"].hidden = Boolean(displayName);
    ui["display-name"].disabled = !canJoin;
    ui.join.disabled = !canJoin;
    ui.chat.hidden = !displayName;
    ui["message-text"].disabled = !canSend;
    ui.send.disabled = !canSend || sending;
    ui["load-older"].hidden = !hasOlder;
    ui["load-older"].disabled = !canSend || loadingOlder;
}

function showError(error) {
    ui.error.textContent = error;
    ui.error.hidden = !error;
}

function renderUsers() {
    const items = [...users.values()].sort((a, b) => a.displayName.localeCompare(b.displayName));
    ui.users.replaceChildren(...items.map(user => {
        const item = document.createElement("li");
        item.textContent = `${user.displayName} · ${user.isOnline ? "Online" : "Offline"}`;
        return item;
    }));
}

function renderMessages(keepPosition = false) {
    const list = ui.messages;
    const previousHeight = list.scrollHeight;
    const previousTop = list.scrollTop;
    const atBottom = previousHeight - previousTop - list.clientHeight < 24;
    const fragment = document.createDocumentFragment();
    for (const message of [...messages.values()].sort((a, b) => a.id - b.id)) {
        const item = document.createElement("li");
        const author = document.createElement("strong");
        author.textContent = message.author;
        const time = document.createElement("time");
        time.dateTime = message.sentAtUtc;
        time.textContent = dateFormat.format(new Date(message.sentAtUtc));
        const text = document.createElement("p");
        text.textContent = message.text;
        item.append(author, time, text);
        fragment.append(item);
    }
    list.replaceChildren(fragment);
    list.scrollTop = keepPosition
        ? previousTop + list.scrollHeight - previousHeight
        : atBottom ? list.scrollHeight : previousTop;
}

function resetConnection(next) {
    generation++;
    joining = sending = loadingOlder = false;
    rosterRevision = highestPresenceRevision = -1;
    users.clear();
    renderUsers();
    setPhase(next);
}

async function start() {
    showError("");
    setPhase("connecting");
    const current = generation;
    try {
        await connection.start();
        await restoreConnection(current);
    } catch (error) {
        if (current === generation) {
            setPhase("disconnected");
            showError(error.message);
        }
    }
}

async function restoreConnection(current) {
    showError("");
    try {
        await refreshRoster(current);
        if (current !== generation) return;
        if (displayName) {
            await joinAndSync(displayName, current);
        } else {
            setPhase("visitor");
        }
    } catch (error) {
        await recoveryFailed(error, current);
    }
}

async function recoveryFailed(error, current) {
    if (current !== generation) return;
    showError(error.message);
    await connection.stop();
}

async function refreshRoster(current) {
    const roster = await connection.invoke("GetRoster");
    if (current !== generation || roster.revision < rosterRevision) return;

    rosterRevision = roster.revision;
    highestPresenceRevision = Math.max(highestPresenceRevision, rosterRevision);
    for (const user of roster.users) {
        if ((users.get(user.userId)?.revision ?? -1) <= rosterRevision) {
            users.set(user.userId, { ...user, revision: rosterRevision });
        }
    }
    renderUsers();
}

function onPresenceChanged(update) {
    if (update.revision <= rosterRevision) return;
    if (update.revision <= (users.get(update.userId)?.revision ?? -1)) return;

    const gap = highestPresenceRevision >= 0 && update.revision > highestPresenceRevision + 1;
    highestPresenceRevision = Math.max(highestPresenceRevision, update.revision);
    users.set(update.userId, update);
    renderUsers();
    if (gap) {
        const current = generation;
        refreshRoster(current).catch(error => {
            if (current === generation) showError(error.message);
        });
    }
}

function mergeMessages(batch) {
    for (const message of batch) messages.set(message.id, message);
}

function advanceAcrossLiveMessages() {
    // A higher live ID alone does not prove that intervening messages were delivered.
    while (messages.has(coveredId + 1)) coveredId++;
}

function onMessageReceived(message) {
    if (messages.has(message.id)) return;
    mergeMessages([message]);
    if (phase === "joined") advanceAcrossLiveMessages();
    renderMessages();
}

async function joinAndSync(name, current) {
    setPhase("joining");
    const resumeAfter = coveredId;
    const user = await connection.invoke("Join", name);
    if (current !== generation) return;

    displayName = user.displayName;
    sessionStorage.setItem(nameKey, displayName);
    renderControls();
    if (resumeAfter === null) {
        const page = await connection.invoke("GetHistory", null, null);
        if (current !== generation) return;
        mergeMessages(page.messages);
        olderCursor = page.messages[0]?.id ?? null;
        hasOlder = page.hasMore;
        coveredId = page.messages.at(-1)?.id ?? 0;
    } else {
        let cursor = resumeAfter;
        let page;
        do {
            page = await connection.invoke("GetHistory", null, cursor);
            if (current !== generation) return;
            mergeMessages(page.messages);
            cursor = page.messages.at(-1)?.id ?? cursor;
            coveredId = cursor;
        } while (page.hasMore);
    }
    advanceAcrossLiveMessages();
    renderMessages();
    setPhase("joined");
}

async function join(event) {
    event.preventDefault();
    if (phase !== "visitor" || joining) return;
    const current = generation;
    joining = true;
    showError("");
    try {
        await joinAndSync(ui["display-name"].value, current);
        if (current === generation) ui["message-text"].focus();
    } catch (error) {
        if (current !== generation) return;
        if (displayName) {
            await recoveryFailed(error, current);
        } else {
            showError(error.message);
            setPhase("visitor");
        }
    } finally {
        if (current === generation) {
            joining = false;
            renderControls();
        }
    }
}

async function send(event) {
    event.preventDefault();
    if (phase !== "joined" || sending) return;
    const current = generation;
    const input = ui["message-text"];
    const text = input.value;
    sending = true;
    renderControls();
    showError("");
    try {
        // The server echo and history are the only sources for rendered messages.
        await connection.invoke("SendMessage", text);
        if (current === generation && input.value === text) input.value = "";
    } catch (error) {
        if (current === generation) showError(error.message);
    } finally {
        if (current === generation) {
            sending = false;
            renderControls();
            input.focus();
        }
    }
}

async function loadOlder() {
    if (phase !== "joined" || loadingOlder || !hasOlder) return;
    const current = generation;
    loadingOlder = true;
    renderControls();
    showError("");
    try {
        const page = await connection.invoke("GetHistory", olderCursor, null);
        if (current !== generation) return;
        mergeMessages(page.messages);
        olderCursor = page.messages[0]?.id ?? olderCursor;
        hasOlder = page.hasMore;
        renderMessages(true);
    } catch (error) {
        if (current === generation) showError(error.message);
    } finally {
        if (current === generation) {
            loadingOlder = false;
            renderControls();
        }
    }
}

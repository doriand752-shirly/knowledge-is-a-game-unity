/**
 * Quick Colyseus smoke via MatchRoom JSON `snapshot` messages (same as Flutter).
 * Host + 2 phones, classique PP-off → drive to linking_submit (+ sorting if time).
 *
 * Usage: node scripts/smoke-gestures.mjs [httpBase]
 */
import { Client } from "colyseus.js";

const HTTP = (process.argv[2] || "http://85.17.145.134:2567").replace(/\/$/, "");
const WS = HTTP.replace(/^http/, "ws");
const deadlineMs = Date.now() + 200_000;
const log = (...a) => console.log("[smoke]", ...a);
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

function wireSnapshot(room) {
  const box = { snap: null };
  room.onMessage("snapshot", (data) => {
    box.snap = data && typeof data === "object" ? data : null;
  });
  room.onMessage("toast", () => {});
  room.onMessage("error", (e) => log("error msg", e));
  room.onMessage("vfx", () => {});
  room.send("request_snapshot");
  return box;
}

async function waitSnap(box, pred, label, maxMs = 90_000) {
  const end = Date.now() + maxMs;
  while (Date.now() < end) {
    if (Date.now() > deadlineMs) throw new Error("global deadline " + label);
    box.room?.send?.("request_snapshot");
    const s = box.snap;
    if (s && pred(s)) return s;
    await sleep(300);
  }
  throw new Error(`timeout ${label} lastPhase=${box.snap?.phase}`);
}

async function main() {
  log("health", HTTP);
  const health = await fetch(`${HTTP}/health`).then((r) => r.json());
  if (!health?.ok) throw new Error("health not ok");

  const created = await fetch(`${HTTP}/api/create-room`, { method: "POST" }).then((r) => r.json());
  const roomCode = created.roomCode;
  log("room", roomCode);

  // Host creates via joinOrCreate; players joinById after lookup (Flutter path)
  const client = new Client(WS);
  const host = await client.joinOrCreate("match", { role: "host", roomCode, name: "TV" });
  const hostBox = wireSnapshot(host);
  hostBox.room = host;

  await sleep(400);
  const lookup = await fetch(`${HTTP}/api/room/${roomCode}`).then((r) => r.json());
  const roomId = lookup.roomId;
  log("roomId", roomId);

  const p1 = await client.joinById(roomId, { role: "player", roomCode, name: "Smoke1" });
  const p2 = await client.joinById(roomId, { role: "player", roomCode, name: "Smoke2" });
  const p1Box = wireSnapshot(p1);
  const p2Box = wireSnapshot(p2);
  p1Box.room = p1;
  p2Box.room = p2;
  log("joined", { host: host.sessionId, p1: p1.sessionId, p2: p2.sessionId });

  const checklist = {
    lobby_start: false,
    linking_submit: false,
    sorting_submit: false,
    clear_hazard_attempted: false,
  };

  await waitSnap(hostBox, (s) => s.phase === "lobby", "lobby");
  host.send("start_match", { mode: "classique", powerPlaysEnabled: false });
  await waitSnap(
    hostBox,
    (s) => s.phase === "countdown" || s.phase === "theme_vote",
    "started"
  );
  checklist.lobby_start = true;
  log("start_match ok (2 phone seats; VPS has no lobby-bot yet)");

  while (Date.now() < deadlineMs) {
    host.send("request_snapshot");
    const s = hostBox.snap || {};
    const ph = String(s.phase || "");

    if (ph === "theme_vote") {
      let themes = [];
      try {
        themes = JSON.parse(s.themeOptionsJson || "[]");
      } catch {
        themes = [];
      }
      const id = themes[0]?.id;
      if (id) {
        p1.send("vote_theme", { themeId: id });
        p2.send("vote_theme", { themeId: id });
      }
      await sleep(400);
      continue;
    }

    if (ph === "question") {
      let choices = [];
      try {
        const q = s.question || {};
        choices = typeof q.choicesJson === "string" ? JSON.parse(q.choicesJson) : q.choicesJson || [];
      } catch {
        choices = [];
      }
      const id = choices[0]?.id || "A";
      p1.send("submit_answer", { value: id });
      p2.send("submit_answer", { value: id });
      await sleep(350);
      continue;
    }

    if (ph === "power_play") {
      p1.send("use_powerup", { powerUpId: "freeze", targetSessionId: p2.sessionId });
      await sleep(400);
      p2.send("clear_hazard");
      checklist.clear_hazard_attempted = true;
      await sleep(400);
      continue;
    }

    if (ph === "linking" && !checklist.linking_submit) {
      let ch = {};
      try {
        ch = JSON.parse(s.challengeJson || "{}");
      } catch {
        ch = {};
      }
      const lefts = ch.lefts || [];
      const rights = ch.rights || [];
      const links = lefts.map((left, i) => ({ left, right: rights[i % Math.max(rights.length, 1)] }));
      p1.send("linking_submit", { links });
      p2.send("linking_submit", { links });
      checklist.linking_submit = true;
      log("linking_submit OK", links.length);
      const end = Date.now() + 45_000;
      while (Date.now() < end) {
        host.send("request_snapshot");
        await sleep(300);
        if (hostBox.snap?.phase && hostBox.snap.phase !== "linking") break;
      }
      log("after linking →", hostBox.snap?.phase);
      continue;
    }

    if (ph === "sorting" && !checklist.sorting_submit) {
      let ch = {};
      try {
        ch = JSON.parse(s.challengeJson || "{}");
      } catch {
        ch = {};
      }
      const items = ch.items || [];
      for (const it of items) {
        p1.send("sorting_submit", { itemId: it.id, side: "L" });
        p2.send("sorting_submit", { itemId: it.id, side: "R" });
        await sleep(40);
      }
      checklist.sorting_submit = true;
      log("sorting_submit OK", items.length);
      break;
    }

    if (ph === "pyramid" || ph === "podium") {
      log("reached", ph);
      break;
    }

    await sleep(280);
  }

  const summary = {
    roomCode,
    phase: hostBox.snap?.phase,
    checklist,
    note: "No bot on deployed VPS/main; smoke used 2 players. PP off ⇒ hazard often skipped.",
  };
  console.log(JSON.stringify(summary, null, 2));

  await Promise.allSettled([host.leave(), p1.leave(), p2.leave()]);

  if (!checklist.lobby_start || !checklist.linking_submit) {
    process.exit(3);
  }
  log("PASS", checklist.sorting_submit ? "linking+sorting" : "linking (sorting not reached)");
}

main().catch((e) => {
  console.error("[smoke] FAIL", e);
  process.exit(1);
});

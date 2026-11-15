import { createServer } from "node:http";
import { join, dirname } from "node:path";
import { existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import express from "express";
import cors from "cors";
import colyseus from "colyseus";
import wsTransport from "@colyseus/ws-transport";

const { Server } = colyseus;
const { WebSocketTransport } = wsTransport;
import { generateRoomCode, normalizeRoomCode } from "@quizene/shared";
import { MatchRoom, findRoomIdByCode } from "./rooms/MatchRoom.js";
import { loadAllQuestions } from "./questions.js";

const __dirname = dirname(fileURLToPath(import.meta.url));
const PORT = Number(process.env.PORT || 2567);
const PUBLIC_WEB_URL =
  process.env.PUBLIC_WEB_URL || `http://localhost:${PORT}`;

const app = express();
app.use(cors());
app.use(express.json());

app.get("/health", (_req, res) => {
  res.json({
    ok: true,
    service: "knowledge-is-a-game",
    stack: "unity-flutter",
    questions: loadAllQuestions().length,
    publicWebUrl: PUBLIC_WEB_URL,
  });
});

app.get("/api/room/:code", (req, res) => {
  const code = normalizeRoomCode(req.params.code || "");
  const roomId = findRoomIdByCode(code);
  if (!roomId) {
    res.status(404).json({ error: "Salle introuvable" });
    return;
  }
  res.json({
    roomCode: code,
    roomId,
    joinUrl: `${PUBLIC_WEB_URL.replace(/\/$/, "")}/p/${code}`,
  });
});

app.post("/api/create-room", (_req, res) => {
  const roomCode = generateRoomCode(4);
  res.json({
    roomCode,
    hostPath: `/host/${roomCode}`,
    playerPath: `/p/${roomCode}`,
    joinUrl: `${PUBLIC_WEB_URL.replace(/\/$/, "")}/p/${roomCode}`,
  });
});

const webDistCandidates = [
  join(__dirname, "../../web/dist"),
  join(__dirname, "../../../apps/web/dist"),
  join(process.cwd(), "apps/web/dist"),
  "/app/apps/web/dist",
];
const webDist = webDistCandidates.find((p) => existsSync(join(p, "index.html")));
if (webDist) {
  app.use(express.static(webDist));
  app.get("*", (req, res, next) => {
    if (
      req.path.startsWith("/api") ||
      req.path.startsWith("/colyseus") ||
      req.path === "/health"
    ) {
      next();
      return;
    }
    res.sendFile(join(webDist, "index.html"));
  });
  console.log(`[web] serving static from ${webDist}`);
} else {
  console.warn("[web] dist not found — API/WebSocket only (use Vite in dev)");
}

const httpServer = createServer(app);
const gameServer = new Server({
  transport: new WebSocketTransport({ server: httpServer }),
});

gameServer.define("match", MatchRoom).filterBy(["roomCode"]);

httpServer.listen(PORT, () => {
  console.log(`[knowledge-is-a-game] listening on :${PORT}`);
  console.log(`[knowledge-is-a-game] PUBLIC_WEB_URL=${PUBLIC_WEB_URL}`);
  console.log(`[knowledge-is-a-game] questions loaded: ${loadAllQuestions().length}`);
});

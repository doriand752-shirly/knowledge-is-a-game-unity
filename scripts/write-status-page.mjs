import { mkdirSync, writeFileSync } from "node:fs";
import { join, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const dist = join(root, "apps/web/dist");
mkdirSync(dist, { recursive: true });
writeFileSync(
  join(dist, "index.html"),
  `<!doctype html>
<html lang="fr">
<head>
  <meta charset="utf-8"/>
  <meta name="viewport" content="width=device-width,initial-scale=1"/>
  <title>Knowledge is a Game — Colyseus</title>
  <style>
    body{font-family:system-ui,sans-serif;background:#1f1630;color:#fff8f0;display:grid;place-items:center;min-height:100vh;margin:0}
    main{max-width:36rem;padding:2rem;text-align:center}
    a{color:#f4c430}
    code{background:#2b1e3a;padding:.2rem .4rem;border-radius:6px}
  </style>
</head>
<body>
  <main>
    <h1>Knowledge is a Game</h1>
    <p>Backend Colyseus (Unity host + Flutter phones).</p>
    <p>Health: <a href="/health">/health</a> · créer une salle: <code>POST /api/create-room</code></p>
    <p>Ancien host web archivé → <code>c:\\Users\\doria\\Knowledge-is-a-Game</code></p>
  </main>
</body>
</html>`
);
console.log("wrote apps/web/dist/index.html");

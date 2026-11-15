# Knowledge is a Game — Unity 3D + Flutter stack

## Play / backend

| What | URL / path |
|------|------------|
| **Colyseus VPS** | http://85.17.145.134:2567 |
| **Health** | http://85.17.145.134:2567/health → `stack: "unity-flutter"` |
| **Create room** | `POST /api/create-room` |
| **Lookup room** | `GET /api/room/:code` |
| **Monorepo** | `c:\Users\doria\Knowledge-is-a-Game-Unity` |
| **GitHub** | https://github.com/doriand752-shirly/knowledge-is-a-game-unity |
| **Web archive** | `c:\Users\doria\Knowledge-is-a-Game` |

## Architecture

- **Unity 6 LTS 3D** (Windows standalone) — TV claymation host: ShowStage procedural set, Maxime, 10 cast puppets + selfie faces, doors, pads A–D, `votes_show` → reveal, PP throw VFX, pyramid
- **Flutter** — phones: QR/code join, cast+selfie, QCM, Power Plays, challenges, pyramid (Android APK + iOS TestFlight)
- **Colyseus Node** — MatchRoom; JSON **`snapshot`** (~400ms)

Original art only (inspired by KiP / Décades feel — no Sony IP).

## How to run

### 1. Server → VPS

```bat
cd c:\Users\doria\Knowledge-is-a-Game-Unity
npm install
npm run build:all
python deploy\upload_to_vps.py
```

### 2. Flutter Android (This PC)

```bat
set PATH=C:\Users\doria\flutter\bin;%PATH%
cd c:\Users\doria\Knowledge-is-a-Game-Unity\mobile
flutter pub get
flutter build apk --release --dart-define=KIAG_API=http://85.17.145.134:2567
```

APK: `mobile\build\app\outputs\flutter-apk\app-release.apk`

### 3. Flutter iOS → TestFlight (macOS + Xcode; Appy Geek ASC path)

**Cannot compile IPA on This Windows PC.** Use Dimitri’s Mac (self-hosted `darwin` worker) or any Mac with Xcode.  
Local `Task` subagents cannot be pinned to another My Machines worker (two attempts stayed on Windows). Start a **new Cloud Agent** with environment = **MacBook Air de Dimitri** + repo `knowledge-is-a-game-unity`, or run `OneDrive/kiag-asc-handoff/RUN_ON_MAC.sh` on that Mac.

Bundle ID: `com.knowledgeisagame.kiagPlayer` (ASC **bundle** already registered).  
**One-time ASC UI:** create App Store Connect app “Knowledge is a Game” with that bundle — API key can list/upload but **cannot CREATE apps** (403).

#### Credentials (same as Appy Geek — never commit / never paste into chat)

| What | Absolute path (Windows) |
|------|-------------------------|
| Appy Geek project | `C:\Users\doria\Appy Geek\` |
| Appy Geek ASC scripts | `C:\Users\doria\Appy Geek\scripts\asc-*.mjs` |
| Local AuthKey file | `C:\Users\doria\Appy Geek\AuthKey_D3336977R5.p8` |
| Expo session | `%USERPROFILE%\.expo\state.json` |
| KiAG synced env (gitignored) | `mobile\ios\asc\.env.asc` |
| KiAG synced EAS key (gitignored) | `mobile\ios\asc\AuthKey_<KEYID>.p8` |
| Mac handoff pack | `C:\Users\doria\kiag-asc-handoff\` and `C:\Users\doria\OneDrive\kiag-asc-handoff\` |

Sync on Windows (writes gitignored files, prints lengths only):

```bat
node mobile\ios\asc\sync-asc-from-appy-geek.mjs
```

#### Mac build + upload

```bash
git clone https://github.com/doriand752-shirly/knowledge-is-a-game-unity
# Copy OneDrive/kiag-asc-handoff → mobile/ios/asc/  (or re-run sync if Expo login exists)
cd mobile
flutter pub get
flutter build ipa --release --dart-define=KIAG_API=http://85.17.145.134:2567
node ios/asc/upload-ipa.mjs build/ios/ipa/*.ipa
# or: cd ios && bundle exec fastlane upload_testflight
```

Fastlane: `mobile/ios/fastlane/` (Appfile + Fastfile). Details: `internal/ios-testflight-shipped.md`.

### 4. Unity 3D Windows host (This PC)

Editor: `C:\Users\doria\AppData\Local\Unity\Hub\Editor\6000.0.85f2\Editor\Unity.exe`  
Project: `c:\Users\doria\Knowledge-is-a-Game-Unity\unity`  
Colyseus SDK vendored under `Assets/Colyseus` (wired in `ColyseusSdkAdapter.cs`).

**Play in Editor:** Hub → Open project → open scene `Assets/Scenes/ShowStage.unity` → Play  
(or empty scene — `ShowBootstrap` auto-builds the clay set).

**Batch build:**

```bat
"%LOCALAPPDATA%\Unity\Hub\Editor\6000.0.85f2\Editor\Unity.exe" -batchmode -nographics -quit ^
  -projectPath c:\Users\doria\Knowledge-is-a-Game-Unity\unity ^
  -executeMethod Kiag.Editor.KiagBuildMenu.BuildWindows ^
  -logFile c:\Users\doria\Knowledge-is-a-Game-Unity\unity\Logs\batch-build.log
```

Output: `unity\Builds\Windows\KnowledgeIsAGame.exe`

Menu: **Kiag → Create ShowStage Scene** / **Kiag → Build Windows Host**

## Match loop (TV + phones)

1. Host creates room (HTTP) + JoinOrCreate Colyseus as `role:host`
2. Players join via Flutter QR / code
3. Lobby: pick cast + selfie → faces on 3D puppets
4. Modes Classique / Décades / Rapide + Power Plays toggle → Start
5. Theme doors → Power Plays (phone UI + 3D throws) → QCM on phone
6. Pad walk → `votes_show` (suspense) → `reveal`
7. Challenges on phone + TV spectacle text
8. Pyramid climb 3D → podium

## Protocols

Phases include `votes_show`. Clients use Colyseus message `snapshot` and sends: `submit_answer`, `use_powerup`, `vote_theme`, challenge submits, `pyramid_answer`, …

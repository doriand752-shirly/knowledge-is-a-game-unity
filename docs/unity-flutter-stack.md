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

### 3. Flutter iOS → TestFlight (Codemagic FREE — This PC only)

**Do not use Dimitri’s Mac.** GitHub Actions is **blocked** (billing / spending limit). Ship IPA via **Codemagic free** personal plan (`codemagic.yaml` → Fastlane `ci_testflight`).

Bundle ID: `com.knowledgeisagame.kiagPlayer` · ASC app `6835082408` · Team `2QKC854C2J`.

**One-time signup + env group `asc`:** [`docs/codemagic-free-ios.md`](codemagic-free-ios.md)  
Trigger after API token: `mobile\scripts\trigger-codemagic-ios.ps1`

#### Credentials (same as Appy Geek — never commit / never paste into chat)

| What | Absolute path (Windows) |
|------|-------------------------|
| Appy Geek project | `C:\Users\doria\Appy Geek\` |
| Appy Geek ASC scripts | `C:\Users\doria\Appy Geek\scripts\asc-*.mjs` |
| Local AuthKey file | `C:\Users\doria\Appy Geek\AuthKey_D3336977R5.p8` |
| Expo session | `%USERPROFILE%\.expo\state.json` |
| KiAG synced env (gitignored) | `mobile\ios\asc\.env.asc` |
| KiAG synced EAS key (gitignored) | `mobile\ios\asc\AuthKey_<KEYID>.p8` |

Sync on Windows (writes gitignored files, prints lengths only):

```bat
node mobile\ios\asc\sync-asc-from-appy-geek.mjs
```

#### Backup — GitHub Actions (after billing fixed)

```bat
cd c:\Users\doria\Knowledge-is-a-Game-Unity
gh workflow run "iOS TestFlight" -f upload=true -f kiag_api=http://85.17.145.134:2567
gh run watch
```

Details: `mobile/scripts/README-ios-testflight.md`.

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

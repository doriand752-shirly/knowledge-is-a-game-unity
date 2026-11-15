# Knowledge is a Game — Unity 3D host + Flutter phones

**Brand:** Knowledge is a Game  
**Realtime:** Colyseus on `http://85.17.145.134:2567`  
**Archive web:** `c:\Users\doria\Knowledge-is-a-Game`  
**Docs:** [docs/unity-flutter-stack.md](docs/unity-flutter-stack.md)

Claymation / stop-motion TV quiz **inspired by** Knowledge is Power — **original art only** (no Sony IP).

## Layout

| Path | Role |
|------|------|
| `unity/` | Unity 6 LTS Windows **3D** host (TV) |
| `unity/Builds/Windows/` | Built `KnowledgeIsAGame.exe` |
| `mobile/` | Flutter Android + iOS player |
| `mobile/build/app/outputs/flutter-apk/` | Release APK |
| `apps/server/` | Colyseus MatchRoom + JSON `snapshot` |
| `packages/shared/` | Protocols, cast, modes, PP, challenges |
| `content/` | FR questions + challenge packs |
| `deploy/` | VPS Docker upload |

## Run Colyseus (This PC → VPS)

```bat
cd c:\Users\doria\Knowledge-is-a-Game-Unity
npm install
npm run build:all
python deploy\upload_to_vps.py
```

Health: http://85.17.145.134:2567/health (`stack: unity-flutter`)

## Flutter Android

```bat
set PATH=C:\Users\doria\flutter\bin;%PATH%
cd c:\Users\doria\Knowledge-is-a-Game-Unity\mobile
flutter pub get
flutter build apk --release --dart-define=KIAG_API=http://85.17.145.134:2567
```

## Flutter iOS → TestFlight (macOS + Xcode required)

This PC is Windows — IPA must be built on a Mac. See `docs/unity-flutter-stack.md` and `mobile/scripts/README-ios-testflight.md`.

```bash
export APPLE_TEAM_ID=XXXXXXXXXX
export KIAG_API=http://85.17.145.134:2567
cd mobile && ./scripts/build_ios_testflight.sh
```

Bundle ID: `com.knowledgeisagame.kiagPlayer`

## Unity 3D host

Editor `6000.0.85f2` · project `unity/` · scene `Assets/Scenes/ShowStage.unity`  
Or **Kiag → Build Windows Host** → `unity/Builds/Windows/KnowledgeIsAGame.exe`

Colyseus SDK is vendored under `Assets/Colyseus`; host join is in `ColyseusSdkAdapter.cs`.

## Modes

- **Classique** — 9 MCQ + Linking + Sorting + Pyramid  
- **Décades** — 9 MCQ + 6 challenges + PP élargis + Pyramid  
- **Rapide** — 6 MCQ + Linking + Pyramid  

FR UI · 10 cast + selfie · votes_show then reveal · 3D PP throws.

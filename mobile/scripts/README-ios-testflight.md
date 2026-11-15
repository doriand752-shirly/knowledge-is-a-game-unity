# iOS TestFlight — Knowledge is a Game (player)

**Do not use Dimitri’s Mac.** This PC + free hosted macOS CI only.

Bundle: `com.knowledgeisagame.kiagPlayer` · ASC app id `6835082408` · Team `2QKC854C2J`

## Codemagic FREE (preferred now)

GitHub Actions is blocked (billing / spending limit). Use Codemagic personal plan: **$0**, **500 macOS M2 minutes/month**, no credit card.

| Piece | Value |
|-------|--------|
| Config | repo-root `codemagic.yaml` → workflow `ios-testflight` |
| Lane | `mobile/ios/fastlane` → `ci_testflight` |
| Setup | [`docs/codemagic-free-ios.md`](../../docs/codemagic-free-ios.md) |
| Trigger | `mobile/scripts/trigger-codemagic-ios.ps1` (needs `CODEMAGIC_API_TOKEN` + `CODEMAGIC_APP_ID`) |

Env group **`asc`** in Codemagic UI (never commit):

| Variable | Source |
|----------|--------|
| `ASC_KEY_ID` | `mobile/ios/asc/.env.asc` |
| `ASC_ISSUER_ID` | same |
| `ASC_KEY_P8` | PEM body of `mobile/ios/asc/AuthKey_*.p8` |
| `APPLE_TEAM_ID` | `2QKC854C2J` |
| `ASC_APP_ID` | `6835082408` |

```bat
cd c:\Users\doria\Knowledge-is-a-Game-Unity
node mobile\ios\asc\sync-asc-from-appy-geek.mjs
rem After Codemagic signup + asc group (see docs/codemagic-free-ios.md):
set CODEMAGIC_API_TOKEN=...
set CODEMAGIC_APP_ID=...
powershell -File mobile\scripts\trigger-codemagic-ios.ps1
```

## GitHub Actions (blocked until billing fixed)

Workflow: `.github/workflows/ios-testflight.yml`  
Same Fastlane lane + same ASC secrets on the repo.

```bat
cd c:\Users\doria\Knowledge-is-a-Game-Unity
gh workflow run "iOS TestFlight" --ref main -f upload=true -f kiag_api=http://85.17.145.134:2567
gh run watch
```

## One-time App Store Connect (Dorian)

1. App **Knowledge is a Game** already exists for this bundle.
2. After first upload processes: TestFlight → Internal Testing → add yourself / testers.
3. First external group needs Beta App Review (optional later).

## Local Mac fallback (optional — not Dimitri)

```bash
cd mobile
flutter pub get
cd ios && bundle install && bundle exec fastlane beta
```

Or: `flutter build ipa` then `node ios/asc/upload-ipa.mjs build/ios/ipa/*.ipa`

## Permissions already in Info.plist

- Camera — selfie clay + QR scan
- Photo library — optional selfie import

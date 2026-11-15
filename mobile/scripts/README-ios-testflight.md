# iOS TestFlight — Knowledge is a Game (player)

**Preferred path:** public GitHub Actions `macos-latest` (free minutes — same as blind-box-closet). This PC only — no Dimitri Mac.

Bundle: `com.knowledgeisagame.kiagPlayer` · ASC app id `6835082408` · Team `2QKC854C2J`  
Repo visibility: **public** (required for free hosted macOS minutes on this account).

## GitHub Actions (recommended — public repo)

Workflow: `.github/workflows/ios-testflight.yml`  
Lane: `mobile/ios/fastlane` → `ci_testflight`

Secrets (set from This PC, never commit):

| Secret | Source |
|--------|--------|
| `ASC_KEY_ID` | `mobile/ios/asc/.env.asc` |
| `ASC_ISSUER_ID` | same |
| `ASC_KEY_P8` | contents of `mobile/ios/asc/AuthKey_*.p8` (EAS-synced) |
| `APPLE_TEAM_ID` | `2QKC854C2J` |
| `ASC_APP_ID` | `6835082408` |

```bat
cd c:\Users\doria\Knowledge-is-a-Game-Unity
gh workflow run "iOS TestFlight" --ref main -f upload=true -f kiag_api=http://85.17.145.134:2567
gh run watch
```

## One-time App Store Connect (Dorian)

1. App **Knowledge is a Game** already exists for this bundle.
2. After first upload processes: TestFlight → Internal Testing → add yourself / testers.
3. First external group needs Beta App Review (optional later).

## Local Mac fallback (optional)

```bash
cd mobile
flutter pub get
cd ios && bundle install && bundle exec fastlane beta
```

Or: `flutter build ipa` then `node ios/asc/upload-ipa.mjs build/ios/ipa/*.ipa`

## Permissions already in Info.plist

- Camera — selfie clay + QR scan
- Photo library — optional selfie import

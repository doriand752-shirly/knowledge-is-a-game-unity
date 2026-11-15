# iOS TestFlight — Knowledge is a Game (player)

**This PC is Windows** — the IPA cannot be compiled here. Use a Mac with Xcode (15+) and an Apple Developer account.

## One-time App Store Connect

1. Create app **Knowledge is a Game** with bundle id `com.knowledgeisagame.kiagPlayer`.
2. Enable **TestFlight** internal testing; add testers.
3. Note your **Team ID** (developer.apple.com → Membership).

## Build & upload (Mac)

```bash
export APPLE_TEAM_ID=XXXXXXXXXX
export KIAG_API=http://85.17.145.134:2567
cd mobile
chmod +x scripts/build_ios_testflight.sh
./scripts/build_ios_testflight.sh
```

Or manually:

```bash
cd mobile
flutter pub get
flutter build ipa --release --dart-define=KIAG_API=http://85.17.145.134:2567 \
  --export-options-plist=ios/ExportOptions.plist
```

Then upload the IPA via Xcode Organizer, Transporter, or `xcrun altool`.

## Permissions already in Info.plist

- Camera — selfie clay + QR scan
- Photo library — optional selfie import

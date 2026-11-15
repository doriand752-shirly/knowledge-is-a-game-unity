# Fastlane (KiAG) — same ASC path as Appy Geek

This Flutter app uses the **same App Store Connect API key** as Appy Geek.

| Lane | Purpose |
|------|---------|
| `ci_testflight` | GitHub Actions: certs + IPA + TestFlight |
| `beta` | Local Mac: build + upload |
| `upload_testflight` | Upload existing IPA only |
| `build_ipa` | Flutter IPA only |

Preferred: workflow `.github/workflows/ios-testflight.yml` on `macos-latest` (no Dimitri Mac).

```bash
# CI (automatic via GH Actions)
# Local:
bundle exec fastlane beta
# or
flutter build ipa --release --dart-define=KIAG_API=http://85.17.145.134:2567
node ios/asc/upload-ipa.mjs build/ios/ipa/*.ipa
```

Upload script: `mobile/ios/asc/upload-ipa.mjs`  
Credentials: `mobile/ios/asc/.env.asc` (gitignored)

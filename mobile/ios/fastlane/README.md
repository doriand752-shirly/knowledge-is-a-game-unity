# Fastlane (KiAG) — same ASC path as Appy Geek

Appy Geek uploads via Expo EAS (`eas submit`) + ASC API key stored in EAS.  
This Flutter app uses the **same API key file** and Fastlane `upload_to_testflight`.

## Credential absolute paths (Windows This PC)

| What | Path |
|------|------|
| Appy Geek source `.p8` | `C:\Users\doria\Appy Geek\AuthKey_D3336977R5.p8` |
| Synced (gitignored) | `mobile/ios/asc/AuthKey_D3336977R5.p8` |
| Env (gitignored) | `mobile/ios/asc/.env.asc` |
| Sync script | `mobile/ios/asc/sync-asc-from-appy-geek.mjs` |
| Upload script | `mobile/ios/asc/upload-ipa.mjs` |

Proven Appy Geek scripts: `C:\Users\doria\Appy Geek\scripts\asc-*.mjs` (JWT from Expo GraphQL `appStoreConnectApiKey`).

## Mac

```bash
# 1. Clone repo
# 2. Copy gitignored asc secrets from Windows (USB / secure copy) OR run sync if Expo login exists
# 3.
cd mobile
bundle exec fastlane beta
# or:
flutter build ipa --release --dart-define=KIAG_API=http://85.17.145.134:2567
node ios/asc/upload-ipa.mjs build/ios/ipa/*.ipa
```

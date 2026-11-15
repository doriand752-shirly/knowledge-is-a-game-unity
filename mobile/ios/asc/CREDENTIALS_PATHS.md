# ASC credential paths (no secrets)

- Appy Geek source (may differ from EAS key): `C:\Users\doria\Appy Geek\AuthKey_D3336977R5.p8`
- **Canonical for KiAG uploads:** EAS App Store Connect API key synced into `mobile/ios/asc/AuthKey_<KEYID>.p8`
- Env (gitignored): `mobile/ios/asc/.env.asc`
- Proven Appy Geek path: Expo GraphQL `appStoreConnectApiKey` + JWT (`scripts/asc-*.mjs`) or `eas submit`
- Handoff pack (Windows, outside git): `C:\Users\doria\kiag-asc-handoff\`
- Never commit `.env.asc` or `*.p8`

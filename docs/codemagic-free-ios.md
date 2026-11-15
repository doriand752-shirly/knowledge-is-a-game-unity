# FREE iOS TestFlight via Codemagic (no Dimitri Mac, no paid GitHub Actions)

**Status:** GitHub Actions is blocked for `doriand752-shirly` (billing / spending limit). Use **Codemagic free personal plan** instead.

| Item | Value |
|------|--------|
| Plan | Personal / Free — **$0** · **500 macOS M2 minutes / month** · no credit card |
| Pricing | https://codemagic.io/pricing/ |
| Machine | `mac_mini_m2` (matches free allowance) |
| Workflow | `ios-testflight` in repo-root `codemagic.yaml` |
| Bundle | `com.knowledgeisagame.kiagPlayer` |
| ASC app | `6835082408` · Team `2QKC854C2J` |
| API | `http://85.17.145.134:2567` |
| Forbidden | MacBook Air de Dimitri |

## Why Codemagic (not the others)

| Option | Free? | Notes |
|--------|-------|--------|
| **Codemagic** | Yes — 500 M2 min/mo, no card | Best fit; YAML already in repo |
| Bitrise Hobby | Yes — 500 min, but **card verify** required | Extra friction; skip unless Codemagic fails |
| Appcircle Starter | Yes — 20 builds/mo | **30 min max** per build — tight for Flutter+pods first run |
| Expo EAS free | N/A | Expo/RN only — **not** this Flutter app |
| GitHub Actions | Blocked | Fix billing later; keep workflow as backup |

## One-time setup (Dorian — ~10 minutes)

### 1. Sign up (free)

1. Open https://codemagic.io/signup
2. Choose **Sign up with GitHub** (account `doriand752-shirly`)
3. Stay on the **Free / personal** plan (500 M2 minutes). Do **not** add a payment method.

### 2. Add this app

1. Codemagic → **Applications** → **Add application**
2. Connect GitHub if prompted → authorize **Codemagic CI/CD**
3. Select repo `doriand752-shirly/knowledge-is-a-game-unity`
4. Project type: **Flutter** (or “other” / YAML — either works; we use `codemagic.yaml`)
5. Finish

### 3. Create secure env group `asc`

Team / personal **Environment variables** → create group named exactly **`asc`** with:

| Variable | Source (This PC — never paste into chat) |
|----------|------------------------------------------|
| `ASC_KEY_ID` | `mobile\ios\asc\.env.asc` |
| `ASC_ISSUER_ID` | same |
| `ASC_KEY_P8` | Full PEM body of `mobile\ios\asc\AuthKey_*.p8` (secure) |
| `APPLE_TEAM_ID` | `2QKC854C2J` |
| `ASC_APP_ID` | `6835082408` |

Mark each as **Secure**. Sync keys first if needed:

```bat
cd c:\Users\doria\Knowledge-is-a-Game-Unity
node mobile\ios\asc\sync-asc-from-appy-geek.mjs
```

### 4. Start the free build

**UI:** Application → select branch `main` (or this PR branch) → **Start new build** → workflow **`ios-testflight`** → Start.

**Or CLI** (after you create an API token under Codemagic → User settings → Integrations → Codemagic API):

```bat
cd c:\Users\doria\Knowledge-is-a-Game-Unity
set CODEMAGIC_API_TOKEN=...   rem from Codemagic UI — never commit
set CODEMAGIC_APP_ID=...      rem Application settings → App ID
powershell -File mobile\scripts\trigger-codemagic-ios.ps1
```

### 5. After green build

1. Wait for ASC processing (~5–30 min)
2. https://appstoreconnect.apple.com/apps/6835082408/testflight/ios
3. Internal Testing → ensure Dorian’s Apple ID is a tester
4. Install via TestFlight on iPhone

## Later: GitHub Actions again

When GitHub billing/spending limit is fixed, `.github/workflows/ios-testflight.yml` remains valid. Until then, **Codemagic free is the ship path**.

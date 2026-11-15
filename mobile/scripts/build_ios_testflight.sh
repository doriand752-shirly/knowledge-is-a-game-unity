#!/usr/bin/env bash
# Build + archive Flutter iOS for TestFlight. Run on macOS with Xcode + Apple Developer login.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

API="${KIAG_API:-http://85.17.145.134:2567}"
TEAM_ID="${APPLE_TEAM_ID:-}"
BUNDLE_ID="com.knowledgeisagame.kiagPlayer"

if [[ "$(uname)" != "Darwin" ]]; then
  echo "ERROR: iOS / TestFlight builds require macOS + Xcode. This script cannot run on Windows."
  exit 1
fi

if [[ -z "$TEAM_ID" ]]; then
  echo "Set APPLE_TEAM_ID to your Apple Developer Team ID (10 chars)."
  exit 1
fi

# Patch ExportOptions team
EXPORT_PLIST="$ROOT/ios/ExportOptions.plist"
/usr/libexec/PlistBuddy -c "Set :teamID $TEAM_ID" "$EXPORT_PLIST" 2>/dev/null \
  || sed -i '' "s/REPLACE_WITH_APPLE_TEAM_ID/$TEAM_ID/" "$EXPORT_PLIST"

flutter pub get
flutter build ipa \
  --release \
  --dart-define="KIAG_API=$API" \
  --export-options-plist="$EXPORT_PLIST"

IPA=$(ls -1 build/ios/ipa/*.ipa 2>/dev/null | head -1 || true)
echo "IPA: ${IPA:-not found}"
echo "Next: open Xcode Organizer → Distribute App → App Store Connect → TestFlight,"
echo "  or: xcrun altool --upload-app -f \"$IPA\" -t ios -u \"\$APPLE_ID\" -p \"@keychain:AC_PASSWORD\""
echo "  or: xcrun notarytool / Transporter / App Store Connect API key."
echo "Bundle ID: $BUNDLE_ID"

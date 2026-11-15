/**
 * Sync App Store Connect API key from Appy Geek / Expo EAS into gitignored files.
 * Writes the EAS key material (not a possibly mismatched local AuthKey_*.p8).
 * Never prints key material.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const OUT = __dirname;
const PROJECT_ID = '7efd1549-548c-45a9-aede-4603bb6efab0';
const BUNDLE_ID = 'com.appigeek.reader';
const EXPO = 'https://api.expo.dev/graphql';

fs.mkdirSync(OUT, { recursive: true });

const statePath = path.join(process.env.USERPROFILE || process.env.HOME || '', '.expo', 'state.json');
if (!fs.existsSync(statePath)) {
  console.error('ASC_SYNC_FAIL no_expo_session');
  process.exit(1);
}
const state = JSON.parse(fs.readFileSync(statePath, 'utf8'));
const ss = state?.auth?.sessionSecret;
if (!ss) {
  console.error('ASC_SYNC_FAIL no_expo_session_secret');
  process.exit(1);
}

async function gql(query, variables = {}) {
  const r = await fetch(EXPO, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'expo-session': ss },
    body: JSON.stringify({ query, variables }),
  });
  const j = await r.json();
  if (j.errors?.length) throw new Error(JSON.stringify(j.errors));
  return j.data;
}

const data = await gql(
  `query($appId:String!){
    app{byId(appId:$appId){
      iosAppCredentials{
        appleAppIdentifier{bundleIdentifier}
        appStoreConnectApiKeyForSubmissions{id}
      }
    }}
  }`,
  { appId: PROJECT_ID },
);
const creds = data.app.byId.iosAppCredentials ?? [];
const match =
  creds.find((c) => c.appleAppIdentifier?.bundleIdentifier === BUNDLE_ID) ||
  creds.find((c) => c.appStoreConnectApiKeyForSubmissions);
const kid = match?.appStoreConnectApiKeyForSubmissions?.id;
if (!kid) {
  console.error('ASC_SYNC_FAIL no_eas_asc_key');
  process.exit(1);
}
const full = await gql(
  `query($id:ID!){appStoreConnectApiKey{byId(id:$id){keyIdentifier issuerIdentifier keyP8}}}`,
  { id: kid },
);
const key = full.appStoreConnectApiKey.byId;
if (!key?.keyP8 || !key.keyIdentifier || !key.issuerIdentifier) {
  console.error('ASC_SYNC_FAIL incomplete_key');
  process.exit(1);
}

const p8Name = `AuthKey_${key.keyIdentifier}.p8`;
const p8Path = path.join(OUT, p8Name);
fs.writeFileSync(p8Path, key.keyP8.endsWith('\n') ? key.keyP8 : key.keyP8 + '\n', 'utf8');

fs.writeFileSync(
  path.join(OUT, '.env.asc'),
  [
    `ASC_KEY_ID=${key.keyIdentifier}`,
    `ASC_ISSUER_ID=${key.issuerIdentifier}`,
    `ASC_KEY_PATH=${p8Path.replace(/\\/g, '/')}`,
    `ASC_KEY_FILENAME=${p8Name}`,
    'APPLE_TEAM_HINT=from_appy_geek_eas',
  ].join('\n') + '\n',
);

fs.writeFileSync(
  path.join(OUT, 'CREDENTIALS_PATHS.md'),
  [
    '# ASC credential paths (no secrets)',
    '',
    '- Appy Geek source (may differ from EAS key): `C:\\Users\\doria\\Appy Geek\\AuthKey_D3336977R5.p8`',
    '- **Canonical for KiAG uploads:** EAS App Store Connect API key synced into `mobile/ios/asc/AuthKey_<KEYID>.p8`',
    '- Env (gitignored): `mobile/ios/asc/.env.asc`',
    '- Proven Appy Geek path: Expo GraphQL `appStoreConnectApiKey` + JWT (`scripts/asc-*.mjs`) or `eas submit`',
    '- Handoff pack (Windows, outside git): `C:\\Users\\doria\\kiag-asc-handoff\\`',
    '- Never commit `.env.asc` or `*.p8`',
    '',
  ].join('\n'),
);

console.log(
  `ASC_SYNC_OK p8Written=true envWritten=true keyIdLen=${String(key.keyIdentifier).length} issuerLen=${String(key.issuerIdentifier).length} p8Bytes=${Buffer.byteLength(key.keyP8)}`,
);

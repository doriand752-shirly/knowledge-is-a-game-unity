/**
 * Upload a Flutter IPA to App Store Connect / TestFlight using the same ASC
 * API-key approach as Appy Geek (JWT ES256). Never prints key material.
 *
 * Usage: node upload-ipa.mjs path/to/app.ipa
 * Reads mobile/ios/asc/.env.asc (ASC_KEY_ID, ASC_ISSUER_ID, ASC_KEY_PATH).
 */
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const envPath = path.join(__dirname, '.env.asc');
const ipa = process.argv[2];
if (!ipa || !fs.existsSync(ipa)) {
  console.error('USAGE: node upload-ipa.mjs <file.ipa>');
  process.exit(1);
}
if (!fs.existsSync(envPath)) {
  console.error('MISSING .env.asc — run sync-asc-from-appy-geek.mjs on Windows first, then sync asc/ to Mac');
  process.exit(1);
}

const env = Object.fromEntries(
  fs
    .readFileSync(envPath, 'utf8')
    .split(/\r?\n/)
    .filter((l) => l && !l.startsWith('#') && l.includes('='))
    .map((l) => {
      const i = l.indexOf('=');
      return [l.slice(0, i), l.slice(i + 1)];
    }),
);

const keyId = env.ASC_KEY_ID;
const issuerId = env.ASC_ISSUER_ID;
const keyPath = env.ASC_KEY_PATH || path.join(__dirname, env.ASC_KEY_FILENAME || 'AuthKey_D3336977R5.p8');
if (!keyId || !issuerId || issuerId === 'REPLACE_ME' || !fs.existsSync(keyPath)) {
  console.error('ASC_ENV_INCOMPLETE');
  process.exit(1);
}

function b64url(input) {
  const buf = Buffer.isBuffer(input) ? input : Buffer.from(input);
  return buf.toString('base64').replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
}
function jwt(keyP8, kid, iss) {
  const header = { alg: 'ES256', kid, typ: 'JWT' };
  const now = Math.floor(Date.now() / 1000);
  const payload = { iss, iat: now, exp: now + 1200, aud: 'appstoreconnect-v1' };
  const u = `${b64url(JSON.stringify(header))}.${b64url(JSON.stringify(payload))}`;
  const s = crypto.createSign('SHA256');
  s.update(u);
  s.end();
  return `${u}.${b64url(s.sign({ key: keyP8, dsaEncoding: 'ieee-p1363' }))}`;
}

const keyP8 = fs.readFileSync(keyPath, 'utf8');
const token = jwt(keyP8, keyId, issuerId);
const apiKeyJson = path.join(__dirname, 'api_key.json');
// altool / notary prefer .p8 + env; use xcrun altool when available (macOS)
fs.writeFileSync(
  apiKeyJson,
  JSON.stringify({ key_id: keyId, issuer_id: issuerId, key: keyP8 }, null, 0),
);

const isDarwin = process.platform === 'darwin';
if (!isDarwin) {
  console.log('UPLOAD_SKIPPED_NOT_DARWIN ipaReady=' + ipa);
  console.log('On Mac: xcrun altool --upload-app -f IPA -t ios --apiKey KEYID --apiIssuer ISSUER');
  // Clean sensitive json
  try { fs.unlinkSync(apiKeyJson); } catch { /* ignore */ }
  process.exit(0);
}

// Place p8 where altool looks: ./private_keys/AuthKey_<KEYID>.p8 or ~/private_keys
const pkDir = path.join(__dirname, 'private_keys');
fs.mkdirSync(pkDir, { recursive: true });
const altoolKey = path.join(pkDir, `AuthKey_${keyId}.p8`);
fs.copyFileSync(keyPath, altoolKey);

const r = spawnSync(
  'xcrun',
  [
    'altool',
    '--upload-app',
    '-f',
    ipa,
    '-t',
    'ios',
    '--apiKey',
    keyId,
    '--apiIssuer',
    issuerId,
  ],
  {
    cwd: __dirname,
    env: { ...process.env, API_PRIVATE_KEYS_DIR: pkDir },
    encoding: 'utf8',
  },
);

try { fs.unlinkSync(apiKeyJson); } catch { /* ignore */ }

if (r.stdout) process.stdout.write(r.stdout.replace(keyP8, '[redacted]').replace(keyId, '[keyId]').replace(issuerId, '[issuer]'));
if (r.stderr) process.stderr.write(r.stderr.replace(keyP8, '[redacted]').replace(keyId, '[keyId]').replace(issuerId, '[issuer]'));
if (r.status !== 0) {
  console.error('UPLOAD_FAIL status=' + r.status);
  process.exit(r.status || 1);
}
console.log('UPLOAD_OK TestFlight processing should start shortly');

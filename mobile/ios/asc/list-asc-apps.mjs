/** List ASC apps (names/bundle ids only). Never prints secrets. */
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const env = Object.fromEntries(
  fs
    .readFileSync(path.join(__dirname, '.env.asc'), 'utf8')
    .split(/\r?\n/)
    .filter((l) => l && l.includes('='))
    .map((l) => {
      const i = l.indexOf('=');
      return [l.slice(0, i), l.slice(i + 1)];
    }),
);
function b64url(input) {
  const buf = Buffer.isBuffer(input) ? input : Buffer.from(input);
  return buf.toString('base64').replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
}
function jwt(keyP8, keyId, issuerId) {
  const header = { alg: 'ES256', kid: keyId, typ: 'JWT' };
  const now = Math.floor(Date.now() / 1000);
  const payload = { iss: issuerId, iat: now, exp: now + 1200, aud: 'appstoreconnect-v1' };
  const u = `${b64url(JSON.stringify(header))}.${b64url(JSON.stringify(payload))}`;
  const s = crypto.createSign('SHA256');
  s.update(u);
  s.end();
  return `${u}.${b64url(s.sign({ key: keyP8, dsaEncoding: 'ieee-p1363' }))}`;
}
const keyP8 = fs.readFileSync(env.ASC_KEY_PATH, 'utf8');
const token = jwt(keyP8, env.ASC_KEY_ID, env.ASC_ISSUER_ID);
const r = await fetch('https://api.appstoreconnect.apple.com/v1/apps?limit=50', {
  headers: { Authorization: 'Bearer ' + token, Accept: 'application/json' },
});
const j = await r.json();
console.log('APPS_STATUS', r.status, 'count', (j.data || []).length);
for (const a of j.data || []) {
  console.log('-', a.attributes?.bundleId, '|', a.attributes?.name, '| id=' + a.id);
}
const b = await fetch('https://api.appstoreconnect.apple.com/v1/bundleIds?limit=100', {
  headers: { Authorization: 'Bearer ' + token, Accept: 'application/json' },
});
const bj = await b.json();
console.log('BUNDLES', b.status);
for (const x of bj.data || []) {
  const id = x.attributes?.identifier || '';
  if (/knowledge|appigeek|kiag|quizene/i.test(id)) console.log('bundle', id);
}

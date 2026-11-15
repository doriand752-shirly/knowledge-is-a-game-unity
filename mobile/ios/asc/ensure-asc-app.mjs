/**
 * Ensure App Store Connect app exists for com.knowledgeisagame.kiagPlayer.
 * Uses Appy Geek ASC API key. Never prints secrets. Writes ASC_APP_ID into .env.asc.
 */
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const BUNDLE = 'com.knowledgeisagame.kiagPlayer';
const NAME = 'Knowledge is a Game';
const ASC = 'https://api.appstoreconnect.apple.com/v1';
const envPath = path.join(__dirname, '.env.asc');

function loadEnv() {
  const env = {};
  for (const line of fs.readFileSync(envPath, 'utf8').split(/\r?\n/)) {
    if (!line || line.startsWith('#') || !line.includes('=')) continue;
    const i = line.indexOf('=');
    env[line.slice(0, i)] = line.slice(i + 1);
  }
  return env;
}
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
async function asc(token, method, p, body) {
  const r = await fetch(ASC + p, {
    method,
    headers: {
      Authorization: 'Bearer ' + token,
      Accept: 'application/json',
      'Content-Type': 'application/json',
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await r.text();
  let j = {};
  try {
    j = text ? JSON.parse(text) : {};
  } catch {
    j = { raw: text?.slice(0, 400) };
  }
  return { status: r.status, ok: r.ok, j };
}

const env = loadEnv();
const keyP8 = fs.readFileSync(env.ASC_KEY_PATH, 'utf8');
const token = jwt(keyP8, env.ASC_KEY_ID, env.ASC_ISSUER_ID);

// Find bundle id resource
const bundles = await asc(token, 'GET', `/bundleIds?filter[identifier]=${encodeURIComponent(BUNDLE)}&limit=5`);
let bundleIdRes = bundles.j.data?.[0]?.id;
if (!bundleIdRes) {
  const created = await asc(token, 'POST', '/bundleIds', {
    data: {
      type: 'bundleIds',
      attributes: { identifier: BUNDLE, name: 'KnowledgeIsAGame', platform: 'IOS' },
    },
  });
  if (!created.ok) {
    console.error('BUNDLE_CREATE_FAIL', created.status, JSON.stringify(created.j.errors || created.j).slice(0, 300));
    process.exit(1);
  }
  bundleIdRes = created.j.data.id;
  console.log('BUNDLE_CREATED');
} else {
  console.log('BUNDLE_EXISTS');
}

const apps = await asc(token, 'GET', `/apps?filter[bundleId]=${encodeURIComponent(BUNDLE)}&limit=5`);
let appId = apps.j.data?.[0]?.id;
if (!appId) {
  // Need company SKU + primary locale
  const createdApp = await asc(token, 'POST', '/apps', {
    data: {
      type: 'apps',
      attributes: {
        name: NAME,
        primaryLocale: 'fr-FR',
        sku: 'knowledge-is-a-game-player',
        bundleId: BUNDLE,
      },
    },
  });
  // Newer ASC API uses relationships for bundleId
  if (!createdApp.ok) {
    const alt = await asc(token, 'POST', '/apps', {
      data: {
        type: 'apps',
        attributes: {
          name: NAME,
          primaryLocale: 'fr-FR',
          sku: 'knowledge-is-a-game-player',
        },
        relationships: {
          bundleId: { data: { type: 'bundleIds', id: bundleIdRes } },
        },
      },
    });
    if (!alt.ok) {
      console.error('APP_CREATE_FAIL', alt.status, JSON.stringify(alt.j.errors || alt.j).slice(0, 400));
      process.exit(1);
    }
    appId = alt.j.data.id;
  } else {
    appId = createdApp.j.data.id;
  }
  console.log('APP_CREATED');
} else {
  console.log('APP_EXISTS');
}

const lines = fs.readFileSync(envPath, 'utf8').split(/\r?\n/).filter((l) => l && !l.startsWith('ASC_APP_ID='));
lines.push(`ASC_APP_ID=${appId}`);
fs.writeFileSync(envPath, lines.join('\n') + '\n');
console.log('ASC_APP_ID_WRITTEN len=' + String(appId).length);

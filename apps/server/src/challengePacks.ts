import { readFileSync, readdirSync, existsSync } from "fs";
import { join } from "path";
import {
  FALLBACK_CHAIN,
  FALLBACK_FILL,
  FALLBACK_ODD,
  FALLBACK_SPIN,
  type ChainReactionPack,
  type FillBlanksPack,
  type OddOneOutPack,
  type SpinToWinPack,
} from "@quizene/shared";

function challengeDirs(): string[] {
  return [
    join(__dirname, "../../../content/challenges"),
    join(__dirname, "../../content/challenges"),
    join(process.cwd(), "content/challenges"),
    "/app/content/challenges",
  ];
}

function findDir(): string | null {
  for (const d of challengeDirs()) {
    if (existsSync(d)) return d;
  }
  return null;
}

function loadKind<T>(fileHint: string, fallback: T[]): T[] {
  const dir = findDir();
  if (!dir) return fallback;
  try {
    const files = readdirSync(dir).filter((f) => f.includes(fileHint) && f.endsWith(".json"));
    const packs: T[] = [];
    for (const f of files) {
      const raw = JSON.parse(readFileSync(join(dir, f), "utf8")) as { packs?: T[] };
      if (Array.isArray(raw.packs)) packs.push(...raw.packs);
    }
    return packs.length ? packs : fallback;
  } catch {
    return fallback;
  }
}

let cached: {
  chain: ChainReactionPack[];
  fill: FillBlanksPack[];
  odd: OddOneOutPack[];
  spin: SpinToWinPack[];
} | null = null;

export function getChallengePacks() {
  if (!cached) {
    cached = {
      chain: loadKind<ChainReactionPack>("chain", FALLBACK_CHAIN),
      fill: loadKind<FillBlanksPack>("fill", FALLBACK_FILL),
      odd: loadKind<OddOneOutPack>("odd", FALLBACK_ODD),
      spin: loadKind<SpinToWinPack>("spin", FALLBACK_SPIN),
    };
  }
  return cached;
}

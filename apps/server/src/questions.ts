import { readFileSync, readdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import {
  THEME_COLORS,
  THEME_EMOJIS,
  type QuestionDef,
  type QuestionPack,
  type ThemeOption,
} from "@quizene/shared";

const __dirname = dirname(fileURLToPath(import.meta.url));

function contentDir(): string {
  const candidates = [
    join(__dirname, "../../../content/questions"),
    join(__dirname, "../../content/questions"),
    join(process.cwd(), "content/questions"),
    "/app/content/questions",
  ];
  for (const c of candidates) {
    try {
      readdirSync(c);
      return c;
    } catch {
      /* try next */
    }
  }
  throw new Error("Impossible de trouver content/questions");
}

let cached: QuestionDef[] | null = null;

/** Match rule: QCM only, exactly 4 choices, with a correct id. */
function isMatchQcm(q: QuestionDef): boolean {
  return (
    q.type === "qcm" &&
    Array.isArray(q.choices) &&
    q.choices.length === 4 &&
    Array.isArray(q.correctChoiceIds) &&
    q.correctChoiceIds.length > 0 &&
    q.correctChoiceIds.every((id) => q.choices!.some((c) => c.id === id))
  );
}

export function loadAllQuestions(): QuestionDef[] {
  if (cached) return cached;
  const dir = contentDir();
  const files = readdirSync(dir).filter((f) => f.endsWith(".json"));
  const all: QuestionDef[] = [];
  for (const file of files) {
    const pack = JSON.parse(readFileSync(join(dir, file), "utf-8")) as QuestionPack;
    for (const q of pack.questions) {
      if (!isMatchQcm(q)) continue;
      all.push({
        ...q,
        type: "qcm",
        timeLimitSec: 10,
      });
    }
  }
  cached = all;
  return all;
}

export function listCategories(): string[] {
  const set = new Set<string>();
  for (const q of loadAllQuestions()) {
    if (q.category?.trim()) set.add(q.category.trim());
  }
  return [...set].sort();
}

export function pickThemeOptions(
  count = 4,
  preferRelatedTo?: string,
  seed = Date.now()
): ThemeOption[] {
  const cats = listCategories();
  let s = seed % 2147483647;
  const rand = () => {
    s = (s * 48271) % 2147483647;
    return s / 2147483647;
  };
  const pool = [...cats];
  for (let i = pool.length - 1; i > 0; i--) {
    const j = Math.floor(rand() * (i + 1));
    [pool[i], pool[j]] = [pool[j]!, pool[i]!];
  }
  // Bias: keep related category first if present
  if (preferRelatedTo && pool.includes(preferRelatedTo)) {
    const idx = pool.indexOf(preferRelatedTo);
    if (idx > 0) {
      const [item] = pool.splice(idx, 1);
      pool.unshift(item!);
    }
  }
  const picked = pool.slice(0, Math.min(count, pool.length));
  // Pad if too few categories
  while (picked.length < count && picked.length > 0) {
    picked.push(picked[picked.length % picked.length]!);
  }
  return picked.map((label, i) => ({
    id: `t${i}-${label}`,
    label,
    emoji: THEME_EMOJIS[i % THEME_EMOJIS.length]!,
    color: THEME_COLORS[i % THEME_COLORS.length]!,
  }));
}

export function pickQuestionFromCategory(
  category: string,
  usedIds: Set<string>,
  seed = Date.now()
): QuestionDef | null {
  const pool = loadAllQuestions().filter(
    (q) => (q.category ?? "").trim() === category && !usedIds.has(q.id)
  );
  const fallback = loadAllQuestions().filter((q) => !usedIds.has(q.id));
  const source = pool.length ? pool : fallback;
  if (!source.length) return null;
  let s = seed % 2147483647;
  const j = Math.floor(((s * 48271) % 2147483647) / 2147483647 * source.length);
  return source[j] ?? source[0]!;
}

export function pickQuestions(count: number, seed = Date.now()): QuestionDef[] {
  const pool = [...loadAllQuestions()];
  let s = seed % 2147483647;
  const rand = () => {
    s = (s * 48271) % 2147483647;
    return s / 2147483647;
  };
  for (let i = pool.length - 1; i > 0; i--) {
    const j = Math.floor(rand() * (i + 1));
    [pool[i], pool[j]] = [pool[j]!, pool[i]!];
  }
  return pool.slice(0, Math.min(count, pool.length));
}

export function normalizeText(s: string): string {
  return s
    .normalize("NFD")
    .replace(/\p{M}/gu, "")
    .toLowerCase()
    .trim()
    .replace(/\s+/g, " ");
}

export function gradeAnswer(
  q: QuestionDef,
  raw: string
): { correct: boolean; closeness?: number } {
  if (q.type === "qcm" || q.type === "vrai_faux") {
    const ids = q.correctChoiceIds ?? [];
    return { correct: ids.includes(raw) };
  }
  if (q.type === "estimation") {
    const n = Number(String(raw).replace(",", "."));
    if (!Number.isFinite(n) || q.correctNumber == null) return { correct: false };
    const tol = q.tolerance ?? 0;
    const dist = Math.abs(n - q.correctNumber);
    return { correct: dist <= tol, closeness: 1 / (1 + dist) };
  }
  const norm = normalizeText(raw);
  const accepted = (q.acceptedAnswers ?? []).map(normalizeText);
  return { correct: accepted.includes(norm) };
}

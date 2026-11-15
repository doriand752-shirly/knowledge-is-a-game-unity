/** Décades challenge shapes (loaded packs live under content/challenges/). */

export interface ChainStep {
  prompt: string;
  answer: string;
}

export interface ChainReactionPack {
  id: string;
  title: string;
  timeLimitSec: number;
  start: string;
  steps: ChainStep[];
  choices: string[];
}

export interface FillBlank {
  id: string;
  options: string[];
  correct: string;
}

export interface FillBlanksPack {
  id: string;
  title: string;
  timeLimitSec: number;
  sentence: string;
  blanks: FillBlank[];
}

export interface OddItem {
  id: string;
  label: string;
}

export interface OddOneOutPack {
  id: string;
  title: string;
  timeLimitSec: number;
  prompt: string;
  items: OddItem[];
  oddId: string;
}

export interface SpinToWinPack {
  id: string;
  title: string;
  timeLimitSec: number;
  prompt: string;
  options: string[];
  correct: string;
}

/** Built-in FR fallbacks if content JSON missing at runtime. */
export const FALLBACK_CHAIN: ChainReactionPack[] = [
  {
    id: "chain-fallback",
    title: "Chaîne pop",
    timeLimitSec: 45,
    start: "Hit",
    steps: [
      { prompt: "Après Hit → ?", answer: "Clip" },
      { prompt: "Après Clip → ?", answer: "Radio" },
      { prompt: "Après Radio → ?", answer: "Fan" },
    ],
    choices: ["Clip", "Radio", "Fan", "Album", "Concert"],
  },
];

export const FALLBACK_FILL: FillBlanksPack[] = [
  {
    id: "fill-fallback",
    title: "Phrase à trous",
    timeLimitSec: 40,
    sentence: "On danse sous les ___ disco.",
    blanks: [{ id: "b1", options: ["boules", "pluies", "neiges"], correct: "boules" }],
  },
];

export const FALLBACK_ODD: OddOneOutPack[] = [
  {
    id: "odd-fallback",
    title: "L’intrus",
    timeLimitSec: 25,
    prompt: "Quel fruit n’est pas jaune ?",
    items: [
      { id: "a", label: "Banane" },
      { id: "b", label: "Citron" },
      { id: "c", label: "Fraise" },
      { id: "d", label: "Maïs" },
    ],
    oddId: "c",
  },
];

export const FALLBACK_SPIN: SpinToWinPack[] = [
  {
    id: "spin-fallback",
    title: "Spin to Win",
    timeLimitSec: 30,
    prompt: "Aligne le satellite de la Terre",
    options: ["Lune", "Mars", "Soleil", "Phobos"],
    correct: "Lune",
  },
];

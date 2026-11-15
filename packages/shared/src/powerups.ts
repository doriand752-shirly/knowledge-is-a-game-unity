/** Knowledge is a Game — Power Plays (classics + named Décades). */

export type ClassicPowerUpId =
  | "freeze"
  | "gloop"
  | "bombles"
  | "nibblers"
  | "points_party"
  | "bet";

/** Named Décades Power Plays only — do not invent unnamed ones. */
export type DecadesPowerUpId = "zipper" | "disco_inferno" | "lockdown" | "millennium_bug";

export type PowerUpId = ClassicPowerUpId | DecadesPowerUpId;

export interface PowerUpDef {
  id: PowerUpId;
  name: string;
  description: string;
  needsTarget: boolean;
  sabotage: boolean;
  beforeQuestion: boolean;
  /** Only in Décades mode pool */
  decadesOnly?: boolean;
}

export const POWER_UPS: Record<PowerUpId, PowerUpDef> = {
  freeze: {
    id: "freeze",
    name: "Freeze",
    description: "Gèle les réponses de la cible — elle doit tapoter pour dégeler.",
    needsTarget: true,
    sabotage: true,
    beforeQuestion: true,
  },
  gloop: {
    id: "gloop",
    name: "Gloop",
    description: "Slime sur l’écran adverse — il faut essuyer pour voir.",
    needsTarget: true,
    sabotage: true,
    beforeQuestion: true,
  },
  bombles: {
    id: "bombles",
    name: "Bombles",
    description: "Bombes flottantes — un tap = court timeout.",
    needsTarget: true,
    sabotage: true,
    beforeQuestion: true,
  },
  nibblers: {
    id: "nibblers",
    name: "Nibblers",
    description: "Des critters croquent les lettres des réponses.",
    needsTarget: true,
    sabotage: true,
    beforeQuestion: true,
  },
  points_party: {
    id: "points_party",
    name: "Points Party",
    description: "Double tes points si tu marques sur cette question.",
    needsTarget: false,
    sabotage: false,
    beforeQuestion: true,
  },
  bet: {
    id: "bet",
    name: "Pari",
    description: "Mise sur un adversaire : s’il a juste, tu gagnes un bonus.",
    needsTarget: true,
    sabotage: false,
    beforeQuestion: true,
  },
  zipper: {
    id: "zipper",
    name: "Zipper",
    description: "Les réponses sont zippées — tire la fermeture pour les lire.",
    needsTarget: true,
    sabotage: true,
    beforeQuestion: true,
    decadesOnly: true,
  },
  disco_inferno: {
    id: "disco_inferno",
    name: "Disco Inferno",
    description: "Lumières disco qui obscurcissent le texte — survit au stroboscope.",
    needsTarget: true,
    sabotage: true,
    beforeQuestion: true,
    decadesOnly: true,
  },
  lockdown: {
    id: "lockdown",
    name: "Lockdown",
    description: "Cadenas sur les réponses — tape les cadenas pour déverrouiller.",
    needsTarget: true,
    sabotage: true,
    beforeQuestion: true,
    decadesOnly: true,
  },
  millennium_bug: {
    id: "millennium_bug",
    name: "Millennium Bug",
    description: "Texte brouillé Y2K — essuie pour démêler les lettres.",
    needsTarget: true,
    sabotage: true,
    beforeQuestion: true,
    decadesOnly: true,
  },
};

export const POWER_UP_LIST = Object.values(POWER_UPS);

export const CLASSIC_POWER_IDS: PowerUpId[] = [
  "freeze",
  "gloop",
  "bombles",
  "nibblers",
  "points_party",
  "bet",
];

export const DECADES_EXTRA_IDS: PowerUpId[] = [
  "zipper",
  "disco_inferno",
  "lockdown",
  "millennium_bug",
];

export const SABOTAGE_IDS: PowerUpId[] = [
  "freeze",
  "gloop",
  "bombles",
  "nibblers",
  "zipper",
  "disco_inferno",
  "lockdown",
  "millennium_bug",
];

export const DOUBLE_PLAY_GAP_RATIO = 0.35;

export function powerUpIdsForMode(decades: boolean): PowerUpId[] {
  return decades ? [...CLASSIC_POWER_IDS, ...DECADES_EXTRA_IDS] : [...CLASSIC_POWER_IDS];
}

export function initialPowerUpCharges(decades = false): Record<PowerUpId, number> {
  const out = {} as Record<PowerUpId, number>;
  for (const id of powerUpIdsForMode(decades)) {
    // Sabotages: 3 charges; buffs: 2 — always something to throw
    out[id] = POWER_UPS[id].sabotage ? 3 : 2;
  }
  return out;
}

/** Match modes — Classique / Décades / Rapide */

export type MatchModeId = "classique" | "decades" | "rapide";

export type ChallengeKind =
  | "linking"
  | "sorting"
  | "chain_reaction"
  | "fill_blanks"
  | "odd_one_out"
  | "spin_to_win";

export interface MatchModeDef {
  id: MatchModeId;
  label: string;
  description: string;
  /** Number of MCQ rounds before pyramid */
  mcqCount: number;
  /** After which MCQ index (1-based completed count) insert which challenge */
  challengesAfterMcq: Partial<Record<number, ChallengeKind>>;
  /** Use Décades Power Play pool */
  decadesPowerPlays: boolean;
}

export const MATCH_MODES: Record<MatchModeId, MatchModeDef> = {
  classique: {
    id: "classique",
    label: "Classique",
    description: "9 QCM + Linking + Sorting + Pyramide",
    mcqCount: 9,
    challengesAfterMcq: {
      3: "linking",
      6: "sorting",
    },
    decadesPowerPlays: false,
  },
  decades: {
    id: "decades",
    label: "Décades",
    description: "9 QCM + 6 challenges + Power Plays élargis + Pyramide",
    mcqCount: 9,
    challengesAfterMcq: {
      1: "chain_reaction",
      2: "fill_blanks",
      3: "linking",
      4: "odd_one_out",
      5: "sorting",
      6: "spin_to_win",
    },
    decadesPowerPlays: true,
  },
  rapide: {
    id: "rapide",
    label: "Rapide",
    description: "6 QCM + Linking + Pyramide",
    mcqCount: 6,
    challengesAfterMcq: {
      3: "linking",
    },
    decadesPowerPlays: false,
  },
};

export const MATCH_MODE_LIST = Object.values(MATCH_MODES);

export const SELFIE_MAX_BYTES = 150_000;

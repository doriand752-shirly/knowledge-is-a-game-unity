export type QuestionType = "qcm" | "vrai_faux" | "estimation" | "texte_court";

export type GamePhase =
  | "lobby"
  | "countdown"
  | "theme_vote"
  | "theme_reveal"
  | "power_play"
  | "question"
  | "votes_show"
  | "reveal"
  | "linking"
  | "sorting"
  | "chain_reaction"
  | "fill_blanks"
  | "odd_one_out"
  | "spin_to_win"
  | "pyramid"
  | "scoreboard"
  | "podium";

export type AvatarAnim =
  | "idle"
  | "correct"
  | "wrong"
  | "cast"
  | "frozen"
  | "celebrate"
  | "walk";

export type HostMood = "idle" | "talk" | "cheer" | "tease" | "wow";

export interface QuestionChoice {
  id: string;
  label: string;
}

export interface QuestionDef {
  id: string;
  type: QuestionType;
  prompt: string;
  choices?: QuestionChoice[];
  correctChoiceIds?: string[];
  correctNumber?: number;
  tolerance?: number;
  acceptedAnswers?: string[];
  difficulty?: 1 | 2 | 3;
  points?: number;
  timeLimitSec?: number;
  category?: string;
}

export interface QuestionPack {
  id: string;
  title: string;
  locale: "fr";
  questions: QuestionDef[];
}

export interface ThemeOption {
  id: string;
  label: string;
  emoji: string;
  color: string;
}

export const BRAND_NAME = "Knowledge is a Game";

/** One phone is enough to start (TV host is not counted). */
export const MIN_PLAYERS = 1;
export const MAX_PLAYERS = 6;
export const DEFAULT_ROUNDS = 9; // 9 MCQ (+ challenges + pyramid outside)
/** Everyone answers in 10 seconds. */
export const DEFAULT_QUESTION_TIME = 10;
export const POWERUP_WINDOW_SEC = 12;
export const THEME_VOTE_SEC = 10;
/** Host roulette before power play — phones wait. */
export const THEME_REVEAL_SEC = 5.5;
/** Hold beat: show locked pads / votes before grading. */
export const VOTE_SHOW_SEC = 3.2;
/** Correct answer + score pop. */
export const REVEAL_SEC = 4.5;

export const PLAYER_COLORS = [
  "#E63946",
  "#457B9D",
  "#2A9D8F",
  "#E9C46A",
  "#9B5DE5",
  "#F4A261",
] as const;

export const THEME_COLORS = ["#FF6B6B", "#4ECDC4", "#FFE66D", "#A78BFA"] as const;

export const THEME_EMOJIS = ["🚪", "🎬", "🌍", "🧠", "🎵", "🚀", "⚽", "📚"] as const;

export const PAD_LETTERS = ["A", "B", "C", "D"] as const;

/** Map choice id a/b/c/d → pad index 0..3 */
export function choiceIdToPadIndex(id: string): number {
  const map: Record<string, number> = { a: 0, b: 1, c: 2, d: 3, A: 0, B: 1, C: 2, D: 3 };
  if (id in map) return map[id]!;
  const n = Number(id);
  if (Number.isFinite(n) && n >= 0 && n <= 3) return n;
  return -1;
}

/** Maxime — répliques FR enjouées */
export function hostLineFor(
  phase: GamePhase,
  ctx: { category?: string; name?: string; correct?: boolean } = {}
): { line: string; mood: HostMood } {
  switch (phase) {
    case "lobby":
      return {
        line: "Bienvenue dans Knowledge is a Game ! Scannez le QR, montez sur le plateau… et préparez vos neurones !",
        mood: "talk",
      };
    case "countdown":
      return {
        line: "Les projecteurs s’allument… ça commence dans trois… deux… un ! Allez-y !",
        mood: "cheer",
      };
    case "theme_vote":
      return {
        line: "Quatre portes, un seul destin ! Votez pour le thème, mes champions !",
        mood: "talk",
      };
    case "theme_reveal":
      return {
        line: ctx.category
          ? `La roulette tourne… et le thème est… ${ctx.category} !`
          : "La roulette tourne… suspense maximal !",
        mood: "wow",
      };
    case "power_play":
      return {
        line: "Power Plays ! Sabotez, doublez, pariez… ou jouez safe. Double Play si vous êtes loin derrière !",
        mood: "tease",
      };
    case "question":
      return {
        line: ctx.category
          ? `Thème ${ctx.category} ! Dix secondes chrono — courez vers les pads A B C D !`
          : "Question ! Dix secondes chrono — go go go !",
        mood: "wow",
      };
    case "votes_show":
      return {
        line: "Temps écoulé ! Regardez où tout le monde s’est locké… suspense !",
        mood: "talk",
      };
    case "reveal":
      return ctx.correct
        ? { line: "Et la bonne réponse est… là ! Bravo les champions !", mood: "cheer" }
        : { line: "Et la bonne réponse est… là ! Ouille pour certains…", mood: "tease" };
    case "linking":
      return {
        line: "Challenge Linking ! Reliez les paires sur votre téléphone — vitesse + précision !",
        mood: "talk",
      };
    case "sorting":
      return {
        line: "Challenge Sorting ! Swipe gauche ou droite — fruit ou légume, terre ou mer…",
        mood: "talk",
      };
    case "chain_reaction":
      return {
        line: "Chain Reaction ! Enchaînez les bonnes réponses — clear-all pour le jackpot !",
        mood: "talk",
      };
    case "fill_blanks":
      return {
        line: "Fill the Blanks ! Complétez la phrase à trous sur votre téléphone !",
        mood: "talk",
      };
    case "odd_one_out":
      return {
        line: "Odd One Out ! Trouvez l’intrus parmi les quatre !",
        mood: "tease",
      };
    case "spin_to_win":
      return {
        line: "Spin to Win ! Faites tourner la roulette et alignez la bonne réponse !",
        mood: "cheer",
      };
    case "pyramid":
      return {
        line: "Finale — la Pyramide du Savoir ! Montez les marches… le premier en haut gagne le parchemin !",
        mood: "wow",
      };
    case "scoreboard":
      return { line: "Tableau des scores — qui grimpe, qui tremble ?", mood: "talk" };
    case "podium":
      return {
        line: ctx.name
          ? `Et le grand gagnant… ${ctx.name} ! Applaudiissons très fort !`
          : "Podium ! Merci d’avoir joué à Knowledge is a Game !",
        mood: "cheer",
      };
    default:
      return { line: "Knowledge is a Game — le show continue !", mood: "idle" };
  }
}

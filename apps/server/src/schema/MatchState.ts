import { Schema, type, MapSchema, ArraySchema } from "@colyseus/schema";

export class PlayerState extends Schema {
  @type("string") sessionId: string = "";
  @type("string") name: string = "";
  @type("boolean") isHost: boolean = false;
  @type("boolean") isReady: boolean = false;
  @type("number") score: number = 0;
  @type("number") slot: number = 0;
  @type("string") color: string = "#E63946";
  @type("string") anim: string = "idle";
  @type("boolean") connected: boolean = true;
  @type("boolean") frozen: boolean = false;
  @type("boolean") hasShield: boolean = false;
  @type("boolean") doubleNext: boolean = false;
  @type("boolean") hasAnswered: boolean = false;
  @type("boolean") lastCorrect: boolean = false;
  @type("number") lastPoints: number = 0;
  @type("string") powerUpsJson: string = "{}";
  @type("string") scrambleOrderJson: string = "";
  @type("string") themeVote: string = "";
  /** -1 = bench / not locked; 0..3 = pads A–D */
  @type("number") lockedAnswerIndex: number = -1;
  /** Active phone hazard: "" | freeze | gloop | bombles | nibblers */
  @type("string") hazard: string = "";
  @type("string") betTargetId: string = "";
  @type("boolean") doublePlayAvailable: boolean = false;
  @type("number") pyramidStep: number = 0;
  @type("number") linkingScore: number = 0;
  @type("number") sortingScore: number = 0;
  @type("boolean") linkingDone: boolean = false;
  /** Compressed JPEG data URL (clay filter); empty = silhouette */
  @type("string") selfieDataUrl: string = "";
  /** Cast character id (ketchup, glitter, …) */
  @type("string") characterId: string = "";
}

export class QuestionPublic extends Schema {
  @type("string") id: string = "";
  @type("string") questionType: string = "qcm";
  @type("string") prompt: string = "";
  @type("string") choicesJson: string = "[]";
  @type("number") timeLimitSec: number = 10;
  @type("number") points: number = 100;
  @type("string") category: string = "";
}

export class MatchState extends Schema {
  @type("string") roomCode: string = "";
  @type("string") phase: string = "lobby";
  /** MCQ index 0..8 within the show */
  @type("number") roundIndex: number = 0;
  @type("number") totalRounds: number = 9;
  @type("number") battleIndex: number = 0;
  @type("number") phaseEndsAt: number = 0;
  @type("number") serverNow: number = 0;
  @type("string") joinUrl: string = "";
  @type("string") lastRevealJson: string = "{}";
  @type("string") lastVfxJson: string = "";
  @type("string") podiumJson: string = "[]";
  @type("string") themeOptionsJson: string = "[]";
  @type("string") selectedThemeId: string = "";
  @type("string") selectedThemeLabel: string = "";
  @type("string") hostLine: string = "";
  @type("string") hostMood: string = "idle";
  @type("number") playerCount: number = 0;
  /** JSON for linking / sorting / pyramid UI */
  @type("string") challengeJson: string = "{}";
  @type("string") throwVfxJson: string = "";
  @type("string") scrollFact: string = "";
  @type("string") segment: string = "lobby";
  /** classique | decades | rapide */
  @type("string") matchMode: string = "classique";
  @type("boolean") powerPlaysEnabled: boolean = true;
  @type(QuestionPublic) question = new QuestionPublic();
  @type({ map: PlayerState }) players = new MapSchema<PlayerState>();
  @type(["string"]) playerOrder = new ArraySchema<string>();
}

import colyseus from "colyseus";
import type { Client } from "colyseus";

const { Room } = colyseus;
import {
  DEFAULT_QUESTION_TIME,
  DOUBLE_PLAY_GAP_RATIO,
  LINKING_PACKS,
  MATCH_MODES,
  MAX_PLAYERS,
  MIN_PLAYERS,
  PLAYER_COLORS,
  POWER_PLAY_SEC,
  POWER_UPS,
  PYRAMID_QUESTIONS,
  PYRAMID_STEPS_TO_WIN,
  REVEAL_SEC,
  SABOTAGE_IDS,
  SCROLL_FACTS,
  SELFIE_MAX_BYTES,
  SORTING_PACKS,
  THEME_REVEAL_SEC,
  THEME_VOTE_SEC,
  VOTE_SHOW_SEC,
  absoluteJoinUrl,
  choiceIdToPadIndex,
  generateRoomCode,
  hostLineFor,
  initialPowerUpCharges,
  CAST_CHARACTERS,
  isCharacterId,
  type ChallengeKind,
  type ClientMessage,
  type GamePhase,
  type LinkingChallenge,
  type MatchModeId,
  type PowerUpId,
  type QuestionDef,
  type SortingChallenge,
  type ThemeOption,
  type ChainReactionPack,
  type FillBlanksPack,
  type OddOneOutPack,
  type SpinToWinPack,
} from "@quizene/shared";
import { MatchState, PlayerState } from "../schema/MatchState.js";
import { gradeAnswer, pickQuestionFromCategory, pickThemeOptions } from "../questions.js";
import { getChallengePacks } from "../challengePacks.js";

const ROOM_REGISTRY = new Map<string, string>();

export function findRoomIdByCode(code: string): string | undefined {
  return ROOM_REGISTRY.get(code.toUpperCase());
}

export class MatchRoom extends Room<MatchState> {
  maxClients = MAX_PLAYERS + 1;
  private currentQuestion: QuestionDef | null = null;
  private usedQuestionIds = new Set<string>();
  private lastCategory = "";
  private themeOptions: ThemeOption[] = [];
  private answers = new Map<string, string>();
  private answerAt = new Map<string, number>();
  private phaseTimer: ReturnType<typeof setTimeout> | null = null;
  private tickTimer: ReturnType<typeof setInterval> | null = null;
  private hostSessionId: string | null = null;
  private publicWebUrl = process.env.PUBLIC_WEB_URL || "http://localhost:2567";
  private linking: LinkingChallenge | null = null;
  private sorting: SortingChallenge | null = null;
  private linkingLinks = new Map<string, { left: string; right: string }[]>();
  private sortingAnswers = new Map<string, Record<string, "L" | "R">>();
  private pyramidQIndex = 0;
  private usedLinking = new Set<string>();
  private usedSorting = new Set<string>();
  private usedChain = new Set<string>();
  private usedFill = new Set<string>();
  private usedOdd = new Set<string>();
  private usedSpin = new Set<string>();
  private questionStartedAt = 0;
  private chainPack: ChainReactionPack | null = null;
  private fillPack: FillBlanksPack | null = null;
  private oddPack: OddOneOutPack | null = null;
  private spinPack: SpinToWinPack | null = null;
  private challengeDone = new Set<string>();

  onCreate(options: { roomCode?: string; publicWebUrl?: string }) {
    const code = (options.roomCode || generateRoomCode(4)).toUpperCase();
    this.setState(new MatchState());
    this.state.roomCode = code;
    if (options.publicWebUrl) this.publicWebUrl = options.publicWebUrl;
    this.state.joinUrl = absoluteJoinUrl(this.publicWebUrl, code);
    this.state.totalRounds = MATCH_MODES.classique.mcqCount;
    this.state.matchMode = "classique";
    this.state.powerPlaysEnabled = true;
    this.state.serverNow = Date.now();
    this.setHostLine("lobby");
    ROOM_REGISTRY.set(code, this.roomId);
    this.setMetadata({ roomCode: code });

    this.tickTimer = setInterval(() => {
      this.state.serverNow = Date.now();
      this.broadcastSnapshot();
    }, 400);

    const types: ClientMessage["type"][] = [
      "set_name",
      "set_role",
      "set_selfie",
      "set_character",
      "set_match_options",
      "start_match",
      "vote_theme",
      "submit_answer",
      "use_powerup",
      "clear_hazard",
      "linking_submit",
      "sorting_submit",
      "chain_submit",
      "fill_blanks_submit",
      "odd_one_out_submit",
      "spin_to_win_submit",
      "pyramid_answer",
      "request_snapshot",
      "skip_phase",
      "play_again",
    ];
    for (const t of types) {
      this.onMessage(t, (client, message) => {
        const payload =
          message && typeof message === "object" ? (message as Record<string, unknown>) : {};
        this.handleMessage(client, { type: t, ...payload } as ClientMessage);
      });
    }

  }

  /** Full JSON view of match — native clients subscribe to "snapshot". */
  private buildSnapshot() {
    const players: Record<string, unknown>[] = [];
    this.state.players.forEach((p, id) => {
      players.push({
        sessionId: id,
        name: p.name,
        isHost: p.isHost,
        isBot: p.isBot,
        score: p.score,
        slot: p.slot,
        color: p.color,
        anim: p.anim,
        connected: p.connected,
        frozen: p.frozen,
        hasAnswered: p.hasAnswered,
        lastCorrect: p.lastCorrect,
        lastPoints: p.lastPoints,
        powerUpsJson: p.powerUpsJson,
        themeVote: p.themeVote,
        lockedAnswerIndex: p.lockedAnswerIndex,
        hazard: p.hazard,
        betTargetId: p.betTargetId,
        doublePlayAvailable: p.doublePlayAvailable,
        doubleNext: p.doubleNext,
        pyramidStep: p.pyramidStep,
        linkingScore: p.linkingScore,
        sortingScore: p.sortingScore,
        linkingDone: p.linkingDone,
        selfieDataUrl: p.selfieDataUrl,
        characterId: p.characterId,
      });
    });
    return {
      roomCode: this.state.roomCode,
      phase: this.state.phase,
      roundIndex: this.state.roundIndex,
      totalRounds: this.state.totalRounds,
      battleIndex: this.state.battleIndex,
      phaseEndsAt: this.state.phaseEndsAt,
      serverNow: this.state.serverNow,
      joinUrl: this.state.joinUrl,
      lastRevealJson: this.state.lastRevealJson,
      podiumJson: this.state.podiumJson,
      themeOptionsJson: this.state.themeOptionsJson,
      selectedThemeId: this.state.selectedThemeId,
      selectedThemeLabel: this.state.selectedThemeLabel,
      hostLine: this.state.hostLine,
      hostMood: this.state.hostMood,
      playerCount: this.state.playerCount,
      challengeJson: this.state.challengeJson,
      throwVfxJson: this.state.throwVfxJson,
      scrollFact: this.state.scrollFact,
      segment: this.state.segment,
      matchMode: this.state.matchMode,
      powerPlaysEnabled: this.state.powerPlaysEnabled,
      question: {
        id: this.state.question.id,
        prompt: this.state.question.prompt,
        choicesJson: this.state.question.choicesJson,
        category: this.state.question.category,
        points: this.state.question.points,
        timeLimitSec: this.state.question.timeLimitSec,
      },
      players,
    };
  }

  private broadcastSnapshot() {
    this.broadcast("snapshot", this.buildSnapshot());
  }

  private modeDef() {
    const id = (this.state.matchMode as MatchModeId) || "classique";
    return MATCH_MODES[id] ?? MATCH_MODES.classique;
  }

  private decadesPool() {
    return this.modeDef().decadesPowerPlays;
  }

  private setHostLine(phase: GamePhase, ctx?: Parameters<typeof hostLineFor>[1]) {
    const { line, mood } = hostLineFor(phase, ctx);
    this.state.hostLine = line;
    this.state.hostMood = mood;
  }

  onJoin(client: Client, options: { role?: string; name?: string }) {
    const role = options.role === "host" ? "host" : "player";
    const p = new PlayerState();
    p.sessionId = client.sessionId;
    p.connected = true;
    p.powerUpsJson = JSON.stringify(initialPowerUpCharges(this.decadesPool()));

    if (role === "host" && !this.hostSessionId) {
      p.isHost = true;
      p.name = options.name?.trim() || "Présentateur";
      p.slot = -1;
      this.hostSessionId = client.sessionId;
    } else {
      if (this.countPlayers() >= MAX_PLAYERS) throw new Error("Salle pleine (max 6 joueurs)");
      p.isHost = false;
      p.name = (options.name?.trim() || `Joueur ${this.countPlayers() + 1}`).slice(0, 16);
      const slot = this.nextSlot();
      p.slot = slot;
      const cast = CAST_CHARACTERS[slot % CAST_CHARACTERS.length]!;
      p.characterId = cast.id;
      p.color = cast.color;
      this.state.playerOrder.push(client.sessionId);
    }

    this.state.players.set(client.sessionId, p);
    this.syncPlayerCount();
    this.broadcast("toast", {
      message: p.isHost ? "Présentateur connecté" : `${p.name} a rejoint (${this.state.playerCount})`,
    });
  }

  async onLeave(client: Client, consented: boolean) {
    const p = this.state.players.get(client.sessionId);
    if (!p) return;
    p.connected = false;
    this.syncPlayerCount();
    try {
      if (!consented) await this.allowReconnection(client, 30);
      p.connected = true;
      this.syncPlayerCount();
    } catch {
      if (p.isHost) this.hostSessionId = null;
      else {
        const idx = this.state.playerOrder.indexOf(client.sessionId);
        if (idx >= 0) this.state.playerOrder.splice(idx, 1);
      }
      this.state.players.delete(client.sessionId);
      this.answers.delete(client.sessionId);
      this.syncPlayerCount();
    }
  }

  onDispose() {
    if (this.phaseTimer) clearTimeout(this.phaseTimer);
    if (this.tickTimer) clearInterval(this.tickTimer);
    ROOM_REGISTRY.delete(this.state.roomCode);
  }

  private countPlayers(): number {
    let n = 0;
    this.state.players.forEach((p) => {
      if (!p.isHost && p.connected) n++;
    });
    return n;
  }

  private countHumanPlayers(): number {
    let n = 0;
    this.state.players.forEach((p) => {
      if (!p.isHost && !p.isBot && p.connected) n++;
    });
    return n;
  }

  private syncPlayerCount() {
    this.state.playerCount = this.countPlayers();
  }

  /** Ensure at least `minPlayers` non-host seats (humans + bots). */
  private ensureBotOpponents(minPlayers = 2) {
    const BOT_NAMES = ["Bot Max", "Bot Pixel", "Bot Neon", "Bot Clay"];
    let guard = 0;
    while (this.countPlayers() < minPlayers && this.countPlayers() < MAX_PLAYERS && guard++ < 6) {
      const id = `bot-${Date.now().toString(36)}-${guard}`;
      const bot = new PlayerState();
      bot.sessionId = id;
      bot.isHost = false;
      bot.isBot = true;
      bot.connected = true;
      bot.name = BOT_NAMES[(this.countPlayers() + guard) % BOT_NAMES.length]!;
      const slot = this.nextSlot();
      bot.slot = slot;
      const cast = CAST_CHARACTERS[slot % CAST_CHARACTERS.length]!;
      bot.characterId = cast.id;
      bot.color = cast.color;
      bot.powerUpsJson = JSON.stringify(initialPowerUpCharges(this.decadesPool()));
      this.state.players.set(id, bot);
      this.state.playerOrder.push(id);
    }
    this.syncPlayerCount();
    if (this.countPlayers() > this.countHumanPlayers()) {
      this.broadcast("toast", {
        message: `Bot adversaire en piste (${this.countPlayers()} joueurs)`,
      });
    }
  }

  private eachBot(fn: (p: PlayerState, id: string) => void) {
    this.state.players.forEach((p, id) => {
      if (p.isBot && !p.isHost) fn(p, id);
    });
  }

  /** Schedule bot actions for the current phase (QCM / portes / challenges). */
  private scheduleBotActions() {
    const phase = this.state.phase;
    const delay = 800 + Math.floor(Math.random() * 1200);
    setTimeout(() => {
      if (this.state.phase !== phase) return;
      if (phase === "theme_vote") this.botsVoteTheme();
      else if (phase === "question") this.botsAnswerQuestion();
      else if (phase === "linking") this.botsFinishLinking();
      else if (phase === "sorting") this.botsFinishSorting();
      else if (phase === "chain_reaction") this.botsFinishChain();
      else if (phase === "fill_blanks") this.botsFinishFill();
      else if (phase === "odd_one_out") this.botsFinishOdd();
      else if (phase === "spin_to_win") this.botsFinishSpin();
      else if (phase === "pyramid") this.botsPyramidAnswer();
    }, delay);
  }

  private botsVoteTheme() {
    if (!this.themeOptions.length) return;
    this.eachBot((pl) => {
      if (pl.themeVote) return;
      const pick = this.themeOptions[Math.floor(Math.random() * this.themeOptions.length)]!;
      pl.themeVote = pick.id;
    });
    this.broadcastSnapshot();
  }

  private botsAnswerQuestion() {
    const q = this.currentQuestion;
    if (!q?.choices?.length) return;
    const correctId = q.correctChoiceIds?.[0];
    this.eachBot((pl, id) => {
      if (pl.hasAnswered || pl.frozen) return;
      // ~55% correct so a human can win
      const right = Math.random() < 0.55 && correctId;
      const choice = right
        ? correctId!
        : q.choices![Math.floor(Math.random() * q.choices!.length)]!.id;
      this.onSubmitAnswer(id, pl, choice);
    });
  }

  private botsFinishLinking() {
    this.eachBot((pl, id) => {
      if (pl.linkingDone) return;
      this.onLinkingSubmit(id, pl, []);
    });
  }

  private botsFinishSorting() {
    if (!this.sorting) return;
    this.eachBot((pl, id) => {
      if (this.challengeDone.has(id)) return;
      for (const item of this.sorting!.items) {
        const side = Math.random() < 0.55 ? item.side : item.side === "L" ? "R" : "L";
        this.onSortingSubmit(id, pl, item.id, side);
      }
    });
  }

  private botsFinishChain() {
    this.eachBot((pl, id) => {
      this.onChainSubmit(id, pl, []);
    });
  }

  private botsFinishFill() {
    this.eachBot((pl, id) => {
      this.onFillSubmit(id, pl, {});
    });
  }

  private botsFinishOdd() {
    if (!this.oddPack?.items?.length) return;
    this.eachBot((pl, id) => {
      if (this.challengeDone.has(id)) return;
      const item =
        Math.random() < 0.55
          ? this.oddPack!.items.find((i) => i.id === this.oddPack!.oddId) ??
            this.oddPack!.items[0]!
          : this.oddPack!.items[Math.floor(Math.random() * this.oddPack!.items.length)]!;
      this.onOddSubmit(id, pl, item.id);
    });
  }

  private botsFinishSpin() {
    if (!this.spinPack?.options?.length) return;
    this.eachBot((pl, id) => {
      if (this.challengeDone.has(id)) return;
      const opts = this.spinPack!.options;
      const pick =
        Math.random() < 0.55
          ? this.spinPack!.correct
          : opts[Math.floor(Math.random() * opts.length)]!;
      this.onSpinSubmit(id, pl, pick);
    });
  }

  private botsPyramidAnswer() {
    const q = PYRAMID_QUESTIONS[this.pyramidQIndex % PYRAMID_QUESTIONS.length];
    if (!q?.choices?.length) return;
    this.eachBot((_pl, id) => {
      const pl = this.state.players.get(id);
      if (!pl || pl.pyramidStep >= PYRAMID_STEPS_TO_WIN) return;
      const right = Math.random() < 0.5;
      const pick = right
        ? q.correctChoiceId
        : q.choices[Math.floor(Math.random() * q.choices.length)]!.id;
      this.onPyramidAnswer(id, pl, pick);
      // Wrong guess → retry shortly so the pyramid can finish without a human.
      if (pick !== q.correctChoiceId && this.state.phase === "pyramid") {
        setTimeout(() => {
          if (this.state.phase === "pyramid") this.botsPyramidAnswer();
        }, 900 + Math.floor(Math.random() * 700));
      }
    });
  }

  private removeBots() {
    const ids: string[] = [];
    this.state.players.forEach((p, id) => {
      if (p.isBot) ids.push(id);
    });
    for (const id of ids) {
      this.state.players.delete(id);
      const idx = this.state.playerOrder.indexOf(id);
      if (idx >= 0) this.state.playerOrder.splice(idx, 1);
    }
    if (ids.length) this.syncPlayerCount();
  }

  private nextSlot(): number {
    const used = new Set<number>();
    this.state.players.forEach((p) => {
      if (!p.isHost && p.slot >= 0) used.add(p.slot);
    });
    for (let i = 0; i < MAX_PLAYERS; i++) if (!used.has(i)) return i;
    return 0;
  }

  private eachPlayer(fn: (p: PlayerState, id: string) => void) {
    this.state.players.forEach((p, id) => {
      if (!p.isHost) fn(p, id);
    });
  }

  private getCharges(p: PlayerState): Record<PowerUpId, number> {
    try {
      return JSON.parse(p.powerUpsJson) as Record<PowerUpId, number>;
    } catch {
      return initialPowerUpCharges(this.decadesPool());
    }
  }

  private setCharges(p: PlayerState, c: Record<PowerUpId, number>) {
    p.powerUpsJson = JSON.stringify(c);
  }

  private clearPhaseTimer() {
    if (this.phaseTimer) {
      clearTimeout(this.phaseTimer);
      this.phaseTimer = null;
    }
  }

  private schedule(ms: number, fn: () => void) {
    this.clearPhaseTimer();
    this.state.phaseEndsAt = Date.now() + ms;
    this.phaseTimer = setTimeout(fn, ms);
  }

  private leaderScore(): number {
    let max = 0;
    this.eachPlayer((pl) => {
      if (pl.score > max) max = pl.score;
    });
    return max;
  }

  private refreshDoublePlayFlags() {
    const lead = this.leaderScore();
    this.eachPlayer((pl) => {
      pl.doublePlayAvailable = lead > 0 && pl.score < lead * (1 - DOUBLE_PLAY_GAP_RATIO);
    });
  }

  private clampSelfie(dataUrl: string): string {
    const s = String(dataUrl || "");
    if (!s.startsWith("data:image/")) return "";
    // ~150KB binary ≈ 200k base64 chars; also hard-cap string length
    if (s.length > SELFIE_MAX_BYTES * 1.4) return "";
    return s.slice(0, Math.floor(SELFIE_MAX_BYTES * 1.37));
  }

  private handleMessage(client: Client, msg: ClientMessage) {
    const p = this.state.players.get(client.sessionId);
    if (!p) return;

    switch (msg.type) {
      case "set_name": {
        if (this.state.phase !== "lobby") return;
        p.name = String(msg.name || "").trim().slice(0, 16) || p.name;
        break;
      }
      case "set_selfie": {
        if (p.isHost) return;
        if (this.state.phase !== "lobby" && this.state.phase !== "countdown") return;
        const clipped = this.clampSelfie(String(msg.dataUrl || ""));
        if (!clipped) {
          client.send("error", { message: "Selfie trop lourd ou invalide (JPEG ≤150 Ko)" });
          return;
        }
        p.selfieDataUrl = clipped;
        this.broadcast("toast", { message: `${p.name} a posé son selfie !` });
        break;
      }
      case "set_character": {
        if (p.isHost) return;
        if (this.state.phase !== "lobby" && this.state.phase !== "countdown") return;
        const cid = String(msg.characterId || "");
        if (!isCharacterId(cid)) {
          client.send("error", { message: "Personnage invalide" });
          return;
        }
        p.characterId = cid;
        const cast = CAST_CHARACTERS.find((c) => c.id === cid);
        if (cast) p.color = cast.color;
        this.broadcast("toast", { message: `${p.name} choisit son personnage !` });
        break;
      }
      case "set_match_options": {
        if (!p.isHost || this.state.phase !== "lobby") return;
        this.applyMatchOptions(msg.mode, msg.powerPlaysEnabled);
        break;
      }
      case "start_match": {
        if (!p.isHost) {
          client.send("error", { message: "Seul le présentateur peut démarrer" });
          return;
        }
        if (this.countHumanPlayers() < MIN_PLAYERS) {
          client.send("error", {
            message: `Il faut au moins ${MIN_PLAYERS} joueur téléphone`,
          });
          return;
        }
        this.applyMatchOptions(msg.mode, msg.powerPlaysEnabled);
        // Solo phone → fill with a bot opponent so the show is 1v1.
        this.ensureBotOpponents(2);
        const mode = this.modeDef();
        this.state.roundIndex = 0;
        this.state.battleIndex = 0;
        this.state.totalRounds = mode.mcqCount;
        this.state.segment = "battle1";
        this.usedQuestionIds.clear();
        this.eachPlayer((pl) => {
          pl.powerUpsJson = JSON.stringify(initialPowerUpCharges(mode.decadesPowerPlays));
        });
        this.beginCountdown();
        break;
      }
      case "vote_theme": {
        if (this.state.phase !== "theme_vote" || p.isHost) return;
        const id = String(msg.themeId || "");
        if (!this.themeOptions.some((t) => t.id === id)) return;
        p.themeVote = id;
        break;
      }
      case "submit_answer": {
        this.onSubmitAnswer(client.sessionId, p, String(msg.value ?? ""));
        break;
      }
      case "use_powerup": {
        this.onUsePowerUp(client, p, msg.powerUpId, msg.targetSessionId, msg.secondPowerUpId);
        break;
      }
      case "clear_hazard": {
        if (p.hazard) {
          p.hazard = "";
          if (p.frozen) p.frozen = false;
        }
        break;
      }
      case "linking_submit": {
        this.onLinkingSubmit(client.sessionId, p, msg.links ?? []);
        break;
      }
      case "sorting_submit": {
        this.onSortingSubmit(client.sessionId, p, msg.itemId, msg.side);
        break;
      }
      case "chain_submit": {
        this.onChainSubmit(client.sessionId, p, msg.answers ?? []);
        break;
      }
      case "fill_blanks_submit": {
        this.onFillSubmit(client.sessionId, p, msg.answers ?? {});
        break;
      }
      case "odd_one_out_submit": {
        this.onOddSubmit(client.sessionId, p, String(msg.itemId ?? ""));
        break;
      }
      case "spin_to_win_submit": {
        this.onSpinSubmit(client.sessionId, p, String(msg.value ?? ""));
        break;
      }
      case "pyramid_answer": {
        this.onPyramidAnswer(client.sessionId, p, String(msg.value ?? ""));
        break;
      }
      case "request_snapshot": {
        client.send("snapshot", this.buildSnapshot());
        break;
      }
      case "skip_phase": {
        if (!p.isHost) return;
        this.advancePhase();
        break;
      }
      case "play_again": {
        if (!p.isHost) return;
        this.resetLobby();
        break;
      }
      default:
        break;
    }
  }

  private applyMatchOptions(mode?: MatchModeId, powerPlaysEnabled?: boolean) {
    if (mode && MATCH_MODES[mode]) {
      this.state.matchMode = mode;
      this.state.totalRounds = MATCH_MODES[mode].mcqCount;
    }
    if (typeof powerPlaysEnabled === "boolean") {
      this.state.powerPlaysEnabled = powerPlaysEnabled;
    }
  }

  private resetLobby() {
    this.clearPhaseTimer();
    this.removeBots();
    this.state.phase = "lobby";
    this.setHostLine("lobby");
    this.state.roundIndex = 0;
    this.state.battleIndex = 0;
    this.state.podiumJson = "[]";
    this.state.lastRevealJson = "{}";
    this.state.themeOptionsJson = "[]";
    this.state.selectedThemeId = "";
    this.state.selectedThemeLabel = "";
    this.state.challengeJson = "{}";
    this.state.throwVfxJson = "";
    this.state.scrollFact = "";
    this.state.segment = "lobby";
    this.state.totalRounds = this.modeDef().mcqCount;
    this.usedQuestionIds.clear();
    this.lastCategory = "";
    this.challengeDone.clear();
    this.eachPlayer((pl) => {
      pl.score = 0;
      pl.anim = "idle";
      pl.frozen = false;
      pl.hazard = "";
      pl.lockedAnswerIndex = -1;
      pl.pyramidStep = 0;
      pl.linkingScore = 0;
      pl.sortingScore = 0;
      pl.linkingDone = false;
      pl.powerUpsJson = JSON.stringify(initialPowerUpCharges(this.decadesPool()));
    });
  }

  private beginCountdown() {
    this.state.phase = "countdown";
    this.state.segment = "battle1";
    this.setHostLine("countdown");
    this.broadcast("toast", {
      message: `Mode ${this.modeDef().label} — ça commence !`,
    });
    this.schedule(3000, () => this.beginThemeVote());
  }

  /** After each MCQ reveal, insert challenge or next theme / pyramid. */
  private afterMcqReveal() {
    const completed = this.state.roundIndex + 1; // 1-based count finished
    const mode = this.modeDef();
    const challenge = mode.challengesAfterMcq[completed];

    if (challenge) {
      this.state.roundIndex = completed;
      this.beginChallenge(challenge);
      return;
    }

    if (completed >= mode.mcqCount) {
      this.state.roundIndex = mode.mcqCount;
      this.beginPyramid();
      return;
    }

    this.state.roundIndex = completed;
    this.state.battleIndex = Math.floor(completed / 3);
    this.state.segment = `battle${this.state.battleIndex + 1}`;
    this.beginThemeVote();
  }

  private resumeAfterChallenge() {
    const completed = this.state.roundIndex;
    const mode = this.modeDef();
    if (completed >= mode.mcqCount) {
      this.beginPyramid();
      return;
    }
    this.state.battleIndex = Math.floor(completed / 3);
    this.state.segment = `battle${this.state.battleIndex + 1}`;
    this.schedule(2200, () => this.beginThemeVote());
  }

  private beginChallenge(kind: ChallengeKind) {
    switch (kind) {
      case "linking":
        this.beginLinking();
        break;
      case "sorting":
        this.beginSorting();
        break;
      case "chain_reaction":
        this.beginChain();
        break;
      case "fill_blanks":
        this.beginFill();
        break;
      case "odd_one_out":
        this.beginOdd();
        break;
      case "spin_to_win":
        this.beginSpin();
        break;
      default:
        this.resumeAfterChallenge();
    }
  }

  private beginThemeVote() {
    this.themeOptions = pickThemeOptions(4, this.lastCategory || undefined);
    this.state.themeOptionsJson = JSON.stringify(this.themeOptions);
    this.state.selectedThemeId = "";
    this.state.selectedThemeLabel = "";
    this.state.phase = "theme_vote";
    this.setHostLine("theme_vote");
    this.eachPlayer((pl) => {
      pl.themeVote = "";
      pl.anim = "idle";
      pl.hasAnswered = false;
      pl.lockedAnswerIndex = -1;
      pl.hazard = "";
      pl.frozen = false;
      pl.betTargetId = "";
      pl.doubleNext = false;
    });
    this.broadcast("toast", { message: "Choisissez une porte de thème !" });
    this.schedule(THEME_VOTE_SEC * 1000, () => this.resolveThemeVote());
    this.scheduleBotActions();
  }

  private resolveThemeVote() {
    if (this.state.phase !== "theme_vote") return;
    this.clearPhaseTimer();
    const tallies = new Map<string, number>();
    for (const t of this.themeOptions) tallies.set(t.id, 0);
    this.eachPlayer((pl) => {
      if (!pl.themeVote) return;
      tallies.set(pl.themeVote, (tallies.get(pl.themeVote) ?? 0) + 1);
    });
    let bestScore = -1;
    const tied: ThemeOption[] = [];
    for (const t of this.themeOptions) {
      const n = tallies.get(t.id) ?? 0;
      if (n > bestScore) {
        bestScore = n;
        tied.length = 0;
        tied.push(t);
      } else if (n === bestScore) tied.push(t);
    }
    const best = tied[Math.floor(Math.random() * tied.length)] ?? this.themeOptions[0]!;
    this.state.selectedThemeId = best.id;
    this.state.selectedThemeLabel = best.label;
    this.lastCategory = best.label;
    this.state.phase = "theme_reveal";
    this.setHostLine("theme_reveal", { category: best.label });
    this.broadcast("toast", { message: "La roulette des portes…" });
    this.broadcast("vfx", { kind: "theme_roulette" });
    this.schedule(THEME_REVEAL_SEC * 1000, () => this.afterThemeReveal());
  }

  private afterThemeReveal() {
    if (this.state.powerPlaysEnabled) this.beginPowerPlay();
    else this.beginQuestion(this.state.selectedThemeLabel || this.lastCategory);
  }

  private beginPowerPlay() {
    if (!this.state.powerPlaysEnabled) {
      this.beginQuestion(this.state.selectedThemeLabel || this.lastCategory);
      return;
    }
    this.refreshDoublePlayFlags();
    this.state.phase = "power_play";
    this.state.throwVfxJson = "";
    this.setHostLine("power_play");
    this.eachPlayer((pl) => {
      pl.anim = "idle";
    });
    this.broadcast("toast", {
      message: "Power Plays ! Choisis une cible puis lance Freeze, Gloop…",
    });
    // Ensure everyone has charges at window open
    this.eachPlayer((pl) => {
      const c = this.getCharges(pl);
      if (Object.values(c).every((n) => !n)) {
        this.setCharges(pl, initialPowerUpCharges(this.decadesPool()));
      }
    });
    this.schedule(POWER_PLAY_SEC * 1000, () =>
      this.beginQuestion(this.state.selectedThemeLabel || this.lastCategory)
    );
  }

  private beginQuestion(forcedCategory?: string) {
    const category = forcedCategory || this.state.selectedThemeLabel || this.lastCategory;
    const q = pickQuestionFromCategory(category, this.usedQuestionIds);
    if (!q) {
      this.beginPyramid();
      return;
    }
    this.usedQuestionIds.add(q.id);
    this.currentQuestion = q;
    this.answers.clear();
    this.answerAt.clear();
    this.state.phase = "question";
    this.setHostLine("question", { category: q.category });
    this.state.question.id = q.id;
    this.state.question.questionType = "qcm";
    this.state.question.prompt = q.prompt;
    this.state.question.choicesJson = JSON.stringify(q.choices ?? []);
    this.state.question.timeLimitSec = DEFAULT_QUESTION_TIME;
    this.state.question.points = q.points ?? 100;
    this.state.question.category = q.category ?? category;
    this.state.lastRevealJson = "{}";

    this.eachPlayer((pl) => {
      pl.hasAnswered = false;
      pl.lastCorrect = false;
      pl.lastPoints = 0;
      pl.lockedAnswerIndex = -1;
      pl.anim = "idle";
      if (pl.hazard === "freeze") pl.frozen = true;
    });

    this.questionStartedAt = Date.now();
    this.schedule(DEFAULT_QUESTION_TIME * 1000, () => this.beginVoteShow());
    this.scheduleBotActions();
  }

  private onSubmitAnswer(sessionId: string, p: PlayerState, value: string) {
    if (this.state.phase !== "question") return;
    if (p.isHost) return;
    if (p.frozen || p.hazard === "freeze") {
      this.clients.find((c) => c.sessionId === sessionId)?.send("error", {
        message: "Dégèle-toi d’abord !",
      });
      return;
    }
    if (p.hasAnswered) return;
    p.hasAnswered = true;
    p.lockedAnswerIndex = choiceIdToPadIndex(value);
    p.anim = "walk";
    this.answers.set(sessionId, value);
    this.answerAt.set(sessionId, Date.now());

    let allDone = true;
    this.eachPlayer((pl) => {
      if (!pl.frozen && !pl.hasAnswered && pl.connected) allDone = false;
    });
    if (allDone) this.beginVoteShow();
  }

  /** Beat 1: show locked pads / votes — no correct answer yet. */
  private beginVoteShow() {
    this.clearPhaseTimer();
    if (!this.currentQuestion) return;
    this.state.phase = "votes_show";
    this.setHostLine("votes_show");
    this.state.lastRevealJson = JSON.stringify({
      votesOnly: true,
      questionId: this.currentQuestion.id,
    });
    this.eachPlayer((pl) => {
      if (pl.hasAnswered) pl.anim = "walk";
    });
    this.broadcast("toast", { message: "Chacun sur son pad — suspense…" });
    this.schedule(VOTE_SHOW_SEC * 1000, () => this.beginReveal());
  }

  /** Beat 2: grade, highlight correct pad, scores. */
  private beginReveal() {
    this.clearPhaseTimer();
    if (!this.currentQuestion) return;
    this.state.phase = "reveal";
    this.setHostLine("reveal");

    const q = this.currentQuestion;
    const results: Record<string, { correct: boolean; points: number; answer: string }> = {};
    const bets: { betterId: string; targetId: string }[] = [];
    const windowMs = DEFAULT_QUESTION_TIME * 1000;

    this.eachPlayer((pl, id) => {
      if (pl.betTargetId) bets.push({ betterId: id, targetId: pl.betTargetId });
      const ans = this.answers.get(id) ?? "";
      const { correct } = gradeAnswer(q, ans);
      let pts = 0;
      if (correct) {
        const at = this.answerAt.get(id) ?? Date.now();
        const elapsed = Math.max(0, Math.min(windowMs, at - this.questionStartedAt));
        const speed = Math.max(0.4, 1 - elapsed / windowMs);
        pts = Math.round((q.points ?? 100) * speed);
        if (pl.doubleNext) {
          pts *= 2;
          pl.doubleNext = false;
        }
        pl.anim = "correct";
        pl.lastCorrect = true;
      } else {
        pl.anim = "wrong";
        pl.lastCorrect = false;
      }
      pl.lastPoints = pts;
      pl.score += pts;
      results[id] = { correct, points: pts, answer: ans };
      this.broadcast("vfx", {
        kind: correct ? "correct" : "wrong",
        targetSessionId: id,
      });
    });

    for (const b of bets) {
      const better = this.state.players.get(b.betterId);
      const targetRes = results[b.targetId];
      if (better && targetRes?.correct) {
        better.score += 40;
        better.lastPoints += 40;
        this.broadcast("toast", { message: `${better.name} gagne son pari ! +40` });
      }
    }

    const correctAnswer =
      q.choices?.find((c) => q.correctChoiceIds?.includes(c.id))?.label ?? "";

    this.state.lastRevealJson = JSON.stringify({
      correctAnswer,
      results,
      questionId: q.id,
      votesOnly: false,
    });

    this.schedule(REVEAL_SEC * 1000, () => this.afterMcqReveal());
  }

  /* ——— Linking ——— */
  private beginLinking() {
    this.state.segment = "linking";
    const pack =
      LINKING_PACKS.find((p) => !this.usedLinking.has(p.id)) ?? LINKING_PACKS[0]!;
    this.usedLinking.add(pack.id);
    this.linking = pack;
    this.linkingLinks.clear();
    this.challengeDone.clear();
    this.eachPlayer((pl) => {
      pl.linkingDone = false;
      pl.linkingScore = 0;
      pl.anim = "idle";
      pl.lockedAnswerIndex = -1;
    });
    const rights = pack.pairs.map((x) => x.right);
    for (let i = rights.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [rights[i], rights[j]] = [rights[j]!, rights[i]!];
    }
    this.state.challengeJson = JSON.stringify({
      kind: "linking",
      id: pack.id,
      title: pack.title,
      lefts: pack.pairs.map((x) => x.left),
      rights,
      timeLimitSec: pack.timeLimitSec,
    });
    this.state.phase = "linking";
    this.setHostLine("linking");
    this.broadcast("toast", { message: `Challenge : ${pack.title}` });
    this.schedule(pack.timeLimitSec * 1000, () => this.endLinking());
    this.scheduleBotActions();
  }

  private onLinkingSubmit(
    sessionId: string,
    p: PlayerState,
    links: { left: string; right: string }[]
  ) {
    if (this.state.phase !== "linking" || p.isHost || p.linkingDone) return;
    this.linkingLinks.set(sessionId, links);
    p.linkingDone = true;
    this.challengeDone.add(sessionId);
    if (!this.linking) return;
    const correctMap = new Map(this.linking.pairs.map((x) => [x.left, x.right]));
    let score = 0;
    for (const l of links) {
      if (correctMap.get(l.left) === l.right) score += 1;
    }
    p.linkingScore = score;
    const clearAll = score === this.linking.pairs.length;
    const speedBonus = Math.max(0, Math.round((this.state.phaseEndsAt - Date.now()) / 1000) * 2);
    const pts = score * 30 + (clearAll ? 50 + speedBonus : 0);
    p.score += pts;
    p.anim = clearAll ? "celebrate" : score > 0 ? "correct" : "wrong";
    this.clients
      .find((c) => c.sessionId === sessionId)
      ?.send("toast", {
        message: clearAll
          ? `Combo parfait ! +${pts}`
          : `${score}/${this.linking.pairs.length} paires · +${pts}`,
      });
    if (this.allChallengeDone()) this.endLinking();
  }

  private endLinking() {
    this.clearPhaseTimer();
    this.broadcast("toast", { message: "Linking terminé — quel spectacle !" });
    this.resumeAfterChallenge();
  }

  /* ——— Sorting ——— */
  private beginSorting() {
    this.state.segment = "sorting";
    const pack =
      SORTING_PACKS.find((p) => !this.usedSorting.has(p.id)) ?? SORTING_PACKS[0]!;
    this.usedSorting.add(pack.id);
    this.sorting = pack;
    this.sortingAnswers.clear();
    this.challengeDone.clear();
    this.eachPlayer((pl) => {
      pl.sortingScore = 0;
      pl.anim = "idle";
      pl.lockedAnswerIndex = -1;
    });
    this.state.challengeJson = JSON.stringify({
      kind: "sorting",
      id: pack.id,
      title: pack.title,
      leftLabel: pack.leftLabel,
      rightLabel: pack.rightLabel,
      items: pack.items.map((i) => ({ id: i.id, label: i.label })),
      timeLimitSec: pack.timeLimitSec,
    });
    this.state.phase = "sorting";
    this.setHostLine("sorting");
    this.broadcast("toast", { message: `Challenge : ${pack.title}` });
    this.schedule(pack.timeLimitSec * 1000, () => this.endSorting());
    this.scheduleBotActions();
  }

  private onSortingSubmit(
    sessionId: string,
    p: PlayerState,
    itemId: string,
    side: "L" | "R"
  ) {
    if (this.state.phase !== "sorting" || p.isHost || !this.sorting) return;
    let map = this.sortingAnswers.get(sessionId);
    if (!map) {
      map = {};
      this.sortingAnswers.set(sessionId, map);
    }
    if (map[itemId]) return;
    map[itemId] = side;
    const item = this.sorting.items.find((i) => i.id === itemId);
    if (item && item.side === side) {
      p.sortingScore += 1;
      // streak: every 3 correct in a row adds bonus (approx via score count)
      const streakBonus = p.sortingScore % 3 === 0 ? 20 : 0;
      p.score += 18 + streakBonus;
      if (streakBonus) {
        this.clients
          .find((c) => c.sessionId === sessionId)
          ?.send("toast", { message: `Streak ×${p.sortingScore} ! +${18 + streakBonus}` });
      }
    } else {
      p.score = Math.max(0, p.score - 5);
    }
    const need = this.sorting.items.length;
    if (Object.keys(map).length >= need) {
      this.challengeDone.add(sessionId);
      if (p.sortingScore === need) {
        p.score += 40;
        p.anim = "celebrate";
        this.clients
          .find((c) => c.sessionId === sessionId)
          ?.send("toast", { message: "Clear all Sorting ! +40" });
      }
    }
    if (this.allChallengeDone()) this.endSorting();
  }

  private endSorting() {
    this.clearPhaseTimer();
    this.broadcast("toast", { message: "Sorting terminé — scores en feu !" });
    this.resumeAfterChallenge();
  }

  /* ——— Chain Reaction ——— */
  private beginChain() {
    const packs = getChallengePacks().chain;
    const pack = packs.find((p) => !this.usedChain.has(p.id)) ?? packs[0]!;
    this.usedChain.add(pack.id);
    this.chainPack = pack;
    this.challengeDone.clear();
    this.state.segment = "chain_reaction";
    this.state.challengeJson = JSON.stringify({
      kind: "chain_reaction",
      id: pack.id,
      title: pack.title,
      start: pack.start,
      steps: pack.steps.map((s) => ({ prompt: s.prompt })),
      choices: pack.choices,
      timeLimitSec: pack.timeLimitSec,
    });
    this.state.phase = "chain_reaction";
    this.setHostLine("chain_reaction");
    this.broadcast("toast", { message: `Challenge : ${pack.title}` });
    this.schedule(pack.timeLimitSec * 1000, () => this.endGenericChallenge("Chaîne terminée !"));
    this.scheduleBotActions();
  }

  private onChainSubmit(sessionId: string, p: PlayerState, answers: string[]) {
    if (this.state.phase !== "chain_reaction" || p.isHost || !this.chainPack) return;
    if (this.challengeDone.has(sessionId)) return;
    this.challengeDone.add(sessionId);
    let score = 0;
    let streak = 0;
    let bestStreak = 0;
    this.chainPack.steps.forEach((step, i) => {
      if ((answers[i] || "").trim().toLowerCase() === step.answer.toLowerCase()) {
        score += 1;
        streak += 1;
        bestStreak = Math.max(bestStreak, streak);
      } else {
        streak = 0;
      }
    });
    const clearAll = score === this.chainPack.steps.length;
    const pts = score * 25 + bestStreak * 8 + (clearAll ? 60 : 0);
    p.score += pts;
    p.anim = clearAll ? "celebrate" : score > 0 ? "correct" : "wrong";
    this.clients
      .find((c) => c.sessionId === sessionId)
      ?.send("toast", {
        message: clearAll
          ? `Chaîne parfaite ! +${pts}`
          : `${score}/${this.chainPack.steps.length} · streak ${bestStreak} · +${pts}`,
      });
    if (this.allChallengeDone()) this.endGenericChallenge("Chaîne terminée !");
  }

  /* ——— Fill the Blanks ——— */
  private beginFill() {
    const packs = getChallengePacks().fill;
    const pack = packs.find((p) => !this.usedFill.has(p.id)) ?? packs[0]!;
    this.usedFill.add(pack.id);
    this.fillPack = pack;
    this.challengeDone.clear();
    this.state.segment = "fill_blanks";
    this.state.challengeJson = JSON.stringify({
      kind: "fill_blanks",
      id: pack.id,
      title: pack.title,
      sentence: pack.sentence,
      blanks: pack.blanks.map((b) => ({ id: b.id, options: b.options })),
      timeLimitSec: pack.timeLimitSec,
    });
    this.state.phase = "fill_blanks";
    this.setHostLine("fill_blanks");
    this.broadcast("toast", { message: `Challenge : ${pack.title}` });
    this.schedule(pack.timeLimitSec * 1000, () => this.endGenericChallenge("Trous remplis !"));
    this.scheduleBotActions();
  }

  private onFillSubmit(sessionId: string, p: PlayerState, answers: Record<string, string>) {
    if (this.state.phase !== "fill_blanks" || p.isHost || !this.fillPack) return;
    if (this.challengeDone.has(sessionId)) return;
    this.challengeDone.add(sessionId);
    let score = 0;
    for (const b of this.fillPack.blanks) {
      if ((answers[b.id] || "") === b.correct) score += 1;
    }
    const clearAll = score === this.fillPack.blanks.length;
    const pts = score * 35 + (clearAll ? 45 : 0);
    p.score += pts;
    p.anim = clearAll ? "celebrate" : score > 0 ? "correct" : "wrong";
    this.clients
      .find((c) => c.sessionId === sessionId)
      ?.send("toast", { message: clearAll ? `Blanks parfait ! +${pts}` : `+${pts}` });
    if (this.allChallengeDone()) this.endGenericChallenge("Trous remplis !");
  }

  /* ——— Odd One Out ——— */
  private beginOdd() {
    const packs = getChallengePacks().odd;
    const pack = packs.find((p) => !this.usedOdd.has(p.id)) ?? packs[0]!;
    this.usedOdd.add(pack.id);
    this.oddPack = pack;
    this.challengeDone.clear();
    this.state.segment = "odd_one_out";
    this.state.challengeJson = JSON.stringify({
      kind: "odd_one_out",
      id: pack.id,
      title: pack.title,
      prompt: pack.prompt,
      items: pack.items,
      timeLimitSec: pack.timeLimitSec,
    });
    this.state.phase = "odd_one_out";
    this.setHostLine("odd_one_out");
    this.broadcast("toast", { message: `Challenge : ${pack.title}` });
    this.schedule(pack.timeLimitSec * 1000, () => this.endGenericChallenge("Intrus révélé !"));
    this.scheduleBotActions();
  }

  private onOddSubmit(sessionId: string, p: PlayerState, itemId: string) {
    if (this.state.phase !== "odd_one_out" || p.isHost || !this.oddPack) return;
    if (this.challengeDone.has(sessionId)) return;
    this.challengeDone.add(sessionId);
    if (itemId === this.oddPack.oddId) {
      const speedBonus = Math.max(0, Math.round((this.state.phaseEndsAt - Date.now()) / 800));
      const pts = 70 + speedBonus;
      p.score += pts;
      p.anim = "celebrate";
      this.clients
        .find((c) => c.sessionId === sessionId)
        ?.send("toast", { message: `Intrus trouvé ! +${pts}` });
    } else {
      p.score = Math.max(0, p.score - 15);
      p.anim = "wrong";
      this.clients
        .find((c) => c.sessionId === sessionId)
        ?.send("toast", { message: "Raté… −15" });
    }
    if (this.allChallengeDone()) this.endGenericChallenge("Intrus révélé !");
  }

  /* ——— Spin to Win ——— */
  private beginSpin() {
    const packs = getChallengePacks().spin;
    const pack = packs.find((p) => !this.usedSpin.has(p.id)) ?? packs[0]!;
    this.usedSpin.add(pack.id);
    this.spinPack = pack;
    this.challengeDone.clear();
    this.state.segment = "spin_to_win";
    this.state.challengeJson = JSON.stringify({
      kind: "spin_to_win",
      id: pack.id,
      title: pack.title,
      prompt: pack.prompt,
      options: pack.options,
      timeLimitSec: pack.timeLimitSec,
    });
    this.state.phase = "spin_to_win";
    this.setHostLine("spin_to_win");
    this.broadcast("toast", { message: `Challenge : ${pack.title}` });
    this.schedule(pack.timeLimitSec * 1000, () => this.endGenericChallenge("Spin terminé !"));
    this.scheduleBotActions();
  }

  private onSpinSubmit(sessionId: string, p: PlayerState, value: string) {
    if (this.state.phase !== "spin_to_win" || p.isHost || !this.spinPack) return;
    if (this.challengeDone.has(sessionId)) return;
    this.challengeDone.add(sessionId);
    if (value === this.spinPack.correct) {
      const speedBonus = Math.max(0, Math.round((this.state.phaseEndsAt - Date.now()) / 700));
      const pts = 80 + speedBonus;
      p.score += pts;
      p.anim = "celebrate";
      this.clients
        .find((c) => c.sessionId === sessionId)
        ?.send("toast", { message: `Spin gagné ! +${pts}` });
    } else {
      p.score = Math.max(0, p.score - 10);
      p.anim = "wrong";
      this.clients
        .find((c) => c.sessionId === sessionId)
        ?.send("toast", { message: "Mauvaise case… −10" });
    }
    if (this.allChallengeDone()) this.endGenericChallenge("Spin terminé !");
  }

  private allChallengeDone(): boolean {
    let ok = true;
    this.eachPlayer((pl, id) => {
      if (pl.connected && !this.challengeDone.has(id)) ok = false;
    });
    return ok;
  }

  private endGenericChallenge(toast: string) {
    this.clearPhaseTimer();
    this.broadcast("toast", { message: toast });
    this.resumeAfterChallenge();
  }

  /* ——— Pyramid ——— */
  private beginPyramid() {
    this.state.segment = "pyramid";
    this.pyramidQIndex = 0;
    const ranked: { id: string; score: number }[] = [];
    this.eachPlayer((pl, id) => ranked.push({ id, score: pl.score }));
    ranked.sort((a, b) => b.score - a.score);
    ranked.forEach((r, i) => {
      const pl = this.state.players.get(r.id);
      if (pl) pl.pyramidStep = Math.max(0, 2 - i);
    });
    this.state.phase = "pyramid";
    this.setHostLine("pyramid");
    this.pushPyramidQuestion();
    this.broadcast("toast", { message: "Pyramide du Savoir !" });
  }

  private pushPyramidQuestion() {
    const q = PYRAMID_QUESTIONS[this.pyramidQIndex % PYRAMID_QUESTIONS.length]!;
    this.state.challengeJson = JSON.stringify({
      kind: "pyramid",
      question: q,
      stepsToWin: PYRAMID_STEPS_TO_WIN,
    });
    this.state.question.prompt = q.prompt;
    this.state.question.choicesJson = JSON.stringify(q.choices);
    this.state.question.id = q.id;
    this.schedule(120_000, () => {
      /* safety */
    });
    this.scheduleBotActions();
  }

  private onPyramidAnswer(sessionId: string, p: PlayerState, value: string) {
    if (this.state.phase !== "pyramid" || p.isHost) return;
    const q = PYRAMID_QUESTIONS[this.pyramidQIndex % PYRAMID_QUESTIONS.length]!;
    if (value !== q.correctChoiceId) {
      this.clients
        .find((c) => c.sessionId === sessionId)
        ?.send("toast", { message: "Raté — retente !" });
      return;
    }
    p.pyramidStep += 1;
    p.anim = "celebrate";
    p.score += 30;
    this.broadcast("vfx", { kind: "pyramid_step", targetSessionId: sessionId });
    if (p.pyramidStep >= PYRAMID_STEPS_TO_WIN) {
      this.finishPyramid(sessionId, p);
      return;
    }
    this.pyramidQIndex += 1;
    this.pushPyramidQuestion();
  }

  private finishPyramid(winnerId: string, winner: PlayerState) {
    this.clearPhaseTimer();
    const fact = SCROLL_FACTS[Math.floor(Math.random() * SCROLL_FACTS.length)]!;
    this.state.scrollFact = fact;
    this.state.challengeJson = JSON.stringify({
      kind: "scroll",
      winnerId,
      winnerName: winner.name,
      fact,
    });
    this.clients
      .find((c) => c.sessionId === winnerId)
      ?.send("toast", { message: `Parchemin du Savoir : ${fact}` });
    this.broadcast("toast", { message: `${winner.name} remporte la pyramide !` });
    this.schedule(4000, () => this.beginPodium());
  }

  private beginPodium() {
    this.clearPhaseTimer();
    this.state.phase = "podium";
    this.state.segment = "podium";
    const ranking: { sessionId: string; name: string; score: number; color: string }[] = [];
    this.eachPlayer((pl, id) => {
      ranking.push({ sessionId: id, name: pl.name, score: pl.score, color: pl.color });
      pl.anim = "celebrate";
    });
    ranking.sort((a, b) => b.score - a.score);
    this.state.podiumJson = JSON.stringify(ranking);
    this.setHostLine("podium", { name: ranking[0]?.name });
    this.broadcast("toast", { message: "Podium !" });
  }

  private advancePhase() {
    switch (this.state.phase) {
      case "countdown":
        this.beginThemeVote();
        break;
      case "theme_vote":
        this.resolveThemeVote();
        break;
      case "theme_reveal":
        this.afterThemeReveal();
        break;
      case "power_play":
        this.beginQuestion(this.state.selectedThemeLabel || this.lastCategory);
        break;
      case "question":
        this.beginVoteShow();
        break;
      case "votes_show":
        this.beginReveal();
        break;
      case "reveal":
        this.afterMcqReveal();
        break;
      case "linking":
        this.endLinking();
        break;
      case "sorting":
        this.endSorting();
        break;
      case "chain_reaction":
      case "fill_blanks":
      case "odd_one_out":
      case "spin_to_win":
        this.endGenericChallenge("Challenge passé");
        break;
      case "pyramid":
        break;
      case "scoreboard":
        this.beginPodium();
        break;
      default:
        break;
    }
  }

  private onUsePowerUp(
    client: Client,
    p: PlayerState,
    powerUpId: PowerUpId,
    targetSessionId?: string,
    secondPowerUpId?: PowerUpId
  ) {
    if (p.isHost) return;
    if (!this.state.powerPlaysEnabled) {
      client.send("error", { message: "Power Plays désactivés" });
      return;
    }
    if (this.state.phase !== "power_play") {
      client.send("error", { message: "Power Plays uniquement avant la question" });
      return;
    }
    const def = POWER_UPS[powerUpId];
    if (!def) {
      client.send("error", { message: "Power Play inconnu" });
      return;
    }
    if (def.decadesOnly && !this.decadesPool()) {
      client.send("error", { message: "Power Play réservé au mode Décades" });
      return;
    }
    let charges = this.getCharges(p);
    // Heal empty inventory (legacy rooms / bad sync)
    if (!charges[powerUpId] && Object.values(charges).every((n) => !n)) {
      charges = initialPowerUpCharges(this.decadesPool());
      this.setCharges(p, charges);
    }
    if ((charges[powerUpId] ?? 0) <= 0) {
      client.send("error", { message: "Plus de charges" });
      return;
    }

    // Auto-pick sole rival if sabotage and no target sent
    let target = targetSessionId;
    if (def.needsTarget && (!target || target === client.sessionId)) {
      const rivals: string[] = [];
      this.eachPlayer((_pl, id) => {
        if (id !== client.sessionId) rivals.push(id);
      });
      if (rivals.length === 1) target = rivals[0];
    }

    const applyOne = (id: PowerUpId, tgt?: string) => {
      const d = POWER_UPS[id];
      if (!d) return false;
      if (d.decadesOnly && !this.decadesPool()) return false;
      if ((charges[id] ?? 0) <= 0) return false;
      if (d.needsTarget) {
        if (!tgt || tgt === client.sessionId) {
          client.send("error", { message: "Choisis une cible d’abord" });
          return false;
        }
        const t = this.state.players.get(tgt);
        if (!t || t.isHost) {
          client.send("error", { message: "Cible invalide" });
          return false;
        }
      }
      charges[id] -= 1;
      this.applyPowerPlay(client.sessionId, p, id, tgt);
      return true;
    };

    const ok = applyOne(powerUpId, target);
    if (
      ok &&
      secondPowerUpId &&
      p.doublePlayAvailable &&
      SABOTAGE_IDS.includes(secondPowerUpId)
    ) {
      applyOne(secondPowerUpId, target);
      p.doublePlayAvailable = false;
    }
    this.setCharges(p, charges);
    if (ok) p.anim = "cast";
  }

  private applyPowerPlay(
    fromId: string,
    from: PlayerState,
    id: PowerUpId,
    targetId?: string
  ) {
    this.state.throwVfxJson = JSON.stringify({
      kind: id,
      fromSessionId: fromId,
      targetSessionId: targetId ?? fromId,
      at: Date.now(),
    });
    this.broadcast("vfx", {
      kind: "power_throw",
      powerUpId: id,
      fromSessionId: fromId,
      targetSessionId: targetId,
    });

    if (id === "points_party") {
      from.doubleNext = true;
      this.broadcast("toast", { message: `${from.name} active Points Party !` });
      return;
    }
    if (id === "bet" && targetId) {
      from.betTargetId = targetId;
      const t = this.state.players.get(targetId);
      this.broadcast("toast", {
        message: `${from.name} parie sur ${t?.name ?? "…"}`,
      });
      return;
    }
    if (!targetId) return;
    const target = this.state.players.get(targetId);
    if (!target) return;
    if (target.hasShield) {
      target.hasShield = false;
      this.broadcast("toast", { message: `${target.name} bloque avec un bouclier !` });
      return;
    }
    target.hazard = id;
    if (id === "freeze") target.frozen = true;
    this.broadcast("toast", {
      message: `${from.name} lance ${POWER_UPS[id].name} sur ${target.name} !`,
    });
  }
}

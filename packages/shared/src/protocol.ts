import type { PowerUpId } from "./powerups.js";
import type { MatchModeId } from "./modes.js";
import type { CharacterId } from "./characters.js";

export type ClientMessage =
  | { type: "set_name"; name: string }
  | { type: "set_role"; role: "host" | "player" }
  | { type: "set_selfie"; dataUrl: string }
  | { type: "set_character"; characterId: CharacterId }
  | {
      type: "set_match_options";
      mode?: MatchModeId;
      powerPlaysEnabled?: boolean;
    }
  | { type: "start_match"; rounds?: number; mode?: MatchModeId; powerPlaysEnabled?: boolean }
  | { type: "vote_theme"; themeId: string }
  | { type: "submit_answer"; value: string }
  | {
      type: "use_powerup";
      powerUpId: PowerUpId;
      targetSessionId?: string;
      secondPowerUpId?: PowerUpId;
    }
  | { type: "clear_hazard" }
  | { type: "linking_submit"; links: { left: string; right: string }[] }
  | { type: "sorting_submit"; itemId: string; side: "L" | "R" }
  | { type: "chain_submit"; answers: string[] }
  | { type: "fill_blanks_submit"; answers: Record<string, string> }
  | { type: "odd_one_out_submit"; itemId: string }
  | { type: "spin_to_win_submit"; value: string }
  | { type: "pyramid_answer"; value: string }
  | { type: "request_snapshot" }
  | { type: "skip_phase" }
  | { type: "play_again" };

export type ServerToast = { message: string };
export type ServerError = { message: string };
export type ServerVfx = {
  kind: string;
  targetSessionId?: string;
  fromSessionId?: string;
  powerUpId?: string;
};

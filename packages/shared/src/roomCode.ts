const ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

export function generateRoomCode(length = 4): string {
  let out = "";
  for (let i = 0; i < length; i++) {
    out += ALPHABET[Math.floor(Math.random() * ALPHABET.length)]!;
  }
  return out;
}

export function normalizeRoomCode(code: string): string {
  return code.trim().toUpperCase().replace(/[^A-Z0-9]/g, "");
}

export function playerJoinPath(roomCode: string): string {
  return `/p/${normalizeRoomCode(roomCode)}`;
}

export function hostPath(roomCode: string): string {
  return `/host/${normalizeRoomCode(roomCode)}`;
}

export function absoluteJoinUrl(publicWebUrl: string, roomCode: string): string {
  const base = publicWebUrl.replace(/\/$/, "");
  return `${base}${playerJoinPath(roomCode)}`;
}

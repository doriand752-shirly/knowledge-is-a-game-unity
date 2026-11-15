/** Quirky claymation-inspired cast (original names — not Sony IP). */

export type CharacterId =
  | "ketchup"
  | "glitter"
  | "scintille"
  | "yeti"
  | "bonnie"
  | "miette"
  | "magus"
  | "bizzarro"
  | "volcan"
  | "toast";

export interface CastCharacter {
  id: CharacterId;
  name: string;
  tagline: string;
  /** Primary costume color */
  color: string;
  accent: string;
  /** SVG body asset under /cast/ */
  bodySrc: string;
  silhouette: "hotdog" | "duchess" | "fairy" | "yeti" | "cowgirl" | "astro" | "mage" | "doc" | "chef" | "robot";
}

export const CAST_CHARACTERS: CastCharacter[] = [
  {
    id: "ketchup",
    name: "Capitaine Ketchup",
    tagline: "Hot-dog héroïque",
    color: "#E63946",
    accent: "#F4C430",
    bodySrc: "/cast/ketchup.svg",
    silhouette: "hotdog",
  },
  {
    id: "glitter",
    name: "Duchesse Glitter",
    tagline: "Éclat royal",
    color: "#9B5DE5",
    accent: "#F4C430",
    bodySrc: "/cast/glitter.svg",
    silhouette: "duchess",
  },
  {
    id: "scintille",
    name: "Fée Scintille",
    tagline: "Poussière d’étoiles",
    color: "#5EC8E8",
    accent: "#FFB4A2",
    bodySrc: "/cast/scintille.svg",
    silhouette: "fairy",
  },
  {
    id: "yeti",
    name: "Yéti Spa",
    tagline: "Zen glacial",
    color: "#A8DADC",
    accent: "#1D3557",
    bodySrc: "/cast/yeti.svg",
    silhouette: "yeti",
  },
  {
    id: "bonnie",
    name: "Bonnie Laser",
    tagline: "Cowgirl turbo",
    color: "#F4A261",
    accent: "#E63946",
    bodySrc: "/cast/bonnie.svg",
    silhouette: "cowgirl",
  },
  {
    id: "miette",
    name: "Major Miette",
    tagline: "Astro friandise",
    color: "#457B9D",
    accent: "#F4C430",
    bodySrc: "/cast/miette.svg",
    silhouette: "astro",
  },
  {
    id: "magus",
    name: "Magus Oops",
    tagline: "Sorts qui… oops",
    color: "#6A4C93",
    accent: "#2EC4B6",
    bodySrc: "/cast/magus.svg",
    silhouette: "mage",
  },
  {
    id: "bizzarro",
    name: "Dr Bizzarro",
    tagline: "Science loufoque",
    color: "#2A9D8F",
    accent: "#E9C46A",
    bodySrc: "/cast/bizzarro.svg",
    silhouette: "doc",
  },
  {
    id: "volcan",
    name: "Chef Volcan",
    tagline: "Cuisine explosive",
    color: "#E76F51",
    accent: "#264653",
    bodySrc: "/cast/volcan.svg",
    silhouette: "chef",
  },
  {
    id: "toast",
    name: "T0AST-3000",
    tagline: "Robot grillé",
    color: "#ADB5BD",
    accent: "#E63946",
    bodySrc: "/cast/toast.svg",
    silhouette: "robot",
  },
];

export const CAST_BY_ID = Object.fromEntries(
  CAST_CHARACTERS.map((c) => [c.id, c])
) as Record<CharacterId, CastCharacter>;

export function isCharacterId(id: string): id is CharacterId {
  return id in CAST_BY_ID;
}

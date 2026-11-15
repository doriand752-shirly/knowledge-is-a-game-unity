/** Linking / Sorting / Pyramid content shapes — denser packs for real challenge. */

export interface LinkingPair {
  left: string;
  right: string;
}

export interface LinkingChallenge {
  id: string;
  title: string;
  pairs: LinkingPair[];
  timeLimitSec: number;
}

export interface SortingItem {
  id: string;
  label: string;
  /** "L" or "R" */
  side: "L" | "R";
}

export interface SortingChallenge {
  id: string;
  title: string;
  leftLabel: string;
  rightLabel: string;
  items: SortingItem[];
  timeLimitSec: number;
}

export interface PyramidQuestion {
  id: string;
  prompt: string;
  choices: { id: string; label: string }[];
  correctChoiceId: string;
}

export const LINKING_PACKS: LinkingChallenge[] = [
  {
    id: "link-capitals",
    title: "Capitales express",
    timeLimitSec: 32,
    pairs: [
      { left: "France", right: "Paris" },
      { left: "Japon", right: "Tokyo" },
      { left: "Égypte", right: "Le Caire" },
      { left: "Canada", right: "Ottawa" },
      { left: "Brésil", right: "Brasília" },
      { left: "Australie", right: "Canberra" },
      { left: "Pérou", right: "Lima" },
      { left: "Kenya", right: "Nairobi" },
    ],
  },
  {
    id: "link-animals",
    title: "Animaux & sons",
    timeLimitSec: 30,
    pairs: [
      { left: "Chien", right: "Aboiement" },
      { left: "Chat", right: "Miaulement" },
      { left: "Vache", right: "Meuglement" },
      { left: "Coq", right: "Chant du coq" },
      { left: "Lion", right: "Rugissement" },
      { left: "Loup", right: "Hurlement" },
      { left: "Abeille", right: "Bourdonnement" },
      { left: "Grenouille", right: "Coassement" },
    ],
  },
  {
    id: "link-sports",
    title: "Sports en feu",
    timeLimitSec: 28,
    pairs: [
      { left: "Football", right: "Ballon rond" },
      { left: "Tennis", right: "Raquette" },
      { left: "Natation", right: "Bassins" },
      { left: "Basket", right: "Panier" },
      { left: "Rugby", right: "Essai" },
      { left: "Escrime", right: "Fleuret" },
      { left: "Boxe", right: "Ring" },
      { left: "Golf", right: "Par" },
    ],
  },
  {
    id: "link-pop",
    title: "Pop culture",
    timeLimitSec: 30,
    pairs: [
      { left: "Harry Potter", right: "Poudlard" },
      { left: "Luke", right: "Sabre laser" },
      { left: "Mario", right: "Champignon" },
      { left: "Sherlock", right: "Watson" },
      { left: "Batman", right: "Gotham" },
      { left: "Hermione", right: "Baguette" },
      { left: "Zelda", right: "Triforce" },
      { left: "Tintin", right: "Milou" },
    ],
  },
];

export const SORTING_PACKS: SortingChallenge[] = [
  {
    id: "sort-fruit-veg",
    title: "Fruit ou légume ?",
    leftLabel: "Fruit",
    rightLabel: "Légume",
    timeLimitSec: 26,
    items: [
      { id: "1", label: "Pomme", side: "L" },
      { id: "2", label: "Carotte", side: "R" },
      { id: "3", label: "Banane", side: "L" },
      { id: "4", label: "Brocoli", side: "R" },
      { id: "5", label: "Tomate", side: "L" },
      { id: "6", label: "Poireau", side: "R" },
      { id: "7", label: "Fraise", side: "L" },
      { id: "8", label: "Courgette", side: "R" },
      { id: "9", label: "Avocat", side: "L" },
      { id: "10", label: "Épinard", side: "R" },
      { id: "11", label: "Mangue", side: "L" },
      { id: "12", label: "Navet", side: "R" },
    ],
  },
  {
    id: "sort-land-sea",
    title: "Terre ou mer ?",
    leftLabel: "Terre",
    rightLabel: "Mer",
    timeLimitSec: 26,
    items: [
      { id: "1", label: "Lion", side: "L" },
      { id: "2", label: "Dauphin", side: "R" },
      { id: "3", label: "Éléphant", side: "L" },
      { id: "4", label: "Requin", side: "R" },
      { id: "5", label: "Aigle", side: "L" },
      { id: "6", label: "Méduse", side: "R" },
      { id: "7", label: "Renard", side: "L" },
      { id: "8", label: "Pieuvre", side: "R" },
      { id: "9", label: "Panda", side: "L" },
      { id: "10", label: "Orque", side: "R" },
      { id: "11", label: "Kangourou", side: "L" },
      { id: "12", label: "Hippocampe", side: "R" },
    ],
  },
  {
    id: "sort-decade",
    title: "Avant / Après 2000 ?",
    leftLabel: "Avant 2000",
    rightLabel: "Après 2000",
    timeLimitSec: 24,
    items: [
      { id: "1", label: "Walkman cassette", side: "L" },
      { id: "2", label: "iPhone", side: "R" },
      { id: "3", label: "Game Boy", side: "L" },
      { id: "4", label: "TikTok", side: "R" },
      { id: "5", label: "Disquette", side: "L" },
      { id: "6", label: "Bluetooth écouteurs", side: "R" },
      { id: "7", label: "Minitel", side: "L" },
      { id: "8", label: "YouTube", side: "R" },
      { id: "9", label: "Tamagotchi", side: "L" },
      { id: "10", label: "Zoom calls", side: "R" },
    ],
  },
];

export const PYRAMID_QUESTIONS: PyramidQuestion[] = [
  {
    id: "pyr1",
    prompt: "Combien de côtés a un triangle ?",
    choices: [
      { id: "a", label: "2" },
      { id: "b", label: "3" },
      { id: "c", label: "4" },
      { id: "d", label: "5" },
    ],
    correctChoiceId: "b",
  },
  {
    id: "pyr2",
    prompt: "Couleur du sang oxygéné chez l’humain ?",
    choices: [
      { id: "a", label: "Bleu" },
      { id: "b", label: "Vert" },
      { id: "c", label: "Rouge" },
      { id: "d", label: "Jaune" },
    ],
    correctChoiceId: "c",
  },
  {
    id: "pyr3",
    prompt: "Planète la plus proche du Soleil ?",
    choices: [
      { id: "a", label: "Vénus" },
      { id: "b", label: "Mercure" },
      { id: "c", label: "Mars" },
      { id: "d", label: "Terre" },
    ],
    correctChoiceId: "b",
  },
  {
    id: "pyr4",
    prompt: "Auteur de « Les Misérables » ?",
    choices: [
      { id: "a", label: "Zola" },
      { id: "b", label: "Hugo" },
      { id: "c", label: "Balzac" },
      { id: "d", label: "Flaubert" },
    ],
    correctChoiceId: "b",
  },
  {
    id: "pyr5",
    prompt: "Combien de minutes dans une heure ?",
    choices: [
      { id: "a", label: "30" },
      { id: "b", label: "45" },
      { id: "c", label: "60" },
      { id: "d", label: "100" },
    ],
    correctChoiceId: "c",
  },
  {
    id: "pyr6",
    prompt: "Océan le plus grand ?",
    choices: [
      { id: "a", label: "Atlantique" },
      { id: "b", label: "Indien" },
      { id: "c", label: "Pacifique" },
      { id: "d", label: "Arctique" },
    ],
    correctChoiceId: "c",
  },
  {
    id: "pyr7",
    prompt: "Élément chimique H ?",
    choices: [
      { id: "a", label: "Hélium" },
      { id: "b", label: "Hydrogène" },
      { id: "c", label: "Hafnium" },
      { id: "d", label: "Holmium" },
    ],
    correctChoiceId: "b",
  },
  {
    id: "pyr8",
    prompt: "Capitale de l’Espagne ?",
    choices: [
      { id: "a", label: "Barcelone" },
      { id: "b", label: "Madrid" },
      { id: "c", label: "Séville" },
      { id: "d", label: "Valence" },
    ],
    correctChoiceId: "b",
  },
];

export const SCROLL_FACTS = [
  "Le miel ne se périme quasiment jamais — on en a trouvé dans des tombes égyptiennes encore comestible.",
  "Les pieuvres ont trois cœurs et du sang bleu.",
  "La Tour Eiffel grandit d’environ 15 cm en été à cause de la chaleur.",
  "Un jour sur Vénus dure plus longtemps qu’une année sur Vénus.",
  "Les bananes sont techniquement des baies ; les fraises non.",
];

export const MCQ_PER_BATTLE = 3;
export const BATTLE_COUNT = 3;
export const TOTAL_MCQ = MCQ_PER_BATTLE * BATTLE_COUNT; // 9
export const PYRAMID_STEPS_TO_WIN = 8;
export const POWER_PLAY_SEC = 18;
export const LINKING_DEFAULT_SEC = 35;
export const SORTING_DEFAULT_SEC = 28;

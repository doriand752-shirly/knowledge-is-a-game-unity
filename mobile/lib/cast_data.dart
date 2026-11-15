class CastCharacter {
  const CastCharacter({
    required this.id,
    required this.name,
    required this.tagline,
    required this.color,
    required this.asset,
  });

  final String id;
  final String name;
  final String tagline;
  final int color;
  final String asset;
}

const castCharacters = <CastCharacter>[
  CastCharacter(id: 'ketchup', name: 'Capitaine Ketchup', tagline: 'Hot-dog héroïque', color: 0xFFE63946, asset: 'assets/cast/ketchup.svg'),
  CastCharacter(id: 'glitter', name: 'Duchesse Glitter', tagline: 'Éclat royal', color: 0xFF9B5DE5, asset: 'assets/cast/glitter.svg'),
  CastCharacter(id: 'scintille', name: 'Fée Scintille', tagline: 'Poussière d’étoiles', color: 0xFF5EC8E8, asset: 'assets/cast/scintille.svg'),
  CastCharacter(id: 'yeti', name: 'Yéti Spa', tagline: 'Zen glacial', color: 0xFFA8DADC, asset: 'assets/cast/yeti.svg'),
  CastCharacter(id: 'bonnie', name: 'Bonnie Laser', tagline: 'Cowgirl turbo', color: 0xFFF4A261, asset: 'assets/cast/bonnie.svg'),
  CastCharacter(id: 'miette', name: 'Major Miette', tagline: 'Astro friandise', color: 0xFF457B9D, asset: 'assets/cast/miette.svg'),
  CastCharacter(id: 'magus', name: 'Magus Oops', tagline: 'Sorts qui… oops', color: 0xFF6A4C93, asset: 'assets/cast/magus.svg'),
  CastCharacter(id: 'bizzarro', name: 'Dr Bizzarro', tagline: 'Science loufoque', color: 0xFF2A9D8F, asset: 'assets/cast/bizzarro.svg'),
  CastCharacter(id: 'volcan', name: 'Chef Volcan', tagline: 'Cuisine explosive', color: 0xFFE76F51, asset: 'assets/cast/volcan.svg'),
  CastCharacter(id: 'toast', name: 'T0AST-3000', tagline: 'Robot grillé', color: 0xFFADB5BD, asset: 'assets/cast/toast.svg'),
];

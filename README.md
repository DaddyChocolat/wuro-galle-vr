# Wuro & Galle : L'Espace Peul entre Campement et Territoire

Projet VR — ENSPM Université de Maroua, Département des Arts et Humanités Numériques.
Réalisé par Eddy (promotion 2022-2027).

## Concept

Les Fulɓe (Peuls) de l'Extrême-Nord Cameroun vivent selon un spectre de modes de vie qui va du nomadisme pastoral pur à la sédentarité complète, avec de nombreuses situations intermédiaires et des passages possibles de l'un à l'autre selon les générations et les aléas (taille du troupeau, opportunités économiques). Ce projet met en regard les deux pôles de ce spectre à travers une immersion VR en deux temps :

- **Wuro** : une journée dans un campement de bergers nomades, de l'aube au départ en transhumance — traite des zébus, démontage de la suudu, marche vers de nouveaux pâturages.
- **Galle** : l'exploration d'une concession familiale semi-sédentaire en banco — cases d'habitation, dudal, greniers à mil — qui garde malgré sa fixité un lien économique et symbolique fort avec l'élevage.

L'objectif n'est pas de figer ces deux formes d'habitat comme des catégories opposées, mais de rendre sensible, par l'expérience VR, la logique commune qui les traverse : une organisation de l'espace pensée autour du troupeau, de l'accueil de l'étranger et de la séparation entre espace public et espace familial.

## Structure du dépôt

```
blender/   fichiers .blend, exports GLTF 2.0 par module
unity/     projet Unity 2022.3 LTS (scènes Campement + Concession)
docs/      Livrable_1/ (dossier documentaire + bibliothèque Zotero)
           Livrable_2/ (modélisation 3D)
           Livrable_3/ (intégration Unity, à constituer)
           Livrable_4/ (narration, tests, rapport réflexif, à constituer)
audio/     sons ambiants, voix-off, musique
```

## Contraintes techniques

- Poste local : Core i5 5e gén, 20 Go RAM, sans GPU dédié
- Modélisation Blender en local ; intégration Unity lourde (lightmap baking, VFX) sur VM GPU louée ponctuellement
- Budget triangles : max 20 000/module, 80-120K/scène chargée
- Textures : 1-2K, atlas
- Voir `docs/scope-reduit.md` pour le détail du périmètre retenu.

## Note éthique

Ce projet représente une culture vivante (Peuls Fulɓe). Engagement : pas de stéréotype ni d'exotisme,
respect de la semteende (pudeur/réserve), sources documentaires citées, distinction claire entre données
attestées et reconstitutions hypothétiques. Voir `docs/Livrable_1/note-ethique.md`.

## Licence / usage

Projet académique, année 2025-2026.

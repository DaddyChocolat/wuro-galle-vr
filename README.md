# Wuro & Galle : L'Espace Peul entre Campement et Territoire

Projet VR — IAN/IHN 851, ENSPM Université de Maroua, Département des Arts et Humanités Numériques.
Réalisé en solo par Eddy (promotion 2022-2027), initialement prévu pour un groupe de 3.

## Concept

Immersion VR dans l'habitat et la cosmogonie des Peuls (Fulɓe) nomades et semi-nomades de l'Extrême-Nord Cameroun :

- **Wuro** : campement de bergers nomades — de l'aube au départ en transhumance.
- **Galle** : concession familiale semi-sédentaire en banco — cases, dudal, greniers.

## Structure du dépôt

```
blender/   fichiers .blend, exports GLTF 2.0 par module
unity/     projet Unity 2022.3 LTS (scènes Campement + Concession)
docs/      corpus documentaire, glossaire, notes éthiques, rapport réflexif
audio/     sons ambiants, voix-off, musique
```

## Contraintes techniques assumées (scope solo)

- Poste local : Core i5 5e gén, 20 Go RAM, sans GPU dédié
- Modélisation Blender en local ; intégration Unity lourde (lightmap baking, VFX) sur VM GPU louée ponctuellement
- Budget triangles : max 20 000/module, 80-120K/scène chargée
- Textures : 1-2K, atlas
- Voir `docs/scope-reduit.md` pour le détail du scope réduit (solo vs groupe de 3) et sa justification.

## Note éthique

Ce projet représente une culture vivante (Peuls Fulɓe). Engagement : pas de stéréotype ni d'exotisme,
respect de la semteende (pudeur/réserve), sources documentaires citées, distinction claire entre données
attestées et reconstitutions hypothétiques. Voir `docs/note-ethique.md`.

## Licence / usage

Projet académique — IAN/IHN 851, année 2025-2026.

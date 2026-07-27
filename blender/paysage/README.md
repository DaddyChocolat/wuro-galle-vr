# Enclos et paysage

Script : `enclos_paysage.py` — terrain (plan subdivisé + Displace bruit procédural), enclos hoggo (piquets coniques en cercle + 2 anneaux Bézier bevelés), mare (forme aplatie).

Toujours pas d'environnement de vérification automatique de mon côté — relu attentivement, notamment l'ordre sélection/actif avant `modifier_apply` (le point qui avait cassé le script zébu). Signale toute erreur immédiate.

## Exécution

1. Onglet Scripting > Open > `enclos_paysage.py` > Run Script.
2. Un grand terrain apparaît sous toute la scène (suudu, cases, zébu inclus), avec un enclos et une mare à l'écart (X=-12).

## Vérification

- **Terrain** : légèrement irrégulier, pas un billard parfaitement plat, mais pas non plus vallonné — l'irrégularité doit être subtile.
- **Enclos** : 14 piquets en cercle régulier, reliés par 2 anneaux horizontaux à hauteurs différentes (comme une clôture à lattes).
- **Mare** : forme ovale aplatie, matériau plus sombre et plus lisse (moins mat) que le sol autour.
- **Triangles** : rapport en Console — budget de référence ici, c'est celui de la scène complète (80 000-120 000), pas 20 000 par module.

## Prochaine étape

Tous les modules de géométrie de l'Étape 2 sont maintenant posés (mobilier, suudu, galle, troupeau, paysage). Prochaine étape : texturing PBR complet de tous les modules (au-delà des matériaux de base déjà en place), puis export GLTF final et assemblage du Livrable 2.

# Zébu — block-out low-poly

Script : `zebu_base.py` — corps par fusion de metaballs (torse, bosse, cou, tête, fanon), converti en mesh, + pattes coniques + cornes en lyre (courbe de Bézier) + oreilles. 2 matériaux de robe (blanc, roux) + 1 matériau corne.

Version 2 après un premier essai où des sphères simplement collées ne donnaient pas une silhouette animale reconnaissable — voir l'explication en tête du script. Toujours pas d'environnement de vérification automatique disponible de mon côté ; une deuxième relecture ciblée a corrigé un bug de sélection avant l'export de cette version, mais l'exécution réelle reste le seul vrai test.

## Exécution

1. Onglet Scripting > Open > `zebu_base.py` > Run Script.
2. Un zébu apparaît à l'écart de la suudu et des cases (décalé sur X), robe blanche par défaut.

## Vérification — soyons précis cette fois

- **Silhouette générale reconnaissable comme bovin** : surface lisse et continue entre torse/cou/tête (plus de jointures dures visibles). Si ça ressemble encore à des formes collées plutôt qu'un corps continu, les éléments metaball ne se chevauchent pas assez —à signaler, ça se corrige en rapprochant les éléments dans le script.
- **Bosse nettement visible** : doit clairement dépasser au-dessus de la ligne du dos, au-dessus des pattes avant — c'est le trait le plus identifiable du zébu, si elle se voit à peine ce n'est pas bon.
- **Fanon** : repli sous le cou/poitrail, visible de profil.
- **Proportions** : corps plus large/profond que long-et-fin (pas un tube), pattes visiblement plus épaisses qu'avant, cou court et épais plutôt que long.
- **Cornes** : émergent du sommet du crâne (pas flottantes ni plantées ailleurs), forme lyre nette (montent, s'écartent, se recourbent vers l'intérieur), base nettement plus épaisse que la pointe.
- **Triangles** : rapport en Console — le script alerte lui-même si le total dépasse 2 000 (trop cher pour un troupeau) et indique quoi ajuster (`mball.resolution`).

Si la silhouette ne convainc toujours pas après cette version, dis-moi précisément ce qui cloche (proportion, position d'un élément, angle) plutôt que "ce n'est pas bon" — je n'ai pas de retour visuel de mon côté, je corrige à l'aveugle sur ta description.

## Robe rousse (2e variante)

Dupliquer l'objet Zebu_Corps (Shift+D), puis dans Material Properties, remplacer `Zebu_Robe_Blanche` par `Zebu_Robe_Rousse` (déjà créé par le script, juste pas assigné par défaut).

## Sculpt mode (optionnel, si un animal se retrouve proche caméra)

1. Sélectionner Zebu_Corps, onglet **Sculpting**.
2. Ajouter un modificateur **Multiresolution**, "Subdivide" 2-3 fois avant de sculpter (le mesh issu des metaballs peut déjà être assez dense — vérifier le nombre de triangles avant d'en rajouter).
3. Brushes de base : Draw, Grab, Smooth. Rester léger — affiner les transitions, pas recréer l'anatomie en détail.
4. Modificateur **Decimate** (Collapse, ratio ~0.1-0.2) pour redescendre au budget troupeau, puis appliquer.

## Prochaine étape

Enclos et paysage (terrain de base, hoggo, mares) — dernier module de géométrie avant la texturing PBR complète.

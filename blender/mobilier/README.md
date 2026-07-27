# Objets domestiques simples

Script : `generer_mobilier.py` — génère calebasse, mortier, pilon et natte par révolution de profil ("tour de potier numérique").

## Exécution

1. Ouvrir Blender, créer un nouveau fichier.
2. Onglet **Scripting** (barre du haut).
3. Open > sélectionner `generer_mobilier.py`.
4. **Run Script** (triangle play, ou Alt+P curseur dans le script).
5. Les 4 objets apparaissent au centre de la scène, superposés — c'est normal, à espacer manuellement ensuite (Object Mode, G pour déplacer).

## Vérification

- **Nombre de triangles** : Window > Toggle System Console (Windows uniquement) avant de lancer le script, pour voir le rapport imprimé. Chaque objet doit être entre 100 et 400 triangles environ — très loin sous le budget de 20 000/module.
- **Normales** : si une face apparaît noire ou inversée en Material Preview, sélectionner l'objet, Edit Mode, tout sélectionner (A), Mesh > Normals > Recalculate Outside (Shift+N).
- **Échelle réelle** : en Object Mode, N pour ouvrir le panneau latéral, onglet Item — vérifier Dimensions. Calebasse ≈ 0,18 m de haut, Mortier ≈ 0,35 m, Pilon ≈ 0,90 m, Natte ≈ 1,1 m de diamètre max. Si les chiffres sont très différents, l'unité de scène Blender n'est probablement pas en mètres (Scene Properties > Units).
- **Forme générale** : la calebasse doit avoir un col resserré visible, le mortier un creux net au sommet, le pilon un renflement à une extrémité. Si un profil produit une forme plate ou trouée, vérifier qu'aucun point du profil n'a été mal recopié (rayon négatif, doublon).

## Matériaux

Script : `materiaux_mobilier.py` — à lancer après `generer_mobilier.py` (les 4 objets doivent déjà exister). Crée un matériau Principled BSDF + grain procédural par objet et l'assigne automatiquement.

1. Onglet **Scripting**, Open > `materiaux_mobilier.py`, Run Script.
2. Passer le viewport 3D en **Material Preview** (icône sphère mi-grise/mi-blanche, en haut à droite du viewport, ou touche Z > Material Preview) pour voir les couleurs — en mode Solid par défaut, les matériaux ne s'affichent pas.

### Vérification matériaux

- Chaque objet doit avoir sa couleur propre (calebasse brune chaude, mortier/pilon brun bois plus terne, natte dorée) — pas de gris/rose uniforme (le rose signale un matériau cassé ou une texture manquante).
- Léger grain visible en zoomant (Material Preview ou Rendered), pas une surface parfaitement lisse et plate.
- Dans l'onglet **Material Properties** (icône sphère à damier, panneau de droite), chaque objet ne doit avoir qu'un seul matériau dans sa liste — s'il y en a plusieurs empilés, relancer le script (il fait `materials.clear()` avant d'assigner, donc ça se corrige tout seul).

## Prochaine étape

Une fois les 4 formes et matériaux validés : export GLTF individuel par objet (File > Export > glTF 2.0, un fichier par objet dans `blender/mobilier/`), puis import test dans Unity pour confirmer que l'échelle et les matériaux survivent au passage.

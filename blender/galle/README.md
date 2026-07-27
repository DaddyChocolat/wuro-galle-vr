# Cases en banco

Script : `case_banco.py` — génère un mur (tube + Solidify + porte découpée par Boolean), un toit conique et une porte, puis répète l'opération pour 3 variantes (rayon/hauteur légèrement différents), espacées sur l'axe X pour la revue.

Environnement de vérification indisponible de mon côté au moment de l'écriture — relu attentivement (en particulier l'ordre d'application des modificateurs, source du bug précédent), mais pas exécuté dans Blender. Signale toute erreur immédiatement.

## Exécution

1. Onglet Scripting > Open > `case_banco.py` > Run Script.
2. 3 cases apparaissent côte à côte (mur + toit conique + porte), à 4 m d'intervalle.

## Vérification

- **Porte visible** : chaque mur doit avoir une ouverture basse nette côté +Y (regarder de face) — pas de trou raté ni de mur resté plein. Si la porte n'apparaît pas, vérifier en Edit Mode que le mur a bien une épaisseur (pas juste une paroi à zéro épaisseur, signe que Solidify n'a pas été appliqué avant Boolean).
- **Mur creux, pas plein** : passer en vue filaire (Z > Wireframe) — on doit voir une coque épaisse, pas un cylindre plein.
- **Toit** : légèrement plus large que le mur (avant-toit), bien centré dessus, sans décalage visible.
- **Triangles** : rapport en Console — attendu autour de 1 500 à 2 500 pour les 3 cases réunies, largement sous le budget.

## Matériaux cases

Script : `materiaux_case.py` — banco (murs), paille (toit), bois (porte), à lancer après `case_banco.py`. Assignation automatique par suffixe de nom d'objet (`_Mur`/`_Toit`/`_Porte`). Vérification : Material Preview, 3 teintes bien distinctes par case, mur nettement plus rugueux/mat que le toit.

## Prochaine étape

Troupeau (zébus, sculpt mode basique) — nouveau module, nouvelle famille de technique (sculpt plutôt que géométrie procédurale pure).

# Armature suudu

Script : `armature_suudu.py` — génère l'armature en branches arquées de la tente nomade (6 arches sur base circulaire, technique des Curve Objects).

## Exécution

1. Nouveau fichier Blender (ou continuer dans celui des objets domestiques).
2. Onglet **Scripting** > Open > `armature_suudu.py` > Run Script.
3. Les 6 arches apparaissent, formant un dôme en lattice.

## Vérification

- **Forme générale** : vu de dessus, les 6 arches doivent former un cercle régulier ; vu de côté, un dôme symétrique d'environ 1,6 m de haut sur 2,6 m de diamètre.
- **Continuité des branches** : chaque arche doit être un tube continu sans rupture ni pincement brutal au sommet — si une arche a un coude anguleux plutôt qu'une courbe lisse, vérifier que les poignées des points de contrôle sont bien en 'AUTO'.
- **Triangles** : rapport dans la Console système, à comparer au budget de 20 000/module. Avec 6 arches à ce niveau de détail, on doit rester sous 3 000 triangles au total.
- **Échelle** : panneau N > Item > Dimensions sur une arche individuelle, doit correspondre à peu près à 2,6 m de large, 1,6 m de haut.

## Nattes seygoore

Script : `nattes_seygoore.py` — à lancer après `armature_suudu.py` (partage les mêmes dimensions). Génère 3 bandes courbes superposées avec un matériau à transparence procédurale (trous imitant le tressage).

Ce script n'a pas pu être vérifié automatiquement avant livraison (environnement d'exécution indisponible côté assistant) — la syntaxe a été relue mais pas testée dans Blender. Signale-moi tout de suite si `Run Script` renvoie une erreur.

### Exécution

1. Onglet Scripting > Open > `nattes_seygoore.py` > Run Script.
2. 3 bandes courbes apparaissent sur/autour de l'armature, avec un espace entre chacune et un trou au sommet.

### Vérification

- **Positionnement** : les 3 bandes doivent envelopper l'armature sans la traverser ni flotter loin d'elle — sinon vérifier que `RAYON_BASE`/`HAUTEUR_DOME` sont identiques dans les deux scripts.
- **Transparence** : passer en Material Preview ou Rendered — chaque bande doit montrer un motif de petits trous irréguliers (effet tressé), pas une surface pleine ni invisible à 100%. En mode Solid, la transparence ne s'affiche pas, c'est normal.
- **Triangles** : rapport en Console, doit rester sous ~1 000 pour les 3 bandes.

## Matériau armature

Script : `materiaux_armature.py` — bois de branche brute, à lancer après `armature_suudu.py`. Vérification : Material Preview, teinte brun grisé mate sur les 6 arches.

## Prochaine étape

Cases en banco (galle) — nouveau module, autre technique (murs pleins, toit conique).

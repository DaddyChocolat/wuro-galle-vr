# Livrable 3 — Intégration VR

## Scope arrêté : pas de casque VR disponible

Contrainte matérielle assumée dès maintenant plutôt que découverte en fin de parcours
(même logique que le scope réduit documenté dans `docs/Livrable_4/scope-reduit.md`) :
sans casque Quest 2/3 pour tester, configurer un rig XR Interaction Toolkit
(contrôleurs, saisie d'objets, déclencheurs narratifs) et builder un APK reviendrait à
livrer du code jamais validé sur le matériel cible — contraire à l'exigence de rigueur
technique du cahier des charges. Le Livrable 3 est donc arrêté à une **intégration
Unity complète et testable au clavier/souris**, pas à un build VR embarqué.

Les pièces spécifiquement VR (rig XR, interactions manette, build APK, capture depuis
le casque) restent hors scope de ce livrable et sont documentées comme telles dans le
rapport réflexif (Livrable 4), pas silencieusement laissées de côté.

## État actuel

Fait :
- `Campement.unity` et `Concession.unity` assemblées (`Assets/Editor/SceneBuilder.cs`), agencement fondé sur `docs/Livrable_1/schema-annote-suudu-galle.md` (gradient entrée/intérieur, foyer équidistant, hoggo en périphérie).
- Éclairage de base (soleil zénith + ambiance) réglé — pas de lightmap baking (nécessiterait la VM GPU, non prioritaire sans casque pour en profiter).
- Feu de camp : Particle System (flammes/fumée/braises) + lumière scintillante, pas de VFX Graph (choix : rester en Built-in Render Pipeline).
- Mobilier domestique placé (natte de prière sur le dudal, mortier/pilon/calebasse près du foyer et des cases).
- **Audio spatialisé** (`AudioSource` 3D, `Assets/Editor/SceneBuilder.cs`) : feu de camp, clochettes + pâturage près du hoggo (Campement) ; appel à la prière au-dessus du dudal, mouton près de l'enclos (Concession) ; vent en ambiance diffuse dans les deux scènes. Un fichier disponible (`uganda-village-at-night`) a été écarté : explicitement nocturne, incohérent avec l'éclairage de zénith des scènes.
- Joueur desktop fonctionnel (CharacterController + collider de sol).

## Ce qui est déposé

- Les deux scènes Unity, jouables et testables au clavier/souris (`Assets/Scenes/Campement.unity`, `Concession.unity`).
- Une vidéo de démonstration capturée depuis l'écran (Game view), pas depuis un casque — à enregistrer et déposer ici en `.mp4`.

---

**Note (mise à jour ultérieure, voir `Assets/Editor/SceneBuilder.cs`) :** le rapport réflexif (Livrable 4) documente l'abandon du Terrain procédural pour un bug de ligne d'horizon nette. Ce bug a depuis été corrigé (bruit appliqué aux hauteurs jusqu'au bord réel du Terrain, plus de plateau parfaitement plat) et un vrai Terrain avec relief, brouillard et végétation éparse est de nouveau en place (`AjouterTerrainSceneActive`, appelé par `ExecuterPipelineComplet`).

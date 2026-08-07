# Notice utilisateur — Wuro & Galle : L'Espace Peul entre Campement et Territoire

## Présentation

Wuro & Galle est une expérience 3D interactive qui fait découvrir deux espaces de la vie peule pastorale du Diamaré camerounais : le **Wuro** (campement mobile de transhumance) et la **Galle** (concession familiale fixe). Les deux scènes sont visitées librement, à pied, au clavier et à la souris.

## Prérequis techniques

- PC Windows avec Unity installé (ou l'exécutable buildé du projet, selon la version remise).
- Aucun casque VR n'est requis : cette version est une intégration Unity desktop, testable au clavier/souris (voir la note de scope, `docs/Livrable_3/README.md`, sur cette limite assumée).
- Écouteurs ou haut-parleurs recommandés : l'expérience utilise un audio spatialisé (feu, troupeau, vent, appel à la prière selon la scène).

## Contrôles

| Action | Touche / geste |
|---|---|
| Se déplacer | Z/Q/S/D ou flèches directionnelles |
| Regarder autour de soi | Souris (déplacement libre, la vue verticale est bornée pour éviter de se retourner) |
| Interagir (boire, piler, prier, s'asseoir, caresser, attiser le feu) | E, à portée d'un objet concerné (un message s'affiche en bas de l'écran) |
| Examiner (légende culturelle sur le dudal, le grenier...) | F, à portée d'un objet concerné |
| Quitter la capture souris | Échap (selon le build) |

Les éléments sonores d'ambiance (voix des PNJ, feu, troupeau, vent...) se déclenchent automatiquement par proximité, sans rien à presser. Certains objets du décor sont en revanche interactifs à la touche E : boire au canari, piler le mil au mortier, prier sur la natte du dudal, s'asseoir sur une natte, caresser le troupeau, souffler sur les braises du feu de camp — un message ("Appuyez sur E pour...") apparaît en bas de l'écran dès que vous êtes assez proche. Rien à ramasser ni à déplacer : ce sont des gestes ponctuels, pas un inventaire ou une mécanique de jeu à objectifs.

## Les deux espaces

**Campement (Wuro)** — deux suudu (tentes semi-nomades) disposées autour d'un foyer central, troupeau à proximité de l'enclos (hoggo), mobilier domestique (natte, mortier, pilon, calebasse, canari) autour du feu. Ambiance sonore : crépitement du feu, clochettes et pâturage du troupeau, vent en fond. Interactions : boire au canari, piler le mil, s'asseoir sur la natte, caresser le troupeau, attiser le feu.

**Concession (Galle)** — organisation spatiale en gradient : le dudal (espace de prière, public, proche de l'entrée) puis les cases d'habitation (privées, plus profondes dans la concession) et le grenier. Cette progression public → privé reproduit une logique spatiale réelle documentée dans le dossier (`docs/Livrable_1/schema-annote-suudu-galle.md`) — se déplacer de l'entrée vers l'intérieur permet de la ressentir plutôt que de la lire seulement. Interactions : boire au canari près de l'entrée (geste d'hospitalité), piler le mil, s'asseoir sur une natte, prier sur la natte du dudal, examiner le dudal et le grenier pour une courte légende culturelle.

## Conseils de visite

Prenez le temps de vous arrêter près du foyer et des zones sonores plutôt que de traverser rapidement les scènes : l'essentiel de l'ambiance (audio spatialisé, éclairage) se révèle à l'arrêt, pas en mouvement continu.

## Limites connues

Cette version est un prototype desktop, pas un build VR embarqué (pas de rig d'interaction manette). Les interactions objets (boire, piler, prier, s'asseoir, caresser, attiser le feu) sont des gestes déclenchés à la touche, pas une vraie saisie/manipulation physique — aucun objet ne se ramasse ni ne se déplace librement. Les personnages sont des silhouettes statiques, sans animation ni dialogue interactif — un choix documenté dans la note éthique du projet, pas un défaut technique non assumé.

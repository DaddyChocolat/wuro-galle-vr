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
| Quitter la capture souris | Échap (selon le build) |

Il n'y a pas de bouton d'interaction à presser : les éléments sonores (voix, ambiances) se déclenchent automatiquement par proximité, en s'approchant simplement des zones concernées. Rien à cliquer, rien à ramasser — l'expérience est une visite guidée par le déplacement, pas un jeu à objectifs.

## Les deux espaces

**Campement (Wuro)** — deux suudu (tentes semi-nomades) disposées autour d'un foyer central, troupeau à proximité de l'enclos (hoggo), mobilier domestique (natte, mortier, pilon, calebasse) autour du feu. Ambiance sonore : crépitement du feu, clochettes et pâturage du troupeau, vent en fond.

**Concession (Galle)** — organisation spatiale en gradient : le dudal (espace de prière, public, proche de l'entrée) puis les cases d'habitation (privées, plus profondes dans la concession) et le grenier. Cette progression public → privé reproduit une logique spatiale réelle documentée dans le dossier (`docs/Livrable_1/schema-annote-suudu-galle.md`) — se déplacer de l'entrée vers l'intérieur permet de la ressentir plutôt que de la lire seulement.

## Conseils de visite

Prenez le temps de vous arrêter près du foyer et des zones sonores plutôt que de traverser rapidement les scènes : l'essentiel de l'ambiance (audio spatialisé, éclairage) se révèle à l'arrêt, pas en mouvement continu.

## Limites connues

Cette version est un prototype desktop, pas un build VR embarqué (pas de rig d'interaction manette, pas de saisie d'objets). Les personnages sont des silhouettes statiques, sans animation ni dialogue interactif — un choix documenté dans la note éthique du projet, pas un défaut technique non assumé.

# Script de narration — « Une journée en transhumance »

Brouillon à valider avant enregistrement (voir note-éthique.md — pudeur/semteende, distinction attesté/hypothèse). Ce script s'appuie sur le rythme documenté dans le glossaire (`naange` = soleil, repère du rythme quotidien : traite à l'aube, déplacement avant le zénith, installation au crépuscule) — pas d'invention de rituel ou de parole non sourcée. À ajuster à ta voix et, si tu le souhaites et que tu as les moyens de le vérifier, à enrichir de quelques mots en fulfulde déjà attestés dans le glossaire plutôt que de phrases complètes inventées (risque d'erreur que je ne peux pas garantir correcte).

## Voix-off cadre (intro, jouable au chargement ou en boucle discrète de fond — Campement)

> « Le jour se règle sur le soleil, *naange*. À l'aube, la traite. Avant que le zénith ne pèse trop fort, il faut déjà penser au prochain point d'eau. Au crépuscule, on installe le camp — jusqu'à la prochaine fois qu'il faudra le démonter. »

Durée indicative : 15-20 secondes. Ton : posé, descriptif, pas emphatique — évite le registre « voix de documentaire exotique ».

## PNJ_Berger (Campement) — lignes assignées à `PNJDialogue.lignes`, jouées dans l'ordre à chaque approche

1. « Le troupeau a besoin d'eau avant que le soleil ne soit trop haut. On ne traîne pas, ici. »
2. « Deux suudu, une seule vie autour du même feu — ce n'est pas la place qui manque, la ressource oui. »
3. « Ce campement ne restera pas là longtemps. C'est fait pour repartir, pas pour durer. »

## PNJ_Femme (Concession) — lignes assignées à `PNJDialogue.lignes`

1. « Ici, celui qui arrive s'arrête d'abord près de l'entrée. On ne va pas plus loin sans y être invité. »
2. « Une concession, ça se répare, ça s'agrandit — ce n'est jamais vraiment fini. »
3. « Le grenier est plein cette saison. Ça ne dure jamais aussi longtemps qu'on le voudrait. »

## Notes de mise en scène / éthique

- Aucune ligne ne nomme ni ne décrit un individu réel — les deux PNJ restent des silhouettes fonctionnelles (berger, femme au foyer), cohérent avec le choix déjà pris de ne pas les individualiser (note éthique, *semteende*).
- Pas de ligne prononcée dans l'espace privé de la concession qui déborderait sur des détails personnels/familiaux — les lignes de PNJ_Femme restent sur des généralités d'organisation spatiale, jamais sur des faits individuels.
- Enregistrement : voix neutre, débit posé. Un micro correct + pièce calme suffisent (pas besoin de studio). Une piste par ligne, nommée clairement (`berger_01.wav`, `femme_02.wav`...) pour l'assignation dans l'Inspector Unity (`PNJDialogue.lignes`, un slot par clip, dans l'ordre de lecture souhaité).
- Si un doute subsiste sur une formulation après relecture, mieux vaut la couper que la garder « probablement correcte » — cohérent avec le principe du projet de signaler l'incertain plutôt que de le lisser.

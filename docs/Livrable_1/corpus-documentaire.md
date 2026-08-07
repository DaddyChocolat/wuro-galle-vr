# Corpus documentaire

Sources rassemblées et organisées en six collections thématiques, couvrant l'architecture, le pastoralisme, la langue et la cosmogonie peules.

## Cosmogonie et pulaaku

- Anneke Breedveld, Mirjam de Bruijn, *L'image des Fulbe. Analyse critique de la construction du concept de pulaaku*, 1996 — https://www.persee.fr/doc/cea_0008-0055_1996_num_36_144_1868
- Tabital Pulaaku International, *Pulaaku* — https://www.tabitalpulaaku.org/pulaaku/

## Fulfulde et glossaire

- *Dictionnaire peul (pular, fulfulde) en ligne*, Lexilogos — https://www.lexilogos.com/peul_dictionnaire.htm
- SIL Global, *Dictionnaire Fulfulde–Français–English* — https://www.sil.org/resources/archives/84268
- *Dictionnaire Fulfulde–français–english et images*, Webonary — https://www.webonary.org/fulfuldeburkina/files/Dictionnaire-Fulfulde-fran%C3%A7ais-english-et-images.pdf

## Galle (concession sédentaire)

- Mohamadou Guidado, *"Saré" et urbanisme : traditions et pratiques architecturales peules à l'épreuve de la modernité, le cas de Ngaoundéré (Cameroun)*
- *Architecture traditionnelle du Nord-Cameroun*, Scribd — https://fr.scribd.com/document/521178837/Architecture-Traditionnelle-Cameroun
- Frédéric Gadmer, *Cases : habitation d'une famille Foulbé*, Doumrou, 23 avril 1918 — https://commons.wikimedia.org/wiki/File:Cases_-_Habitation_d%27une_famille_Foulb%C3%A9_-_Dumeru_-_M%C3%A9diath%C3%A8que_de_l%27architecture_et_du_patrimoine_-_AP62T177747.jpg
- Frédéric Gadmer, *Maison : entrée d'une maison Foulbé*, Garoua, 10 janvier 1918 — https://commons.wikimedia.org/wiki/File:Maison_-_Entr%C3%A9e_d%27une_maison_Foulb%C3%A9_-_Garoua_-_M%C3%A9diath%C3%A8que_de_l%27architecture_et_du_patrimoine_-_AP62T157133.jpg
- Abdouraman Mana 33, *Case traditionnelle des peulhs*, 2023 — https://commons.wikimedia.org/wiki/File:Case_traditionnelle_des_peulhs.jpg

## Troupeau et territoire

- Christian Seignobos, Olivier Iyébi-Mandjek, *Atlas de la province Extrême-Nord Cameroun*, IRD Éditions, 2005 — https://books.openedition.org/irdeditions/11582
- *L'élevage du Nord-Cameroun, entre transhumance et sédentarité*, CIRAD — https://agritrop.cirad.fr/590274/
- *Migrations Toupouri et déforestation dans les territoires d'élevage bovin de la plaine du Diamaré*, Canadian Journal of Tropical Geography — https://revuecangeotrop.ca/volume-7-numero-1/4762/
- *Fulani herd in the dust*, photographie, Cameroun du Nord — https://commons.wikimedia.org/wiki/File:Fulani_herd_in_the_dust.jpg

## Wuro (campement nomade)

- Gidadorabia, *Hut built by Fulani herdsmen*, Nigeria, 2023 — https://commons.wikimedia.org/wiki/File:Hut_built_by_Fulani_herdsmen.jpg (référence régionale peule élargie ; le lieu exact, d'après les métadonnées Commons, est le Nigeria et non le Cameroun)

Une source initialement retenue pour ce thème (*Nyorgo Maroua*, OUMAROU OB, Wikimedia Commons) a été écartée après vérification : l'image ne correspond pas à sa description (elle montre une vannerie de style est-africain plutôt que des ustensiles peuls camerounais), et Commons la signale lui-même comme non vérifiée. Décision documentée ici pour traçabilité méthodologique.

## Modèles 3D externes

- treesclimber, *Human Basemeshes* (base humaine masculine/féminine, rigged, sans traits sculptés), OpenGameArt.org — https://opengameart.org/content/human-basemeshes — licence CC0 (domaine public, aucune attribution requise). Utilisée pour le corps du joueur (`blender/personnage/personnage_joueur.py`) à la place des segments procéduraux précédents ; conservée sans visage ni traits individualisés pour rester cohérente avec `note-ethique.md` (silhouette sans traits, *semteende*), seule la teinte de peau a été modifiée (matériau ajouté, pas de retouche de la géométrie).
- maximorengifo2022, *Brahman bull Zebu*, Sketchfab — https://sketchfab.com/3d-models/brahman-bull-zebu-3e2254015caa4c878347e2985a927edd — licence CC Attribution (CC-BY 4.0) : **attribution requise**, respectée par cette entrée. Remplace le block-out metaball (`zebu_base.py`) du troupeau (`blender/troupeau/zebu_realiste.py`) — silhouette réelle avec bosse et cornes en lyre (trait distinctif du zébu, absent des alternatives libres de connexion trouvées sur Poly Pizza). Trois robes (blanche/rousse/noire) exportées depuis le même maillage source, les teintes rousse et noire appliquées par multiplication shader plutôt qu'une seconde texture peinte.
- Kenney, *Particle Pack* (80+ sprites : feu, fumée, étincelles, magie...), OpenGameArt.org — https://opengameart.org/content/particle-pack-80-sprites — licence CC0 (domaine public, aucune attribution requise). Textures de flamme/fumée/braise (`flamme.png`, `fumee.png`, `braise.png`, dans `unity/wuro-galle-vr/Assets/Textures/Particules/`) appliquées aux systèmes de particules du foyer (`SceneBuilder.CreerParticulesFlammes/Fumee/Braises`), à la place d'un dégradé radial généré en code (un simple cercle flou, sans silhouette de flamme réelle).

## Fonds sonores

- schmutz, *Cow Bells* — https://freesound.org/people/schmutz/sounds/359125/
- davorl, enregistrement de troupeau — https://freesound.org/people/davorl/sounds/651517/
- felix.blume, enregistrements de terrain (x3) — https://freesound.org/people/felix.blume/sounds/156414/, /174445/, /641588/
- lareunce1, enregistrement — https://freesound.org/people/lareunce1/sounds/513506/
- Angel_Perez_Grandi, enregistrement — https://freesound.org/people/Angel_Perez_Grandi/sounds/44465/
- Martineerok, enregistrement — https://freesound.org/people/Martineerok/sounds/329857/
- kyles, enregistrement — https://freesound.org/people/kyles/sounds/637523/
- spurioustransients, enregistrement — https://freesound.org/people/spurioustransients/sounds/513565/
- jppi_Stu, enregistrement — https://freesound.org/people/jppi_Stu/sounds/23725/
- FreeSoundsLibrary : cowbell, cow with bell, goat, pan flute, flûte, flamme (licences à vérifier individuellement avant usage) — freesoundslibrary.com

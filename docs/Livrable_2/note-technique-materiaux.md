# Note technique — choix des matériaux

## Wuro & Galle : L'Espace Peul entre Campement et Territoire

Cette note documente les choix de matériaux appliqués aux cinq modules 3D du projet (mobilier domestique, armature et nattes de la suudu, cases en banco du galle, troupeau, enclos et paysage) : la technique employée, les paramètres retenus et la justification de chaque choix au regard du sujet représenté.

## Technique commune : Principled BSDF + grain procédural

Les quatorze matériaux du projet reposent tous sur le même nœud de base dans Blender : le **Principled BSDF**, un modèle de rendu physiquement plausible (PBR) qui combine en un seul nœud la couleur de surface (Base Color), la rugosité (Roughness — 0 : poli et brillant, 1 : totalement mat) et, plus ponctuellement, la transparence (Alpha).

Pour éviter des surfaces plates et artificielles sans recourir à des textures peintes — étape prévue plus tard dans le module de texturing complet —, chaque matériau reçoit un léger relief procédural : une texture de bruit (**Noise Texture**) pilote un nœud **Bump**, qui perturbe légèrement les normales de la surface pour simuler un grain (bois, terre séchée, paille) sans ajouter de géométrie. L'échelle du bruit et l'intensité du bump varient selon la matière représentée : un grain fin et discret pour le bois travaillé (mortier, pilon), un grain plus large et irrégulier pour le banco monté à la main.

Un cas particulier — les nattes seygoore — utilise une seconde texture procédurale, **Voronoi**, pour piloter non pas le relief mais la transparence (Alpha) : la cellule de Voronoi crée un motif de taches irrégulières qui, une fois converties en un dégradé net via un nœud **Color Ramp**, simule les trous laissés par le tressage. C'est un gabarit de visualisation, pas une texture finale — la vraie texture tressée (peinte ou photographiée) est prévue pour l'étape de texturing PBR complète.

## Mobilier domestique

Trois matériaux pour quatre objets : un bois pyrogravé plus poli pour la calebasse (rugosité 0,55, teinte chaude), un bois brut plus mat pour le mortier et le pilon (rugosité 0,75, grain plus marqué — l'outil n'est pas poncé), et une paille tressée dorée et très mate pour la natte de sol (rugosité 0,85). La distinction de rugosité entre calebasse et mortier/pilon traduit une différence d'usage réelle : la calebasse, récipient de service, reçoit une finition plus soignée que les outils de pilage.

## Armature et nattes — suudu

L'armature reprend la teinte de bois brut du mobilier mais en plus terne et plus rugueuse (rugosité 0,82) : une branche coupée telle quelle n'a pas la même finition qu'un objet façonné. Les nattes seygoore utilisent la transparence Voronoi décrite plus haut, avec une teinte paille cohérente avec la natte de sol du mobilier — les deux éléments proviennent de la même famille de matériau végétal.

## Cases en banco — galle

Trois matériaux distincts par élément architectural : le mur en banco (terre séchée, rugosité 0,92, grain large pour marquer une paroi montée à la main plutôt que coulée), le toit en paille (rugosité 0,85, grain fin fibreux), et la porte en bois — à laquelle on a délibérément donné la même teinte que le bois brut du mobilier et de l'armature, pour que tous les éléments en bois de la scène restent visuellement cohérents entre eux malgré des scripts de génération différents.

## Troupeau — zébus

Deux robes (blanche et rousse, rugosité 0,6 toutes les deux — le pelage n'est ni mat ni brillant) pour représenter la variabilité réelle des robes zébu, et un matériau corne distinct (rugosité 0,35, plus lisse — la kératine réfléchit davantage la lumière que le pelage). Aucun grain procédural ici : à l'échelle du block-out actuel, la variation de couleur entre robes suffit à distinguer les individus ; un travail de texture (marques, salissures) est prévu au texturing complet plutôt qu'en gabarit procédural, pour ne pas fixer prématurément une apparence qui devra être validée visuellement.

## Enclos et paysage

Trois matériaux : un sol latéritique très mat (rugosité 0,95, cohérent avec la terre du Cameroun soudano-sahélien plutôt qu'un sol générique), un bois épineux pour les piquets et anneaux de l'enclos (rugosité 0,85, même famille que les autres bois bruts du projet), et une eau de mare volontairement plus lisse (rugosité 0,1) pour créer un contraste visuel net avec le sol environnant et signaler clairement sa fonction dans la scène.

## Limites et prochaine étape

Ces quatorze matériaux sont des **gabarits procéduraux**, pas des textures finales : ils utilisent des couleurs plates et des reliefs générés par nœuds plutôt que des images peintes ou photographiées, une approche compatible avec la configuration matérielle du poste de travail (pas de carte graphique dédiée) et suffisante pour valider les proportions, les contrastes et la lisibilité de la scène avant de passer aux textures PBR complètes.

Un point de vigilance identifié pour la suite : certains choix de couleur (robe zébu, terre latéritique, paille) mériteraient d'être confrontés à des références photographiques de la zone de Maroua plutôt qu'à une palette générique de "savane africaine" — c'est le risque de folklorisation à surveiller à l'étape de texturing complet, où des textures peintes à la main remplaceront ces gabarits.

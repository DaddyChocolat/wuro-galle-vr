"""
PNJ (Berger, Femme) — base humaine réelle CC0, Wuro & Galle

Même source que le corps du joueur (personnage_joueur.py) : "Human
Basemeshes" par treesclimber, OpenGameArt.org, licence CC0 — voir
docs/Livrable_1/corpus-documentaire.md. Remplace le placeholder capsule de
SceneBuilder.CreerPNJPlaceholder, qui détonnait par rapport au joueur/
troupeau maintenant réalistes.

Contrairement au joueur, les PNJ n'ont pas besoin d'articulation (pas
d'interaction, PNJDialogue est purement passif — une ligne audio se
déclenche par proximité, aucune pose n'est jamais appliquée) : le squelette
est exporté mais reste en pose de repos, jamais manipulé côté Unity. Deux
exports séparés, la base masculine pour PNJ_Berger, la base féminine pour
PNJ_Femme — variété minimale mais réelle, pas juste la même silhouette
reskinnée.

Toujours sans visage ni traits sculptés (le mesh source n'en a pas) —
silhouette sans traits individualisés (note éthique, *semteende*), même
principe que pour le joueur.

Habits (ajout) : les PNJ restaient torse nu jusqu'ici — écart signalé (les
tenues nomades/peules annoncées n'existaient en réalité pas encore). Ajout
d'un grand boubou (robe ample, cône évasé) + coiffe (chapeau conique de
berger en paille pour l'homme, foulard noué pour la femme), construits en
primitives simples pour rester cohérents avec le reste du style de l'atelier
(suudu, cases, mobilier — tout en primitives Blender, pas de sculpt).
Proportions dérivées de HAUTEUR_CIBLE via des ratios anthropométriques
standards plutôt que des bounds du maillage : le maillage source est en pose
de repos bras légèrement écartés (pas un vrai A-pose serré), une largeur
d'épaule mesurée sur les bounds XY engloberait une partie du bras et
donnerait une robe bien trop large — même piège que l'axe nez-queue mal
mesuré sur le zébu (zebu_realiste.py).

Exécution : headless uniquement (voir personnage_joueur.py pour l'explication
du contournement bpy.data.libraries.load - le .blend source fait planter
Blender au chargement normal) :
  blender --background --python pnj_base.py
"""

import bpy
import os

CHEMIN_SOURCE_CC0 = os.path.join(os.path.dirname(os.path.abspath(__file__)), "source_cc0", "Human Basemeshes.blend")
HAUTEUR_CIBLE = 1.75  # m — légèrement moins que le joueur (1.78) pour une variété subtile, pas pour une raison narrative


def charger_base_cc0(nom_mesh, nom_armature):
    """Identique à personnage_joueur.py : append (pas open/link) pour éviter le crash au chargement direct du .blend source."""
    with bpy.data.libraries.load(CHEMIN_SOURCE_CC0, link=False) as (data_from, data_to):
        data_to.objects = [n for n in data_from.objects if n in (nom_mesh, nom_armature)]

    for obj in data_to.objects:
        if obj is not None:
            bpy.context.collection.objects.link(obj)

    return bpy.data.objects[nom_mesh], bpy.data.objects[nom_armature]


def mettre_a_lechelle_et_poser_au_sol(mesh, armature, hauteur_cible):
    bpy.context.view_layer.update()
    min_z = min((mesh.matrix_world @ v.co).z for v in mesh.data.vertices)
    max_z = max((mesh.matrix_world @ v.co).z for v in mesh.data.vertices)
    hauteur_actuelle = max_z - min_z
    if hauteur_actuelle <= 0:
        raise RuntimeError("Hauteur du mesh source invalide (bounds dégénérés).")

    echelle = hauteur_cible / hauteur_actuelle
    armature.scale = (echelle, echelle, echelle)
    bpy.context.view_layer.update()

    min_z_apres = min((mesh.matrix_world @ v.co).z for v in mesh.data.vertices)
    armature.location.z -= min_z_apres
    bpy.context.view_layer.update()


def centre_horizontal(mesh):
    """Centre XY du maillage (utilisé pour aligner les habits) — fiable même en pose de repos
    bras écartés, contrairement à une largeur mesurée sur les bounds (voir note en tête de fichier)."""
    coords = [(mesh.matrix_world @ v.co) for v in mesh.data.vertices]
    xs = [c.x for c in coords]
    ys = [c.y for c in coords]
    return (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2


def creer_materiau_tissu(nom, couleur, rugosite=0.85):
    mat = bpy.data.materials.new(name=nom)
    mat.use_nodes = True
    mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*couleur, 1.0)
    mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = rugosite
    mat.node_tree.nodes["Principled BSDF"].inputs["Metallic"].default_value = 0.0
    return mat


def creer_boubou(centre_x, centre_y, hauteur_cible, couleur, nom):
    """Grand boubou (robe ample sahélienne) : cône évasé des épaules jusqu'aux mollets.
    Ratios anthropométriques (pas les bounds du maillage, voir note en tête de fichier) :
    largeur d'épaule ≈ 0.13 x taille (bideltoïde), evasement caractéristique du boubou vers le bas."""
    demi_largeur_epaule = hauteur_cible * 0.13

    h_epaule = hauteur_cible * 0.80
    h_ourlet = hauteur_cible * 0.12  # tombe jusqu'au mollet
    rayon_haut = demi_largeur_epaule * 1.35   # ample, flotte par-dessus le corps plutôt que moulant
    rayon_bas = demi_largeur_epaule * 2.3     # évasement caractéristique du boubou

    bpy.ops.mesh.primitive_cone_add(
        vertices=16, radius1=rayon_bas, radius2=rayon_haut,
        depth=h_epaule - h_ourlet,
        location=(centre_x, centre_y, (h_epaule + h_ourlet) / 2),
    )
    boubou = bpy.context.active_object
    boubou.name = nom
    boubou.data.materials.append(creer_materiau_tissu(f"{nom}_Tissu", couleur))
    return boubou


def creer_chapeau_berger(centre_x, centre_y, hauteur_cible, couleur, nom):
    """Chapeau conique en paille du berger peul, posé sur le sommet du crâne."""
    demi_largeur_tete = hauteur_cible * 0.042
    h_base = hauteur_cible * 0.985
    h_pointe = hauteur_cible * 1.22

    bpy.ops.mesh.primitive_cone_add(
        vertices=12, radius1=demi_largeur_tete * 2.6, radius2=0.004,
        depth=h_pointe - h_base,
        location=(centre_x, centre_y, (h_pointe + h_base) / 2),
    )
    chapeau = bpy.context.active_object
    chapeau.name = nom
    chapeau.data.materials.append(creer_materiau_tissu(f"{nom}_Paille", couleur, rugosite=0.9))
    return chapeau


def creer_foulard(centre_x, centre_y, hauteur_cible, couleur, nom):
    """Foulard noué autour de la tête (femme peul) — tore aplati au niveau du front."""
    demi_largeur_tete = hauteur_cible * 0.042
    h_foulard = hauteur_cible * 0.955

    bpy.ops.mesh.primitive_torus_add(
        major_radius=demi_largeur_tete * 1.2, minor_radius=demi_largeur_tete * 0.6,
        location=(centre_x, centre_y, h_foulard),
        major_segments=16, minor_segments=8,
    )
    foulard = bpy.context.active_object
    foulard.name = nom
    foulard.data.materials.append(creer_materiau_tissu(f"{nom}_Tissu", couleur))
    return foulard


def creer_materiau_peau():
    """Même teinte que le joueur (0.24,0.13,0.08) — cohérence visuelle demandée entre joueur et PNJ."""
    mat = bpy.data.materials.new(name="PNJ_Peau")
    mat.use_nodes = True
    principled = mat.node_tree.nodes["Principled BSDF"]
    principled.inputs["Base Color"].default_value = (0.24, 0.13, 0.08, 1.0)
    principled.inputs["Roughness"].default_value = 0.55
    return mat


def exporter_glb(objets, chemins):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objets:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objets[0]

    for chemin in chemins:
        os.makedirs(os.path.dirname(chemin), exist_ok=True)
        bpy.ops.export_scene.gltf(
            filepath=chemin,
            export_format='GLB',
            use_selection=True,
            export_apply=False,
            export_yup=True,
            export_skins=True,
            export_animations=False,
        )
        print(f"[pnj_base.py] Exporté -> {chemin}")


def generer(nom_mesh_source, nom_armature_source, nom_export, nom_mesh_final, nom_armature_final,
            couleur_boubou, coiffe):
    """coiffe : 'chapeau' (berger, paille) ou 'foulard' (femme, tissu noué)."""
    mesh, armature = charger_base_cc0(nom_mesh_source, nom_armature_source)
    mesh.name = nom_mesh_final
    armature.name = nom_armature_final

    mettre_a_lechelle_et_poser_au_sol(mesh, armature, HAUTEUR_CIBLE)

    mat = creer_materiau_peau()
    mesh.data.materials.clear()
    mesh.data.materials.append(mat)

    cx, cy = centre_horizontal(mesh)
    boubou = creer_boubou(cx, cy, HAUTEUR_CIBLE, couleur_boubou, f"{nom_mesh_final}_Boubou")
    if coiffe == 'chapeau':
        coiffe_obj = creer_chapeau_berger(cx, cy, HAUTEUR_CIBLE, (0.68, 0.55, 0.28), f"{nom_mesh_final}_Chapeau")
    else:
        coiffe_obj = creer_foulard(cx, cy, HAUTEUR_CIBLE, couleur_boubou, f"{nom_mesh_final}_Foulard")

    print(f"  {mesh.name} : {len(mesh.data.vertices)} sommets, {len(mesh.data.polygons)} polygones")
    print(f"  {boubou.name} + {coiffe_obj.name} ajoutés (habits, primitives statiques — le PNJ ne change jamais de pose)")

    if bpy.app.background:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        repo_root = os.path.abspath(os.path.join(script_dir, "..", ".."))
        exporter_glb([mesh, armature, boubou, coiffe_obj], [
            os.path.join(script_dir, f"{nom_export}.glb"),
            os.path.join(repo_root, "unity", "wuro-galle-vr", "Assets", "Models", "Personnage", f"{nom_export}.glb"),
        ])

    # Nettoie la scène avant le prochain personnage (male/femelle exportés dans le même passage).
    for obj in (mesh, armature, boubou, coiffe_obj):
        bpy.data.objects.remove(obj, do_unlink=True)


if __name__ == "__main__":
    if bpy.app.background:
        bpy.ops.wm.read_factory_settings(use_empty=True)

    print("\n--- PNJ (base CC0) ---")
    # Boubou indigo pour le berger (teinture à l'indigo, très associée aux Peuls) +
    # chapeau conique de paille (couvre-chef traditionnel du berger peul/sahélien).
    generer("basemesh_male", "basemesh_male_rig", "PNJ_Homme", "PNJ_Homme_Mesh", "PNJ_Homme_Armature",
            couleur_boubou=(0.14, 0.16, 0.42), coiffe='chapeau')
    # Boubou/pagne ocre-rouille pour la femme + foulard assorti noué.
    generer("basemesh_female", "basemesh_female_rig", "PNJ_Femme", "PNJ_Femme_Mesh", "PNJ_Femme_Armature",
            couleur_boubou=(0.58, 0.24, 0.11), coiffe='foulard')
    print()

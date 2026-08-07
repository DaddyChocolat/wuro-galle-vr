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

Exécution : headless uniquement (voir personnage_joueur.py pour l'explication
du contournement bpy.data.libraries.load — le .blend source fait planter
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


def generer(nom_mesh_source, nom_armature_source, nom_export, nom_mesh_final, nom_armature_final):
    mesh, armature = charger_base_cc0(nom_mesh_source, nom_armature_source)
    mesh.name = nom_mesh_final
    armature.name = nom_armature_final

    mettre_a_lechelle_et_poser_au_sol(mesh, armature, HAUTEUR_CIBLE)

    mat = creer_materiau_peau()
    mesh.data.materials.clear()
    mesh.data.materials.append(mat)

    print(f"  {mesh.name} : {len(mesh.data.vertices)} sommets, {len(mesh.data.polygons)} polygones")

    if bpy.app.background:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        repo_root = os.path.abspath(os.path.join(script_dir, "..", ".."))
        exporter_glb([mesh, armature], [
            os.path.join(script_dir, f"{nom_export}.glb"),
            os.path.join(repo_root, "unity", "wuro-galle-vr", "Assets", "Models", "Personnage", f"{nom_export}.glb"),
        ])

    # Nettoie la scène avant le prochain personnage (male/femelle exportés dans le même passage).
    bpy.data.objects.remove(mesh, do_unlink=True)
    bpy.data.objects.remove(armature, do_unlink=True)


if __name__ == "__main__":
    if bpy.app.background:
        bpy.ops.wm.read_factory_settings(use_empty=True)

    print("\n--- PNJ (base CC0) ---")
    generer("basemesh_male", "basemesh_male_rig", "PNJ_Homme", "PNJ_Homme_Mesh", "PNJ_Homme_Armature")
    generer("basemesh_female", "basemesh_female_rig", "PNJ_Femme", "PNJ_Femme_Mesh", "PNJ_Femme_Armature")
    print()

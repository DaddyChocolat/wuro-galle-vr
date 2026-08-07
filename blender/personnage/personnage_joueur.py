"""
Corps du joueur — base humaine réelle (rig complet), Wuro & Galle

Remplace la version précédente (segments tronconiques procéduraux, sans
genou) par une VRAIE base mesh humaine rigged, sourcée en CC0 : "Human
Basemeshes" par treesclimber, OpenGameArt.org
(https://opengameart.org/content/human-basemeshes), licence CC0 (domaine
public, aucune attribution requise). ~580 sommets, squelette complet
(hanche > colonne > thorax > cou/tête, épaule > bras > avant-bras > main
avec doigts individuels, cuisse > tibia > pied par jambe) — un vrai genou
articulé, ce que la version précédente n'avait pas.

Pourquoi ne pas juste ouvrir le fichier CC0 tel quel : le .blend original
fait planter Blender 4.1 au chargement normal (shape key corrompue,
"KEKey.000 has an invalid 'from' pointer") — contournement : on APPEND
seulement les objets utiles via bpy.data.libraries.load() plutôt que
d'ouvrir le fichier comme scène principale (ce qui évite de charger l'état
fenêtre/UI corrompu qui cause le crash).

Sans visage ni traits sculptés (le mesh source n'en a pas — un des critères
de choix), matériau teinte de peau appliqué ici (voir note éthique :
silhouette sans traits individualisés).

Exécution : headless uniquement pour l'instant (le fichier source doit être
chargé via bpy.data.libraries.load, pas via l'UI standard) :
  blender --background --python personnage_joueur.py
"""

import bpy
import os

CHEMIN_SOURCE_CC0 = os.path.join(os.path.dirname(os.path.abspath(__file__)), "source_cc0", "Human Basemeshes.blend")
HAUTEUR_CIBLE = 1.78  # m, cohérent avec CharacterController.height=1.8 (SceneBuilder.CreerJoueur)


def charger_base_cc0(nom_mesh, nom_armature):
    """Append (pas open/link) pour éviter le crash au chargement direct du .blend source (voir docstring)."""
    with bpy.data.libraries.load(CHEMIN_SOURCE_CC0, link=False) as (data_from, data_to):
        data_to.objects = [n for n in data_from.objects if n in (nom_mesh, nom_armature)]

    for obj in data_to.objects:
        if obj is not None:
            bpy.context.collection.objects.link(obj)

    mesh = bpy.data.objects[nom_mesh]
    armature = bpy.data.objects[nom_armature]
    return mesh, armature


def mettre_a_lechelle_et_poser_au_sol(mesh, armature, hauteur_cible):
    """Redimensionne l'ensemble mesh+armature pour une hauteur totale donnée, pieds au sol (Z=0)."""
    # Applique toute transformation en attente pour lire des bounds propres.
    bpy.context.view_layer.update()

    min_z = min((mesh.matrix_world @ v.co).z for v in mesh.data.vertices)
    max_z = max((mesh.matrix_world @ v.co).z for v in mesh.data.vertices)
    hauteur_actuelle = max_z - min_z
    if hauteur_actuelle <= 0:
        raise RuntimeError("Hauteur du mesh source invalide (bounds dégénérés).")

    echelle = hauteur_cible / hauteur_actuelle
    armature.scale = (echelle, echelle, echelle)
    bpy.context.view_layer.update()

    # Repositionne pour que le pied le plus bas touche Z=0 après mise à l'échelle.
    min_z_apres = min((mesh.matrix_world @ v.co).z for v in mesh.data.vertices)
    armature.location.z -= min_z_apres
    bpy.context.view_layer.update()


def creer_materiau_peau():
    """Teinte de peau chaude, assombrie/resaturée pour rester lisible sous le
    soleil de zénith + color grading chaud de la scène (voir SceneBuilder.
    ConfigurerColorGrading) — même valeur que la version précédente
    (0.24,0.13,0.08), qui se détachait bien du décor en test."""
    mat = bpy.data.materials.new(name="Personnage_Peau")
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
            export_apply=False,  # NE PAS appliquer les transforms : casserait le binding armature/mesh (skinning)
            export_yup=True,
            export_skins=True,
            export_animations=False,  # pas d'animation baked ici — les poses sont pilotées côté Unity (CorpsJoueur.cs)
        )
        print(f"[personnage_joueur.py] Exporté -> {chemin}")


if __name__ == "__main__":
    if bpy.app.background:
        bpy.ops.wm.read_factory_settings(use_empty=True)

    mesh, armature = charger_base_cc0("basemesh_male", "basemesh_male_rig")
    mesh.name = "Personnage_Mesh"
    armature.name = "Personnage_Armature"

    mettre_a_lechelle_et_poser_au_sol(mesh, armature, HAUTEUR_CIBLE)

    mat = creer_materiau_peau()
    mesh.data.materials.clear()
    mesh.data.materials.append(mat)

    print(f"\n--- Personnage (base CC0 rigged) ---")
    print(f"  {mesh.name} : {len(mesh.data.vertices)} sommets, {len(mesh.data.polygons)} polygones")
    print(f"  Squelette : {len(armature.data.bones)} os\n")

    if bpy.app.background:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        repo_root = os.path.abspath(os.path.join(script_dir, "..", ".."))
        exporter_glb([mesh, armature], [
            os.path.join(script_dir, "Personnage.glb"),
            os.path.join(repo_root, "unity", "wuro-galle-vr", "Assets", "Models", "Personnage", "Personnage.glb"),
        ])

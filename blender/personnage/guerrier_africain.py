"""
Guerrier africain (corps du joueur) — base réelle CC-BY, Wuro & Galle

Remplace la base CC0 (personnage_joueur.py) comme corps du joueur, sur
demande explicite : "African Warrior" par avenirdesign, Sketchfab
(https://sketchfab.com/3d-models/african-warrior-6146c47389a44ad281b0e866bad33fed),
licence CC Attribution (CC-BY 4.0) — attribution obligatoire, voir
docs/Livrable_1/corpus-documentaire.md.

Le fichier a été extrait par l'utilisateur directement dans
blender/troupeau/source_cc0/ (scene.gltf + scene.bin + textures/, mélangé
avec d'autres téléchargements dans le même dossier — scene.gltf référence
ses textures en chemin relatif, donc l'import fonctionne malgré le mélange,
mais aucun autre fichier de ce dossier ne doit être renommé/déplacé).

Deux armatures présentes dans le fichier source : "AFRICANWARRIORrig" (198 os,
rig de déformation complet) et "metarig" (64 os, noms proches de la convention
du projet : hips_metarig, spine_metarig, thigh.L_metarig...). Les deux sont
conservées à l'export — CorpsJoueur.cs (Contains() plutôt qu'égalité stricte,
voir commentaire du script) a une chance de reconnaître les os du metarig
malgré le suffixe "_metarig", mais rien ne garantit que ce rig anime
effectivement le maillage (à vérifier visuellement après export — voir
docs/Livrable_4/notice-utilisateur.md si le corps reste figé en pose de repos
pendant les interactions).

Exécution : headless (le fichier n'est pas un .blend corrompu comme la base
CC0 précédente, l'import direct via bpy.ops.import_scene.gltf suffit) :
  blender --background --python guerrier_africain.py
"""

import bpy
import os

CHEMIN_GLTF = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "troupeau", "source_cc0", "scene.gltf")
HAUTEUR_CIBLE = 1.78  # m — identique à personnage_joueur.py (cohérence avec le reste du projet)


def importer():
    bpy.ops.import_scene.gltf(filepath=os.path.abspath(CHEMIN_GLTF))
    objets = list(bpy.context.selected_objects)
    if not objets:
        raise RuntimeError("Aucun objet importé depuis scene.gltf.")
    return objets


def racine_visuelle(objets):
    """Le nœud 'Sketchfab_model' ou 'Root' englobe tout — plus simple à mettre à l'échelle
    qu'un par un. Retombe sur le premier objet sans parent si absent."""
    for nom in ("Sketchfab_model", "Root"):
        for obj in objets:
            if obj.name == nom:
                return obj
    for obj in objets:
        if obj.parent is None:
            return obj
    return objets[0]


def mettre_a_lechelle(objets, racine, hauteur_cible):
    bpy.context.view_layer.update()
    zs = []
    for obj in objets:
        if obj.type != 'MESH':
            continue
        for v in obj.data.vertices:
            zs.append((obj.matrix_world @ v.co).z)
    if not zs:
        raise RuntimeError("Aucun sommet trouvé pour mesurer la hauteur.")
    min_z, max_z = min(zs), max(zs)
    hauteur_actuelle = max_z - min_z
    if hauteur_actuelle <= 0:
        raise RuntimeError("Bounds dégénérés.")

    echelle = hauteur_cible / hauteur_actuelle
    racine.scale = (echelle, echelle, echelle)
    bpy.context.view_layer.update()

    # pieds au sol
    zs_apres = []
    for obj in objets:
        if obj.type != 'MESH':
            continue
        for v in obj.data.vertices:
            zs_apres.append((obj.matrix_world @ v.co).z)
    racine.location.z -= min(zs_apres)


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
            export_apply=False,  # préserve le skinning (armature)
            export_yup=True,
            export_skins=True,
            export_animations=False,
        )
        print(f"[guerrier_africain.py] Exporté -> {chemin}")


if __name__ == "__main__":
    if bpy.app.background:
        bpy.ops.wm.read_factory_settings(use_empty=True)

    objets = importer()
    racine = racine_visuelle(objets)
    print(f"--- Guerrier africain (base CC-BY) ---")
    print(f"  {len(objets)} objets importés, racine visuelle = {racine.name}")

    mettre_a_lechelle(objets, racine, HAUTEUR_CIBLE)

    if bpy.app.background:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        repo_root = os.path.abspath(os.path.join(script_dir, "..", ".."))
        # BUG constaté : exporter seulement les racines (parent is None) ne suffit pas —
        # l'exporteur glTF de Blender en mode use_selection n'inclut PAS automatiquement
        # les enfants non sélectionnés d'un objet sélectionné (contrairement à une
        # intuition "sélection implique hiérarchie") : le premier essai a produit un
        # glb de 344 octets (racine vide, rien dedans). Il faut sélectionner TOUS les
        # objets importés explicitement.
        exporter_glb(objets, [
            os.path.join(script_dir, "GuerrierAfricain.glb"),
            os.path.join(repo_root, "unity", "wuro-galle-vr", "Assets", "Models", "Personnage", "GuerrierAfricain.glb"),
        ])

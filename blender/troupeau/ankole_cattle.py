"""
Ankole cattle (remplace le zébu) — base réelle CC-BY probable, Wuro & Galle

Modèle "Ankole Cattle" par kenchoo (collection "Farm animal", Sketchfab),
téléchargé par l'utilisateur. license.txt absent du zip — à documenter dans
docs/Livrable_1/corpus-documentaire.md dès confirmation de l'URL/licence
exacte (les autres modèles gratuits de cet auteur repérés en recherche sont
en CC Attribution, cohérent avec une collection "downloadable" gratuite, mais
pas une preuve suffisante en soi).

Contrairement au zébu (zebu_realiste.py), le matériau embarqué dans le glb
semble correct dès l'import (vérifié par rendu Blender direct, sans
reconstruction de matériau : pelage brun correctement visible, pas de rendu
gris/métallique) — conservé tel quel, pas de reconstruction PBR manuelle ici.

Mesure de la longueur : même piège que le zébu (bounds bruts non fiables,
l'axe nez-queue du maillage source n'est pas forcément aligné sur un axe
particulier, et les cornes — très longues chez l'Ankole — faussent encore
plus le calcul qu'un simple écart d'orientation). Percentile 5-95 sur X et Y
pour exclure les extrémités (bout des cornes, queue) plutôt que le min/max
brut, qui avait donné une bête de plusieurs mètres de haut sur le zébu.

Exécution : headless :
  blender --background --python ankole_cattle.py
"""

import bpy
import os
import numpy as np

CHEMIN_GLB = os.path.join(os.path.dirname(os.path.abspath(__file__)), "source_cc0", "source", "Ankole cattle.glb")
LONGUEUR_CORPS_CIBLE = 2.4  # m, nez-queue (hors cornes) — l'Ankole est un peu plus grand que le Brahman zébu (2.2 m)


def importer():
    bpy.ops.import_scene.gltf(filepath=CHEMIN_GLB)
    tous = list(bpy.context.selected_objects)
    if not tous:
        raise RuntimeError("Aucun objet importé depuis Ankole cattle.glb.")
    return tous


def racine_visuelle(tous):
    for obj in tous:
        if obj.parent is None:
            return obj
    raise RuntimeError("Aucune racine trouvée.")


def mesurer_longueur_robuste(tous):
    """Étendue horizontale (X ou Y, la plus grande) en excluant les 5% extrêmes de
    chaque côté — écarte le bout des cornes/queue sans dépendre de savoir sur quel
    axe elles pointent."""
    coords = []
    for obj in tous:
        if obj.type != 'MESH':
            continue
        bpy.context.view_layer.update()
        for v in obj.data.vertices:
            coords.append(obj.matrix_world @ v.co)
    xs = np.array([c.x for c in coords])
    ys = np.array([c.y for c in coords])
    etendue_x = np.percentile(xs, 95) - np.percentile(xs, 5)
    etendue_y = np.percentile(ys, 95) - np.percentile(ys, 5)
    return max(etendue_x, etendue_y)


def mettre_a_lechelle(tous, racine, longueur_cible):
    longueur_actuelle = mesurer_longueur_robuste(tous)
    if longueur_actuelle <= 0:
        raise RuntimeError("Bounds dégénérés.")
    echelle = longueur_cible / longueur_actuelle
    racine.scale = (echelle, echelle, echelle)
    bpy.context.view_layer.update()

    zs = []
    for obj in tous:
        if obj.type != 'MESH':
            continue
        for v in obj.data.vertices:
            zs.append((obj.matrix_world @ v.co).z)
    racine.location.z -= min(zs)  # pattes au sol
    print(f"  Longueur mesurée avant échelle : {longueur_actuelle:.3f} -> échelle {echelle:.4f}")


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
            export_apply=True,
            export_yup=True,
        )
        print(f"[ankole_cattle.py] Exporté -> {chemin}")


if __name__ == "__main__":
    if bpy.app.background:
        bpy.ops.wm.read_factory_settings(use_empty=True)

    tous = importer()
    racine = racine_visuelle(tous)
    print(f"--- Ankole cattle ---")
    print(f"  {len(tous)} objet(s) importé(s), racine = {racine.name}")

    mettre_a_lechelle(tous, racine, LONGUEUR_CORPS_CIBLE)

    if bpy.app.background:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        repo_root = os.path.abspath(os.path.join(script_dir, "..", ".."))
        # Même piège que guerrier_africain.py : exporter seulement la racine ne suffit
        # pas, il faut tous les objets importés explicitement sélectionnés.
        exporter_glb(tous, [
            os.path.join(script_dir, "AnkoleCattle.glb"),
            os.path.join(repo_root, "unity", "wuro-galle-vr", "Assets", "Models", "Troupeau", "AnkoleCattle.glb"),
        ])

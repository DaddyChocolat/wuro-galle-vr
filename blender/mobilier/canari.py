"""
Canari (jarre à eau en terre cuite) — Wuro & Galle

Même technique que generer_mobilier.py : "tour de potier numérique" (spin
d'un profil 2D autour de l'axe Z). Objet manquant jusqu'ici alors qu'il est
central à l'interaction "boire de l'eau" — présent aussi bien au campement
(à l'ombre de la suudu) qu'à la concession (près de l'entrée, geste
d'hospitalité traditionnel : offrir de l'eau à l'arrivant).

Forme : panse large et basse (le canari est posé au sol ou sur un support
bas, pas suspendu comme la calebasse), col resserré, bord évasé assez large
pour y plonger une calebasse et puiser. Échelle réelle : environ 40 cm de
haut, ventre ~44 cm de diamètre — dimensions d'un canari domestique courant
(pas la grande jarre de stockage collective, plus haute).

Exécution interactive (identique aux autres scripts du dossier) :
  1. Onglet "Scripting".
  2. Open > ce fichier > Run Script (Alt+P).
  3. L'objet "Canari" apparaît, matériau terre cuite déjà assigné.
  4. Export manuel : File > Export > glTF 2.0, voir README.md.

Exécution headless (génère + exporte automatiquement, utilisé pour produire
Canari.glb sans repasser par l'interface) :
  blender --background --python canari.py
Dans ce mode uniquement (bpy.app.background == True), le script part d'une
scène vide et écrit directement le résultat dans blender/mobilier/Canari.glb
ET dans unity/wuro-galle-vr/Assets/Models/Mobilier/Canari.glb (les deux
copies existantes pour Calebasse/Mortier/Pilon/Natte). En mode interactif,
rien n'est écrasé ni exporté automatiquement — comportement identique aux
autres scripts du dossier.
"""

import bpy
import bmesh
import math
import os

SEGMENTS_REVOLUTION = 16


def creer_mesh_depuis_profil(nom, points_profil, segments=SEGMENTS_REVOLUTION):
    """Identique à generer_mobilier.py — dupliqué ici pour que ce fichier
    reste exécutable seul (headless ou GUI) sans dépendre d'un import relatif."""
    mesh = bpy.data.meshes.new(nom)
    bm = bmesh.new()

    verts_profil = [bm.verts.new((r, 0, h)) for (r, h) in points_profil]
    for i in range(len(verts_profil) - 1):
        bm.edges.new((verts_profil[i], verts_profil[i + 1]))

    bmesh.ops.spin(
        bm,
        geom=bm.verts[:] + bm.edges[:],
        cent=(0, 0, 0),
        axis=(0, 0, 1),
        angle=math.radians(360),
        steps=segments,
        use_duplicate=False,
    )
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0001)
    bm.normal_update()

    bm.to_mesh(mesh)
    bm.free()

    obj = bpy.data.objects.new(nom, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def creer_canari():
    """
    Canari : panse large et basse, col resserré, bord évasé large (assez
    pour y plonger une calebasse). Base légèrement arrondie mais stable au
    sol (pas de pointe, contrairement à un profil calebasse classique).
    """
    profil = [
        (0.00, 0.00),
        (0.10, 0.00),   # base large, stable au sol (posé directement, pas de pointe)
        (0.17, 0.02),
        (0.21, 0.08),
        (0.22, 0.16),   # ventre le plus large
        (0.20, 0.24),
        (0.15, 0.32),   # resserrement net du col
        (0.11, 0.36),
        (0.13, 0.40),   # bord évasé, large ouverture pour puiser
        (0.00, 0.40),
    ]
    return creer_mesh_depuis_profil("Canari", profil)


def creer_materiau_terre_cuite():
    """Terre cuite non émaillée : brun-rouge mat, grain procédural marqué
    (surface non lissée, cuite au four traditionnel — pas de vernis)."""
    mat = bpy.data.materials.new(name="Canari_Terre_Cuite")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    sortie = nodes.new("ShaderNodeOutputMaterial")
    sortie.location = (400, 0)

    principled = nodes.new("ShaderNodeBsdfPrincipled")
    principled.location = (100, 0)
    principled.inputs["Base Color"].default_value = (0.55, 0.28, 0.17, 1.0)  # brun-rouge argile cuite
    principled.inputs["Roughness"].default_value = 0.8

    bruit = nodes.new("ShaderNodeTexNoise")
    bruit.location = (-400, -200)
    bruit.inputs["Scale"].default_value = 25.0

    bump = nodes.new("ShaderNodeBump")
    bump.location = (-150, -200)
    bump.inputs["Strength"].default_value = 0.2  # grain plus marqué que la calebasse : argile brute

    links.new(bruit.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], principled.inputs["Normal"])
    links.new(principled.outputs["BSDF"], sortie.inputs["Surface"])

    return mat


def rapport_triangles(obj):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    eval_obj = obj.evaluated_get(depsgraph)
    mesh_eval = eval_obj.to_mesh()
    mesh_eval.calc_loop_triangles()
    n_tris = len(mesh_eval.loop_triangles)
    eval_obj.to_mesh_clear()
    print(f"\n--- Budget triangles (Canari) ---\n  {obj.name:12s} : {n_tris:5d} triangles\n")
    return n_tris


def exporter_glb(chemins):
    """Exporte l'objet actif (sélection courante) vers chaque chemin donné."""
    for chemin in chemins:
        os.makedirs(os.path.dirname(chemin), exist_ok=True)
        bpy.ops.export_scene.gltf(
            filepath=chemin,
            export_format='GLB',
            use_selection=True,
            export_apply=True,
            export_yup=True,
        )
        print(f"[canari.py] Exporté -> {chemin}")


if __name__ == "__main__":
    if bpy.app.background:
        # Mode headless : scène vide garantie, pas d'objet parasite (ex. Cube
        # de démarrage) dans l'export — comportement non destructif pour
        # l'utilisateur puisqu'aucun .blend ouvert n'est modifié/écrasé.
        bpy.ops.wm.read_factory_settings(use_empty=True)

    canari = creer_canari()
    bpy.ops.object.shade_smooth()

    mat = creer_materiau_terre_cuite()
    canari.data.materials.clear()
    canari.data.materials.append(mat)

    rapport_triangles(canari)

    if bpy.app.background:
        bpy.ops.object.select_all(action='DESELECT')
        canari.select_set(True)
        bpy.context.view_layer.objects.active = canari

        script_dir = os.path.dirname(os.path.abspath(__file__))
        repo_root = os.path.abspath(os.path.join(script_dir, "..", ".."))
        exporter_glb([
            os.path.join(script_dir, "Canari.glb"),
            os.path.join(repo_root, "unity", "wuro-galle-vr", "Assets", "Models", "Mobilier", "Canari.glb"),
        ])

"""
Enclos et paysage — terrain, hoggo (enclos), mare (Wuro & Galle)

Nouvelle technique : le modificateur Displace. Il déplace chaque sommet
d'un maillage verticalement selon la valeur (claire = monte, sombre =
descend) d'une texture — ici un bruit procédural — au lieu d'un plan
parfaitement plat. Combiné à une subdivision du plan (plus de sommets =
plus de points de mesure), ça donne un sol légèrement irrégulier, sans
avoir à sculpter le terrain à la main.

L'enclos (hoggo) réutilise deux techniques déjà vues : des piquets coniques
répartis en cercle (comme les segments de l'armature suudu), reliés par
deux anneaux horizontaux — des cercles de Bézier (opérateur intégré, pas
besoin de placer les points à la main comme pour les arches) avec Bevel
Depth pour l'épaisseur, comme les branches et les cornes.

La mare est une forme simple, aplatie et irrégulière, légèrement enfoncée
dans le sol.

Exécution : onglet Scripting > Open > ce fichier > Run Script.
"""

import bpy
import bmesh
import math
import os

TAILLE_TERRAIN = 26.0       # plan carré, couvre toute la scène (campement + concession)
SUBDIVISIONS_TERRAIN = 30   # résolution du plan avant déplacement
TUILAGE_SOL = 10             # nombre de répétitions de la texture sur toute la largeur du plan


def _charger_texture_reference(nom_fichier):
    """Charge une image depuis blender/textures/reference/ (même helper que materiaux_case.py)."""
    if not bpy.data.filepath:
        print(f"ATTENTION : .blend non sauvegardé — impossible de localiser {nom_fichier}.")
        return None
    dossier_blend = os.path.dirname(bpy.data.filepath)
    chemin = os.path.normpath(os.path.join(dossier_blend, "..", "textures", "reference", nom_fichier))
    if not os.path.exists(chemin):
        print(f"ATTENTION : texture introuvable : {chemin} — couleur plate utilisée à la place.")
        return None
    return bpy.data.images.load(chemin, check_existing=True)


def _tuiler_uv(obj, facteur):
    """
    Le plan par défaut n'a qu'une seule répétition de la texture sur toute sa
    largeur (26 m) — une photo de sable étirée sur 26 m serait floue et
    méconnaissable. On multiplie les coordonnées UV autour du centre pour que
    l'image se répète 'facteur' fois (le sampler Image Texture est en mode
    REPEAT par défaut, compatible glTF).
    """
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(obj.data)
    uv_layer = bm.loops.layers.uv.active
    for face in bm.faces:
        for loop in face.loops:
            uv = loop[uv_layer].uv
            loop[uv_layer].uv = ((uv.x - 0.5) * facteur + 0.5, (uv.y - 0.5) * facteur + 0.5)
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.object.mode_set(mode='OBJECT')

DECALAGE_ENCLOS = (-12.0, 0.0)
RAYON_ENCLOS = 4.0
NOMBRE_PIQUETS = 14
HAUTEUR_PIQUET = 1.1

DECALAGE_MARE = (-12.0, 5.5)


def creer_terrain():
    """Plan subdivisé + Displace (bruit procédural) pour un sol légèrement irrégulier."""
    bpy.ops.mesh.primitive_plane_add(size=TAILLE_TERRAIN, location=(0, 0, 0))
    terrain = bpy.context.active_object
    terrain.name = "Terrain"

    # Subdivision : sans ça, le plan n'a que 4 sommets et Displace n'a
    # presque rien à déplacer.
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.subdivide(number_cuts=SUBDIVISIONS_TERRAIN)
    bpy.ops.object.mode_set(mode='OBJECT')

    texture_bruit = bpy.data.textures.new("Terrain_Bruit", type='CLOUDS')
    texture_bruit.noise_scale = 3.0

    displace = terrain.modifiers.new(name="Irregularites", type='DISPLACE')
    displace.texture = texture_bruit
    displace.strength = 0.15  # faible : irrégularités de terrain, pas des collines

    bpy.context.view_layer.objects.active = terrain
    terrain.select_set(True)
    bpy.ops.object.modifier_apply(modifier=displace.name)
    bpy.ops.object.shade_smooth()

    _tuiler_uv(terrain, TUILAGE_SOL)

    mat_sol = bpy.data.materials.new(name="Sol_Laterite")
    mat_sol.use_nodes = True
    principled = mat_sol.node_tree.nodes["Principled BSDF"]
    principled.inputs["Roughness"].default_value = 0.95

    # Texture réelle (sable_ref.jpg, déjà extraite du corpus iconographique
    # Livrable 1 mais pas encore utilisée) plutôt qu'une couleur plate : même
    # logique que les matériaux des cases (materiaux_case.py) — reliée
    # directement aux UV du mesh, sans nœud Mapping/Generated (non exportable
    # en glTF). Retombe sur la couleur latérite d'origine si l'image manque.
    image_sol = _charger_texture_reference("sable_ref.jpg")
    if image_sol is not None:
        tex_sol = mat_sol.node_tree.nodes.new("ShaderNodeTexImage")
        tex_sol.location = (-300, 300)
        tex_sol.image = image_sol
        mat_sol.node_tree.links.new(tex_sol.outputs["Color"], principled.inputs["Base Color"])
    else:
        principled.inputs["Base Color"].default_value = (0.55, 0.28, 0.16, 1.0)

    terrain.data.materials.append(mat_sol)

    return terrain


def creer_enclos():
    """Piquets coniques en cercle + 2 anneaux horizontaux (cercles de Bézier bevelés)."""
    cx, cy = DECALAGE_ENCLOS
    piquets = []

    for i in range(NOMBRE_PIQUETS):
        angle = 2 * math.pi * i / NOMBRE_PIQUETS
        x = cx + RAYON_ENCLOS * math.cos(angle)
        y = cy + RAYON_ENCLOS * math.sin(angle)
        bpy.ops.mesh.primitive_cone_add(
            vertices=6, radius1=0.04, radius2=0.015, depth=HAUTEUR_PIQUET,
            location=(x, y, HAUTEUR_PIQUET / 2),
        )
        piquet = bpy.context.active_object
        piquet.name = f"Hoggo_Piquet_{i + 1}"
        piquets.append(piquet)

    anneaux = []
    for i, h in enumerate([0.4, 0.85]):
        bpy.ops.curve.primitive_bezier_circle_add(radius=RAYON_ENCLOS, location=(cx, cy, h))
        anneau = bpy.context.active_object
        anneau.name = f"Hoggo_Anneau_{i + 1}"
        anneau.data.bevel_depth = 0.02
        anneau.data.bevel_resolution = 2
        bpy.ops.object.convert(target='MESH')
        anneaux.append(anneau)

    mat_bois = bpy.data.materials.new(name="Enclos_Bois_epineux")
    mat_bois.use_nodes = True
    mat_bois.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.32, 0.22, 0.12, 1.0)
    mat_bois.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.85
    for obj in piquets + anneaux:
        obj.data.materials.append(mat_bois)

    return piquets + anneaux


def creer_mare():
    """Forme aplatie et irrégulière, légèrement enfoncée dans le sol."""
    bpy.ops.mesh.primitive_ico_sphere_add(
        subdivisions=2, radius=1.3,
        location=(DECALAGE_MARE[0], DECALAGE_MARE[1], -0.08),
    )
    mare = bpy.context.active_object
    mare.name = "Mare"
    mare.scale = (1.4, 1.0, 0.12)  # aplatie et légèrement ovale

    mat_eau = bpy.data.materials.new(name="Eau_Mare")
    mat_eau.use_nodes = True
    principled = mat_eau.node_tree.nodes["Principled BSDF"]
    principled.inputs["Base Color"].default_value = (0.15, 0.22, 0.2, 1.0)
    principled.inputs["Roughness"].default_value = 0.1  # eau = surface plutôt lisse
    mare.data.materials.append(mat_eau)

    return mare


def rapport_triangles(objets):
    print("\n--- Budget triangles (enclos et paysage) ---")
    total = 0
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objets:
        eval_obj = obj.evaluated_get(depsgraph)
        mesh_eval = eval_obj.to_mesh()
        mesh_eval.calc_loop_triangles()
        n_tris = len(mesh_eval.loop_triangles)
        eval_obj.to_mesh_clear()
        total += n_tris
        print(f"  {obj.name:18s} : {n_tris:5d} triangles")
    print(f"  {'TOTAL':18s} : {total:5d} triangles")
    print("  (budget scène complète : 80 000 à 120 000 triangles)\n")


if __name__ == "__main__":
    terrain = creer_terrain()
    enclos = creer_enclos()
    mare = creer_mare()

    rapport_triangles([terrain] + enclos + [mare])

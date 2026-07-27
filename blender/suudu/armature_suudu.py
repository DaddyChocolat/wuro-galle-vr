"""
Armature de la suudu — structure en branches arquées (Wuro & Galle)

Nouvelle technique : les Curve Objects (objets courbe). Une courbe de
Bézier est définie par quelques points de contrôle ; Blender calcule seul
une trajectoire lisse qui les relie. Idéal pour une branche courbée : 3
points (base, sommet, base opposée) suffisent à générer l'arche. Le
paramètre Bevel Depth transforme ensuite cette ligne mathématique en tube,
pour simuler l'épaisseur réelle de la branche.

La suudu est modélisée comme une base circulaire sur laquelle plusieurs
arches identiques sont plantées, chacune traversant le sommet du dôme et
tournée d'un angle différent autour de l'axe central — comme les méridiens
d'un globe. C'est la technique de construction réelle d'une armature en
branches courbées plantées en cercle.

Non couvert par ce script (à ajouter dans une passe de détail ultérieure) :
les nœuds de fixation en corde aux points de croisement des arches.

Exécution : onglet Scripting > Open > ce fichier > Run Script.
"""

import bpy
import math

# --- Dimensions réelles (mètres) ---
RAYON_BASE = 1.3          # rayon au sol (diamètre ~2,6 m)
HAUTEUR_DOME = 1.6         # hauteur au sommet
NOMBRE_ARCHES = 6          # branches arquées réparties autour du dôme
EPAISSEUR_BRANCHE = 0.02   # rayon du tube = branche d'environ 4 cm de diamètre


def creer_arche(nom, rayon, hauteur):
    """
    Une branche arquée = une courbe de Bézier à 3 points (départ au sol,
    sommet du dôme, arrivée au sol de l'autre côté). Les poignées 'AUTO'
    laissent Blender calculer une courbure lisse et naturelle entre eux,
    sans qu'on ait à la régler à la main.
    """
    courbe_data = bpy.data.curves.new(nom, type='CURVE')
    courbe_data.dimensions = '3D'
    courbe_data.resolution_u = 8  # segments entre chaque point de contrôle

    spline = courbe_data.splines.new('BEZIER')
    spline.bezier_points.add(2)  # 1 point déjà présent + 2 ajoutés = 3 au total

    points = [(-rayon, 0, 0), (0, 0, hauteur), (rayon, 0, 0)]
    for i, (x, y, z) in enumerate(points):
        p = spline.bezier_points[i]
        p.co = (x, y, z)
        p.handle_left_type = 'AUTO'
        p.handle_right_type = 'AUTO'

    courbe_data.bevel_depth = EPAISSEUR_BRANCHE
    courbe_data.bevel_resolution = 3  # section du tube peu détaillée (branche vue de loin)

    obj = bpy.data.objects.new(nom, courbe_data)
    bpy.context.collection.objects.link(obj)
    return obj


def creer_armature_suudu():
    """
    Génère toutes les arches réparties autour de l'axe vertical. Chaque
    arche traverse le sommet du dôme en entier (comme un méridien) : il
    suffit donc de répartir les angles sur 180°, pas 360°, pour couvrir
    tout le tour sans dupliquer deux fois la même arche.
    """
    arches = []
    for i in range(NOMBRE_ARCHES):
        angle = math.pi * i / NOMBRE_ARCHES
        arche = creer_arche(f"Suudu_Arche_{i + 1}", RAYON_BASE, HAUTEUR_DOME)
        arche.rotation_euler = (0, 0, angle)
        arches.append(arche)
    return arches


def convertir_en_mesh(objets):
    """
    Convertit les courbes en mesh — nécessaire avant l'export GLTF, qui ne
    lit pas les objets courbe de Blender directement.
    """
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objets:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objets[0]
    bpy.ops.object.convert(target='MESH')


def rapport_triangles(objets):
    print("\n--- Budget triangles (armature suudu) ---")
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
    print("  (budget max recommandé pour ce module : 20 000 triangles)\n")


if __name__ == "__main__":
    arches = creer_armature_suudu()
    convertir_en_mesh(arches)
    rapport_triangles(arches)

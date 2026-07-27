"""
Génération procédurale des objets domestiques simples — Wuro & Galle
Calebasse, mortier, pilon, natte.

Technique principale : le "spin" (tour de potier numérique). On décrit un
profil 2D — une suite de points qui trace le contour de l'objet vu en coupe,
d'un seul côté de l'axe central — puis on fait tourner ce profil à 360°
autour de l'axe vertical Z pour générer la forme 3D complète. C'est la
méthode standard pour tout objet à symétrie de révolution : calebasse,
mortier, pilon, pot, vase. La natte, elle, n'est pas un objet de révolution :
elle est traitée séparément comme un simple disque aplati.

À exécuter dans Blender :
  1. Onglet "Scripting" (en haut de l'écran).
  2. Ouvrir ce fichier (Open > naviguer jusqu'à ce script).
  3. Bouton "Run Script" (triangle play en haut de l'éditeur de texte),
     ou Alt+P avec le curseur dans le script.
  4. Les 4 objets apparaissent dans la scène 3D.
  5. Le nombre de triangles de chacun s'affiche dans la Console système
     (Window > Toggle System Console, sous Windows).
"""

import bpy
import bmesh
import math


# Nombre de segments autour de l'axe de révolution.
# 16 segments = suffisant pour un objet tenu en main, regardé de près en VR,
# sans peser sur le budget triangles. Ne monter à 24-32 que si l'objet devient
# un point focal rapproché de la scène.
SEGMENTS_REVOLUTION = 16


def creer_mesh_depuis_profil(nom, points_profil, segments=SEGMENTS_REVOLUTION):
    """
    Crée un objet 3D par révolution d'un profil 2D autour de l'axe Z.

    points_profil : liste de tuples (rayon, hauteur), du bas vers le haut,
                     décrivant le contour de l'objet vu en coupe.
                     Exemple : (0.10, 0.06) = 10 cm de l'axe, 6 cm de haut.
                     Un profil qui commence et finit avec rayon = 0.0 se
                     referme proprement en pointe (base et sommet pincés).
    """
    mesh = bpy.data.meshes.new(nom)
    bm = bmesh.new()

    # Ligne de profil : une suite de sommets reliés par des arêtes.
    verts_profil = [bm.verts.new((r, 0, h)) for (r, h) in points_profil]
    for i in range(len(verts_profil) - 1):
        bm.edges.new((verts_profil[i], verts_profil[i + 1]))

    # bmesh.ops.spin fait tourner cette géométrie de 360° autour de l'axe Z,
    # en générant "segments" copies intermédiaires reliées entre elles :
    # le résultat est un volume plein, refermé automatiquement sur lui-même.
    bmesh.ops.spin(
        bm,
        geom=bm.verts[:] + bm.edges[:],
        cent=(0, 0, 0),
        axis=(0, 0, 1),
        angle=math.radians(360),
        steps=segments,
        use_duplicate=False,
    )

    # Soude les sommets dupliqués à la jonction de fin de révolution (la
    # "couture" à 360°), sinon la surface resterait ouverte à cet endroit.
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0001)
    bm.normal_update()

    bm.to_mesh(mesh)
    bm.free()

    obj = bpy.data.objects.new(nom, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def creer_calebasse():
    """
    Calebasse : forme bulbeuse avec un col resserré et un petit bord évasé.
    Échelle réelle : environ 18 cm de haut.
    """
    profil = [
        (0.00, 0.00),
        (0.06, 0.00),
        (0.09, 0.02),
        (0.10, 0.06),   # ventre le plus large
        (0.09, 0.10),
        (0.06, 0.14),   # resserrement du col
        (0.045, 0.16),
        (0.05, 0.18),   # petit bord évasé de l'ouverture
        (0.00, 0.18),
    ]
    return creer_mesh_depuis_profil("Calebasse", profil)


def creer_mortier():
    """
    Mortier : base large et stable, paroi épaisse, creux conique peu profond
    au sommet. Échelle réelle : environ 35 cm de haut.
    """
    profil = [
        (0.00, 0.00),
        (0.14, 0.00),   # base large, stable au sol
        (0.15, 0.03),
        (0.13, 0.10),
        (0.12, 0.25),
        (0.14, 0.32),
        (0.15, 0.35),   # bord extérieur au sommet
        (0.10, 0.33),   # bord intérieur du creux (paroi épaisse)
        (0.04, 0.20),   # paroi intérieure du creux, en cône
        (0.00, 0.15),   # fond du creux
    ]
    return creer_mesh_depuis_profil("Mortier", profil)


def creer_pilon():
    """
    Pilon : long manche avec une extrémité renflée pour piler.
    Échelle réelle : environ 90 cm de long, construit debout sur l'axe Z
    (à coucher dans la scène si besoin, selon la mise en situation).
    """
    profil = [
        (0.00, 0.00),
        (0.035, 0.00),  # extrémité renflée, pour piler
        (0.04, 0.05),
        (0.025, 0.12),  # resserrement vers le manche
        (0.022, 0.65),  # manche, quasi cylindrique
        (0.028, 0.80),  # léger renflement de préhension
        (0.02, 0.88),
        (0.00, 0.90),
    ]
    # Objet fin et allongé : moins de segments suffisent, pas besoin de 16.
    return creer_mesh_depuis_profil("Pilon", profil, segments=12)


def creer_natte():
    """
    Natte (sekko) : natte tressée plate et ovale, épaisseur fine.
    Pas un objet de révolution — un simple disque aplati. Le motif tressé
    sera géré en texture (Shader Editor, étape suivante), pas en géométrie,
    pour rester très léger en triangles.
    """
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=20,
        radius=0.55,
        depth=0.015,  # 1,5 cm d'épaisseur
        location=(0, 0, 0),
    )
    obj = bpy.context.active_object
    obj.name = "Natte"
    obj.scale = (1.0, 0.7, 1.0)  # ovale plutôt que cercle parfait
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return obj


def rapport_triangles(objets):
    """
    Affiche le nombre de triangles de chaque objet dans la Console système,
    pour vérifier le budget avant de passer à la texturing (Shader Editor).
    """
    print("\n--- Budget triangles (objets domestiques) ---")
    total = 0
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objets:
        eval_obj = obj.evaluated_get(depsgraph)
        mesh_eval = eval_obj.to_mesh()
        mesh_eval.calc_loop_triangles()
        n_tris = len(mesh_eval.loop_triangles)
        eval_obj.to_mesh_clear()
        total += n_tris
        print(f"  {obj.name:12s} : {n_tris:5d} triangles")
    print(f"  {'TOTAL':12s} : {total:5d} triangles")
    print("  (budget max recommandé pour ce module : 20 000 triangles)\n")


if __name__ == "__main__":
    objets_crees = [
        creer_calebasse(),
        creer_mortier(),
        creer_pilon(),
        creer_natte(),
    ]
    rapport_triangles(objets_crees)

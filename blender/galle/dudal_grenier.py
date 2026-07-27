"""
Dudal et grenier — compléments de la concession (galle)

Deux logiques différentes, à dessein :

- Le DUDAL (espace de prière) est modélisé de façon volontairement minimale :
  une aire de terre balayée, nue, légèrement surélevée pour éviter le
  z-fighting avec le terrain. Aucune clôture ni marquage n'est ajouté, faute
  de source fiable sur leur forme précise dans le Diamaré — voir la note
  éthique. La natte de prière (module mobilier déjà modélisé, Natte.glb)
  sera posée dessus au moment de l'assemblage Unity, pas ici.

- Le GRENIER reprend la forme sahélienne courante (bac surélevé sur pilotis
  + toit conique), avec la même technique que les cases en banco
  (Solidify pour l'épaisseur du bac). Nouveauté par rapport à case_banco.py :
  les pilotis (jambes), simples cylindres fins qui surélèvent le bac du sol
  — protection traditionnelle contre l'humidité et les rongeurs.

Exécution : onglet Scripting > Open > ce fichier > Run Script.
Export ensuite chaque objet séparément en .glb (comme pour les autres
modules) : sélectionner les pièces du dudal ou du grenier, File > Export >
glTF 2.0 (.glb), "Export Selected Only" coché.
"""

import bpy
import math

# --- Dudal --------------------------------------------------------------

DUDAL_RAYON = 2.0
DUDAL_DECALAGE_X = 0.0

# --- Grenier --------------------------------------------------------------

GRENIER_RAYON_BAC = 0.6
GRENIER_HAUTEUR_BAC = 1.0
GRENIER_HAUTEUR_PILOTIS = 0.7
GRENIER_RAYON_PILOTIS = 0.06
GRENIER_NOMBRE_PILOTIS = 4
GRENIER_HAUTEUR_TOIT = 0.5
GRENIER_DECALAGE_X = 6.0  # décalé pour la revue visuelle, comme les cases


def creer_dudal():
    """Aire de terre balayée : un disque plat, légèrement surélevé du sol."""
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=32,
        radius=DUDAL_RAYON,
        depth=0.02,
        location=(DUDAL_DECALAGE_X, 0, 0.01),
    )
    dudal = bpy.context.active_object
    dudal.name = "Dudal_Sol"
    return dudal


def creer_grenier():
    """Bac cylindrique creux (Solidify) sur pilotis, toit conique de paille."""
    hauteur_bac_z = GRENIER_HAUTEUR_PILOTIS + GRENIER_HAUTEUR_BAC / 2

    # Bac : même technique que le mur des cases (tube creux + Solidify),
    # sans découpe de porte — un grenier se remplit par le haut, pas par une
    # ouverture latérale.
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=20,
        radius=GRENIER_RAYON_BAC,
        depth=GRENIER_HAUTEUR_BAC,
        location=(GRENIER_DECALAGE_X, 0, hauteur_bac_z),
    )
    bac = bpy.context.active_object
    bac.name = "Grenier_Bac"

    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_mode(type='FACE')
    bpy.ops.mesh.select_all(action='DESELECT')
    import bmesh as _bmesh
    bm = _bmesh.from_edit_mesh(bac.data)
    bm.faces.ensure_lookup_table()
    for f in bm.faces:
        if abs(f.normal.z) > 0.9:
            f.select = True
    _bmesh.update_edit_mesh(bac.data)
    bpy.ops.mesh.delete(type='FACE')
    bpy.ops.object.mode_set(mode='OBJECT')

    solidify = bac.modifiers.new(name="Epaisseur", type='SOLIDIFY')
    solidify.thickness = 0.08
    bpy.context.view_layer.objects.active = bac
    bpy.ops.object.modifier_apply(modifier=solidify.name)

    # Pilotis : répartis en cercle sous le bac.
    pilotis = []
    rayon_pilotis_cercle = GRENIER_RAYON_BAC * 0.7
    for i in range(GRENIER_NOMBRE_PILOTIS):
        angle = 2 * math.pi * i / GRENIER_NOMBRE_PILOTIS
        x = GRENIER_DECALAGE_X + rayon_pilotis_cercle * math.cos(angle)
        y = rayon_pilotis_cercle * math.sin(angle)
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=8,
            radius=GRENIER_RAYON_PILOTIS,
            depth=GRENIER_HAUTEUR_PILOTIS,
            location=(x, y, GRENIER_HAUTEUR_PILOTIS / 2),
        )
        p = bpy.context.active_object
        p.name = f"Grenier_Pilotis_{i + 1}"
        pilotis.append(p)

    # Toit conique, légèrement plus large que le bac (avant-toit), comme
    # pour les cases.
    bpy.ops.mesh.primitive_cone_add(
        vertices=20,
        radius1=GRENIER_RAYON_BAC * 1.25,
        radius2=0.0,
        depth=GRENIER_HAUTEUR_TOIT,
        location=(GRENIER_DECALAGE_X, 0, hauteur_bac_z + GRENIER_HAUTEUR_BAC / 2 + GRENIER_HAUTEUR_TOIT / 2),
    )
    toit = bpy.context.active_object
    toit.name = "Grenier_Toit"

    return [bac, toit] + pilotis


def rapport_triangles(objets, nom_module):
    print(f"\n--- Budget triangles ({nom_module}) ---")
    total = 0
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objets:
        eval_obj = obj.evaluated_get(depsgraph)
        mesh_eval = eval_obj.to_mesh()
        mesh_eval.calc_loop_triangles()
        n_tris = len(mesh_eval.loop_triangles)
        eval_obj.to_mesh_clear()
        total += n_tris
        print(f"  {obj.name:20s} : {n_tris:5d} triangles")
    print(f"  {'TOTAL':20s} : {total:5d} triangles")
    print("  (budget max recommandé pour ce module : 20 000 triangles)\n")


if __name__ == "__main__":
    dudal = creer_dudal()
    rapport_triangles([dudal], "dudal")

    grenier_objets = creer_grenier()
    rapport_triangles(grenier_objets, "grenier")

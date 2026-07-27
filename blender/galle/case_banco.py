"""
Case en banco — module réutilisable pour la concession (galle)

Deux nouvelles techniques par rapport aux scripts précédents :

- Solidify (modificateur) : donne une épaisseur réelle à une surface fine.
  Le mur est d'abord créé comme un simple tube creux (une paroi sans
  épaisseur), puis Solidify pousse une copie de cette surface vers
  l'extérieur pour simuler un mur plein de ~15 cm — sans avoir à modéliser
  séparément la face intérieure et la face extérieure.

- Boolean (modificateur, mode Difference) : soustrait un volume d'un autre,
  comme un emporte-pièce dans de la pâte. Une petite boîte ("le cutter"),
  positionnée à l'emplacement de la porte, est soustraite du mur plein pour
  y creuser une ouverture basse.

Une case = 3 objets distincts (mur, toit, porte), pour permettre des
matériaux différents ensuite (banco / paille / bois). Le script génère 3
variantes légèrement différentes (rayon et hauteur), comme prévu pour la
concession réduite (3 cases + dudal + 1 grenier).

Exécution : onglet Scripting > Open > ce fichier > Run Script.
"""

import bpy
import math

EPAISSEUR_MUR = 0.15
LARGEUR_PORTE = 0.6
HAUTEUR_PORTE = 1.1  # porte basse : on doit se baisser pour entrer

# (rayon, hauteur_mur, hauteur_toit, position_x) — 3 variantes, espacées
# pour la revue visuelle ; le placement définitif se fera à l'assemblage
# de la scène Unity, pas ici.
VARIANTES_CASES = [
    (1.4, 1.7, 1.2, 0.0),
    (1.5, 1.8, 1.3, 4.0),
    (1.6, 1.9, 1.35, 8.0),
]


def creer_mur(nom, rayon, hauteur, decalage_x):
    """Tube creux + Solidify (épaisseur) + Boolean (porte découpée)."""
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=24,
        radius=rayon,
        depth=hauteur,
        location=(decalage_x, 0, hauteur / 2),
    )
    mur = bpy.context.active_object
    mur.name = nom

    # Retire les faces du haut et du bas : seule la paroi latérale doit
    # rester, pour que Solidify épaississe un mur, pas un cylindre plein.
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_mode(type='FACE')
    bpy.ops.mesh.select_all(action='DESELECT')
    import bmesh as _bmesh
    bm = _bmesh.from_edit_mesh(mur.data)
    bm.faces.ensure_lookup_table()
    for f in bm.faces:
        # Les faces de bouchon (haut/bas) ont une normale quasi verticale.
        if abs(f.normal.z) > 0.9:
            f.select = True
    _bmesh.update_edit_mesh(mur.data)
    bpy.ops.mesh.delete(type='FACE')
    bpy.ops.object.mode_set(mode='OBJECT')

    solidify = mur.modifiers.new(name="Epaisseur", type='SOLIDIFY')
    solidify.thickness = EPAISSEUR_MUR

    # Cutter de porte : une boîte basse, positionnée au bord du mur, côté +Y.
    bpy.ops.mesh.primitive_cube_add(
        size=1,
        location=(decalage_x, rayon, HAUTEUR_PORTE / 2),
    )
    cutter = bpy.context.active_object
    cutter.name = f"{nom}_cutter_temp"
    cutter.scale = (LARGEUR_PORTE, EPAISSEUR_MUR * 4, HAUTEUR_PORTE)

    boolean = mur.modifiers.new(name="Porte", type='BOOLEAN')
    boolean.operation = 'DIFFERENCE'
    boolean.object = cutter

    # Applique les deux modificateurs dans l'ordre (épaisseur d'abord, pour
    # que la découpe de porte traverse un mur déjà épaissi).
    bpy.context.view_layer.objects.active = mur
    bpy.ops.object.modifier_apply(modifier=solidify.name)
    bpy.ops.object.modifier_apply(modifier=boolean.name)

    bpy.data.objects.remove(cutter, do_unlink=True)

    return mur


def creer_toit(nom, rayon, hauteur_mur, hauteur_toit, decalage_x):
    """Toit conique, légèrement plus large que le mur pour former un avant-toit."""
    bpy.ops.mesh.primitive_cone_add(
        vertices=24,
        radius1=rayon * 1.15,
        radius2=0.0,
        depth=hauteur_toit,
        location=(decalage_x, 0, hauteur_mur + hauteur_toit / 2),
    )
    toit = bpy.context.active_object
    toit.name = nom
    return toit


def creer_porte(nom, rayon, decalage_x):
    """Porte basse en bois : un simple panneau plat dans l'ouverture."""
    bpy.ops.mesh.primitive_cube_add(
        size=1,
        location=(decalage_x, rayon + EPAISSEUR_MUR * 0.5, HAUTEUR_PORTE / 2),
    )
    porte = bpy.context.active_object
    porte.name = nom
    porte.scale = (LARGEUR_PORTE * 0.9, 0.03, HAUTEUR_PORTE * 0.95)
    return porte


def creer_case(index, rayon, hauteur_mur, hauteur_toit, decalage_x):
    mur = creer_mur(f"Case{index}_Mur", rayon, hauteur_mur, decalage_x)
    toit = creer_toit(f"Case{index}_Toit", rayon, hauteur_mur, hauteur_toit, decalage_x)
    porte = creer_porte(f"Case{index}_Porte", rayon, decalage_x)
    return [mur, toit, porte]


def rapport_triangles(objets):
    print("\n--- Budget triangles (cases en banco) ---")
    total = 0
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objets:
        eval_obj = obj.evaluated_get(depsgraph)
        mesh_eval = eval_obj.to_mesh()
        mesh_eval.calc_loop_triangles()
        n_tris = len(mesh_eval.loop_triangles)
        eval_obj.to_mesh_clear()
        total += n_tris
        print(f"  {obj.name:14s} : {n_tris:5d} triangles")
    print(f"  {'TOTAL':14s} : {total:5d} triangles")
    print("  (budget max recommandé pour ce module : 20 000 triangles)\n")


if __name__ == "__main__":
    tous_objets = []
    for i, (rayon, h_mur, h_toit, dx) in enumerate(VARIANTES_CASES, start=1):
        tous_objets.extend(creer_case(i, rayon, h_mur, h_toit, dx))
    rapport_triangles(tous_objets)

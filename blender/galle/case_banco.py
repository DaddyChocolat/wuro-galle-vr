"""
Case en banco — module réutilisable pour la concession (galle)

Techniques :

- Solidify (modificateur) : donne une épaisseur réelle à une surface fine.
  Le mur est d'abord créé comme un simple tube creux (une paroi sans
  épaisseur), puis Solidify pousse une copie de cette surface vers
  l'extérieur pour simuler un mur plein de ~15 cm — sans avoir à modéliser
  séparément la face intérieure et la face extérieure.

- Boolean (modificateur, mode Difference) : soustrait un volume d'un autre,
  comme un emporte-pièce dans de la pâte. Une petite boîte ("le cutter"),
  positionnée à l'emplacement de la porte, est soustraite du mur plein pour
  y creuser une ouverture basse.

- Displace (modificateur) : déforme la GÉOMÉTRIE réelle (pas juste l'ombrage
  comme un Bump shader) à partir d'une texture procédurale (Clouds). Contrai-
  rement au Bump, ça se voit sur la silhouette et au rasant — nécessite une
  subdivision préalable (bpy.ops.mesh.subdivide) pour avoir assez de
  géométrie à déformer.

- Bevel (modificateur) : arrondit légèrement les arêtes vives. Un bord
  mathématiquement parfait se voit immédiatement comme "fabriqué en CAO" ;
  même un arrondi de quelques millimètres suffit à casser cet effet.

- vertex_random (opérateur) : jitter aléatoire de certains sommets (ici
  l'anneau de base du toit) pour casser un cercle parfait — silhouette de
  chaume irrégulière plutôt qu'un cône géométrique net.

Une case = 3 objets distincts (mur, toit, porte), pour permettre des
matériaux différents ensuite (banco / paille / bois). Le script génère 3
variantes légèrement différentes (rayon et hauteur), comme prévu pour la
concession réduite (3 cases + dudal + 1 grenier).

Exécution : onglet Scripting > Open > ce fichier > Run Script.
Vérification après coup : lis le rapport de triangles imprimé en Console
(budget max 20 000 triangles pour tout le module) et regarde le résultat en
mode d'affichage "Rendered" (pas Solid) pour juger du relief réel.
"""

import bpy
import math
import bmesh

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


def ajouter_displacement(obj, force, echelle, seed):
    """
    Relief de surface réel (géométrie déformée, pas un Bump shader) via une
    texture procédurale Clouds. 'force' est en mètres (amplitude du relief),
    'echelle' contrôle la taille des irrégularités (plus petit = grain plus
    fin), 'seed' évite que toutes les variantes aient exactement le même motif.
    """
    tex = bpy.data.textures.new(f"{obj.name}_bruit_disp", type='CLOUDS')
    tex.noise_scale = echelle
    tex.noise_depth = 2
    tex.noise_basis = 'ORIGINAL_PERLIN'

    mod = obj.modifiers.new(name="Relief", type='DISPLACE')
    mod.texture = tex
    mod.strength = force
    mod.mid_level = 0.5
    mod.texture_coords = 'GLOBAL'  # évite un motif qui suit la déformation d'un coup

    return mod


def ajouter_bevel(obj, largeur=0.012, segments=2):
    """Arrondi léger des arêtes — évite l'effet "CAO" d'arêtes parfaitement nettes."""
    mod = obj.modifiers.new(name="Arrondi", type='BEVEL')
    mod.width = largeur
    mod.segments = segments
    mod.limit_method = 'ANGLE'
    mod.angle_limit = math.radians(45)
    return mod


def creer_mur(nom, rayon, hauteur, decalage_x, seed=0):
    """Tube creux, subdivisé + déformé (relief réel), puis Solidify (épaisseur) + Boolean (porte)."""
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
    bm = bmesh.from_edit_mesh(mur.data)
    bm.faces.ensure_lookup_table()
    for f in bm.faces:
        # Les faces de bouchon (haut/bas) ont une normale quasi verticale.
        if abs(f.normal.z) > 0.9:
            f.select = True
    bmesh.update_edit_mesh(mur.data)
    bpy.ops.mesh.delete(type='FACE')

    # Subdivision : la paroi n'avait qu'un seul niveau vertical (24 quads) —
    # pas assez de géométrie pour qu'un vrai relief (Displace) se voie.
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.subdivide(number_cuts=2)
    bpy.ops.object.mode_set(mode='OBJECT')

    deplacement = ajouter_displacement(mur, force=0.035, echelle=3.0, seed=seed)

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

    bevel = ajouter_bevel(mur, largeur=0.012)

    # Applique tous les modificateurs dans l'ordre de la pile (relief, puis
    # épaisseur, puis découpe de porte, puis arrondi des arêtes).
    bpy.context.view_layer.objects.active = mur
    bpy.ops.object.modifier_apply(modifier=deplacement.name)
    bpy.ops.object.modifier_apply(modifier=solidify.name)
    bpy.ops.object.modifier_apply(modifier=boolean.name)
    bpy.ops.object.modifier_apply(modifier=bevel.name)

    bpy.data.objects.remove(cutter, do_unlink=True)

    return mur


def creer_toit(nom, rayon, hauteur_mur, hauteur_toit, decalage_x, seed=0):
    """
    Toit conique, légèrement plus large que le mur pour former un avant-toit.
    Bord de base irrégulier (frange de chaume, pas un cercle parfait) +
    relief de surface réel.
    """
    bpy.ops.mesh.primitive_cone_add(
        vertices=24,
        radius1=rayon * 1.15,
        radius2=0.0,
        depth=hauteur_toit,
        location=(decalage_x, 0, hauteur_mur + hauteur_toit / 2),
    )
    toit = bpy.context.active_object
    toit.name = nom

    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.subdivide(number_cuts=2)  # résolution nécessaire pour le relief + la frange

    # Frange irrégulière : jitter aléatoire de l'anneau de base (bord du
    # toit), pour casser le cercle parfait — silhouette de chaume, pas un
    # cône CAO. Sélectionne uniquement les sommets les plus bas.
    bpy.ops.mesh.select_all(action='DESELECT')
    bm = bmesh.from_edit_mesh(toit.data)
    bm.verts.ensure_lookup_table()
    z_min = min(v.co.z for v in bm.verts)
    for v in bm.verts:
        v.select = abs(v.co.z - z_min) < 0.02
    bmesh.update_edit_mesh(toit.data)

    bpy.ops.transform.vertex_random(offset=0.06, uniform=0.0, normal=0.0, seed=seed)

    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.object.mode_set(mode='OBJECT')

    deplacement = ajouter_displacement(toit, force=0.05, echelle=4.0, seed=seed + 10)
    bevel = ajouter_bevel(toit, largeur=0.01)

    bpy.context.view_layer.objects.active = toit
    bpy.ops.object.modifier_apply(modifier=deplacement.name)
    bpy.ops.object.modifier_apply(modifier=bevel.name)

    return toit


def creer_porte(nom, rayon, decalage_x):
    """Porte basse en bois : un panneau plat, arêtes légèrement arrondies."""
    bpy.ops.mesh.primitive_cube_add(
        size=1,
        location=(decalage_x, rayon + EPAISSEUR_MUR * 0.5, HAUTEUR_PORTE / 2),
    )
    porte = bpy.context.active_object
    porte.name = nom
    porte.scale = (LARGEUR_PORTE * 0.9, 0.03, HAUTEUR_PORTE * 0.95)

    # Applique l'échelle sur la géométrie AVANT le Bevel : sinon la largeur
    # du biseau serait déformée de façon non-uniforme par le scale (surtout
    # sur l'axe Y, très fin) et pourrait créer une géométrie dégénérée.
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    bevel = ajouter_bevel(porte, largeur=0.008, segments=2)
    bpy.context.view_layer.objects.active = porte
    bpy.ops.object.modifier_apply(modifier=bevel.name)

    return porte


def creer_case(index, rayon, hauteur_mur, hauteur_toit, decalage_x):
    mur = creer_mur(f"Case{index}_Mur", rayon, hauteur_mur, decalage_x, seed=index)
    toit = creer_toit(f"Case{index}_Toit", rayon, hauteur_mur, hauteur_toit, decalage_x, seed=index)
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

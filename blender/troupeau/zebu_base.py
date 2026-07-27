"""
Zébu — block-out low-poly par metaballs (Wuro & Galle)

Reprise après un premier essai raté : des sphères simplement jointes
laissent des jointures dures visibles (ça ressemble à des balles collées,
pas à un animal). Les metaballs corrigent ça : chaque élément influence
l'espace autour de lui, et la surface finale se calcule comme une fusion
lisse et continue de tous les éléments qui se chevauchent — exactement
l'outil fait pour bloquer une silhouette organique (corps + bosse + cou
qui se raccordent naturellement, sans couture).

Fonctionnement : on crée un objet Metaball, on lui ajoute des éléments
(ellipsoïdes) positionnés et dimensionnés comme des "os" de la silhouette
(torse, bosse, cou, tête, fanon), puis on convertit le tout en mesh classique
une fois la forme obtenue — c'est cette conversion qui "fige" la fusion en
géométrie exportable.

Proportions ciblées (zébu à bosse, type Fulani) : ~2,2 m du nez à la base
de la queue, ~1,4-1,5 m au garrot (bosse comprise), bosse nettement visible
au-dessus de la ligne du dos, cou court et épais, fanon sous le cou,
cornes en lyre.

Cornes : même technique que l'armature suudu (courbe de Bézier + Bevel
Depth), avec un profil affiné pour un aspect lyre plus net.

Exécution : onglet Scripting > Open > ce fichier > Run Script.
"""

import bpy
import math

DECALAGE_X = -6.0  # à l'écart de la suudu (X=0) et des cases (X=0,4,8)
HAUTEUR_PATTES = 0.68


def creer_corps_metaball():
    """
    Corps complet (torse, bosse, cou, tête, fanon) comme fusion de
    metaballs, converti ensuite en un seul mesh lisse.
    """
    mball = bpy.data.metaballs.new("ZebuMetaData")
    mball.resolution = 0.09
    mball.render_resolution = 0.09
    obj = bpy.data.objects.new("Zebu_Corps", mball)
    obj.location = (DECALAGE_X, 0, HAUTEUR_PATTES)
    bpy.context.collection.objects.link(obj)

    def ajouter(co, taille, rigidite=2.0):
        el = mball.elements.new()
        el.co = co
        el.type = 'ELLIPSOID'
        el.size_x, el.size_y, el.size_z = taille
        el.stiffness = rigidite
        return el

    # Torse : le volume principal, position de référence (0,0,0) = centre du corps.
    ajouter((0.0, 0.0, 0.0), (0.75, 0.40, 0.40))

    # Bosse : nettement au-dessus de la ligne du dos, au tiers avant du
    # torse (au-dessus des pattes avant) — c'est LE trait distinctif du
    # zébu, elle doit se voir clairement en silhouette, pas juste affleurer.
    ajouter((0.35, 0.0, 0.30), (0.26, 0.24, 0.24), rigidite=2.2)

    # Cou : épais et court, incliné vers l'avant-haut, raccorde le torse à
    # la tête. Un seul élément large suffit, la fusion fait le reste.
    ajouter((0.95, 0.0, 0.18), (0.30, 0.19, 0.19), rigidite=2.0)

    # Tête : plus petite, légèrement allongée vers l'avant (museau).
    ajouter((1.42, 0.0, 0.22), (0.20, 0.14, 0.15), rigidite=2.2)
    ajouter((1.58, 0.0, 0.18), (0.10, 0.09, 0.09), rigidite=2.5)  # museau, affiné

    # Fanon : repli de peau sous le cou/poitrail — plat et allongé verticalement.
    ajouter((0.75, 0.0, -0.12), (0.10, 0.05, 0.16), rigidite=1.6)

    # Conversion en mesh classique (nécessaire pour l'export GLTF et pour
    # pouvoir assigner des matériaux normalement). L'objet doit être à la
    # fois sélectionné et actif : bpy.data.objects.new()/link() ne
    # sélectionne pas automatiquement, contrairement aux opérateurs
    # bpy.ops.mesh.primitive_*_add() utilisés ailleurs dans ce fichier.
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target='MESH')
    bpy.ops.object.shade_smooth()

    return obj


def creer_pattes():
    """
    4 pattes coniques (légèrement plus épaisses en haut qu'en bas, comme
    une vraie jambe), positionnées sous les masses réelles du corps :
    avant sous la bosse/cou, arrière sous l'arrière du torse.
    """
    positions = [
        (0.30, -0.28, "Zebu_Patte_AVG"),
        (0.30, 0.28, "Zebu_Patte_AVD"),
        (-0.45, -0.28, "Zebu_Patte_ARG"),
        (-0.45, 0.28, "Zebu_Patte_ARD"),
    ]
    pattes = []
    for dx, dy, nom in positions:
        bpy.ops.mesh.primitive_cone_add(
            vertices=8, radius1=0.11, radius2=0.075, depth=HAUTEUR_PATTES,
            location=(DECALAGE_X + dx, dy, HAUTEUR_PATTES / 2),
        )
        patte = bpy.context.active_object
        patte.name = nom
        bpy.ops.object.shade_smooth()
        pattes.append(patte)
    return pattes


def creer_cornes():
    """
    Cornes en lyre : courbe de Bézier à 4 points, base large qui s'affine
    nettement vers la pointe. Émergent du sommet de la tête, écartées à la
    base de la largeur du crâne.
    """
    def creer_une_corne(nom, cote):
        courbe_data = bpy.data.curves.new(nom, type='CURVE')
        courbe_data.dimensions = '3D'
        courbe_data.resolution_u = 6

        spline = courbe_data.splines.new('BEZIER')
        spline.bezier_points.add(3)

        # Profil lyre : part du crâne, monte et s'écarte, se recourbe vers
        # l'intérieur au sommet.
        points = [
            (0.0, cote * 0.10, 0.0),
            (0.0, cote * 0.22, 0.30),
            (0.0, cote * 0.30, 0.50),
            (0.0, cote * 0.18, 0.62),
        ]
        rayons = [1.0, 0.75, 0.5, 0.22]  # base épaisse -> pointe fine
        for i, (x, y, z) in enumerate(points):
            p = spline.bezier_points[i]
            p.co = (x, y, z)
            p.handle_left_type = 'AUTO'
            p.handle_right_type = 'AUTO'
            p.radius = rayons[i]

        courbe_data.bevel_depth = 0.035
        courbe_data.bevel_resolution = 3

        obj = bpy.data.objects.new(nom, courbe_data)
        obj.location = (DECALAGE_X + 1.42, 0, HAUTEUR_PATTES + 0.32)
        bpy.context.collection.objects.link(obj)
        return obj

    corne_g = creer_une_corne("Zebu_Corne_G", -1)
    corne_d = creer_une_corne("Zebu_Corne_D", 1)

    bpy.ops.object.select_all(action='DESELECT')
    corne_g.select_set(True)
    corne_d.select_set(True)
    bpy.context.view_layer.objects.active = corne_g
    bpy.ops.object.convert(target='MESH')
    bpy.ops.object.shade_smooth()

    return [corne_g, corne_d]


def creer_oreilles():
    oreilles = []
    for cote, nom in [(-1, "Zebu_Oreille_G"), (1, "Zebu_Oreille_D")]:
        bpy.ops.mesh.primitive_cone_add(
            vertices=6, radius1=0.09, radius2=0.02, depth=0.16,
            location=(DECALAGE_X + 1.30, cote * 0.20, HAUTEUR_PATTES + 0.28),
        )
        oreille = bpy.context.active_object
        oreille.rotation_euler = (0, math.radians(85), math.radians(35 * cote))
        oreille.name = nom
        oreilles.append(oreille)
    return oreilles


def creer_materiaux_robes():
    mat_blanc = bpy.data.materials.new(name="Zebu_Robe_Blanche")
    mat_blanc.use_nodes = True
    mat_blanc.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.85, 0.82, 0.75, 1.0)
    mat_blanc.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.6

    mat_roux = bpy.data.materials.new(name="Zebu_Robe_Rousse")
    mat_roux.use_nodes = True
    mat_roux.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.45, 0.22, 0.1, 1.0)
    mat_roux.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.6

    mat_corne = bpy.data.materials.new(name="Zebu_Corne")
    mat_corne.use_nodes = True
    mat_corne.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.25, 0.2, 0.15, 1.0)
    mat_corne.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.35

    return mat_blanc, mat_roux, mat_corne


def rapport_triangles(objets):
    print("\n--- Budget triangles (zébu, base) ---")
    total = 0
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objets:
        eval_obj = obj.evaluated_get(depsgraph)
        mesh_eval = eval_obj.to_mesh()
        mesh_eval.calc_loop_triangles()
        n_tris = len(mesh_eval.loop_triangles)
        eval_obj.to_mesh_clear()
        total += n_tris
        print(f"  {obj.name:16s} : {n_tris:5d} triangles")
    print(f"  {'TOTAL':16s} : {total:5d} triangles (x nombre d'animaux dans le troupeau final)")
    print("  (budget max recommandé pour ce module : 20 000 triangles)\n")
    if total > 2000:
        print("  ATTENTION : au-dessus de 2 000 tri pour un seul zébu, c'est cher pour un")
        print("  troupeau de plusieurs bêtes. Augmenter mball.resolution (ex: 0.12-0.15)")
        print("  pour réduire, ou ajouter un modificateur Decimate sur Zebu_Corps.\n")


if __name__ == "__main__":
    corps = creer_corps_metaball()
    pattes = creer_pattes()
    cornes = creer_cornes()
    oreilles = creer_oreilles()

    mat_blanc, mat_roux, mat_corne = creer_materiaux_robes()
    corps.data.materials.append(mat_blanc)
    for p in pattes:
        p.data.materials.append(mat_blanc)
    for o in oreilles:
        o.data.materials.append(mat_blanc)
    for c in cornes:
        c.data.materials.append(mat_corne)

    rapport_triangles([corps] + pattes + cornes + oreilles)

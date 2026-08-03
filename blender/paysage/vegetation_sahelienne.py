"""
Végétation sahélienne — acacia parasol (Wuro & Galle)

Silhouette caractéristique du Sahel : tronc fin et légèrement penché,
couronne APLATIE et étalée (acacia parasol, Vachellia/Acacia tortilis) —
la forme la plus reconnaissable de la savane soudano-sahélienne du Diamaré.
Remplace les cônes procéduraux génériques utilisés jusqu'ici comme
"buissons" (SceneBuilder.cs, CreerVegetationEparse) : un cône vertical ne
ressemble à aucune végétation réelle de la région, un acacia parasol si.

Pas de photo de référence utilisée ici (aucune image de feuillage isolé
disponible dans le corpus à une résolution exploitable pour du texturing) :
couleur plate + relief géométrique (Displace), même logique que le mur en
banco (case_banco.py) plutôt qu'un pari sur un bruit procédural non
exportable en glTF (voir materiaux_case.py pour la leçon complète).

Exécution : onglet Scripting > Open > ce fichier > Run Script.
Export ensuite en GLTF (Acacia.glb) — un seul module réutilisé plusieurs
fois par Unity (voir SceneBuilder.cs, CreerVegetationEparse), donc le coût
en triangles ne se paie qu'une fois, peu importe le nombre d'exemplaires
dispersés dans la scène.
"""

import bpy
import math
import random

VARIANTES_ACACIA = [
    (2.0, 1.4, 0.0),   # (hauteur_tronc, rayon_couronne, decalage_x) — variante compacte
    (2.6, 1.9, 4.0),   # variante plus grande, écartée pour la revue visuelle
    (2.2, 1.6, 8.0),   # variante intermédiaire
]


def ajouter_displacement(obj, force, echelle, seed):
    """Relief de surface réel (géométrie déformée) — même helper que case_banco.py."""
    tex = bpy.data.textures.new(f"{obj.name}_bruit_disp", type='CLOUDS')
    tex.noise_scale = echelle
    tex.noise_depth = 2
    tex.noise_basis = 'ORIGINAL_PERLIN'

    mod = obj.modifiers.new(name="Relief", type='DISPLACE')
    mod.texture = tex
    mod.strength = force
    mod.mid_level = 0.5
    mod.texture_coords = 'GLOBAL'
    return mod


def ajouter_bevel(obj, largeur=0.02, segments=2):
    mod = obj.modifiers.new(name="Arrondi", type='BEVEL')
    mod.width = largeur
    mod.segments = segments
    mod.limit_method = 'ANGLE'
    mod.angle_limit = math.radians(45)
    return mod


def creer_tronc(nom, hauteur, decalage_x, seed):
    """Tronc fin, légèrement penché (vent dominant sahélien) — pas un poteau parfaitement vertical."""
    rng = random.Random(seed)
    rayon = 0.05 + rng.uniform(-0.01, 0.01)

    bpy.ops.mesh.primitive_cylinder_add(
        vertices=8, radius=rayon, depth=hauteur,
        location=(decalage_x, 0, hauteur / 2),
    )
    tronc = bpy.context.active_object
    tronc.name = nom

    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.subdivide(number_cuts=2)
    bpy.ops.object.mode_set(mode='OBJECT')

    # Inclinaison légère et aléatoire (axe X et Y) : un tronc d'acacia n'est
    # presque jamais parfaitement droit, surtout exposé au vent de la plaine.
    tronc.rotation_euler = (
        math.radians(rng.uniform(-11, 11)),
        math.radians(rng.uniform(-11, 11)),
        0,
    )

    disp = ajouter_displacement(tronc, force=0.015, echelle=6.0, seed=seed)
    bevel = ajouter_bevel(tronc, largeur=0.008)
    bpy.context.view_layer.objects.active = tronc
    bpy.ops.object.modifier_apply(modifier=disp.name)
    bpy.ops.object.modifier_apply(modifier=bevel.name)

    return tronc, rayon


def creer_couronne(nom, rayon, position_z, decalage_x, seed):
    """
    Couronne APLATIE (silhouette "parasol", pas un dôme rond) : la proportion
    hauteur/rayon de la couronne est l'élément qui rend la silhouette
    reconnaissable comme acacia sahélien plutôt que comme un arbre générique.
    """
    rng = random.Random(seed + 100)
    hauteur_couronne = rayon * rng.uniform(0.30, 0.42)  # aplatissement marqué

    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=14, ring_count=8, radius=rayon,
        location=(decalage_x, 0, position_z),
    )
    couronne = bpy.context.active_object
    couronne.name = nom
    couronne.scale = (1.0, 1.0, hauteur_couronne / rayon)

    bpy.context.view_layer.objects.active = couronne
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    # Jitter du contour extérieur (pas seulement la base comme le toit de
    # chaume) : casse la silhouette de dôme géométrique parfait, donne un
    # feuillage épars et irrégulier plutôt qu'un ballon.
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.transform.vertex_random(offset=0.14, uniform=0.0, normal=0.0, seed=seed)
    bpy.ops.object.mode_set(mode='OBJECT')

    disp = ajouter_displacement(couronne, force=0.07, echelle=2.2, seed=seed)
    bevel = ajouter_bevel(couronne, largeur=0.02)
    bpy.ops.object.modifier_apply(modifier=disp.name)
    bpy.ops.object.modifier_apply(modifier=bevel.name)

    return couronne


def creer_materiau_plat(nom, couleur_rgb, roughness):
    """Couleur plate garantie compatible glTF — même principe que materiaux_case.py."""
    mat = bpy.data.materials.new(name=nom)
    mat.use_nodes = True
    principled = mat.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = (*couleur_rgb, 1.0)
    principled.inputs["Roughness"].default_value = roughness
    return mat


def creer_acacia(index, hauteur_tronc, rayon_couronne, decalage_x):
    seed = index
    tronc, rayon_tronc = creer_tronc(f"Acacia{index}_Tronc", hauteur_tronc, decalage_x, seed)
    couronne = creer_couronne(
        f"Acacia{index}_Couronne", rayon_couronne,
        position_z=hauteur_tronc * 0.92, decalage_x=decalage_x, seed=seed,
    )
    return [tronc, couronne]


def rapport_triangles(objets):
    print("\n--- Budget triangles (acacia parasol) ---")
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
    print("  (un seul module réutilisé plusieurs fois dans Unity : ce coût ne se paie qu'une fois)\n")


def _rendre_apercu(objets, nom_image, resolution=(900, 700)):
    """Même helper que dans materiaux_case.py / nattes_seygoore.py (dupliqué par convention)."""
    import os
    import mathutils

    if not objets or not bpy.data.filepath:
        print("ATTENTION : aperçu ignoré (aucun objet ou .blend non sauvegardé).")
        return None

    mins = mathutils.Vector((min(min((o.matrix_world @ mathutils.Vector(c))[i] for c in o.bound_box) for o in objets) for i in range(3)))
    maxs = mathutils.Vector((max(max((o.matrix_world @ mathutils.Vector(c))[i] for c in o.bound_box) for o in objets) for i in range(3)))
    centre = (mins + maxs) / 2
    rayon = max((maxs - mins).x, (maxs - mins).y, (maxs - mins).z, 0.5)

    camera = bpy.data.objects.get("Camera_Apercu")
    if camera is None:
        cam_data = bpy.data.cameras.new("Camera_Apercu")
        camera = bpy.data.objects.new("Camera_Apercu", cam_data)
        bpy.context.collection.objects.link(camera)
    distance = rayon * 2.4
    camera.location = centre + mathutils.Vector((distance * 0.8, -distance * 0.9, distance * 0.6))
    camera.rotation_euler = (centre - camera.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.camera = camera

    if not any(o.type == 'LIGHT' and o.data.type == 'SUN' for o in bpy.context.scene.objects):
        sun_data = bpy.data.lights.new("Sun_Apercu", type='SUN')
        sun_data.energy = 3.0
        sun_obj = bpy.data.objects.new("Sun_Apercu", sun_data)
        bpy.context.collection.objects.link(sun_obj)
        sun_obj.rotation_euler = (0.9, 0.3, 0.6)

    scene = bpy.context.scene
    try:
        scene.render.engine = 'BLENDER_EEVEE'
    except TypeError:
        scene.render.engine = 'BLENDER_EEVEE_NEXT'
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.image_settings.file_format = 'PNG'

    dossier_apercus = os.path.normpath(os.path.join(os.path.dirname(bpy.data.filepath), "..", "apercus"))
    os.makedirs(dossier_apercus, exist_ok=True)
    chemin_sortie = os.path.join(dossier_apercus, f"{nom_image}.png")
    scene.render.filepath = chemin_sortie

    bpy.ops.render.render(write_still=True)
    print(f"Aperçu rendu : {chemin_sortie}")
    return chemin_sortie


if __name__ == "__main__":
    mat_ecorce = creer_materiau_plat("Acacia_Ecorce", couleur_rgb=(0.32, 0.26, 0.20), roughness=0.88)
    mat_feuillage = creer_materiau_plat("Acacia_Feuillage", couleur_rgb=(0.42, 0.46, 0.32), roughness=0.85)
    # Vert-de-gris terne et poussiéreux, pas un vert vif : le feuillage
    # d'acacia en zone soudano-sahélienne est épars, jamais luxuriant.

    tous_objets = []
    for i, (h_tronc, r_couronne, dx) in enumerate(VARIANTES_ACACIA, start=1):
        tronc, couronne = creer_acacia(i, h_tronc, r_couronne, dx)
        tronc.data.materials.append(mat_ecorce)
        couronne.data.materials.append(mat_feuillage)
        tous_objets.extend([tronc, couronne])

    rapport_triangles(tous_objets)
    _rendre_apercu(tous_objets, "acacia_apercu")

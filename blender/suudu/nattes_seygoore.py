"""
Nattes seygoore — couverture tressée de la suudu (Wuro & Galle)

Géométrie : même technique que l'armature (une courbe de profil qu'on fait
tourner à 360° autour de l'axe Z), mais appliquée ici à des bandes creuses
plutôt qu'à un solide plein — 3 bandes superposées avec un petit espace
entre elles, plutôt qu'une coque continue, pour rester fidèle à la façon
dont plusieurs nattes se chevauchent réellement sur l'armature. Le sommet
reste ouvert (trou de fumée / ventilation).

Nouveauté : la transparence alpha. Le canal Alpha d'un matériau contrôle
l'opacité de chaque point de la surface (1 = opaque, 0 = invisible). En
pilotant l'Alpha par une texture procédurale (Voronoi) plutôt qu'une valeur
fixe, on obtient des trous irréguliers qui laissent filtrer la lumière —
l'effet recherché pour une natte tressée. Le matériau ci-dessous est un
gabarit procédural ; la vraie texture tressée (peinte ou photographiée)
viendra à l'étape de texturing PBR complète.

Exécution : onglet Scripting > Open > ce fichier > Run Script.
Doit être lancé après armature_suudu.py pour partager les mêmes dimensions.
"""

import bpy
import bmesh
import math

# --- Dimensions (partagées avec l'armature) ---
RAYON_BASE = 1.3
HAUTEUR_DOME = 1.6

# --- Bandes : chaque tuple est une plage (t_debut, t_fin) le long du profil
# du dôme, de 0 (base) à 1 (sommet). Les espaces entre bandes correspondent
# au chevauchement partiel réel des nattes, pas à des trous accidentels.
BANDES = [
    (0.05, 0.35),
    (0.40, 0.65),
    (0.70, 0.90),   # s'arrête avant le sommet -> trou de fumée conservé
]

SEGMENTS_REVOLUTION = 20  # bandes très visibles, un peu plus de segments que le mobilier


def point_profil_dome(t):
    """
    Point du profil du dôme à la position t (0 = base, 1 = sommet), sur une
    courbe en quart d'ellipse — cohérente avec la forme générale de
    l'armature en arches.
    """
    r = RAYON_BASE * math.cos(t * math.pi / 2)
    h = HAUTEUR_DOME * math.sin(t * math.pi / 2)
    return (r, h)


def creer_bande(nom, t_debut, t_fin, n_points=6, segments=SEGMENTS_REVOLUTION):
    """
    Crée une bande courbe par révolution d'un segment de profil ouvert
    (contrairement au mobilier, ce profil ne touche jamais l'axe -> le
    résultat est une bande creuse, pas un solide fermé).
    """
    mesh = bpy.data.meshes.new(nom)
    bm = bmesh.new()

    points_profil = [point_profil_dome(t_debut + (t_fin - t_debut) * i / (n_points - 1))
                      for i in range(n_points)]
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


def creer_materiau_alpha():
    """
    Matériau procédural avec transparence pilotée par une texture Voronoi :
    gabarit pour visualiser l'effet de trouée lumineuse avant la vraie
    texture tressée.
    """
    mat = bpy.data.materials.new(name="Natte_seygoore_alpha")
    mat.use_nodes = True

    # blend_method / shadow_method n'existent pas dans toutes les versions
    # de Blender (EEVEE Next, à partir de 4.2, a retiré shadow_method et
    # changé les options de blend_method) -> réglage défensif, ignoré
    # silencieusement si l'attribut n'existe pas sur cette version.
    if hasattr(mat, "blend_method"):
        mat.blend_method = 'CLIP'       # active la transparence en aperçu/rendu temps réel
    if hasattr(mat, "shadow_method"):
        mat.shadow_method = 'CLIP'      # les trous laissent aussi passer l'ombre, pas juste la couleur

    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    sortie = nodes.new("ShaderNodeOutputMaterial")
    sortie.location = (600, 0)

    principled = nodes.new("ShaderNodeBsdfPrincipled")
    principled.location = (300, 0)
    principled.inputs["Base Color"].default_value = (0.62, 0.48, 0.24, 1.0)  # doré paille
    principled.inputs["Roughness"].default_value = 0.8
    if "Anisotropic" in principled.inputs:
        principled.inputs["Anisotropic"].default_value = 0.3  # fibres tressées, pas mat uniforme

    voronoi = nodes.new("ShaderNodeTexVoronoi")
    voronoi.location = (-200, -300)
    voronoi.inputs["Scale"].default_value = 25.0  # densité du motif de trous

    rampe_alpha = nodes.new("ShaderNodeValToRGB")
    rampe_alpha.location = (50, -300)
    # Ajuste le point de coupure : au-delà de ~0.4, opaque ; en-dessous, trou.
    rampe_alpha.color_ramp.elements[0].position = 0.35
    rampe_alpha.color_ramp.elements[1].position = 0.45

    # Variation de couleur (brins plus foncés/clairs) — même logique que les
    # autres matériaux du projet, pour casser l'aplat uniforme d'avant.
    bruit_couleur = nodes.new("ShaderNodeTexNoise")
    bruit_couleur.location = (-400, 200)
    bruit_couleur.inputs["Scale"].default_value = 20.0

    rampe_couleur = nodes.new("ShaderNodeValToRGB")
    rampe_couleur.location = (-150, 200)
    rampe_couleur.color_ramp.elements[0].position = 0.35
    rampe_couleur.color_ramp.elements[1].position = 0.65

    mix_couleur = nodes.new("ShaderNodeMixRGB")
    mix_couleur.location = (100, 200)
    mix_couleur.inputs["Color1"].default_value = (0.62, 0.48, 0.24, 1.0)
    mix_couleur.inputs["Color2"].default_value = (0.48, 0.36, 0.16, 1.0)  # brins plus vieillis

    links.new(bruit_couleur.outputs["Fac"], rampe_couleur.inputs["Fac"])
    links.new(rampe_couleur.outputs["Color"], mix_couleur.inputs["Fac"])
    links.new(mix_couleur.outputs["Color"], principled.inputs["Base Color"])

    links.new(voronoi.outputs["Distance"], rampe_alpha.inputs["Fac"])
    links.new(rampe_alpha.outputs["Color"], principled.inputs["Alpha"])
    links.new(principled.outputs["BSDF"], sortie.inputs["Surface"])

    return mat


def rapport_triangles(objets):
    print("\n--- Budget triangles (nattes seygoore) ---")
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
    materiau = creer_materiau_alpha()
    bandes_crees = []
    for i, (t0, t1) in enumerate(BANDES):
        bande = creer_bande(f"Natte_seygoore_bande_{i + 1}", t0, t1)
        bande.data.materials.append(materiau)
        bandes_crees.append(bande)
    rapport_triangles(bandes_crees)

"""
Corps du joueur — silhouette stylisée articulée (Wuro & Galle)

Remplace le placeholder de capsules générées en code (CorpsJoueur.cs,
Assets/Scripts/) par une vraie géométrie modélisée, dans le même esprit que
les autres modules du projet : segments tronconiques (technique "tour de
potier" déjà utilisée pour le canari/la calebasse — bmesh.ops.spin sur un
profil 2D) plutôt que des primitives brutes, pour une silhouette moins
"jeu de blocs" qu'une simple capsule.

Silhouette sans traits individualisés (note éthique, *semteende*) : pas de
visage, pas de détail vestimentaire figuratif — un matériau neutre sombre
unique, identique à celui déjà utilisé pour le placeholder des PNJ
(SceneBuilder.CreerPNJPlaceholder) et l'ancien corps du joueur, pour rester
visuellement cohérent entre les deux.

Point important pour l'articulation : contrairement à une capsule Unity
(pivot au centre géométrique), chaque segment ici a son ORIGINE À
L'ARTICULATION (hanche pour la jambe, taille pour le torse, épaule pour le
bras) et la géométrie s'étend depuis ce point — pour qu'une rotation
appliquée côté Unity (CorpsJoueur.AppliquerPose) pivote vraiment depuis
l'articulation, pas depuis le milieu du segment.

Hiérarchie : tous les segments sont exportés à plat (sans parenté Blender)
positionnés en coordonnées MONDE formant un bonhomme debout, pieds au sol
(Z=0) — c'est Assets/Scripts/CorpsJoueur.cs qui reconstruit la hiérarchie
Bassin > Torse > Tête / Bras_G / Bras_D, Bassin > Jambe_G / Jambe_D côté
Unity après import (SetParent avec worldPositionStays=true : la pose
debout définie ici est préservée, pas besoin de recalculer les offsets
à la main).

Exécution interactive : onglet Scripting > Open > ce fichier > Run Script.
Exécution headless (génère + exporte automatiquement) :
  blender --background --python personnage_joueur.py
"""

import bpy
import bmesh
import math
import os


def creer_mesh_depuis_profil(nom, points_profil, segments=10):
    """Identique à canari.py/generer_mobilier.py : spin d'un profil 2D
    (rayon, z) autour de l'axe Z local pour un solide de révolution lisse,
    sans les coutures dures d'une capsule assemblée à partir de primitives."""
    mesh = bpy.data.meshes.new(nom)
    bm = bmesh.new()

    verts_profil = [bm.verts.new((r, 0, z)) for (r, z) in points_profil]
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


def creer_segment(nom, profil, position_monde):
    """Segment tronconique dont l'origine (0,0,0) est au point d'articulation
    (voir profil de chaque appelant) ; position_monde place cette
    articulation dans la scène."""
    obj = creer_mesh_depuis_profil(nom, profil)
    obj.location = position_monde
    return obj


def creer_tete(position_monde, rayon=0.13):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=rayon, segments=12, ring_count=8, location=position_monde)
    tete = bpy.context.active_object
    tete.name = "Tete"
    return tete


def creer_materiau_peau():
    """
    Teinte de peau chaude et naturelle plutôt que le gris neutre uniforme
    utilisé jusqu'ici — le personnage représente quelqu'un du Diamaré
    (Sahel camerounais), un silhouette grise ne le lisait pas comme une
    personne du tout. RESTE volontairement sans visage ni traits
    individualisés (note éthique, *semteende*) : un aplat de couleur stylisé,
    pas un shader de peau réaliste ni une tentative de représenter "la"
    couleur de peau africaine — juste un ton plausible et respectueux plutôt
    qu'un gris qui n'évoquait rien de spécifique. Différent du gris du
    placeholder PNJ (SceneBuilder.CreerPNJPlaceholder) : écart assumé pour
    l'instant, les PNJ restent des silhouettes plus abstraites/en retrait,
    le joueur est ce qu'on regarde le plus souvent (bras, corps en 3e
    personne) donc mérite cette touche en premier.
    """
    mat = bpy.data.materials.new(name="Personnage_Peau")
    mat.use_nodes = True
    principled = mat.node_tree.nodes["Principled BSDF"]
    # Assombri/resaturé par rapport à un premier essai (0.36,0.22,0.14) : sous
    # le soleil de zénith + color grading chaud de la scène (voir
    # SceneBuilder.ConfigurerColorGrading), cette première teinte délavait
    # presque au même ton que les murs en banco des cases — le personnage se
    # fondait dans le décor au lieu de s'en détacher comme une personne.
    principled.inputs["Base Color"].default_value = (0.24, 0.13, 0.08, 1.0)
    principled.inputs["Roughness"].default_value = 0.55
    return mat


def rapport_triangles(objets):
    print("\n--- Budget triangles (Personnage joueur) ---")
    total = 0
    depsgraph = bpy.context.evaluated_depsgraph_get()
    for obj in objets:
        eval_obj = obj.evaluated_get(depsgraph)
        mesh_eval = eval_obj.to_mesh()
        mesh_eval.calc_loop_triangles()
        n_tris = len(mesh_eval.loop_triangles)
        eval_obj.to_mesh_clear()
        total += n_tris
        print(f"  {obj.name:10s} : {n_tris:5d} triangles")
    print(f"  {'TOTAL':10s} : {total:5d} triangles\n")


def exporter_glb(objets, chemins):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objets:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objets[0]

    for chemin in chemins:
        os.makedirs(os.path.dirname(chemin), exist_ok=True)
        bpy.ops.export_scene.gltf(
            filepath=chemin,
            export_format='GLB',
            use_selection=True,
            export_apply=True,
            export_yup=True,
        )
        print(f"[personnage_joueur.py] Exporté -> {chemin}")


if __name__ == "__main__":
    if bpy.app.background:
        bpy.ops.wm.read_factory_settings(use_empty=True)

    # Hauteur totale visée ~1,8 m (cohérent avec CharacterController.height
    # dans SceneBuilder.CreerJoueur, et la caméra à hauteur des yeux Y=1,6 m).

    # Jambes : origine à la hanche (Z=0,85, haut du profil), s'étend au sol.
    profil_jambe = [(0.11, 0.0), (0.095, -0.42), (0.08, -0.85)]
    jambe_g = creer_segment("Jambe_G", profil_jambe, (-0.12, 0.0, 0.85))
    jambe_d = creer_segment("Jambe_D", profil_jambe, (0.12, 0.0, 0.85))

    # Bassin : bloc légèrement tronconique, centré entre les deux hanches.
    profil_bassin = [(0.18, 0.10), (0.19, -0.02), (0.16, -0.10)]
    bassin = creer_segment("Bassin", profil_bassin, (0.0, 0.0, 0.95))

    # Torse : origine à la taille (attache sur le bassin), s'étend vers les
    # épaules — plus large en haut (carrure) qu'à la taille.
    profil_torse = [(0.17, 0.0), (0.19, 0.15), (0.22, 0.35), (0.20, 0.50)]
    torse = creer_segment("Torse", profil_torse, (0.0, 0.0, 1.05))

    # Tête : sphère simple, pas de traits (note éthique).
    tete = creer_tete((0.0, 0.0, 1.68))

    # Bras : origine à l'épaule, légèrement en retrait du sommet du torse.
    profil_bras = [(0.05, 0.0), (0.045, -0.25), (0.04, -0.5)]
    bras_g = creer_segment("Bras_G", profil_bras, (-0.24, 0.0, 1.48))
    bras_d = creer_segment("Bras_D", profil_bras, (0.24, 0.0, 1.48))

    parties = [bassin, torse, tete, bras_g, bras_d, jambe_g, jambe_d]

    for obj in parties:
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.shade_smooth()

    mat = creer_materiau_peau()
    for obj in parties:
        obj.data.materials.clear()
        obj.data.materials.append(mat)

    rapport_triangles(parties)

    if bpy.app.background:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        repo_root = os.path.abspath(os.path.join(script_dir, "..", ".."))
        exporter_glb(parties, [
            os.path.join(script_dir, "Personnage.glb"),
            os.path.join(repo_root, "unity", "wuro-galle-vr", "Assets", "Models", "Personnage", "Personnage.glb"),
        ])

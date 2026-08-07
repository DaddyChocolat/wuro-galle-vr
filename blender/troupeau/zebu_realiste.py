"""
Zébu réaliste — base CC-BY, Wuro & Galle

Remplace le block-out metaball (zebu_base.py) par un vrai modèle détaillé :
"Brahman bull Zebu" par maximorengifo2022, Sketchfab
(https://sketchfab.com/3d-models/brahman-bull-zebu-3e2254015caa4c878347e2985a927edd),
licence CC Attribution (CC-BY) — attribution obligatoire, voir
docs/Livrable_1/corpus-documentaire.md. ~33 800 triangles, texturé PBR
(diffuse/normal/roughness/metallic), silhouette réelle avec bosse et cornes
en lyre (contrairement aux options CC0 trouvées sans connexion requise —
Poly Pizza etc. — qui étaient stylisées/jouet et sans bosse, le trait
distinctif documenté du zébu).

Le fichier .mtl référencé par base.obj est absent du téléchargement (zip
incomplet côté source) : les matériaux sont reconstruits ici à partir des
4 textures fournies plutôt que de dépendre du .mtl manquant.

Deux robes exportées à partir du MÊME maillage (comme troupeau_robe_blanche/
rousse existants) : la texture diffuse d'origine (blanc/gris clair, robe
Brahman typique) pour "blanche", et une teinte rousse appliquée par
multiplication dans le shader (pas de nouvelle texture peinte) pour
"rousse" — préserve la variabilité de robe déjà documentée dans
note-technique-materiaux.md sans télécharger un second modèle.

Exécution : headless uniquement (import OBJ tel que téléchargé) :
  blender --background --python zebu_realiste.py
"""

import bpy
import math
import os
import numpy as np

DOSSIER_SOURCE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "source_cc0", "brahman-bull-zebu", "source")
CHEMIN_OBJ = os.path.join(DOSSIER_SOURCE, "base.obj")
LONGUEUR_CIBLE = 2.2  # m, nez-queue — cohérent avec la proportion documentée dans zebu_base.py


def importer_et_nettoyer():
    bpy.ops.wm.obj_import(filepath=CHEMIN_OBJ)
    objets = [o for o in bpy.context.selected_objects if o.type == 'MESH']
    if not objets:
        raise RuntimeError("Aucun mesh importé depuis base.obj.")
    zebu = objets[0]
    zebu.name = "Zebu_Realiste"
    return zebu


def mettre_a_lechelle(zebu, longueur_cible):
    bpy.context.view_layer.update()
    coords = [(zebu.matrix_world @ v.co) for v in zebu.data.vertices]
    xs = [c.x for c in coords]
    ys = [c.y for c in coords]
    etendue_x = max(xs) - min(xs)
    etendue_y = max(ys) - min(ys)
    # Bug constaté : l'axe nez-queue du maillage source (Sketchfab, orientation
    # inconnue à l'import OBJ) ne correspond pas forcément à X. Utiliser X seul
    # a mesuré la LARGEUR (bête étroite) au lieu de la longueur, d'où une échelle
    # ~4-5x trop grande (zébu de plusieurs mètres de haut au lieu de ~1,5 m,
    # constaté via DiagnosticTailles.cs). Après import, Z est déjà l'axe vertical
    # (Blender Z-up) — la longueur nez-queue est donc la plus grande étendue
    # HORIZONTALE (X ou Y), quelle que soit l'orientation d'origine du maillage.
    longueur_actuelle = max(etendue_x, etendue_y)
    if longueur_actuelle <= 0:
        raise RuntimeError("Bounds dégénérés sur le zébu importé.")
    echelle = longueur_cible / longueur_actuelle
    zebu.scale = (echelle, echelle, echelle)
    bpy.ops.object.select_all(action='DESELECT')
    zebu.select_set(True)
    bpy.context.view_layer.objects.active = zebu
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    bpy.context.view_layer.update()
    min_z = min((zebu.matrix_world @ v.co).z for v in zebu.data.vertices)
    zebu.location.z -= min_z  # pattes au sol


def charger_texture(nom_fichier, colorspace='sRGB'):
    chemin = os.path.join(DOSSIER_SOURCE, nom_fichier)
    img = bpy.data.images.load(chemin)
    img.colorspace_settings.name = colorspace
    return img


def creer_texture_teintee(image_source, teinte, nom):
    """Multiplie les canaux RGB de image_source par teinte et renvoie une NOUVELLE
    image (numpy, rapide même sur un PNG 2048x2048). Remplace l'ancienne approche
    (nœud ShaderNodeMixRGB entre la texture et Base Color) : constaté après export
    que l'exporteur glTF de Blender ne "voit" pas à travers ce montage — le
    baseColorFactor exporté restait (1,1,1,1) pour TOUTES les robes, y compris
    rousse/noire (vérifié côté Unity : DiagnosticTailles.DiagnostiquerMateriauxTroupeau
    montrait baseColorFactor identique sur les 3 robes, d'où l'aspect gris uniforme
    en jeu, quelle que soit la robe). Cuire la teinte directement dans les pixels
    évite toute dépendance à ce que l'exporteur sache reconnaître le montage de
    nœuds — la texture exportée EST déjà la bonne couleur, sans facteur à extraire."""
    largeur, hauteur = image_source.size
    pixels = np.array(image_source.pixels[:], dtype=np.float32).reshape((hauteur, largeur, 4))
    pixels[:, :, 0] *= teinte[0]
    pixels[:, :, 1] *= teinte[1]
    pixels[:, :, 2] *= teinte[2]

    img_teinte = bpy.data.images.new(nom, width=largeur, height=hauteur, alpha=True)
    img_teinte.colorspace_settings.name = 'sRGB'
    img_teinte.pixels.foreach_set(pixels.ravel())
    img_teinte.pack()  # embarquée dans le .blend/l'export, pas de fichier externe à gérer
    return img_teinte


def creer_materiau_pbr(nom, teinte_multiplicative=None):
    """Matériau Principled BSDF branché sur les 4 textures fournies (diffuse/
    normal/roughness/metallic). teinte_multiplicative (RGB) : si fourni, une
    variante de la texture diffuse est cuite (pixels multipliés, voir
    creer_texture_teintee) plutôt que multipliée dans le graphe de nœuds."""
    mat = bpy.data.materials.new(name=nom)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    sortie = nodes.new("ShaderNodeOutputMaterial")
    sortie.location = (600, 0)
    principled = nodes.new("ShaderNodeBsdfPrincipled")
    principled.location = (300, 0)
    links.new(principled.outputs["BSDF"], sortie.inputs["Surface"])

    image_diffuse = charger_texture("texture_diffuse.png", 'sRGB')
    if teinte_multiplicative is not None:
        image_diffuse = creer_texture_teintee(image_diffuse, teinte_multiplicative, f"{nom}_diffuse_teintee")

    tex_diffuse = nodes.new("ShaderNodeTexImage")
    tex_diffuse.location = (-400, 200)
    tex_diffuse.image = image_diffuse
    links.new(tex_diffuse.outputs["Color"], principled.inputs["Base Color"])

    tex_rough = nodes.new("ShaderNodeTexImage")
    tex_rough.location = (-400, -50)
    tex_rough.image = charger_texture("texture_roughness.png", 'Non-Color')
    links.new(tex_rough.outputs["Color"], principled.inputs["Roughness"])

    # texture_metallic.png du téléchargement Sketchfab est quasi blanche partout
    # (vérifié à l'œil) : branchée sur Metallic, elle rend la bête ~100% métallique
    # (surface qui ne réfléchit que l'environnement, pas d'albedo visible — le
    # pelage apparaissait gris/noir uniforme en jeu au lieu de la texture diffuse
    # réelle). Un pelage de zébu est organique, pas métallique : valeur constante
    # à 0 plutôt que de dépendre de cette texture inexploitable.
    principled.inputs["Metallic"].default_value = 0.0

    tex_normal = nodes.new("ShaderNodeTexImage")
    tex_normal.location = (-400, -550)
    tex_normal.image = charger_texture("texture_normal.png", 'Non-Color')
    normal_map = nodes.new("ShaderNodeNormalMap")
    normal_map.location = (-100, -550)
    links.new(tex_normal.outputs["Color"], normal_map.inputs["Color"])
    links.new(normal_map.outputs["Normal"], principled.inputs["Normal"])

    return mat


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
        print(f"[zebu_realiste.py] Exporté -> {chemin}")


if __name__ == "__main__":
    if bpy.app.background:
        bpy.ops.wm.read_factory_settings(use_empty=True)

    zebu = importer_et_nettoyer()
    mettre_a_lechelle(zebu, LONGUEUR_CIBLE)

    print(f"\n--- Zébu réaliste (base CC-BY) ---")
    print(f"  {zebu.name} : {len(zebu.data.vertices)} sommets, {len(zebu.data.polygons)} polygones\n")

    script_dir = os.path.dirname(os.path.abspath(__file__))
    repo_root = os.path.abspath(os.path.join(script_dir, "..", ".."))

    # Robe blanche : texture d'origine, sans modification.
    mat_blanc = creer_materiau_pbr("Zebu_Realiste_Blanche")
    zebu.data.materials.clear()
    zebu.data.materials.append(mat_blanc)
    if bpy.app.background:
        exporter_glb([zebu], [
            os.path.join(script_dir, "ZebuRealiste_blanc.glb"),
            os.path.join(repo_root, "unity", "wuro-galle-vr", "Assets", "Models", "Troupeau", "ZebuRealiste_blanc.glb"),
        ])

    # Robe rousse : même géométrie, teinte multipliée sur la diffuse (pas de
    # nouvelle texture peinte) — préserve la variabilité robe blanche/rousse
    # déjà documentée (note-technique-materiaux.md) sans second téléchargement.
    mat_roux = creer_materiau_pbr("Zebu_Realiste_Rousse", teinte_multiplicative=(0.55, 0.28, 0.14))
    zebu.data.materials.clear()
    zebu.data.materials.append(mat_roux)
    if bpy.app.background:
        exporter_glb([zebu], [
            os.path.join(script_dir, "ZebuRealiste_roux.glb"),
            os.path.join(repo_root, "unity", "wuro-galle-vr", "Assets", "Models", "Troupeau", "ZebuRealiste_roux.glb"),
        ])

    # Robe noire : troisième variante demandée (troupeau sahélien réel : robes
    # blanche/grise, rousse ET noire) — même principe de teinte multiplicative,
    # sans texture supplémentaire.
    mat_noir = creer_materiau_pbr("Zebu_Realiste_Noire", teinte_multiplicative=(0.05, 0.05, 0.05))
    zebu.data.materials.clear()
    zebu.data.materials.append(mat_noir)
    if bpy.app.background:
        exporter_glb([zebu], [
            os.path.join(script_dir, "ZebuRealiste_noir.glb"),
            os.path.join(repo_root, "unity", "wuro-galle-vr", "Assets", "Models", "Troupeau", "ZebuRealiste_noir.glb"),
        ])

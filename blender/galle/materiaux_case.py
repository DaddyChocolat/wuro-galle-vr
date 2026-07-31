"""
Matériaux — cases en banco (Wuro & Galle)

IMPORTANT (leçon apprise à l'export) : l'exporteur glTF de Blender ne
comprend PAS les graphes de nœuds complexes (Noise -> ColorRamp -> Mix pour
la couleur, coordonnées "Generated" pour une texture). Quand il ne reconnaît
pas le montage, il exporte un matériau blanc par défaut — c'est exactement
ce qui s'est produit (murs blancs dans Unity alors que Blender montrait la
bonne couleur). glTF ne connaît QUE deux choses pour la Base Color : une
valeur plate, ou une image reliée via les coordonnées UV du mesh. Donc :

  - "*_Mur"   -> banco : couleur PLATE (pas de bruit de couleur — ne
                 s'exporterait pas). Le relief vient du modificateur Displace
                 (case_banco.py), qui LUI est bien baké dans la géométrie et
                 s'exporte normalement.
  - "*_Toit"  -> paille : texture RÉELLE (photo sourcée du corpus, recadrée
                 en blender/textures/reference/chaume_ref.jpg), reliée à la
                 Base Color via les coordonnées UV par défaut du mesh (pas de
                 nœud Mapping/Generated cette fois — non exportable).
  - "*_Porte" -> bois : couleur plate, même raison que le mur.

Exécution : après case_banco.py (les objets Case*_Mur/_Toit/_Porte doivent
exister). Onglet Scripting > Open > ce fichier > Run Script.
Le .blend doit être sauvegardé dans blender/galle/ pour que le chemin
relatif vers blender/textures/reference/ se résolve correctement.

Vérification après export : ouvre le .glb dans un visualisateur externe
(ex. https://gltf-viewer.donmccurdy.com/ ou Babylon.js Sandbox) AVANT de
réimporter dans Unity — ça confirme si le matériau a vraiment été exporté,
sans attendre un aller-retour Unity complet.
"""

import bpy
import os
import mathutils


def _rendre_apercu(objets, nom_image, resolution=(900, 700)):
    """
    Rend un PNG cadré automatiquement sur les objets donnés, dans
    blender/apercus/. Évite d'ouvrir Blender et d'orbiter la vue 3D à la main
    pour juger du résultat visuel après une passe de matériaux — un coup
    d'œil sur l'image suffit. Réutilise Camera_Apercu/Sun_Apercu s'ils
    existent déjà (relançable sans créer de doublons).
    """
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
        scene.render.engine = 'BLENDER_EEVEE_NEXT'  # Blender 4.2+ a renommé le moteur
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.image_settings.file_format = 'PNG'

    dossier_apercus = os.path.normpath(os.path.join(os.path.dirname(bpy.data.filepath), "..", "apercus"))
    os.makedirs(dossier_apercus, exist_ok=True)
    chemin_sortie = os.path.join(dossier_apercus, f"{nom_image}.png")
    scene.render.filepath = chemin_sortie

    bpy.ops.render.render(write_still=True)
    print(f"Aperçu rendu : {chemin_sortie}")
    return chemin_sortie


def _charger_texture_reference(nom_fichier):
    """
    Charge une image depuis blender/textures/reference/, en chemin relatif au
    .blend actuellement ouvert (qui doit vivre dans blender/<module>/).
    Retourne None si introuvable, plutôt que de planter le script.
    """
    if not bpy.data.filepath:
        print(f"ATTENTION : .blend non sauvegardé — impossible de localiser {nom_fichier}, texture non chargée.")
        return None
    dossier_blend = os.path.dirname(bpy.data.filepath)
    chemin = os.path.normpath(os.path.join(dossier_blend, "..", "textures", "reference", nom_fichier))
    if not os.path.exists(chemin):
        print(f"ATTENTION : texture introuvable : {chemin} — texture non chargée, couleur plate utilisée à la place.")
        return None
    return bpy.data.images.load(chemin, check_existing=True)


def _materiau_couleur_plate(nom, couleur_rgb, roughness):
    """
    Matériau simple, garanti compatible glTF : juste une couleur plate sur le
    Principled BSDF, aucun nœud de bruit qui serait de toute façon ignoré à
    l'export. Le relief vient de la géométrie (Displace), pas du shader.
    """
    mat = bpy.data.materials.new(name=nom)
    mat.use_nodes = True
    principled = mat.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = (*couleur_rgb, 1.0)
    principled.inputs["Roughness"].default_value = roughness
    return mat


def _materiau_texture_reelle(nom, texture_reference, roughness, anisotropic=0.0,
                              couleur_secours=(0.6, 0.5, 0.3)):
    """
    Matériau avec une vraie photo en Base Color, reliée via les coordonnées
    UV PAR DÉFAUT du mesh (aucun nœud TexCoord/Mapping — glTF ne les exporte
    pas). Si la texture est introuvable, retombe sur une couleur plate de
    secours plutôt que de planter.
    """
    mat = bpy.data.materials.new(name=nom)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links

    principled = nodes.get("Principled BSDF")
    principled.inputs["Roughness"].default_value = roughness
    if "Anisotropic" in principled.inputs:
        principled.inputs["Anisotropic"].default_value = anisotropic

    image = _charger_texture_reference(texture_reference)
    if image is None:
        principled.inputs["Base Color"].default_value = (*couleur_secours, 1.0)
        return mat

    tex_couleur = nodes.new("ShaderNodeTexImage")
    tex_couleur.location = (-300, 300)
    tex_couleur.image = image
    # Pas de Vector connecté : Blender utilise automatiquement l'UV Map active
    # du mesh, ce qui EST exportable en glTF (contrairement à "Generated").
    links.new(tex_couleur.outputs["Color"], principled.inputs["Base Color"])

    return mat


if __name__ == "__main__":
    mat_banco = _materiau_couleur_plate(
        "Case_Banco_mur",
        couleur_rgb=(0.58, 0.36, 0.20),
        roughness=0.92,
    )
    mat_paille = _materiau_texture_reelle(
        "Case_Paille_toit",
        texture_reference="chaume_ref.jpg",
        roughness=0.85,
        anisotropic=0.35,
        couleur_secours=(0.68, 0.54, 0.26),
    )
    mat_bois = _materiau_couleur_plate(
        "Case_Bois_porte",
        couleur_rgb=(0.28, 0.16, 0.09),
        roughness=0.75,
    )

    suffixes_materiaux = [
        ("_Mur", mat_banco),
        ("_Toit", mat_paille),
        ("_Porte", mat_bois),
    ]

    compteurs = {"_Mur": 0, "_Toit": 0, "_Porte": 0}
    for obj in bpy.data.objects:
        for suffixe, mat in suffixes_materiaux:
            if obj.name.endswith(suffixe) and obj.name.startswith("Case"):
                obj.data.materials.clear()
                obj.data.materials.append(mat)
                compteurs[suffixe] += 1

    print("\n--- Matériaux cases banco assignés ---")
    for suffixe, n in compteurs.items():
        print(f"  {suffixe:8s} : {n} objet(s)")
    if sum(compteurs.values()) == 0:
        print("ATTENTION : aucun objet 'Case*_Mur/_Toit/_Porte' trouvé — lance d'abord case_banco.py")
    else:
        objets_cases = [o for o in bpy.data.objects if o.name.startswith("Case")]
        _rendre_apercu(objets_cases, "cases_apercu")

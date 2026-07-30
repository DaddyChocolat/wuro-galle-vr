"""
Matériaux — cases en banco (Wuro & Galle)

3 matériaux, appliqués selon le nom d'objet :
  - "*_Mur"   -> banco (terre/argile séchée), procédural (grain + variation de
                 couleur) — pas de photo de référence propre disponible dans
                 le corpus pour un mur en banco (les photos sourcées montrent
                 des cases en paille, pas en banco), donc on reste procédural
                 ici plutôt que d'aller chercher une texture non vérifiée.
  - "*_Toit"  -> paille de toiture. RÉEL cette fois : texture tirée de
                 docs/Livrable_1/iconographie/Hut_built_by_Fulani_herdsmen.jpg
                 (photo déjà sourcée/citée dans le corpus documentaire —
                 recadrée en blender/textures/reference/chaume_ref.jpg), pas
                 un bruit procédural. Vrai détail photographique (brins,
                 irrégularités, ombres réelles) qu'aucune texture procédurale
                 ne peut imiter.
  - "*_Porte" -> bois brut, procédural (pas de photo de porte en bois exploi-
                 table isolément dans le corpus).

Exécution : après case_banco.py (les objets Case*_Mur/_Toit/_Porte
doivent exister). Onglet Scripting > Open > ce fichier > Run Script.
Le .blend doit être sauvegardé dans blender/galle/ pour que le chemin
relatif vers blender/textures/reference/ se résolve correctement.
"""

import bpy
import os


def _charger_texture_reference(nom_fichier):
    """
    Charge une image depuis blender/textures/reference/, en chemin relatif au
    .blend actuellement ouvert (qui doit vivre dans blender/<module>/).
    Retourne None (avec un message clair) si le fichier n'est pas trouvé,
    plutôt que de planter le script — le matériau retombe alors sur du
    procédural pur pour ne pas bloquer tout le reste.
    """
    if not bpy.data.filepath:
        print(f"ATTENTION : .blend non sauvegardé — impossible de localiser {nom_fichier}, texture non chargée.")
        return None
    dossier_blend = os.path.dirname(bpy.data.filepath)
    chemin = os.path.normpath(os.path.join(dossier_blend, "..", "textures", "reference", nom_fichier))
    if not os.path.exists(chemin):
        print(f"ATTENTION : texture introuvable : {chemin} — texture non chargée, matériau restera procédural.")
        return None
    return bpy.data.images.load(chemin, check_existing=True)


def _materiau_pbr(nom, couleur_rgb, roughness, echelle_grain, intensite_grain,
                   couleur_variation=None, echelle_patch=4.0, anisotropic=0.0,
                   texture_reference=None, echelle_texture=3.0):
    """
    - Si texture_reference est fourni (nom de fichier dans blender/textures/
      reference/) : Base Color ET Bump viennent de la vraie photo (deux nœuds
      Image Texture, l'un en colorspace normal pour l'affichage, l'autre en
      Non-Color pour piloter le Bump correctement).
    - Sinon : comportement procédural (Noise -> Bump + variation de couleur
      Noise -> ColorRamp -> Mix), comme avant.
    """
    mat = bpy.data.materials.new(name=nom)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    sortie = nodes.new("ShaderNodeOutputMaterial")
    sortie.location = (600, 0)

    principled = nodes.new("ShaderNodeBsdfPrincipled")
    principled.location = (300, 0)
    principled.inputs["Base Color"].default_value = (*couleur_rgb, 1.0)
    principled.inputs["Roughness"].default_value = roughness
    if "Anisotropic" in principled.inputs:
        principled.inputs["Anisotropic"].default_value = anisotropic

    image = _charger_texture_reference(texture_reference) if texture_reference else None

    if image is not None:
        mapping_coord = nodes.new("ShaderNodeTexCoord")
        mapping_coord.location = (-700, 0)

        mapping = nodes.new("ShaderNodeMapping")
        mapping.location = (-500, 0)
        mapping.inputs["Scale"].default_value = (echelle_texture, echelle_texture, echelle_texture)
        links.new(mapping_coord.outputs["Generated"], mapping.inputs["Vector"])

        tex_couleur = nodes.new("ShaderNodeTexImage")
        tex_couleur.location = (-200, 100)
        tex_couleur.image = image
        links.new(mapping.outputs["Vector"], tex_couleur.inputs["Vector"])
        links.new(tex_couleur.outputs["Color"], principled.inputs["Base Color"])

        tex_bump_src = nodes.new("ShaderNodeTexImage")
        tex_bump_src.location = (-200, -250)
        tex_bump_src.image = image
        tex_bump_src.image.colorspace_settings.name = 'Non-Color'
        links.new(mapping.outputs["Vector"], tex_bump_src.inputs["Vector"])

        bump = nodes.new("ShaderNodeBump")
        bump.location = (100, -250)
        bump.inputs["Strength"].default_value = intensite_grain
        links.new(tex_bump_src.outputs["Color"], bump.inputs["Height"])
        links.new(bump.outputs["Normal"], principled.inputs["Normal"])

        links.new(principled.outputs["BSDF"], sortie.inputs["Surface"])
        return mat

    # --- Chemin procédural (pas de photo de référence disponible) ---
    bruit_grain = nodes.new("ShaderNodeTexNoise")
    bruit_grain.location = (-400, -250)
    bruit_grain.inputs["Scale"].default_value = echelle_grain

    bump = nodes.new("ShaderNodeBump")
    bump.location = (0, -250)
    bump.inputs["Strength"].default_value = intensite_grain

    links.new(bruit_grain.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], principled.inputs["Normal"])

    if couleur_variation is not None:
        bruit_patch = nodes.new("ShaderNodeTexNoise")
        bruit_patch.location = (-400, 150)
        bruit_patch.inputs["Scale"].default_value = echelle_patch

        rampe = nodes.new("ShaderNodeValToRGB")
        rampe.location = (-150, 150)
        rampe.color_ramp.elements[0].position = 0.35
        rampe.color_ramp.elements[1].position = 0.65

        mix_couleur = nodes.new("ShaderNodeMixRGB")
        mix_couleur.location = (100, 150)
        mix_couleur.inputs["Color1"].default_value = (*couleur_rgb, 1.0)
        mix_couleur.inputs["Color2"].default_value = (*couleur_variation, 1.0)

        links.new(bruit_patch.outputs["Fac"], rampe.inputs["Fac"])
        links.new(rampe.outputs["Color"], mix_couleur.inputs["Fac"])
        links.new(mix_couleur.outputs["Color"], principled.inputs["Base Color"])

    links.new(principled.outputs["BSDF"], sortie.inputs["Surface"])

    return mat


if __name__ == "__main__":
    mat_banco = _materiau_pbr(
        "Case_Banco_mur",
        couleur_rgb=(0.58, 0.36, 0.20),
        roughness=0.92,
        echelle_grain=15.0,
        intensite_grain=0.35,
        couleur_variation=(0.44, 0.27, 0.15),
        echelle_patch=3.5,
    )
    mat_paille = _materiau_pbr(
        "Case_Paille_toit",
        couleur_rgb=(0.68, 0.54, 0.26),  # utilisé seulement si la texture ne charge pas (secours)
        roughness=0.85,
        echelle_grain=45.0,
        intensite_grain=0.6,   # plus fort que le procédural : le relief suit maintenant la vraie photo
        anisotropic=0.35,
        texture_reference="chaume_ref.jpg",
        echelle_texture=2.5,
    )
    mat_bois = _materiau_pbr(
        "Case_Bois_porte",
        couleur_rgb=(0.28, 0.16, 0.09),
        roughness=0.75,
        echelle_grain=60.0,
        intensite_grain=0.2,
        couleur_variation=(0.20, 0.11, 0.06),
        echelle_patch=8.0,
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

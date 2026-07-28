"""
Matériaux — cases en banco (Wuro & Galle)

3 matériaux, même technique que les précédents (Principled BSDF + grain
procédural Noise -> Bump), appliqués selon le nom d'objet :
  - "*_Mur"   -> banco (terre/argile séchée), très mat, grain marqué
                 (paroi montée à la main, pas lisse).
  - "*_Toit"  -> paille de toiture, dorée, mate, grain fibreux.
  - "*_Porte" -> bois brut, même teinte que les autres bois du projet
                 (armature, mortier/pilon) pour une cohérence visuelle
                 entre tous les éléments en bois de la scène.

Exécution : après case_banco.py (les objets Case*_Mur/_Toit/_Porte
doivent exister). Onglet Scripting > Open > ce fichier > Run Script.
"""

import bpy


def _materiau_pbr(nom, couleur_rgb, roughness, echelle_grain, intensite_grain,
                   couleur_variation=None, echelle_patch=4.0, anisotropic=0.0):
    """
    Version enrichie : en plus du grain (Noise -> Bump, micro-relief de surface),
    ajoute une VARIATION DE COULEUR à plus grande échelle (Noise -> ColorRamp ->
    Mix Color) pour casser l'aplat uniforme d'avant — un mur en banco ou un toit
    de chaume n'a jamais une teinte parfaitement constante (tachage, usure,
    irrégularités du séchage). Reste 100% procédural (pas de texture externe,
    cohérent avec le choix déjà fait pour le Terrain — question de licence/source).

    couleur_variation : deuxième teinte mélangée avec couleur_rgb (si None, pas
    de variation de couleur, juste l'ancien comportement).
    anisotropic : reflet directionnel (utile pour la paille, fibres alignées).
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

    # Grain fin (micro-relief de surface, inchangé par rapport à avant).
    bruit_grain = nodes.new("ShaderNodeTexNoise")
    bruit_grain.location = (-400, -250)
    bruit_grain.inputs["Scale"].default_value = echelle_grain

    bump = nodes.new("ShaderNodeBump")
    bump.location = (0, -250)
    bump.inputs["Strength"].default_value = intensite_grain

    links.new(bruit_grain.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], principled.inputs["Normal"])

    if couleur_variation is not None:
        # Variation de couleur à plus grande échelle (patchs), mélangée avec la
        # teinte de base via un Noise différent (basse fréquence) + ColorRamp
        # pour contrôler le contraste des patchs plutôt qu'un dégradé continu.
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
        echelle_grain=15.0,   # grain plus large : paroi montée à la main, pas un tissage fin
        intensite_grain=0.35,
        couleur_variation=(0.44, 0.27, 0.15),  # patchs plus sombres : usure/humidité, séchage irrégulier
        echelle_patch=3.5,
    )
    mat_paille = _materiau_pbr(
        "Case_Paille_toit",
        couleur_rgb=(0.68, 0.54, 0.26),
        roughness=0.85,
        echelle_grain=45.0,
        intensite_grain=0.25,
        couleur_variation=(0.52, 0.40, 0.18),  # brins plus foncés/vieillis mélangés au chaume neuf
        echelle_patch=6.0,
        anisotropic=0.35,  # reflet directionnel : fibres de chaume alignées, pas une surface mate uniforme
    )
    mat_bois = _materiau_pbr(
        "Case_Bois_porte",
        couleur_rgb=(0.28, 0.16, 0.09),  # même teinte que le bois brut des autres modules
        roughness=0.75,
        echelle_grain=60.0,
        intensite_grain=0.2,
        couleur_variation=(0.20, 0.11, 0.06),  # veinage bois plus sombre
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

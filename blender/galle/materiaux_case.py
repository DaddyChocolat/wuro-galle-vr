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


def _materiau_pbr(nom, couleur_rgb, roughness, echelle_grain, intensite_grain):
    mat = bpy.data.materials.new(name=nom)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    sortie = nodes.new("ShaderNodeOutputMaterial")
    sortie.location = (400, 0)

    principled = nodes.new("ShaderNodeBsdfPrincipled")
    principled.location = (100, 0)
    principled.inputs["Base Color"].default_value = (*couleur_rgb, 1.0)
    principled.inputs["Roughness"].default_value = roughness

    bruit = nodes.new("ShaderNodeTexNoise")
    bruit.location = (-400, -200)
    bruit.inputs["Scale"].default_value = echelle_grain

    bump = nodes.new("ShaderNodeBump")
    bump.location = (-150, -200)
    bump.inputs["Strength"].default_value = intensite_grain

    links.new(bruit.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], principled.inputs["Normal"])
    links.new(principled.outputs["BSDF"], sortie.inputs["Surface"])

    return mat


if __name__ == "__main__":
    mat_banco = _materiau_pbr(
        "Case_Banco_mur",
        couleur_rgb=(0.58, 0.36, 0.20),
        roughness=0.92,
        echelle_grain=15.0,   # grain plus large : paroi montée à la main, pas un tissage fin
        intensite_grain=0.35,
    )
    mat_paille = _materiau_pbr(
        "Case_Paille_toit",
        couleur_rgb=(0.68, 0.54, 0.26),
        roughness=0.85,
        echelle_grain=45.0,
        intensite_grain=0.25,
    )
    mat_bois = _materiau_pbr(
        "Case_Bois_porte",
        couleur_rgb=(0.28, 0.16, 0.09),  # même teinte que le bois brut des autres modules
        roughness=0.75,
        echelle_grain=60.0,
        intensite_grain=0.2,
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

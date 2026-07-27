"""
Matériaux — dudal et grenier (Wuro & Galle)

Même technique que materiaux_case.py (Principled BSDF + grain procédural),
appliqués selon le nom d'objet :
  - "Dudal_Sol"        -> terre battue/balayée, plus claire et plus lisse
                          que le sol latérite environnant (aire entretenue,
                          pas un sol naturel) — roughness plus faible.
  - "Grenier_Bac"      -> même famille que le banco des cases, mais un peu
                          plus clair (argile+paille tressée, technique de
                          stockage différente du mur d'habitation).
  - "Grenier_Toit"     -> paille, réutilise la teinte du toit des cases.
  - "Grenier_Pilotis_*" -> bois brut, même teinte que les autres bois.

Exécution : après dudal_grenier.py (les objets doivent exister).
Onglet Scripting > Open > ce fichier > Run Script.
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
    mat_dudal = _materiau_pbr(
        "Dudal_Terre_battue",
        couleur_rgb=(0.62, 0.48, 0.32),  # plus clair que Sol_Laterite : terre entretenue
        roughness=0.7,                    # plus lisse : aire balayée régulièrement
        echelle_grain=25.0,
        intensite_grain=0.15,              # grain discret, pas de relief marqué
    )
    mat_grenier_bac = _materiau_pbr(
        "Grenier_Argile_tressee",
        couleur_rgb=(0.62, 0.40, 0.24),
        roughness=0.88,
        echelle_grain=20.0,
        intensite_grain=0.3,
    )
    mat_grenier_toit = _materiau_pbr(
        "Grenier_Paille",
        couleur_rgb=(0.68, 0.54, 0.26),  # même teinte que Case_Paille_toit
        roughness=0.85,
        echelle_grain=45.0,
        intensite_grain=0.25,
    )
    mat_pilotis = _materiau_pbr(
        "Grenier_Bois_pilotis",
        couleur_rgb=(0.28, 0.16, 0.09),  # même teinte que le bois brut des autres modules
        roughness=0.75,
        echelle_grain=60.0,
        intensite_grain=0.2,
    )

    compteurs = {"Dudal_Sol": 0, "Grenier_Bac": 0, "Grenier_Toit": 0, "Grenier_Pilotis": 0}

    for obj in bpy.data.objects:
        if obj.name == "Dudal_Sol":
            obj.data.materials.clear()
            obj.data.materials.append(mat_dudal)
            compteurs["Dudal_Sol"] += 1
        elif obj.name == "Grenier_Bac":
            obj.data.materials.clear()
            obj.data.materials.append(mat_grenier_bac)
            compteurs["Grenier_Bac"] += 1
        elif obj.name == "Grenier_Toit":
            obj.data.materials.clear()
            obj.data.materials.append(mat_grenier_toit)
            compteurs["Grenier_Toit"] += 1
        elif obj.name.startswith("Grenier_Pilotis"):
            obj.data.materials.clear()
            obj.data.materials.append(mat_pilotis)
            compteurs["Grenier_Pilotis"] += 1

    print("\n--- Matériaux dudal/grenier assignés ---")
    for nom, n in compteurs.items():
        print(f"  {nom:16s} : {n} objet(s)")
    if sum(compteurs.values()) == 0:
        print("ATTENTION : aucun objet trouvé — lance d'abord dudal_grenier.py")

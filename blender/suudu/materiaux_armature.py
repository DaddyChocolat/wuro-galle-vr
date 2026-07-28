"""
Matériau — armature suudu (Wuro & Galle)

Même technique que les matériaux du mobilier : Principled BSDF + grain
procédural (Noise -> Bump). Couleur bois de branche brute, non poncée —
plus terne et plus rugueuse que le bois des objets domestiques (mortier,
pilon), pour marquer la différence entre une branche coupée telle quelle
et du bois travaillé.

Exécution : après armature_suudu.py (les 6 objets Suudu_Arche_* doivent
exister). Onglet Scripting > Open > ce fichier > Run Script.
"""

import bpy


def creer_materiau_branche():
    """
    Comme les autres matériaux du projet : grain fin (Noise -> Bump) + variation
    de couleur à plus grande échelle (Noise -> ColorRamp -> Mix) pour casser
    l'aplat uniforme — une branche brute a des zones plus grises/usées, pas une
    teinte parfaitement constante sur toute sa longueur.
    """
    mat = bpy.data.materials.new(name="Branche_bois_brut")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    sortie = nodes.new("ShaderNodeOutputMaterial")
    sortie.location = (600, 0)

    principled = nodes.new("ShaderNodeBsdfPrincipled")
    principled.location = (300, 0)
    principled.inputs["Base Color"].default_value = (0.32, 0.24, 0.16, 1.0)  # brun grisé, bois brut
    principled.inputs["Roughness"].default_value = 0.82

    bruit_grain = nodes.new("ShaderNodeTexNoise")
    bruit_grain.location = (-400, -250)
    bruit_grain.inputs["Scale"].default_value = 60.0  # grain fin, fibres du bois

    bump = nodes.new("ShaderNodeBump")
    bump.location = (0, -250)
    bump.inputs["Strength"].default_value = 0.2

    links.new(bruit_grain.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], principled.inputs["Normal"])

    bruit_patch = nodes.new("ShaderNodeTexNoise")
    bruit_patch.location = (-400, 150)
    bruit_patch.inputs["Scale"].default_value = 5.0

    rampe = nodes.new("ShaderNodeValToRGB")
    rampe.location = (-150, 150)
    rampe.color_ramp.elements[0].position = 0.35
    rampe.color_ramp.elements[1].position = 0.65

    mix_couleur = nodes.new("ShaderNodeMixRGB")
    mix_couleur.location = (100, 150)
    mix_couleur.inputs["Color1"].default_value = (0.32, 0.24, 0.16, 1.0)
    mix_couleur.inputs["Color2"].default_value = (0.22, 0.20, 0.18, 1.0)  # zones grisées/usées

    links.new(bruit_patch.outputs["Fac"], rampe.inputs["Fac"])
    links.new(rampe.outputs["Color"], mix_couleur.inputs["Fac"])
    links.new(mix_couleur.outputs["Color"], principled.inputs["Base Color"])

    links.new(principled.outputs["BSDF"], sortie.inputs["Surface"])

    return mat


if __name__ == "__main__":
    mat = creer_materiau_branche()
    n_assignes = 0
    for obj in bpy.data.objects:
        if obj.name.startswith("Suudu_Arche"):
            obj.data.materials.clear()
            obj.data.materials.append(mat)
            n_assignes += 1
    print(f"\nMatériau '{mat.name}' assigné à {n_assignes} arche(s).")
    if n_assignes == 0:
        print("ATTENTION : aucun objet 'Suudu_Arche_*' trouvé — lance d'abord armature_suudu.py")

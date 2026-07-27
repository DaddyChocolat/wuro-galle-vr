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
    mat = bpy.data.materials.new(name="Branche_bois_brut")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()

    sortie = nodes.new("ShaderNodeOutputMaterial")
    sortie.location = (400, 0)

    principled = nodes.new("ShaderNodeBsdfPrincipled")
    principled.location = (100, 0)
    principled.inputs["Base Color"].default_value = (0.32, 0.24, 0.16, 1.0)  # brun grisé, bois brut
    principled.inputs["Roughness"].default_value = 0.82

    bruit = nodes.new("ShaderNodeTexNoise")
    bruit.location = (-400, -200)
    bruit.inputs["Scale"].default_value = 60.0  # grain fin, fibres du bois

    bump = nodes.new("ShaderNodeBump")
    bump.location = (-150, -200)
    bump.inputs["Strength"].default_value = 0.2

    links.new(bruit.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], principled.inputs["Normal"])
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

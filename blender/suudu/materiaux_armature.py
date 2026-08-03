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
    Couleur plate garantie compatible glTF (même principe que materiaux_case.py
    / materiaux_mobilier.py) : le Base Color était auparavant piloté par un
    graphe Noise -> ColorRamp -> Mix pour varier la teinte le long de la
    branche, mais ce type de graphe n'est pas reconnu par l'exporteur glTF —
    il retombe silencieusement sur un matériau blanc par défaut (voir
    rapport-reflexif.md 3.3, déjà corrigé pour les cases mais pas ici).
    Le grain fin (Noise -> Bump -> Normal) reste : un Normal ne bloque pas
    l'export, seul le Base Color doit rester une couleur plate ou une texture
    image reliée aux UV.
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
    principled.inputs["Base Color"].default_value = (0.30, 0.22, 0.15, 1.0)  # brun grisé, bois brut
    principled.inputs["Roughness"].default_value = 0.82

    bruit_grain = nodes.new("ShaderNodeTexNoise")
    bruit_grain.location = (-400, -250)
    bruit_grain.inputs["Scale"].default_value = 60.0  # grain fin, fibres du bois

    bump = nodes.new("ShaderNodeBump")
    bump.location = (0, -250)
    bump.inputs["Strength"].default_value = 0.2

    links.new(bruit_grain.outputs["Fac"], bump.inputs["Height"])
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

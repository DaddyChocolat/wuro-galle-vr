"""
Matériaux de base — objets domestiques simples (Wuro & Galle)

Crée un matériau PBR simple pour chacun des 4 objets générés par
generer_mobilier.py, à exécuter après ce premier script (les objets
Calebasse, Mortier, Pilon, Natte doivent déjà exister dans la scène).

Chaque matériau est construit autour du nœud Principled BSDF (voir
explication dans le chat) : une couleur de base (Base Color) et une
rugosité (Roughness). On ajoute une texture de bruit procédurale (Noise
Texture -> Bump -> Normal) pour donner un léger grain de surface sans
avoir besoin d'une image peinte à la main — suffisant à ce stade du
projet ; les vraies textures peintes/photographiées viendront à l'étape
de texturing complète (O2).

Exécution : onglet Scripting > Open > ce fichier > Run Script.
"""

import bpy


def creer_materiau_pbr(nom, couleur_rgb, roughness, intensite_grain=0.15):
    """
    Crée un matériau avec Principled BSDF + un léger grain procédural.

    couleur_rgb : tuple (R, G, B) entre 0 et 1 (pas de valeur alpha ici).
    roughness   : 0.0 = poli/brillant, 1.0 = totalement mat.
    intensite_grain : force du relief de surface (bump), 0 = surface lisse.
    """
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
    bruit.inputs["Scale"].default_value = 40.0  # grain fin, pas de grosses taches

    bump = nodes.new("ShaderNodeBump")
    bump.location = (-150, -200)
    bump.inputs["Strength"].default_value = intensite_grain

    links.new(bruit.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], principled.inputs["Normal"])
    links.new(principled.outputs["BSDF"], sortie.inputs["Surface"])

    return mat


def assigner(nom_objet, materiau):
    obj = bpy.data.objects.get(nom_objet)
    if obj is None:
        print(f"  ATTENTION : objet '{nom_objet}' introuvable — lance d'abord generer_mobilier.py")
        return
    obj.data.materials.clear()
    obj.data.materials.append(materiau)
    print(f"  {nom_objet:12s} -> matériau '{materiau.name}' assigné")


if __name__ == "__main__":
    print("\n--- Assignation des matériaux ---")

    mat_calebasse = creer_materiau_pbr(
        "Calebasse_bois_pyrogravé",
        couleur_rgb=(0.35, 0.18, 0.08),   # brun chaud, écorce séchée
        roughness=0.55,                    # légèrement poli, pas mat total
        intensite_grain=0.12,
    )
    assigner("Calebasse", mat_calebasse)

    mat_bois_brut = creer_materiau_pbr(
        "Bois_brut",
        couleur_rgb=(0.28, 0.16, 0.09),   # brun bois brut, un peu plus terne
        roughness=0.75,
        intensite_grain=0.25,              # surface plus rugueuse (non poncée)
    )
    assigner("Mortier", mat_bois_brut)
    assigner("Pilon", mat_bois_brut)  # même matériau, même origine (bois brut)

    mat_paille = creer_materiau_pbr(
        "Paille_tressée",
        couleur_rgb=(0.62, 0.48, 0.24),   # doré/paille
        roughness=0.85,                    # très mat, fibreux
        intensite_grain=0.3,
    )
    assigner("Natte", mat_paille)

    print("--- Terminé ---\n")

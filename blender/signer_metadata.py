"""
Signature numérique discrète — Wuro & Galle VR

Insère "Daddy_Chocolat_36" dans le champ standard "copyright" de la section
asset de chaque .glb (métadonnée officielle glTF 2.0, invisible à l'écran,
lisible par tout inspecteur de fichier — preuve d'auteur/d'antériorité).

Script Python pur, ne nécessite pas bpy : exécutable depuis l'onglet
Scripting de Blender (Open > ce fichier > Run Script) ou n'importe quel
Python installé sur la machine.

Modifie les fichiers en place. À lancer une seule fois, après tous les
exports .glb finaux (un nouvel export écraserait la signature).
"""

import json
import os
import struct

SIGNATURE = "Daddy_Chocolat_36"
RACINE_BLENDER = os.path.dirname(os.path.abspath(__file__))  # dossier blender/


def signer_glb(chemin):
    with open(chemin, "rb") as f:
        data = f.read()

    if data[:4] != b"glTF":
        print(f"  ignoré (pas un .glb valide) : {chemin}")
        return False

    version, _longueur_totale = struct.unpack_from("<II", data, 4)
    offset = 12
    chunk_json = None
    chunks_apres = []

    while offset < len(data):
        chunk_len, chunk_type = struct.unpack_from("<I4s", data, offset)
        chunk_data_start = offset + 8
        chunk_data = data[chunk_data_start:chunk_data_start + chunk_len]
        if chunk_type == b"JSON":
            chunk_json = chunk_data
        else:
            chunks_apres.append((chunk_type, chunk_data))
        offset = chunk_data_start + chunk_len

    if chunk_json is None:
        print(f"  ignoré (pas de chunk JSON) : {chemin}")
        return False

    asset_json = json.loads(chunk_json.decode("utf-8"))
    asset_json.setdefault("asset", {})
    asset_json["asset"]["copyright"] = SIGNATURE

    nouveau_json = json.dumps(asset_json, separators=(",", ":")).encode("utf-8")
    while len(nouveau_json) % 4 != 0:  # padding requis par le format glTF
        nouveau_json += b" "

    sortie = bytearray()
    sortie += b"glTF"
    sortie += struct.pack("<II", version, 0)  # longueur totale, corrigée plus bas
    sortie += struct.pack("<I4s", len(nouveau_json), b"JSON")
    sortie += nouveau_json
    for chunk_type, chunk_data in chunks_apres:
        sortie += struct.pack("<I4s", len(chunk_data), chunk_type)
        sortie += chunk_data

    struct.pack_into("<I", sortie, 8, len(sortie))

    with open(chemin, "wb") as f:
        f.write(sortie)

    print(f"  signé : {chemin}")
    return True


if __name__ == "__main__":
    print(f"--- Signature '{SIGNATURE}' — insertion dans tous les .glb ---")
    n = 0
    for dossier, _, fichiers in os.walk(RACINE_BLENDER):
        for nom in fichiers:
            if nom.lower().endswith(".glb"):
                if signer_glb(os.path.join(dossier, nom)):
                    n += 1
    print(f"\n{n} fichier(s) .glb signé(s).")

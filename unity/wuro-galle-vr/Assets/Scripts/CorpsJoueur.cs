using UnityEngine;

/// <summary>
/// Corps du joueur : silhouette simple (capsules/sphère), même esprit que le
/// placeholder des PNJ dans SceneBuilder.CreerPNJPlaceholder — "silhouette
/// sans traits individualisés" (note éthique) en attendant un vrai modèle
/// stylisé fait en Blender. Sert à donner au joueur un corps visible pendant
/// les interactions scriptées à la 3e personne (prier, s'asseoir — voir
/// CameraTierceUtils), pas en vue FPS normale (le corps resterait invisible/
/// clippé si affiché juste sous une caméra placée à hauteur des yeux).
///
/// N'a pas de squelette articulé : les poses (Debout/Genoux/Assis) sont de
/// simples préréglages de position/rotation sur quelques capsules, pas une
/// animation osseuse — simplification assumée pour rester dans le même
/// registre "placeholder fonctionnel" que le reste du projet (voir
/// AudioProceduralUtils, CreerTextureProceduraleSol) en attendant un vrai
/// personnage rigged si le temps le permet plus tard.
/// </summary>
public class CorpsJoueur : MonoBehaviour
{
    public enum Pose { Debout, Genoux, Assis }

    private Transform bassin, torse, tete, brasG, brasD, jambeG, jambeD;
    private Vector3 bassinPosDebout;
    private Quaternion torseRotDebout, jambeGRotDebout, jambeDRotDebout;

    void Awake()
    {
        Construire();
        gameObject.SetActive(false); // invisible par défaut (vue FPS) — CameraTierceUtils l'active pour les scènes 3e personne
    }

    void Construire()
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.16f, 0.13f, 0.11f); // même teinte neutre/sombre que le placeholder PNJ

        bassin = CreerCapsule("Bassin", transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.28f, 0.18f, 0.28f), mat);
        bassinPosDebout = bassin.localPosition;

        torse = CreerCapsule("Torse", bassin, new Vector3(0f, 0.55f, 0f), new Vector3(0.24f, 0.35f, 0.16f), mat);
        torseRotDebout = torse.localRotation;

        tete = CreerSphere("Tete", torse, new Vector3(0f, 0.62f, 0f), 0.14f, mat);

        brasG = CreerCapsule("Bras_G", torse, new Vector3(-0.28f, 0.15f, 0f), new Vector3(0.07f, 0.32f, 0.07f), mat);
        brasD = CreerCapsule("Bras_D", torse, new Vector3(0.28f, 0.15f, 0f), new Vector3(0.07f, 0.32f, 0.07f), mat);

        jambeG = CreerCapsule("Jambe_G", bassin, new Vector3(-0.12f, -0.45f, 0f), new Vector3(0.1f, 0.45f, 0.1f), mat);
        jambeD = CreerCapsule("Jambe_D", bassin, new Vector3(0.12f, -0.45f, 0f), new Vector3(0.1f, 0.45f, 0.1f), mat);
        jambeGRotDebout = jambeG.localRotation;
        jambeDRotDebout = jambeD.localRotation;
    }

    static Transform CreerCapsule(string nom, Transform parent, Vector3 posLocale, Vector3 echelle, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = nom;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = posLocale;
        go.transform.localScale = echelle;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // pas d'obstacle physique, purement visuel
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    static Transform CreerSphere(string nom, Transform parent, Vector3 posLocale, float rayon, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = nom;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = posLocale;
        go.transform.localScale = Vector3.one * rayon * 2f;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

    /// <summary>Applique instantanément une pose (pas d'interpolation ici — CameraTierceUtils gère le fondu de la caméra autour).</summary>
    public void AppliquerPose(Pose pose)
    {
        switch (pose)
        {
            case Pose.Debout:
                bassin.localPosition = bassinPosDebout;
                torse.localRotation = torseRotDebout;
                jambeG.localRotation = jambeGRotDebout;
                jambeD.localRotation = jambeDRotDebout;
                break;

            case Pose.Genoux: // prière : à genoux, buste incliné vers l'avant
                bassin.localPosition = bassinPosDebout + new Vector3(0f, -0.5f, 0f);
                torse.localRotation = torseRotDebout * Quaternion.Euler(45f, 0f, 0f);
                jambeG.localRotation = jambeGRotDebout * Quaternion.Euler(-100f, 0f, 0f);
                jambeD.localRotation = jambeDRotDebout * Quaternion.Euler(-100f, 0f, 0f);
                break;

            case Pose.Assis: // s'asseoir sur la natte : bassin abaissé, buste droit, jambes repliées vers l'avant
                bassin.localPosition = bassinPosDebout + new Vector3(0f, -0.45f, -0.05f);
                torse.localRotation = torseRotDebout * Quaternion.Euler(10f, 0f, 0f);
                jambeG.localRotation = jambeGRotDebout * Quaternion.Euler(-80f, 8f, 0f);
                jambeD.localRotation = jambeDRotDebout * Quaternion.Euler(-80f, -8f, 0f);
                break;
        }
    }
}

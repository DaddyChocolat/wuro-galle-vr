using UnityEngine;

/// <summary>
/// Corps du joueur : silhouette stylisée articulée, modélisée en Blender
/// (blender/personnage/personnage_joueur.py — solides de révolution tronconiques,
/// pas de capsules brutes) et importée en glTF. Remplace l'ancien placeholder
/// de capsules générées en code : même esprit "silhouette sans traits
/// individualisés" (note éthique, *semteende*), mais des proportions et une
/// silhouette réelles plutôt que des primitives.
///
/// La hiérarchie (Bassin > Torse > Tête / Bras_G / Bras_D, Bassin > Jambe_G /
/// Jambe_D) est déjà construite à l'édition par SceneBuilder.CreerJoueur —
/// ce composant se contente de RETROUVER les transforms par nom (recherche
/// récursive, la profondeur exacte n'importe pas) et d'appliquer les poses.
/// Chaque segment a son origine à l'articulation réelle (hanche/taille/
/// épaule, voir le script Blender) plutôt qu'au centre géométrique d'une
/// capsule : une rotation locale pivote donc vraiment depuis l'articulation.
///
/// Invisible par défaut (Awake() désactive gameObject) — affiché seulement
/// pendant les séquences 3e personne (prier/s'asseoir, voir
/// CameraTierceUtils), pour la même raison que l'ancien placeholder : un
/// corps visible sous une caméra à hauteur des yeux se clipperait en vue FPS
/// normale.
/// </summary>
public class CorpsJoueur : MonoBehaviour
{
    public enum Pose { Debout, Genoux, Assis }

    private Transform bassin, torse, tete, brasG, brasD, jambeG, jambeD;
    private Vector3 bassinPosDebout;
    private Quaternion torseRotDebout, jambeGRotDebout, jambeDRotDebout;
    private bool pretACondition;

    void Awake()
    {
        bassin = TrouverRecursif(transform, "Bassin");
        torse = bassin != null ? TrouverRecursif(bassin, "Torse") : null;
        tete = torse != null ? TrouverRecursif(torse, "Tete") : null;
        brasG = torse != null ? TrouverRecursif(torse, "Bras_G") : null;
        brasD = torse != null ? TrouverRecursif(torse, "Bras_D") : null;
        jambeG = bassin != null ? TrouverRecursif(bassin, "Jambe_G") : null;
        jambeD = bassin != null ? TrouverRecursif(bassin, "Jambe_D") : null;

        pretACondition = bassin != null && torse != null && jambeG != null && jambeD != null;
        if (!pretACondition)
        {
            Debug.LogWarning("[CorpsJoueur] Hiérarchie incomplète (Bassin/Torse/Jambe_G/Jambe_D introuvables) — " +
                "vérifie que SceneBuilder.CreerJoueur a bien instancié et reparenté Personnage.glb.");
            gameObject.SetActive(false);
            return;
        }

        bassinPosDebout = bassin.localPosition;
        torseRotDebout = torse.localRotation;
        jambeGRotDebout = jambeG.localRotation;
        jambeDRotDebout = jambeD.localRotation;

        gameObject.SetActive(false); // invisible par défaut (vue FPS) — CameraTierceUtils l'active pour les scènes 3e personne
    }

    /// <summary>Recherche récursive par nom exact, comme SceneBuilder.TrouverEnfant — nécessaire ici aussi car ce composant n'a pas accès à l'assembly Editor.</summary>
    static Transform TrouverRecursif(Transform racine, string nom)
    {
        foreach (Transform enfant in racine)
        {
            if (enfant.name == nom) return enfant;
            var trouve = TrouverRecursif(enfant, nom);
            if (trouve != null) return trouve;
        }
        return null;
    }

    /// <summary>Applique instantanément une pose (pas d'interpolation ici — CameraTierceUtils gère le fondu de la caméra autour).</summary>
    public void AppliquerPose(Pose pose)
    {
        if (!pretACondition) return;

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

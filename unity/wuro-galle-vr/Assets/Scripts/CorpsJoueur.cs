using UnityEngine;

/// <summary>
/// Corps du joueur : base humaine réelle en CC0 (voir
/// blender/personnage/personnage_joueur.py — "Human Basemeshes" par
/// treesclimber, OpenGameArt.org, licence CC0/domaine public), squelette
/// complet importé (skinning réel, pas des segments rigides) — remplace la
/// version précédente (solides de révolution tronconiques, jambe en un seul
/// segment sans genou). Sans visage ni traits sculptés (le mesh source n'en
/// a pas), teinte de peau appliquée dans le script Blender — silhouette
/// toujours sans traits individualisés (note éthique, *semteende*).
///
/// La hiérarchie d'os (hips > spine > chest > ..., hips > thigh.L/R >
/// shin.L/R > foot.L/R) vient directement de l'armature Blender/glTF —
/// aucun reparentage nécessaire côté SceneBuilder cette fois (contrairement
/// à la version précédente : le squelette est déjà correctement construit
/// à l'export, pas assemblé à partir de pièces plates).
///
/// Invisible par défaut (Awake() désactive gameObject) — affiché seulement
/// pendant les séquences 3e personne (prier/s'asseoir, voir
/// CameraTierceUtils).
/// </summary>
public class CorpsJoueur : MonoBehaviour
{
    public enum Pose { Debout, Genoux, Assis }

    private Transform hips, spine, chest, thighG, shinG, thighD, shinD, brasG, brasD;
    private Vector3 hipsPosDebout;
    private Quaternion spineRotDebout, chestRotDebout, thighGRotDebout, shinGRotDebout, thighDRotDebout, shinDRotDebout, brasGRotDebout, brasDRotDebout;
    private bool pretACondition;

    void Awake()
    {
        hips = TrouverRecursif(transform, "hips");
        spine = hips != null ? TrouverRecursif(hips, "spine") : null;
        chest = spine != null ? TrouverRecursif(spine, "chest") : null;
        thighG = hips != null ? TrouverRecursif(hips, "thigh.L") : null;
        shinG = thighG != null ? TrouverRecursif(thighG, "shin.L") : null;
        thighD = hips != null ? TrouverRecursif(hips, "thigh.R") : null;
        shinD = thighD != null ? TrouverRecursif(thighD, "shin.R") : null;
        // Pose de repos (bind) du squelette source = T-pose (bras à l'horizontale) —
        // sans correction, "Genoux"/"Assis" gardait les bras tendus sur le côté,
        // ce qui casse complètement l'effet (bug visuel réel constaté sur capture
        // d'écran). upper_arm.L/R ramène les bras le long du corps.
        brasG = chest != null ? TrouverRecursif(chest, "upper_arm.L") : null;
        brasD = chest != null ? TrouverRecursif(chest, "upper_arm.R") : null;

        pretACondition = hips != null && spine != null && thighG != null && shinG != null && thighD != null && shinD != null;
        if (!pretACondition)
        {
            Debug.LogWarning("[CorpsJoueur] Squelette incomplet (hips/spine/thigh/shin introuvables) — " +
                "vérifie que SceneBuilder.CreerJoueur a bien instancié Personnage.glb.");
            gameObject.SetActive(false);
            return;
        }

        hipsPosDebout = hips.localPosition;
        spineRotDebout = spine.localRotation;
        chestRotDebout = chest != null ? chest.localRotation : Quaternion.identity;
        thighGRotDebout = thighG.localRotation;
        shinGRotDebout = shinG.localRotation;
        thighDRotDebout = thighD.localRotation;
        shinDRotDebout = shinD.localRotation;
        brasGRotDebout = brasG != null ? brasG.localRotation : Quaternion.identity;
        brasDRotDebout = brasD != null ? brasD.localRotation : Quaternion.identity;

        gameObject.SetActive(false); // invisible par défaut (vue FPS) — CameraTierceUtils l'active pour les scènes 3e personne
    }

    /// <summary>Recherche récursive par nom exact — nécessaire ici aussi car ce composant n'a pas accès à l'assembly Editor (voir SceneBuilder.TrouverEnfant, même logique).</summary>
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
                hips.localPosition = hipsPosDebout;
                spine.localRotation = spineRotDebout;
                if (chest != null) chest.localRotation = chestRotDebout;
                thighG.localRotation = thighGRotDebout;
                shinG.localRotation = shinGRotDebout;
                thighD.localRotation = thighDRotDebout;
                shinD.localRotation = shinDRotDebout;
                if (brasG != null) brasG.localRotation = brasGRotDebout;
                if (brasD != null) brasD.localRotation = brasDRotDebout;
                break;

            case Pose.Genoux: // prière : à genoux, buste incliné vers l'avant — genou réel plié (shin replié sous la cuisse), bras ramenés le long du corps (pose de repos du squelette = T-pose)
                hips.localPosition = hipsPosDebout + new Vector3(0f, -0.42f, 0f);
                spine.localRotation = spineRotDebout * Quaternion.Euler(30f, 0f, 0f);
                if (chest != null) chest.localRotation = chestRotDebout * Quaternion.Euler(15f, 0f, 0f);
                thighG.localRotation = thighGRotDebout * Quaternion.Euler(-15f, 0f, 0f);
                shinG.localRotation = shinGRotDebout * Quaternion.Euler(150f, 0f, 0f);
                thighD.localRotation = thighDRotDebout * Quaternion.Euler(-15f, 0f, 0f);
                shinD.localRotation = shinDRotDebout * Quaternion.Euler(150f, 0f, 0f);
                if (brasG != null) brasG.localRotation = brasGRotDebout * Quaternion.Euler(0f, 0f, 75f);
                if (brasD != null) brasD.localRotation = brasDRotDebout * Quaternion.Euler(0f, 0f, -75f);
                break;

            case Pose.Assis: // s'asseoir sur la natte : hanches basses, cuisses relevées vers l'avant, genou plié, bras le long du corps
                hips.localPosition = hipsPosDebout + new Vector3(0f, -0.48f, -0.05f);
                spine.localRotation = spineRotDebout * Quaternion.Euler(5f, 0f, 0f);
                if (chest != null) chest.localRotation = chestRotDebout * Quaternion.Euler(5f, 0f, 0f);
                thighG.localRotation = thighGRotDebout * Quaternion.Euler(-85f, 5f, 0f);
                shinG.localRotation = shinGRotDebout * Quaternion.Euler(95f, 0f, 0f);
                thighD.localRotation = thighDRotDebout * Quaternion.Euler(-85f, -5f, 0f);
                shinD.localRotation = shinDRotDebout * Quaternion.Euler(95f, 0f, 0f);
                if (brasG != null) brasG.localRotation = brasGRotDebout * Quaternion.Euler(0f, 0f, 70f);
                if (brasD != null) brasD.localRotation = brasDRotDebout * Quaternion.Euler(0f, 0f, -70f);
                break;
        }
    }
}

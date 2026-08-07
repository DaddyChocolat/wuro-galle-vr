using UnityEngine;

/// <summary>
/// Sons courts générés en code (synthèse simple : sinusoïdes, bruit filtré,
/// enveloppes), pour les nouvelles interactions (boire, piler, souffler sur
/// le feu) tant qu'aucun enregistrement réel n'a été sourcé. Même logique
/// que CreerTextureProceduraleSol dans SceneBuilder.cs pour le sol : un
/// placeholder fonctionnel et sans question de licence (rien de téléchargé,
/// voir note-ethique.md), explicitement remplaçable plus tard par un vrai
/// enregistrement/sample Freesound si le temps le permet.
/// </summary>
public static class AudioProceduralUtils
{
    const int FrequenceEchantillonnage = 44100;

    /// <summary>Gorgée d'eau : 2-3 "glous" (sinusoïde grave descendante + enveloppe rapide) espacés, avec un léger bruit pour l'aspect liquide.</summary>
    public static AudioClip ClipGorgeeEau()
    {
        float duree = 0.55f;
        int nEch = Mathf.RoundToInt(duree * FrequenceEchantillonnage);
        var donnees = new float[nEch];

        float[] instants = { 0.03f, 0.20f, 0.37f };
        var rng = new System.Random(1);

        foreach (float debut in instants)
        {
            int iDebut = Mathf.RoundToInt(debut * FrequenceEchantillonnage);
            int longueurPulse = Mathf.RoundToInt(0.14f * FrequenceEchantillonnage);

            for (int i = 0; i < longueurPulse && iDebut + i < nEch; i++)
            {
                float t = i / (float)FrequenceEchantillonnage;
                float enveloppe = Mathf.Exp(-t * 22f) * Mathf.Clamp01(t / 0.005f); // attaque quasi instantanée, décroissance exponentielle
                float frequence = Mathf.Lerp(210f, 140f, t / 0.14f); // "glou" descendant
                float onde = Mathf.Sin(2f * Mathf.PI * frequence * t);
                float bruit = ((float)rng.NextDouble() * 2f - 1f) * 0.15f;
                donnees[iDebut + i] += (onde * 0.7f + bruit) * enveloppe * 0.5f;
            }
        }

        return CreerClip("GorgeeEau_Procedural", donnees);
    }

    /// <summary>Coup sourd (pilon dans le mortier) : bruit filtré passe-bas, attaque immédiate, décroissance rapide — timbre bois/sourd plutôt que métallique.</summary>
    public static AudioClip ClipCoupSourd()
    {
        float duree = 0.3f;
        int nEch = Mathf.RoundToInt(duree * FrequenceEchantillonnage);
        var donnees = new float[nEch];
        var rng = new System.Random(2);

        float filtreEtat = 0f;
        const float coeffFiltre = 0.12f; // proche de 0 = filtrage fort (plus grave/sourd)

        for (int i = 0; i < nEch; i++)
        {
            float t = i / (float)FrequenceEchantillonnage;
            float bruit = (float)rng.NextDouble() * 2f - 1f;
            filtreEtat += coeffFiltre * (bruit - filtreEtat); // filtre passe-bas une pôle
            float enveloppe = Mathf.Exp(-t * 18f);
            donnees[i] = filtreEtat * enveloppe * 1.4f;
        }

        return CreerClip("CoupSourd_Procedural", donnees);
    }

    /// <summary>Souffle sur les braises : bruit filtré, enveloppe montée/descente douce (attaque 0.15s, relâche 0.35s) — pas un simple clic.</summary>
    public static AudioClip ClipSouffle()
    {
        float duree = 0.75f;
        int nEch = Mathf.RoundToInt(duree * FrequenceEchantillonnage);
        var donnees = new float[nEch];
        var rng = new System.Random(3);

        float filtreEtat = 0f;
        const float coeffFiltre = 0.06f; // filtrage plus fort qu'un coup sourd : souffle doux, pas de grain dur

        for (int i = 0; i < nEch; i++)
        {
            float t = i / (float)FrequenceEchantillonnage;
            float bruit = (float)rng.NextDouble() * 2f - 1f;
            filtreEtat += coeffFiltre * (bruit - filtreEtat);

            float attaque = Mathf.Clamp01(t / 0.15f);
            float relache = Mathf.Clamp01((duree - t) / 0.35f);
            float enveloppe = Mathf.Min(attaque, relache);

            donnees[i] = filtreEtat * enveloppe * 1.6f;
        }

        return CreerClip("Souffle_Procedural", donnees);
    }

    static AudioClip CreerClip(string nom, float[] donnees)
    {
        var clip = AudioClip.Create(nom, donnees.Length, 1, FrequenceEchantillonnage, false);
        clip.SetData(donnees, 0);
        return clip;
    }
}

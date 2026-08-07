using System.Collections;
using UnityEngine;

/// <summary>
/// Interaction "attiser le feu" — à poser sur le FeuDeCamp (Campement
/// uniquement, seule scène avec un foyer actif). Souffle sur les braises :
/// booste brièvement l'intensité lumineuse (via FeuDeCampScintillement.
/// intensiteBase) et le taux d'émission des particules de flammes/braises,
/// avec un son de souffle procédural, puis revient à la normale.
/// </summary>
[RequireComponent(typeof(InteractionProximite))]
[RequireComponent(typeof(AudioSource))]
public class ActionAttiserFeu : MonoBehaviour
{
    [Tooltip("Assigné par SceneBuilder : le composant sur \"Lumiere_Feu\".")]
    public FeuDeCampScintillement scintillement;

    [Tooltip("Assigné par SceneBuilder : Particules_Flammes et Particules_Braises.")]
    public ParticleSystem[] particulesABooster;

    public float multiplicateurIntensite = 1.6f;
    public float multiplicateurEmission = 1.5f;
    public float duree = 2.5f;

    private AudioSource source;
    private bool enCours = false;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.maxDistance = 6f;

        var interaction = GetComponent<InteractionProximite>();
        interaction.verbe = "souffler sur les braises";
        interaction.delaiRejeu = duree + 1f;
        interaction.onInteraction.AddListener(Attiser);
    }

    void Attiser()
    {
        if (enCours) return;
        StartCoroutine(RoutineAttiser());
    }

    private IEnumerator RoutineAttiser()
    {
        enCours = true;
        source.PlayOneShot(AudioProceduralUtils.ClipSouffle());
        PromptInteractionUI.AfficherMessage("Vous soufflez sur les braises — le feu reprend un instant.", duree);

        float intensiteOrigine = scintillement != null ? scintillement.intensiteBase : 0f;
        if (scintillement != null) scintillement.intensiteBase = intensiteOrigine * multiplicateurIntensite;

        var emissionsOrigine = new float[particulesABooster?.Length ?? 0];
        if (particulesABooster != null)
        {
            for (int i = 0; i < particulesABooster.Length; i++)
            {
                if (particulesABooster[i] == null) continue;
                var emission = particulesABooster[i].emission;
                emissionsOrigine[i] = emission.rateOverTimeMultiplier;
                emission.rateOverTimeMultiplier = emissionsOrigine[i] * multiplicateurEmission;
            }
        }

        yield return new WaitForSeconds(duree);

        if (scintillement != null) scintillement.intensiteBase = intensiteOrigine;
        if (particulesABooster != null)
        {
            for (int i = 0; i < particulesABooster.Length; i++)
            {
                if (particulesABooster[i] == null) continue;
                var emission = particulesABooster[i].emission;
                emission.rateOverTimeMultiplier = emissionsOrigine[i];
            }
        }

        enCours = false;
    }
}

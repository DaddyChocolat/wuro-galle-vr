using System.Collections;
using UnityEngine;

/// <summary>
/// Interaction "piler" — à poser sur un Mortier, avec une référence vers son
/// Pilon (assignée par SceneBuilder.AjouterInteractionsSceneActive, les deux
/// objets étant déjà voisins dans la scène). Anime le pilon en va-et-vient
/// vertical (4 coups) avec un son sourd à chaque impact — pas d'animation de
/// personnage (silhouettes statiques, voir note éthique), seul l'objet bouge.
/// </summary>
[RequireComponent(typeof(InteractionProximite))]
[RequireComponent(typeof(AudioSource))]
public class ActionPiler : MonoBehaviour
{
    [Tooltip("Assigné par SceneBuilder : le Pilon voisin de ce Mortier.")]
    public Transform pilon;

    public int nombreDeCoups = 4;
    public float amplitude = 0.12f;
    public float dureeParCoup = 0.35f;

    private AudioSource source;
    private bool enCours = false;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.maxDistance = 6f;

        var interaction = GetComponent<InteractionProximite>();
        interaction.verbe = "piler le mil";
        interaction.delaiRejeu = nombreDeCoups * dureeParCoup + 0.5f; // pas de nouvelle série tant que la précédente n'est pas finie
        interaction.onInteraction.AddListener(Piler);
    }

    void Piler()
    {
        if (enCours || pilon == null) return;
        StartCoroutine(RoutinePiler());
    }

    private IEnumerator RoutinePiler()
    {
        enCours = true;
        Vector3 positionHaute = pilon.localPosition;
        Vector3 positionBasse = positionHaute + Vector3.down * amplitude;

        PromptInteractionUI.AfficherMessage("Vous pilez le mil dans le mortier.", nombreDeCoups * dureeParCoup);

        for (int coup = 0; coup < nombreDeCoups; coup++)
        {
            yield return DeplacerSur(pilon, positionHaute, positionBasse, dureeParCoup * 0.4f);
            source.PlayOneShot(AudioProceduralUtils.ClipCoupSourd());
            yield return DeplacerSur(pilon, positionBasse, positionHaute, dureeParCoup * 0.6f);
        }

        pilon.localPosition = positionHaute;
        enCours = false;
    }

    private IEnumerator DeplacerSur(Transform cible, Vector3 depart, Vector3 arrivee, float duree)
    {
        for (float t = 0f; t < duree; t += Time.deltaTime)
        {
            cible.localPosition = Vector3.Lerp(depart, arrivee, t / duree);
            yield return null;
        }
        cible.localPosition = arrivee;
    }
}

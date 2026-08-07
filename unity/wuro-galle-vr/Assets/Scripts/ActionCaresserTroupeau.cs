using UnityEngine;

/// <summary>
/// Interaction "caresser" — à poser sur un module Troupeau (robe blanche,
/// rousse, noire ou Ankole). Joue un son dédié à l'interaction (voir
/// zebu_caresse.wav — extrait court de Audio/651517__davorl__grazing-cows...,
/// déjà documenté/licencié dans le projet, licence CC0 : pas de nouveau
/// téléchargement nécessaire). Écart avec le choix initial ("aucun son
/// dédié, ferait doublon avec l'ambiance") : demandé explicitement ensuite —
/// un retour sonore direct sur le geste rend l'interaction plus satisfaisante
/// que l'ambiance seule, qui continue de tourner en fond par ailleurs.
/// </summary>
[RequireComponent(typeof(InteractionProximite))]
public class ActionCaresserTroupeau : MonoBehaviour
{
    [TextArea]
    public string message = "Vous caressez le troupeau. Les clochettes tintent doucement.";
    public AudioClip son;

    AudioSource audioSource;

    void Awake()
    {
        var interaction = GetComponent<InteractionProximite>();
        interaction.verbe = "caresser";
        interaction.delaiRejeu = 3f;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.maxDistance = 6f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.clip = son;

        interaction.onInteraction.AddListener(() =>
        {
            PromptInteractionUI.AfficherMessage(message);
            if (son != null) audioSource.PlayOneShot(son);
        });
    }
}

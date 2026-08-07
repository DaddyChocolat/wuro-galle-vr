using UnityEngine;

/// <summary>
/// Interaction "caresser" — à poser sur un module Troupeau (robe blanche ou
/// rousse). Aucun son dédié : l'ambiance du hoggo joue déjà les clochettes et
/// le pâturage (voir SceneBuilder.CreerSourceAudio, Audio_Clochettes/
/// Audio_Paturage) — en rejouer un ici ferait doublon plutôt qu'un vrai
/// retour. Le geste se traduit donc par le message seul, cohérent avec
/// l'absence d'animation sur ce module (silhouettes statiques).
/// </summary>
[RequireComponent(typeof(InteractionProximite))]
public class ActionCaresserTroupeau : MonoBehaviour
{
    [TextArea]
    public string message = "Vous caressez le troupeau. Les clochettes tintent doucement.";

    void Awake()
    {
        var interaction = GetComponent<InteractionProximite>();
        interaction.verbe = "caresser";
        interaction.delaiRejeu = 3f;
        interaction.onInteraction.AddListener(() => PromptInteractionUI.AfficherMessage(message));
    }
}

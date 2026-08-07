using UnityEngine;

/// <summary>
/// Interaction "s'asseoir" — à poser sur une Natte (Campement ou Concession).
/// Même mécanique de bascule 3e personne qu'ActionPrier.cs (voir
/// CameraTierceUtils/CorpsJoueur), mais plus courte et avec une pose assise
/// (pas agenouillée) sans connotation religieuse : un simple moment de repos
/// près du foyer/dans la case.
/// </summary>
[RequireComponent(typeof(InteractionProximite))]
public class ActionSasseoir : MonoBehaviour
{
    [TextArea]
    public string message = "Vous vous asseyez un instant sur la natte.";

    public float dureeTransition = 0.4f;
    public float dureeMaintien = 2f;

    void Awake()
    {
        var interaction = GetComponent<InteractionProximite>();
        interaction.verbe = "s'asseoir";
        interaction.delaiRejeu = dureeTransition + dureeMaintien + 0.5f;
        interaction.onInteraction.AddListener(Asseoir);
    }

    void Asseoir()
    {
        CameraTierceUtils.Jouer(
            CorpsJoueur.Pose.Assis, dureeTransition, dureeMaintien,
            pendantLeMaintien: () => PromptInteractionUI.AfficherMessage(message, dureeMaintien));
    }
}

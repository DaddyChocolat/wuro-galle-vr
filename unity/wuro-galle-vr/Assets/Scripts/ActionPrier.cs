using UnityEngine;

/// <summary>
/// Interaction "prier" — à poser sur le Dudal/la Natte_Priere de la
/// Concession (le dudal est l'espace de prière documenté, voir
/// schema-annote-suudu-galle.md ; pas d'équivalent au Campement, choix
/// assumé). Déclenche un bref moment de recueillement scripté à la 3e
/// personne (bascule vers CameraTierce, corps du joueur agenouillé — voir
/// CameraTierceUtils/CorpsJoueur) accompagné d'un message contextualisant le
/// geste, sans reprendre l'audio d'appel à la prière déjà en ambiance sur la
/// scène.
/// </summary>
[RequireComponent(typeof(InteractionProximite))]
public class ActionPrier : MonoBehaviour
{
    [TextArea]
    public string message = "Vous marquez une pause de recueillement, agenouillé, tourné vers la Mecque.";

    public float dureeTransition = 0.6f;
    public float dureeMaintien = 3f;

    void Awake()
    {
        var interaction = GetComponent<InteractionProximite>();
        interaction.verbe = "prier";
        interaction.delaiRejeu = dureeTransition + dureeMaintien + 0.5f;
        interaction.onInteraction.AddListener(Prier);
    }

    void Prier()
    {
        CameraTierceUtils.Jouer(
            CorpsJoueur.Pose.Genoux, dureeTransition, dureeMaintien,
            pendantLeMaintien: () => PromptInteractionUI.AfficherMessage(message, dureeMaintien));
    }
}

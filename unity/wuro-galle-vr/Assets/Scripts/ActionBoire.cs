using UnityEngine;

/// <summary>
/// Interaction "boire" — à poser sur un Canari. Joue un son de gorgée d'eau
/// (procédural, voir AudioProceduralUtils), un jet d'eau visible du goulot
/// jusqu'à la bouche du joueur (voir EauCanariUtils) et affiche un message de
/// confirmation. Purement immersif : aucun effet sur le joueur (pas de
/// mécanique de soif/santé dans ce projet).
/// </summary>
[RequireComponent(typeof(InteractionProximite))]
[RequireComponent(typeof(AudioSource))]
public class ActionBoire : MonoBehaviour
{
    [TextArea]
    public string message = "Vous buvez une gorgée d'eau fraîche du canari.";

    [Tooltip("Hauteur approximative du goulot au-dessus de la base du Canari (m), point de départ du jet d'eau.")]
    public float hauteurGoulot = 0.32f;

    const float DureeJetEau = 0.55f; // alignée sur la durée du clip ClipGorgeeEau

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.maxDistance = 5f;

        var interaction = GetComponent<InteractionProximite>();
        interaction.verbe = "boire";
        interaction.onInteraction.AddListener(Boire);
    }

    void Boire()
    {
        source.PlayOneShot(AudioProceduralUtils.ClipGorgeeEau());
        EauCanariUtils.Jouer(transform.position + Vector3.up * hauteurGoulot, DureeJetEau);
        PromptInteractionUI.AfficherMessage(message);
    }
}

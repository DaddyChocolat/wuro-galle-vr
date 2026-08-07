using UnityEngine;

/// <summary>
/// Interaction "examiner" — micro-moment documentaire : affiche une légende
/// culturelle courte (sourcée du corpus, voir docs/Livrable_1) sur un objet
/// normalement muet (Dudal, Grenier, Hoggo...). Sert directement le principe
/// non négociable de rigueur documentaire (scope-reduit.md) : transforme du
/// décor passif en information plutôt que de tout laisser à la notice
/// utilisateur externe à l'expérience.
/// </summary>
[RequireComponent(typeof(InteractionProximite))]
public class ActionExaminer : MonoBehaviour
{
    [TextArea(2, 5)]
    [Tooltip("Légende affichée, assignée par SceneBuilder selon l'objet (Dudal, Grenier, Hoggo...).")]
    public string legende = "";

    public float dureeAffichage = 6f;

    void Awake()
    {
        var interaction = GetComponent<InteractionProximite>();
        interaction.verbe = "examiner";
        // Touche distincte de la touche d'action par défaut (E) : "examiner" cohabite
        // souvent avec une autre interaction très proche (ex. Dudal + Natte_Priere,
        // quasiment à la même position) — éviter que les deux zones ne répondent au
        // même appui, plutôt que de gérer une priorité entre zones superposées.
        interaction.toucheInteraction = KeyCode.F;
        interaction.delaiRejeu = 1.5f;
        interaction.onInteraction.AddListener(() => PromptInteractionUI.AfficherMessage(legende, dureeAffichage));
    }
}

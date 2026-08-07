using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Brique générique pour toute interaction "active" du joueur (par opposition
/// à PNJDialogue.cs, qui est passif — se déclenche seul par proximité).
/// Ici, le joueur doit être dans la zone ET appuyer sur une touche : un
/// message ("Appuyez sur E pour boire") s'affiche via PromptInteractionUI
/// tant qu'il est à portée, l'action se déclenche à l'appui.
///
/// Ne fait rien par elle-même : les scripts ActionXxx.cs (RequireComponent)
/// s'abonnent à onInteraction en Awake() pour définir le comportement réel
/// (boire, piler, prier...). Permet de réutiliser la même zone/touche/cooldown
/// pour des actions très différentes sans dupliquer cette mécanique.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class InteractionProximite : MonoBehaviour
{
    [Tooltip("Verbe affiché dans le message (\"Appuyez sur E pour {verbe}\"), ex. \"boire\", \"prier\".")]
    public string verbe = "interagir";

    [Tooltip("Rayon de la zone de détection (mètres).")]
    public float rayon = 2f;

    public KeyCode toucheInteraction = KeyCode.E;

    [Tooltip("Délai minimum (secondes) entre deux déclenchements, pour éviter le spam.")]
    public float delaiRejeu = 2f;

    public UnityEvent onInteraction = new UnityEvent();

    private bool joueurDansLaZone = false;
    private float dernierDeclenchement = -999f;

    void Start()
    {
        var zone = GetComponent<SphereCollider>();
        zone.isTrigger = true;
        zone.radius = rayon;
    }

    void Update()
    {
        if (!joueurDansLaZone) return;

        if (Input.GetKeyDown(toucheInteraction) && Time.time - dernierDeclenchement >= delaiRejeu)
        {
            dernierDeclenchement = Time.time;
            onInteraction.Invoke();
        }
    }

    void OnTriggerEnter(Collider autre)
    {
        if (!autre.CompareTag("Player")) return;
        joueurDansLaZone = true;
        PromptInteractionUI.Afficher(this, $"Appuyez sur [{toucheInteraction}] pour {verbe}");
    }

    void OnTriggerExit(Collider autre)
    {
        if (!autre.CompareTag("Player")) return;
        joueurDansLaZone = false;
        PromptInteractionUI.Masquer(this);
    }
}

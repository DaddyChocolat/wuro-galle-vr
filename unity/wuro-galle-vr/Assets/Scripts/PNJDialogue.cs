using UnityEngine;

/// <summary>
/// Déclenche une ligne audio pré-enregistrée quand le joueur entre dans la zone
/// (SphereCollider en trigger). Plusieurs lignes possibles, jouées dans l'ordre
/// à chaque nouvelle entrée (pas en boucle tant que le joueur reste dans la zone —
/// délaiRejeu l'empêche).
///
/// Ne fait PAS de dialogue interactif ni d'IA : uniquement des lignes fixes,
/// choix assumé (voir la note dans SceneBuilder.cs / chat du projet).
/// </summary>
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(AudioSource))]
public class PNJDialogue : MonoBehaviour
{
    [Tooltip("Lignes pré-enregistrées à jouer par proximité, dans l'ordre. À remplir : enregistrer la voix, importer les clips, puis les glisser ici dans l'Inspector.")]
    public AudioClip[] lignes;

    [Tooltip("Délai minimum (secondes) avant de pouvoir rejouer une ligne, pour éviter le spam si le joueur reste dans la zone.")]
    public float delaiRejeu = 8f;

    private AudioSource source;
    private int indexLigne = 0;
    private float dernierDeclenchement = -999f;

    void Start()
    {
        source = GetComponent<AudioSource>();
        GetComponent<SphereCollider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider autre)
    {
        if (!autre.CompareTag("Player")) return;
        if (Time.time - dernierDeclenchement < delaiRejeu) return;
        if (lignes == null || lignes.Length == 0)
        {
            Debug.LogWarning($"[PNJDialogue] {name} : aucune ligne assignée — rien à jouer.");
            return;
        }

        source.clip = lignes[indexLigne % lignes.Length];
        source.Play();
        indexLigne++;
        dernierDeclenchement = Time.time;
    }
}

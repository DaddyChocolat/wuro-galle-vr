using UnityEngine;

/// <summary>
/// Fait varier l'intensité d'une lumière ponctuelle pour simuler le
/// scintillement d'un feu de camp (bruit de Perlin plutôt qu'un flicker
/// purement aléatoire, pour une variation plus organique/moins nerveuse).
/// À accrocher sur le GameObject "Lumiere_Feu", enfant de "FeuDeCamp"
/// (mis en place automatiquement par SceneBuilder.cs).
/// </summary>
[RequireComponent(typeof(Light))]
public class FeuDeCampScintillement : MonoBehaviour
{
    public float intensiteBase = 2.5f;
    public float amplitude = 0.6f;
    public float vitesse = 8f;

    private Light lumiere;
    private float decalageAleatoire;

    void Start()
    {
        lumiere = GetComponent<Light>();
        decalageAleatoire = Random.Range(0f, 100f); // désynchronise plusieurs feux éventuels
    }

    void Update()
    {
        float bruit = Mathf.PerlinNoise(Time.time * vitesse, decalageAleatoire);
        lumiere.intensity = intensiteBase + (bruit - 0.5f) * 2f * amplitude;
    }
}

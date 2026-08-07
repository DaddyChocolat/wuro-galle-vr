using UnityEngine;

/// <summary>
/// Fait dériver lentement un nuage (quad plat, voir SceneBuilder.CreerNuages)
/// dans la direction du vent, et le replace de l'autre côté de la zone
/// jouable quand il en sort — boucle discrète, pas de limite de distance qui
/// ferait "disparaître" un nuage visiblement. Partie du décor "climat/météo"
/// (avec Vent_Poussiere et le brouillard déjà en place, voir
/// SceneBuilder.AjouterDecorSceneActive) : un ciel figé détonnait avec le
/// reste de l'ambiance sonore (vent en boucle).
/// </summary>
public class NuagesDerive : MonoBehaviour
{
    public Vector3 direction = new Vector3(1f, 0f, 0.3f);
    public float vitesse = 0.4f;
    public float limite = 70f; // distance (m, sur X) au-delà de laquelle le nuage revient de l'autre côté

    void Update()
    {
        transform.position += direction.normalized * vitesse * Time.deltaTime;

        if (transform.position.x > limite)
            transform.position -= new Vector3(limite * 2f, 0f, 0f);
        else if (transform.position.x < -limite)
            transform.position += new Vector3(limite * 2f, 0f, 0f);
    }
}

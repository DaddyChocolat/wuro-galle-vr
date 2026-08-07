using UnityEngine;

/// <summary>
/// Effet visuel "boire" : un jet de gouttes part du goulot du Canari et
/// rejoint la bouche du joueur (position approchée : caméra, légèrement en
/// dessous et devant — pas de vrai modèle de tête). Purement procédural
/// (Particle System généré en code), même esprit que les particules du feu
/// dans SceneBuilder.CreerParticulesFlammes — pas de texture/asset externe.
/// Se détruit tout seul une fois le jet terminé.
/// </summary>
public static class EauCanariUtils
{
    /// <summary>Lance le jet d'eau depuis `origineCanari` vers la bouche du joueur actuel (retrouvé par tag "Player"). Ne fait rien si le joueur est introuvable.</summary>
    public static void Jouer(Vector3 origineCanari, float duree)
    {
        var joueurGO = GameObject.FindGameObjectWithTag("Player");
        if (joueurGO == null) { Debug.LogWarning("[EauCanariUtils] Joueur introuvable (tag \"Player\")."); return; }

        var controleur = joueurGO.GetComponent<FirstPersonController>();
        if (controleur == null || controleur.vueCamera == null)
        {
            Debug.LogWarning("[EauCanariUtils] FirstPersonController ou vueCamera introuvable sur le joueur.");
            return;
        }

        // Bouche approchée : sous les yeux, légèrement vers l'avant.
        Vector3 bouche = controleur.vueCamera.position
            - controleur.vueCamera.up * 0.12f
            + controleur.vueCamera.forward * 0.08f;

        Vector3 trajet = bouche - origineCanari;
        float distance = trajet.magnitude;
        if (distance < 0.01f) return;

        var go = new GameObject("Particules_EauCanari");
        go.transform.position = origineCanari;
        go.transform.rotation = Quaternion.LookRotation(trajet.normalized);

        var ps = go.AddComponent<ParticleSystem>();
        // AddComponent<ParticleSystem>() démarre la lecture immédiatement (Play On
        // Awake = true par défaut) — modifier main.duration juste après, sur un
        // système déjà en train de jouer, provoque l'erreur console "Setting the
        // duration while system is still playing is not supported". On l'arrête
        // avant de toucher au module main, et on désactive playOnAwake pour ne pas
        // relancer tout seul avant le ps.Play() explicite plus bas.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.duration = duree;
        main.loop = false;
        // Vitesse calibrée pour parcourir la distance canari->bouche pendant `duree`
        // (avant l'effet de la gravité, qui ajoute juste une légère chute — arc discret).
        main.startSpeed = distance / duree;
        main.startLifetime = duree;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
        main.gravityModifier = 0.35f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;
        main.stopAction = ParticleSystemStopAction.Destroy; // auto-nettoyage, pas besoin de coroutine dédiée

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40, 40, 3, duree / 3f) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 4f;
        shape.radius = 0.03f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradientEau = new Gradient();
        gradientEau.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.75f, 0.85f, 0.95f), 0f),
                new GradientColorKey(new Color(0.6f, 0.75f, 0.9f), 1f),
            },
            new[]
            {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(0.6f, 0.8f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = gradientEau;

        var shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        go.GetComponent<ParticleSystemRenderer>().material = new Material(shader);

        ps.Play();
        Object.Destroy(go, duree + 1f); // sécurité si stopAction ne suffit pas (ex. particules encore en vol)
    }
}

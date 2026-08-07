using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Petit "moment scripté" partagé par ActionPrier.cs et ActionSasseoir.cs :
/// la caméra du joueur s'abaisse doucement (position de recueillement/assise),
/// tient la pose, puis remonte — le contrôleur FirstPersonController est
/// désactivé le temps de la pause pour que le mouvement souris/clavier ne
/// vienne pas immédiatement écraser l'animation (son Update() réécrit
/// vueCamera.localRotation à chaque frame sinon). Le joueur ne peut donc pas
/// bouger pendant la pause — assumé : c'est le sens même d'un geste de
/// recueillement/assise, pas un bug de contrôle.
/// </summary>
public static class PauseImmersiveUtils
{
    private static Runner runner;

    private static void SAssurerCreee()
    {
        if (runner != null) return;
        var go = new GameObject("PauseImmersive_Runner");
        UnityEngine.Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<Runner>();
    }

    /// <summary>Lance la pause sur le joueur actuel (retrouvé par tag "Player"). Ne fait rien si le joueur ou sa caméra sont introuvables (log un avertissement).</summary>
    public static void Jouer(float hauteurDip, float dureeDescente, float dureeMaintien, float dureeRemontee, Action pendantLeMaintien = null)
    {
        var joueurGO = GameObject.FindGameObjectWithTag("Player");
        if (joueurGO == null) { Debug.LogWarning("[PauseImmersiveUtils] Joueur introuvable (tag \"Player\")."); return; }

        var controleur = joueurGO.GetComponent<FirstPersonController>();
        if (controleur == null || controleur.vueCamera == null)
        {
            Debug.LogWarning("[PauseImmersiveUtils] FirstPersonController ou vueCamera introuvable sur le joueur.");
            return;
        }

        SAssurerCreee();
        runner.StartCoroutine(Routine(controleur, hauteurDip, dureeDescente, dureeMaintien, dureeRemontee, pendantLeMaintien));
    }

    private static IEnumerator Routine(FirstPersonController controleur, float hauteurDip, float dureeDescente, float dureeMaintien, float dureeRemontee, Action pendantLeMaintien)
    {
        Transform cam = controleur.vueCamera;
        Vector3 positionOrigine = cam.localPosition;
        controleur.enabled = false;

        for (float t = 0f; t < dureeDescente; t += Time.deltaTime)
        {
            cam.localPosition = positionOrigine + Vector3.down * hauteurDip * (t / dureeDescente);
            yield return null;
        }
        cam.localPosition = positionOrigine + Vector3.down * hauteurDip;

        pendantLeMaintien?.Invoke();
        yield return new WaitForSeconds(dureeMaintien);

        for (float t = 0f; t < dureeRemontee; t += Time.deltaTime)
        {
            cam.localPosition = Vector3.Lerp(positionOrigine + Vector3.down * hauteurDip, positionOrigine, t / dureeRemontee);
            yield return null;
        }
        cam.localPosition = positionOrigine;

        controleur.enabled = true;
    }

    private class Runner : MonoBehaviour { }
}

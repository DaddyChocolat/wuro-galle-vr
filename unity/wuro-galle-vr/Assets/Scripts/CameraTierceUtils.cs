using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Bascule scriptée vers la caméra à la 3e personne (voir SceneBuilder.CreerJoueur
/// pour "CameraTierce", enfant du Joueur, désactivée par défaut) pour montrer
/// le corps du joueur (CorpsJoueur.cs) dans une pose donnée — utilisé par
/// ActionPrier et ActionSasseoir pour "montrer comment le personnage prie/
/// s'assoit", contrairement au simple dip de caméra de PauseImmersiveUtils.
/// Même principe de fonctionnement (coroutine sur un runner DontDestroyOnLoad,
/// contrôleur désactivé le temps de la pause) que PauseImmersiveUtils.
/// </summary>
public static class CameraTierceUtils
{
    private static Runner runner;

    private static void SAssurerCreee()
    {
        if (runner != null) return;
        var go = new GameObject("CameraTierce_Runner");
        UnityEngine.Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<Runner>();
    }

    /// <summary>Lance la séquence 3e personne sur le joueur actuel (retrouvé par tag "Player"). Ne fait rien si une pièce nécessaire est introuvable (log un avertissement).</summary>
    public static void Jouer(CorpsJoueur.Pose pose, float dureeTransition, float dureeMaintien, Action pendantLeMaintien = null)
    {
        var joueurGO = GameObject.FindGameObjectWithTag("Player");
        if (joueurGO == null) { Debug.LogWarning("[CameraTierceUtils] Joueur introuvable (tag \"Player\")."); return; }

        var controleur = joueurGO.GetComponent<FirstPersonController>();
        var corps = joueurGO.GetComponentInChildren<CorpsJoueur>(true);
        var camTierceT = joueurGO.transform.Find("CameraTierce");

        if (controleur == null || corps == null || camTierceT == null || controleur.vueCamera == null)
        {
            Debug.LogWarning("[CameraTierceUtils] FirstPersonController, CorpsJoueur ou CameraTierce introuvable sur le joueur.");
            return;
        }

        var camJoueur = controleur.vueCamera.GetComponent<Camera>();
        var ecouteurJoueur = controleur.vueCamera.GetComponent<AudioListener>();
        var camTierce = camTierceT.GetComponent<Camera>();
        var ecouteurTierce = camTierceT.GetComponent<AudioListener>();

        SAssurerCreee();
        runner.StartCoroutine(Routine(controleur, corps, pose, camJoueur, ecouteurJoueur, camTierce, ecouteurTierce, dureeTransition, dureeMaintien, pendantLeMaintien));
    }

    private static IEnumerator Routine(FirstPersonController controleur, CorpsJoueur corps, CorpsJoueur.Pose pose,
        Camera camJoueur, AudioListener ecouteurJoueur, Camera camTierce, AudioListener ecouteurTierce,
        float dureeTransition, float dureeMaintien, Action pendantLeMaintien)
    {
        controleur.enabled = false;

        corps.gameObject.SetActive(true);
        corps.AppliquerPose(pose);

        camJoueur.enabled = false;
        if (ecouteurJoueur != null) ecouteurJoueur.enabled = false;
        camTierce.enabled = true;
        if (ecouteurTierce != null) ecouteurTierce.enabled = true;

        pendantLeMaintien?.Invoke();
        yield return new WaitForSeconds(dureeTransition + dureeMaintien);

        corps.AppliquerPose(CorpsJoueur.Pose.Debout);

        camTierce.enabled = false;
        if (ecouteurTierce != null) ecouteurTierce.enabled = false;
        camJoueur.enabled = true;
        if (ecouteurJoueur != null) ecouteurJoueur.enabled = true;

        corps.gameObject.SetActive(false);
        controleur.enabled = true;
    }

    private class Runner : MonoBehaviour { }
}

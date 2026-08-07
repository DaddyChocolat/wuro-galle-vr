using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WuroGalle.Editor
{
    /// <summary>
    /// Test automatique "on ouvre la scène et on appuie sur Play" — sans intervention
    /// humaine sur l'Éditeur : charge chaque scène, entre en Play mode, laisse tourner
    /// quelques secondes (le temps que Start()/Awake() s'exécutent), capture une image
    /// de la Game view (rendu réel de CameraJoueur), sort du Play mode, recommence sur
    /// l'autre scène. Toute exception/erreur de log pendant le test est comptée et
    /// rapportée à la fin.
    ///
    /// Ne remplace pas un vrai test manuel (pas d'input clavier/souris simulé, donc pas
    /// de vérification des déplacements ni des interactions à la touche E/F) — sert à
    /// détecter les erreurs au démarrage (NullReferenceException, composant manquant...)
    /// et à produire une image de contrôle du rendu réel en Play mode, pas juste de la
    /// scène en Edit mode.
    ///
    /// Usage CLI (SANS -quit : ce script quitte lui-même une fois le test terminé) :
    ///   Unity.exe -batchmode -projectPath "..." -executeMethod WuroGalle.Editor.PlayModeSmokeTest.LancerTest -logFile "..."
    /// </summary>
    public static class PlayModeSmokeTest
    {
        enum Etape
        {
            Inactif, OuvrirCampement, AttendreStabilisationCampement, CapturerCampement, AttendreArretCampement,
            OuvrirConcession, AttendreStabilisationConcession, CapturerConcession, AttendreArretConcession,
            Termine
        }

        const double DelaiStabilisation = 4.0;   // secondes en Play mode avant capture (laisse Awake/Start se dérouler)
        const double DelaiApresArret = 1.0;      // secondes après isPlaying=false avant de changer de scène
        const double TimeoutSecurite = 90.0;     // filet anti-blocage : force la sortie même si un état ne se résout jamais

        static Etape etape = Etape.Inactif;
        static double prochainTop;
        static double heureDepart;
        static int erreurs;
        static string dossierCaptures;
        static bool optionsOriginalesActivees;
        static EnterPlayModeOptions optionsOriginales;

        [MenuItem("Wuro&Galle/Test automatique Play Mode (screenshots)")]
        public static void LancerTest()
        {
            if (etape != Etape.Inactif) { Debug.LogWarning("[PlayModeSmokeTest] Déjà en cours."); return; }

            dossierCaptures = Path.Combine(Application.dataPath, "..", "..", "..", "captures_playmode");
            Directory.CreateDirectory(dossierCaptures);

            // Par défaut, Unity fait un domain reload COMPLET à chaque entrée en Play
            // mode — ça réinitialise tous les champs static de cette classe (donc
            // "etape" retombe à Inactif) ET désabonne Tick de EditorApplication.update
            // (l'abonnement ne survit pas au reload). Résultat observé : le test
            // s'arrête silencieusement juste après le premier isPlaying=true, sans
            // jamais rappeler Tick, donc sans jamais rappeler isPlaying=false — Play
            // mode tourne indéfiniment en arrière-plan (boucle de warnings "no audio
            // listener" observée, des centaines de milliers de lignes de log). On
            // désactive donc le domain reload le temps du test, et on restaure le
            // réglage d'origine dans Terminer().
            optionsOriginalesActivees = EditorSettings.enterPlayModeOptionsEnabled;
            optionsOriginales = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            erreurs = 0;
            Application.logMessageReceived += SurLog;
            EditorApplication.update += Tick;

            heureDepart = EditorApplication.timeSinceStartup;
            etape = Etape.OuvrirCampement;
            Debug.Log("[PlayModeSmokeTest] Démarrage du test automatique (Campement puis Concession).");
        }

        static void SurLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                erreurs++;
                Debug.LogWarning($"[PlayModeSmokeTest] Erreur détectée pendant le test : {condition}");
            }
        }

        static void Tick()
        {
            if (etape == Etape.Inactif) return;

            if (EditorApplication.timeSinceStartup - heureDepart > TimeoutSecurite)
            {
                Debug.LogError("[PlayModeSmokeTest] TIMEOUT de sécurité atteint — arrêt forcé du test (une étape n'a pas abouti).");
                Terminer(force: true);
                return;
            }

            switch (etape)
            {
                case Etape.OuvrirCampement:
                    EditorSceneManager.OpenScene("Assets/Scenes/Campement.unity");
                    EditorApplication.isPlaying = true;
                    prochainTop = EditorApplication.timeSinceStartup + DelaiStabilisation;
                    etape = Etape.AttendreStabilisationCampement;
                    break;

                case Etape.AttendreStabilisationCampement:
                    if (EditorApplication.timeSinceStartup >= prochainTop) etape = Etape.CapturerCampement;
                    break;

                case Etape.CapturerCampement:
                    CapturerEtArreter("Campement");
                    prochainTop = EditorApplication.timeSinceStartup + DelaiApresArret;
                    etape = Etape.AttendreArretCampement;
                    break;

                case Etape.AttendreArretCampement:
                    if (EditorApplication.timeSinceStartup >= prochainTop && !EditorApplication.isPlaying) etape = Etape.OuvrirConcession;
                    break;

                case Etape.OuvrirConcession:
                    EditorSceneManager.OpenScene("Assets/Scenes/Concession.unity");
                    EditorApplication.isPlaying = true;
                    prochainTop = EditorApplication.timeSinceStartup + DelaiStabilisation;
                    etape = Etape.AttendreStabilisationConcession;
                    break;

                case Etape.AttendreStabilisationConcession:
                    if (EditorApplication.timeSinceStartup >= prochainTop) etape = Etape.CapturerConcession;
                    break;

                case Etape.CapturerConcession:
                    CapturerEtArreter("Concession");
                    prochainTop = EditorApplication.timeSinceStartup + DelaiApresArret;
                    etape = Etape.AttendreArretConcession;
                    break;

                case Etape.AttendreArretConcession:
                    if (EditorApplication.timeSinceStartup >= prochainTop && !EditorApplication.isPlaying) Terminer(force: false);
                    break;
            }
        }

        /// <summary>Rend la Game view réelle (CameraJoueur, tag MainCamera) dans une texture et l'enregistre en PNG, puis sort du Play mode.</summary>
        static void CapturerEtArreter(string nomScene)
        {
            var joueurGO = GameObject.FindGameObjectWithTag("Player");
            if (joueurGO != null)
                Debug.Log($"[PlayModeSmokeTest] Diagnostic '{nomScene}' — position Joueur : {joueurGO.transform.position}, rotation : {joueurGO.transform.eulerAngles}");
            else
                Debug.LogWarning($"[PlayModeSmokeTest] Diagnostic '{nomScene}' — Joueur (tag Player) introuvable.");

            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError($"[PlayModeSmokeTest] Camera.main introuvable en Play mode sur '{nomScene}' — pas de capture, mais le test continue.");
            }
            else
            {
                const int largeur = 1280, hauteur = 720;
                var rt = new RenderTexture(largeur, hauteur, 24);
                var precedente = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();

                var actif = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(largeur, hauteur, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, largeur, hauteur), 0, 0);
                tex.Apply();

                string chemin = Path.Combine(dossierCaptures, $"{nomScene}_playmode.png");
                File.WriteAllBytes(chemin, tex.EncodeToPNG());
                Debug.Log($"[PlayModeSmokeTest] Capture Play mode enregistrée : {chemin}");

                cam.targetTexture = precedente;
                RenderTexture.active = actif;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
            }

            EditorApplication.isPlaying = false;
        }

        static void Terminer(bool force)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= SurLog;
            etape = Etape.Inactif;

            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;

            EditorSettings.enterPlayModeOptions = optionsOriginales;
            EditorSettings.enterPlayModeOptionsEnabled = optionsOriginalesActivees;
            AssetDatabase.SaveAssets();

            Debug.Log(force
                ? $"[PlayModeSmokeTest] Arrêt forcé (timeout). Erreurs détectées avant arrêt : {erreurs}."
                : $"[PlayModeSmokeTest] Test terminé sur les deux scènes. Erreurs détectées : {erreurs}. Captures dans : {dossierCaptures}");

            if (Application.isBatchMode)
                EditorApplication.Exit(force ? 1 : (erreurs > 0 ? 1 : 0));
        }
    }
}

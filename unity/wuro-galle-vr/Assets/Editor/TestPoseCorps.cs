using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WuroGalle.Editor
{
    /// <summary>
    /// Diagnostic ponctuel : PlayModeSmokeTest ne simule aucune touche, donc ne
    /// déclenche jamais CameraTierceUtils (prier/s'asseoir) — impossible de voir
    /// le résultat des poses Genoux/Assis autrement qu'en jouant manuellement au
    /// clavier dans l'Éditeur. Ce script force la pose directement en Play mode
    /// (sans passer par InteractionProximite/le clavier) pour capturer un
    /// screenshot de contrôle. Outil de dev, pas un test à faire tourner en CI.
    ///
    /// Usage CLI (SANS -quit) :
    ///   Unity.exe -batchmode -projectPath "..." -executeMethod WuroGalle.Editor.TestPoseCorps.LancerTest -logFile "..."
    /// </summary>
    public static class TestPoseCorps
    {
        enum Etape { Inactif, Ouvrir, AttendreStabilisation, ForcerPose, AttendrePose, Capturer, Arreter, Fini }

        const double DelaiStabilisation = 4.0;
        const double DelaiApresPose = 1.0;
        const double TimeoutSecurite = 90.0;

        static Etape etape = Etape.Inactif;
        static double prochainTop, heureDepart;
        static string dossierCaptures;
        static bool optionsOriginalesActivees;
        static EnterPlayModeOptions optionsOriginales;

        [MenuItem("Wuro&Galle/Test : capturer la pose Genoux (Concession)")]
        public static void LancerTest()
        {
            if (etape != Etape.Inactif) return;

            dossierCaptures = Path.Combine(Application.dataPath, "..", "..", "..", "captures_playmode");
            Directory.CreateDirectory(dossierCaptures);

            optionsOriginalesActivees = EditorSettings.enterPlayModeOptionsEnabled;
            optionsOriginales = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            EditorApplication.update += Tick;
            heureDepart = EditorApplication.timeSinceStartup;
            etape = Etape.Ouvrir;
            Debug.Log("[TestPoseCorps] Démarrage.");
        }

        static void Tick()
        {
            if (etape == Etape.Inactif) return;

            if (EditorApplication.timeSinceStartup - heureDepart > TimeoutSecurite)
            {
                Debug.LogError("[TestPoseCorps] TIMEOUT sécurité.");
                Terminer();
                return;
            }

            switch (etape)
            {
                case Etape.Ouvrir:
                    EditorSceneManager.OpenScene("Assets/Scenes/Concession.unity");
                    EditorApplication.isPlaying = true;
                    prochainTop = EditorApplication.timeSinceStartup + DelaiStabilisation;
                    etape = Etape.AttendreStabilisation;
                    break;

                case Etape.AttendreStabilisation:
                    if (EditorApplication.timeSinceStartup >= prochainTop) etape = Etape.ForcerPose;
                    break;

                case Etape.ForcerPose:
                    ForcerPoseGenoux();
                    prochainTop = EditorApplication.timeSinceStartup + DelaiApresPose;
                    etape = Etape.AttendrePose;
                    break;

                case Etape.AttendrePose:
                    if (EditorApplication.timeSinceStartup >= prochainTop) etape = Etape.Capturer;
                    break;

                case Etape.Capturer:
                    CapturerEcran("Concession_pose_genoux");
                    etape = Etape.Arreter;
                    break;

                case Etape.Arreter:
                    EditorApplication.isPlaying = false;
                    etape = Etape.Fini;
                    break;

                case Etape.Fini:
                    if (!EditorApplication.isPlaying) Terminer();
                    break;
            }
        }

        static void ForcerPoseGenoux()
        {
            var joueurGO = GameObject.FindGameObjectWithTag("Player");
            if (joueurGO == null) { Debug.LogError("[TestPoseCorps] Joueur introuvable."); return; }

            var corps = joueurGO.GetComponentInChildren<CorpsJoueur>(true);
            var camTierceT = joueurGO.transform.Find("CameraTierce");
            var controleur = joueurGO.GetComponent<FirstPersonController>();

            if (corps == null || camTierceT == null || controleur == null)
            {
                Debug.LogError("[TestPoseCorps] CorpsJoueur, CameraTierce ou FirstPersonController introuvable.");
                return;
            }

            controleur.enabled = false;
            corps.gameObject.SetActive(true);
            corps.AppliquerPose(CorpsJoueur.Pose.Genoux);

            var camJoueur = controleur.vueCamera.GetComponent<Camera>();
            var camTierce = camTierceT.GetComponent<Camera>();
            camJoueur.enabled = false;
            camTierce.enabled = true;

            Debug.Log("[TestPoseCorps] Pose Genoux forcée, caméra tierce activée.");
        }

        static void CapturerEcran(string nom)
        {
            var cam = Camera.main != null ? Camera.main : GameObject.FindGameObjectWithTag("Player")?.transform.Find("CameraTierce")?.GetComponent<Camera>();
            // Camera.main ne trouve que les caméras actives taguées MainCamera ; CameraTierce n'a pas ce tag.
            var camTierceT = GameObject.FindGameObjectWithTag("Player")?.transform.Find("CameraTierce");
            if (camTierceT != null)
            {
                var ct = camTierceT.GetComponent<Camera>();
                if (ct != null && ct.enabled) cam = ct;
            }

            if (cam == null) { Debug.LogError("[TestPoseCorps] Aucune caméra à capturer."); return; }

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

            string chemin = Path.Combine(dossierCaptures, $"{nom}.png");
            File.WriteAllBytes(chemin, tex.EncodeToPNG());
            Debug.Log($"[TestPoseCorps] Capture enregistrée : {chemin}");

            cam.targetTexture = precedente;
            RenderTexture.active = actif;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }

        static void Terminer()
        {
            EditorApplication.update -= Tick;
            etape = Etape.Inactif;
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;

            EditorSettings.enterPlayModeOptions = optionsOriginales;
            EditorSettings.enterPlayModeOptionsEnabled = optionsOriginalesActivees;
            AssetDatabase.SaveAssets();

            Debug.Log("[TestPoseCorps] Terminé.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}

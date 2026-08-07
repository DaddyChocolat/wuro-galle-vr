using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace WuroGalle.Editor
{
    /// <summary>
    /// Génère une séquence d'images (une caméra libre suit un parcours scripté à
    /// travers Campement puis Concession) pour un montage vidéo "flythrough" en
    /// dehors du Play mode — pas besoin de simuler des touches, juste de déplacer
    /// une caméra le long d'un chemin de points de passage et de rendre chaque
    /// frame. L'encodage en MP4 (ffmpeg) se fait en dehors de Unity, voir
    /// docs/Livrable_4/ (script séparé).
    ///
    /// Les systèmes de particules (feu, poussière) ne jouent pas tout seuls en
    /// Edit mode — on les fait avancer manuellement image par image via
    /// ParticleSystem.Simulate() pour qu'ils apparaissent vivants dans le rendu.
    ///
    /// Usage CLI (synchrone, pas besoin de -quit séparé ni de Play mode) :
    ///   Unity.exe -batchmode -quit -projectPath "..." -executeMethod WuroGalle.Editor.VideoFlythrough.GenererToutesLesFrames -logFile "..."
    /// </summary>
    public static class VideoFlythrough
    {
        const int FrameRate = 12;
        const int Largeur = 1280, Hauteur = 720;
        const int QualiteJpg = 85;

        class Waypoint
        {
            public Vector3 Position;
            public Vector3 RegardVers;
            public float DureeArrivee;   // secondes de trajet depuis le waypoint précédent
            public float DureeMaintien;  // secondes d'arrêt une fois arrivé
            public Waypoint(Vector3 position, Vector3 regardVers, float dureeArrivee, float dureeMaintien)
            {
                Position = position; RegardVers = regardVers;
                DureeArrivee = dureeArrivee; DureeMaintien = dureeMaintien;
            }
        }

        [MenuItem("Wuro&Galle/Générer le flythrough vidéo (frames)")]
        public static void GenererToutesLesFrames()
        {
            string dossierBase = Path.Combine(Application.dataPath, "..", "..", "..", "video_frames");

            GenererScene("Campement", ConstruireCheminCampement(), 200f, Path.Combine(dossierBase, "campement"));
            GenererScene("Concession", ConstruireCheminConcession(), 190f, Path.Combine(dossierBase, "concession"));

            Debug.Log("[VideoFlythrough] Toutes les frames générées.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Passage rapide (quelques secondes par scène) pour valider le chemin/l'interpolation avant de lancer la génération complète (des milliers de frames).</summary>
        [MenuItem("Wuro&Galle/Générer le flythrough vidéo (aperçu rapide)")]
        public static void GenererApercu()
        {
            string dossierBase = Path.Combine(Application.dataPath, "..", "..", "..", "video_frames_apercu");

            GenererScene("Campement", ConstruireCheminCampement(), 12f, Path.Combine(dossierBase, "campement"));
            GenererScene("Concession", ConstruireCheminConcession(), 12f, Path.Combine(dossierBase, "concession"));

            Debug.Log("[VideoFlythrough] Aperçu généré.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void GenererScene(string nomScene, List<Waypoint> chemin, float dureeCibleSecondes, string dossierSortie)
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/{nomScene}.unity", OpenSceneMode.Single);
            Directory.CreateDirectory(dossierSortie);

            // CorpsJoueur.Awake() se cache lui-même normalement (SetActive(false)),
            // mais Awake() ne s'exécute qu'en Play mode — jamais déclenché ici
            // puisqu'on rend en Edit mode (pas besoin du Play mode juste pour
            // déplacer une caméra). Sans ce correctif, le corps du joueur reste
            // planté, actif, immobile en pose de repos, en plein milieu du plan
            // (bug réel constaté sur l'aperçu rapide avant ce correctif).
            var joueurGO = GameObject.Find("Joueur");
            if (joueurGO != null)
            {
                var corpsT = joueurGO.transform.Find("Corps");
                if (corpsT != null) corpsT.gameObject.SetActive(false);
            }

            // Met à l'échelle toutes les durées du chemin pour que le total tombe
            // exactement sur dureeCibleSecondes (plus simple que d'ajuster chaque
            // waypoint à la main).
            float totalBrut = 0f;
            foreach (var wp in chemin) totalBrut += wp.DureeArrivee + wp.DureeMaintien;
            float facteur = dureeCibleSecondes / totalBrut;
            foreach (var wp in chemin) { wp.DureeArrivee *= facteur; wp.DureeMaintien *= facteur; }

            var camGO = new GameObject("Camera_Flythrough");
            var cam = camGO.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;

            var particules = Object.FindObjectsOfType<ParticleSystem>();

            var rt = new RenderTexture(Largeur, Hauteur, 24);
            var tex = new Texture2D(Largeur, Hauteur, TextureFormat.RGB24, false);

            int totalFrames = Mathf.CeilToInt(dureeCibleSecondes * FrameRate);
            const float dt = 1f / FrameRate;

            for (int frame = 0; frame < totalFrames; frame++)
            {
                float tGlobal = frame / (float)FrameRate;
                PositionnerCamera(camGO.transform, chemin, tGlobal);

                foreach (var ps in particules)
                {
                    if (ps != null) ps.Simulate(dt, true, false, true);
                }

                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, Largeur, Hauteur), 0, 0);
                tex.Apply();

                string chemin2 = Path.Combine(dossierSortie, $"frame_{frame:D5}.jpg");
                File.WriteAllBytes(chemin2, tex.EncodeToJPG(QualiteJpg));

                if (frame % 200 == 0)
                    Debug.Log($"[VideoFlythrough] {nomScene} : frame {frame}/{totalFrames}");
            }

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGO);

            Debug.Log($"[VideoFlythrough] {nomScene} terminé : {totalFrames} frames dans {dossierSortie}");
        }

        /// <summary>Interpole position + point regardé le long du chemin au temps global t (secondes), avec un lissage ease-in-out à chaque segment.</summary>
        static void PositionnerCamera(Transform cam, List<Waypoint> chemin, float t)
        {
            float cursor = 0f;
            for (int i = 0; i < chemin.Count; i++)
            {
                var wp = chemin[i];
                float segmentTrajet = wp.DureeArrivee;
                float segmentMaintien = wp.DureeMaintien;

                if (i == 0)
                {
                    // Premier waypoint : pas de trajet vers lui, juste le maintien initial.
                    if (t <= cursor + segmentMaintien || chemin.Count == 1)
                    {
                        cam.position = wp.Position;
                        cam.rotation = Quaternion.LookRotation((wp.RegardVers - wp.Position).normalized, Vector3.up);
                        return;
                    }
                    cursor += segmentMaintien;
                    continue;
                }

                var precedent = chemin[i - 1];
                if (t <= cursor + segmentTrajet)
                {
                    float local = Mathf.Clamp01((t - cursor) / Mathf.Max(segmentTrajet, 0.0001f));
                    float lisse = local * local * (3f - 2f * local); // smoothstep
                    Vector3 pos = Vector3.Lerp(precedent.Position, wp.Position, lisse);
                    Vector3 regarde = Vector3.Lerp(precedent.RegardVers, wp.RegardVers, lisse);
                    cam.position = pos;
                    cam.rotation = Quaternion.LookRotation((regarde - pos).normalized, Vector3.up);
                    return;
                }
                cursor += segmentTrajet;

                if (t <= cursor + segmentMaintien)
                {
                    cam.position = wp.Position;
                    cam.rotation = Quaternion.LookRotation((wp.RegardVers - wp.Position).normalized, Vector3.up);
                    return;
                }
                cursor += segmentMaintien;
            }

            // Après le dernier waypoint (arrondi de durée) : reste sur la dernière pose.
            var dernier = chemin[chemin.Count - 1];
            cam.position = dernier.Position;
            cam.rotation = Quaternion.LookRotation((dernier.RegardVers - dernier.Position).normalized, Vector3.up);
        }

        static List<Waypoint> ConstruireCheminCampement()
        {
            return new List<Waypoint>
            {
                new Waypoint(new Vector3(0, 1.6f, 7f), new Vector3(0, 0.5f, 0), 0f, 3f),                    // plan d'ensemble depuis l'entrée
                new Waypoint(new Vector3(0, 1.6f, 2.5f), new Vector3(0, 0.4f, 0), 4f, 2f),                  // vers le foyer
                new Waypoint(new Vector3(-2.5f, 1.6f, 3.3f), new Vector3(-2.5f, 1f, 0f), 3f, 3f),           // Suudu_1 (rayon réel ~1.3m — caméra à distance de sécurité pour montrer tout le dôme, pas juste le chaume collé à l'objectif)
                new Waypoint(new Vector3(-2.8f, 1.5f, -2.8f), new Vector3(-1.4f, 0.4f, -1.1f), 3f, 2.5f),   // Canari_Foyer, vue rapprochée (reculée pour ne plus raser le mur de Suudu_1)
                new Waypoint(new Vector3(1f, 1.7f, 3.3f), new Vector3(-1f, 1.3f, 1f), 3f, 3f),              // PNJ_Berger, vue de face à distance de corps entier (~3m, évite le cadrage genoux/tête coupée)
                new Waypoint(new Vector3(1.2f, 1.4f, 3f), new Vector3(0.6f, 0.3f, 1.4f), 3f, 2.5f),         // mobilier (natte/mortier/pilon/calebasse)
                new Waypoint(new Vector3(2.5f, 1.6f, 3.3f), new Vector3(2.5f, 1f, 0f), 3f, 2.5f),           // Suudu_2 (même correction que Suudu_1)
                new Waypoint(new Vector3(0, 4.5f, 9f), new Vector3(0, 1f, 0), 4f, 3f),                      // plan large campement + horizon
                new Waypoint(new Vector3(7f, 2f, 5f), new Vector3(10.5f, 1f, 0f), 4f, 2f),                   // approche de l'enclos (~6m du centre — 4-4.5m s'est révélé insuffisant : une bête placée près du bord du disque de 3.3m dépasse largement au-delà, la caméra finissait dans sa géométrie). Enclos réel en x=+10.5 (bounds.center mesuré via DiagnosticTailles), pas -10.5 : conversion Blender Z-up -> Unity inverse l'axe X, l'ancien commentaire recopiait la coordonnée Blender telle quelle.
                new Waypoint(new Vector3(10.5f, 2.2f, 7f), new Vector3(10.5f, 1f, 0f), 3f, 4f),              // vue rapprochée du troupeau, côté sud, à distance de sécurité (~7m, légèrement surélevée)
                new Waypoint(new Vector3(4f, 2f, 6f), new Vector3(10.5f, 1f, 0f), 3f, 3.5f),                 // autre angle sur le troupeau, côté village
                new Waypoint(new Vector3(9f, 3.2f, 5f), new Vector3(10.5f, 1f, 0f), 4f, 3f),                 // vue surélevée enclos + horizon/végétation (rapprochée/abaissée — l'enclos est près du bord est du terrain, un plan trop large/haut débordait dans le vide)
                new Waypoint(new Vector3(3f, 2f, -6f), new Vector3(0, 1f, 0), 5f, 2.5f),                    // véhicule autour vers la végétation éparse
                new Waypoint(new Vector3(0, 5f, 11f), new Vector3(0, 1f, 0), 5f, 4f),                       // plan large final (z=16 précédent dépassait le terrain (demi-étendue ~13m) — vide/skybox visible)
            };
        }

        static List<Waypoint> ConstruireCheminConcession()
        {
            return new List<Waypoint>
            {
                new Waypoint(new Vector3(0, 1.6f, -7f), new Vector3(0, 0.5f, -2f), 0f, 3f),                 // plan d'ensemble depuis l'entrée
                new Waypoint(new Vector3(3f, 1.7f, -4.5f), new Vector3(3f, 1.2f, -1f), 4f, 3f),               // Case_Hote (reculée à ~3.5m — la position précédente collait la caméra dans le mur, image noire)
                new Waypoint(new Vector3(-3.2f, 1.4f, -4.6f), new Vector3(-2.2f, 0.3f, -3f), 3f, 2.5f),      // Canari_Entree (reculée pour ne plus cadrer le canari en plongée extrême)
                new Waypoint(new Vector3(4.5f, 1.6f, -3.5f), new Vector3(5.5f, 0.5f, -2.5f), 4f, 3.5f),      // Dudal (prière, décalé)
                new Waypoint(new Vector3(-7f, 1.7f, 3.5f), new Vector3(-4f, 1.2f, 2f), 5f, 3f),              // Galle_1 (reculée — même correction que Case_Hote)
                new Waypoint(new Vector3(0, 1.6f, 3.5f), new Vector3(0, 1.5f, 1f), 4f, 3f),                  // Grenier
                new Waypoint(new Vector3(2.0f, 2.2f, 1.5f), new Vector3(-1.4f, 0.4f, 0.3f), 3f, 2.5f),        // Foyer, ajouté près du Grenier (la Concession n'en avait pas jusqu'ici) — position/hauteur choisies pour que la TRANSITION depuis le waypoint Grenier ne traverse pas son corps (constaté : ligne droite direct passait à travers, image noire)
                new Waypoint(new Vector3(2, 1.6f, 4.5f), new Vector3(1.5f, 0.5f, 3f), 3f, 2.5f),             // mobilier des cases
                new Waypoint(new Vector3(7.5f, 1.7f, 3.5f), new Vector3(4f, 1.2f, 2f), 4f, 3f),              // Galle_2 (reculée — même correction que Case_Hote)
                new Waypoint(new Vector3(2.5f, 1.7f, 8f), new Vector3(0, 1.2f, 5f), 3f, 3f),                 // Galle_3 (reculée par précaution, même famille de modèle)
                new Waypoint(new Vector3(2.5f, 1.7f, 5.5f), new Vector3(0.5f, 1.3f, 3.5f), 3f, 3f),          // PNJ_Femme, vue de face à distance de corps entier (~2.8m)
                new Waypoint(new Vector3(0, 5f, -2f), new Vector3(0, 1f, 2f), 4f, 3f),                       // plan large concession + horizon
                new Waypoint(new Vector3(7f, 2f, 5f), new Vector3(10.5f, 1f, 0f), 4f, 2f),                   // approche enclos (même distance de sécurité que Campement, ~6m ; enclos réel en x=+10.5, voir commentaire Campement)
                new Waypoint(new Vector3(10.5f, 2.2f, 7f), new Vector3(10.5f, 1f, 0f), 3f, 4f),              // vue rapprochée du troupeau, côté sud, distance de sécurité ~7m
                new Waypoint(new Vector3(9f, 3.2f, 5f), new Vector3(10.5f, 1f, 0f), 4f, 3f),                 // vue surélevée enclos (même correction que Campement — bord de terrain proche)
                new Waypoint(new Vector3(0, 5f, -11f), new Vector3(0, 1f, 0), 5f, 4f),                       // plan large final (même correction que Campement — z=-14 dépassait le terrain)
            };
        }
    }
}

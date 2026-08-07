using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WuroGalle.Editor
{
    /// <summary>
    /// Diagnostic ponctuel : les captures Play mode montrent la caméra du joueur quasi
    /// collée à de la géométrie (toit de suudu/case) dès le spawn, alors que le
    /// diagnostic de position confirme que le Joueur est bien au point prévu par
    /// SceneBuilder (pas de chute, pas de dérive). Hypothèse à vérifier : les modules
    /// GLTF importés (Suudu, cases...) sont-ils à la bonne échelle (~1.3-1.9 m de
    /// rayon documenté) ou nettement plus grands qu'attendu (souci d'unités à l'export
    /// Blender) ? Ce script logue les bounds (monde) réels des objets clés, en Edit
    /// mode (pas besoin de Play).
    /// </summary>
    public static class DiagnosticTailles
    {
        [MenuItem("Wuro&Galle/Diagnostic : tailles réelles des objets clés")]
        public static void Diagnostiquer()
        {
            DiagnostiquerScene("Campement", new[] { "Suudu_1", "Suudu_2", "FeuDeCamp", "Joueur", "Paysage", "Acacia_0", "Acacia_1", "Vegetation_Eparse" });
            DiagnostiquerScene("Concession", new[] { "Dudal", "Case_Hote", "Galle_1", "Galle_3", "Grenier", "Joueur", "Paysage" });

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Vue de dessus (caméra orthographique temporaire) des deux scènes — pour repérer d'un coup d'œil un objet mal placé/à la mauvaise échelle, sans dépendre de la vue FPS embarquée. Nécessite l'Éditeur SANS -nographics (a besoin de vraiment rendre).</summary>
        [MenuItem("Wuro&Galle/Diagnostic : vue de dessus (screenshots)")]
        public static void CapturerVueDessus()
        {
            CapturerVueDessusScene("Campement");
            CapturerVueDessusScene("Concession");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void CapturerVueDessusScene(string nomScene)
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/{nomScene}.unity");

            var camGO = new GameObject("Camera_Diagnostic_Dessus");
            camGO.transform.position = new Vector3(0f, 25f, 0f);
            camGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 16f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(1f, 0f, 1f); // magenta franc : distingue le "vide" du reste sans ambiguïté

            const int largeur = 1024, hauteur = 1024;
            var rt = new RenderTexture(largeur, hauteur, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(largeur, hauteur, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, largeur, hauteur), 0, 0);
            tex.Apply();

            string dossier = Path.Combine(Application.dataPath, "..", "..", "..", "captures_playmode");
            Directory.CreateDirectory(dossier);
            string chemin = Path.Combine(dossier, $"{nomScene}_dessus.png");
            File.WriteAllBytes(chemin, tex.EncodeToPNG());
            Debug.Log($"[Diagnostic] Vue de dessus enregistrée : {chemin}");

            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGO);
        }

        static void DiagnostiquerScene(string nomScene, string[] objets)
        {
            Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{nomScene}.unity");
            foreach (var nom in objets)
            {
                var go = TrouverDansScene(scene, nom);
                if (go == null) { Debug.LogWarning($"[Diagnostic] {nomScene}: '{nom}' introuvable."); continue; }

                var renderers = go.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    Debug.Log($"[Diagnostic] {nomScene}.{nom} : aucun Renderer — position={go.transform.position}, scale={go.transform.localScale}");
                    continue;
                }

                Bounds b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                Debug.Log($"[Diagnostic] {nomScene}.{nom} : position={go.transform.position}, scale={go.transform.localScale}, bounds.center={b.center}, bounds.size={b.size}");
            }
        }

        static Transform TrouverEnfant(Transform parent, string nom)
        {
            if (parent.name == nom) return parent;
            foreach (Transform enfant in parent)
            {
                var trouve = TrouverEnfant(enfant, nom);
                if (trouve != null) return trouve;
            }
            return null;
        }

        static GameObject TrouverDansScene(Scene scene, string nom)
        {
            foreach (var racine in scene.GetRootGameObjects())
            {
                if (racine.name == nom) return racine;
                var enfant = TrouverEnfant(racine.transform, nom);
                if (enfant != null) return enfant.gameObject;
            }
            return null;
        }
    }
}

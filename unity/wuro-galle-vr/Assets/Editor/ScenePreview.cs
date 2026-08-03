using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WuroGalle.Editor
{
    /// <summary>
    /// Génère un aperçu PNG de "CameraJoueur" pour Campement et Concession, en ouvrant
    /// chaque scène en mode Single (aucune ambiguïté GameObject.Find entre scènes,
    /// voir le bug documenté dans SceneBuilder.cs / rapport-reflexif.md 3.2).
    /// Outil de vérification réutilisable, pas un menu permanent du pipeline.
    /// </summary>
    public static class ScenePreview
    {
        const int Largeur = 1280;
        const int Hauteur = 720;

        [MenuItem("Wuro&Galle/Aperçu : rendre Campement + Concession")]
        public static void RenderApercus()
        {
            RenderScene("Campement");
            RenderScene("Concession");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>
        /// Diagnostic : rend Concession deux fois, avec puis sans ombres sur Soleil_Zenith,
        /// pour isoler si l'assombrissement des toits vient d'un artefact d'ombre portée
        /// (shadow acne) sur la géométrie déplacée du toit plutôt que du matériau/texture.
        /// Ne sauvegarde jamais la scène (comparaison jetable).
        /// </summary>
        public static void DiagnostiquerOmbres()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Concession.unity", OpenSceneMode.Single);
            GameObject soleil = TrouverRacine(scene, "Soleil_Zenith");
            Light lumiere = soleil != null ? soleil.GetComponent<Light>() : null;

            if (lumiere != null)
            {
                lumiere.shadows = LightShadows.None;
                CapturerCamera(scene, "Concession_sans_ombres");
                lumiere.shadows = LightShadows.Soft;
                CapturerCamera(scene, "Concession_avec_ombres");
            }
            else
            {
                Debug.LogError("[ScenePreview] Soleil_Zenith introuvable pour le diagnostic.");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>
        /// Diagnostic : pour chaque MeshRenderer de la scène Concession, calcule le
        /// pourcentage de faces dont la normale pointe à l'opposé du soleil (N·L <= 0,
        /// donc non éclairées par la lumière directionnelle quel que soit l'état des
        /// ombres portées) + l'état de l'éclairage ambiant (mode, intensité, skybox).
        /// </summary>
        public static void DiagnostiquerEclairage()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Concession.unity", OpenSceneMode.Single);
            GameObject soleil = TrouverRacine(scene, "Soleil_Zenith");
            Vector3 dirLumiere = soleil.GetComponent<Light>().transform.forward; // direction de PROPAGATION de la lumière
            Vector3 versLumiere = -dirLumiere; // direction surface -> soleil, utilisée dans N.L

            Debug.Log($"[Diag] Direction du soleil (forward)={dirLumiere}, ambientMode={RenderSettings.ambientMode}, " +
                $"ambientIntensity={RenderSettings.ambientIntensity}, ambientLight={RenderSettings.ambientLight}, " +
                $"skybox={(RenderSettings.skybox != null ? RenderSettings.skybox.name : "AUCUN")}");

            foreach (var mf in Object.FindObjectsOfType<MeshFilter>())
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null) continue;
                var normals = mesh.normals;
                var tris = mesh.triangles;
                if (normals == null || normals.Length == 0) continue;

                int litCount = 0, total = 0;
                var vertsWorldNormals = new Vector3[normals.Length];
                for (int i = 0; i < normals.Length; i++)
                    vertsWorldNormals[i] = mf.transform.TransformDirection(normals[i]).normalized;

                for (int i = 0; i < tris.Length; i += 3)
                {
                    Vector3 n = (vertsWorldNormals[tris[i]] + vertsWorldNormals[tris[i + 1]] + vertsWorldNormals[tris[i + 2]]).normalized;
                    total++;
                    if (Vector3.Dot(n, versLumiere) > 0f) litCount++;
                }

                if (total > 0 && (mf.name.Contains("Toit") || mf.name.Contains("Mur") || mf.name.Contains("Case")))
                {
                    Debug.Log($"[Diag] {mf.transform.root.name}/{mf.name}: {litCount}/{total} faces eclairees ({100f*litCount/total:F0}%)");
                }
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>Diagnostic : dump complet du matériau du toit de Case_Hote (shader, propriétés).</summary>
        public static void DiagnostiquerMateriauToit()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Concession.unity", OpenSceneMode.Single);
            GameObject caseHote = GameObject.Find("Case_Hote");
            var renderer = caseHote.transform.Find("Case1_Toit").GetComponent<MeshRenderer>();
            Material mat = renderer.sharedMaterial;

            Debug.Log($"[Diag] Materiau='{mat.name}' shader='{mat.shader.name}'");

            int n = ShaderUtil.GetPropertyCount(mat.shader);
            for (int i = 0; i < n; i++)
            {
                string name = ShaderUtil.GetPropertyName(mat.shader, i);
                var type = ShaderUtil.GetPropertyType(mat.shader, i);
                if (!mat.HasProperty(name)) continue;
                switch (type)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        Debug.Log($"[Diag]  {name} (Color) = {mat.GetColor(name)}");
                        break;
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        Debug.Log($"[Diag]  {name} (Float) = {mat.GetFloat(name)}");
                        break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        var tex = mat.GetTexture(name);
                        Debug.Log($"[Diag]  {name} (Tex) = {(tex != null ? tex.name + $" {tex.width}x{tex.height}" : "NULL")}");
                        break;
                }
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Diagnostic : dump des matériaux de Suudu_1 (Campement).</summary>
        public static void DiagnostiquerMateriauxSuudu()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Campement.unity", OpenSceneMode.Single);
            GameObject suudu = GameObject.Find("Suudu_1");
            foreach (var renderer in suudu.GetComponentsInChildren<MeshRenderer>())
            {
                Material mat = renderer.sharedMaterial;
                if (mat == null) { Debug.Log($"[Diag] {renderer.name}: PAS DE MATERIAU"); continue; }
                var baseColor = mat.HasProperty("baseColorFactor") ? mat.GetColor("baseColorFactor").ToString() : "n/a";
                var baseTex = mat.HasProperty("baseColorTexture") && mat.GetTexture("baseColorTexture") != null ? mat.GetTexture("baseColorTexture").name : "aucune";
                var normTex = mat.HasProperty("normalTexture") && mat.GetTexture("normalTexture") != null ? mat.GetTexture("normalTexture").name : "aucune";
                bool doubleSided = mat.HasProperty("_CullMode") && mat.GetFloat("_CullMode") == 0;
                Debug.Log($"[Diag] {renderer.name}: mat='{mat.name}' baseColorFactor={baseColor} baseColorTex={baseTex} normalTex={normTex} cullMode={(mat.HasProperty("_CullMode") ? mat.GetFloat("_CullMode").ToString() : "n/a")}");
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static GameObject TrouverRacine(Scene scene, string nom)
        {
            foreach (GameObject racine in scene.GetRootGameObjects())
            {
                if (racine.name == nom) return racine;
            }
            return null;
        }

        static void RenderScene(string nomScene)
        {
            Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{nomScene}.unity", OpenSceneMode.Single);
            CapturerCamera(scene, $"{nomScene}_apercu");
        }

        static void CapturerCamera(Scene scene, string nomFichier)
        {
            GameObject camObj = null;
            foreach (GameObject racine in scene.GetRootGameObjects())
            {
                if (racine.name == "CameraJoueur") { camObj = racine; break; }
                var t = racine.transform.Find("CameraJoueur");
                if (t != null) { camObj = t.gameObject; break; }
            }

            if (camObj == null)
            {
                Debug.LogError($"[ScenePreview] CameraJoueur introuvable dans {scene.name}.");
                return;
            }

            Camera cam = camObj.GetComponent<Camera>();
            var rt = new RenderTexture(Largeur, Hauteur, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Largeur, Hauteur, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Largeur, Hauteur), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            string dossier = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "docs", "Livrable_4", "apercus_unity"));
            Directory.CreateDirectory(dossier);
            string chemin = Path.Combine(dossier, $"{nomFichier}.png");
            File.WriteAllBytes(chemin, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            Debug.Log($"[ScenePreview] Aperçu enregistré : {chemin}");
        }

        /// <summary>
        /// Diagnostic : liste tous les objets racine d'une scène avec leur distance à
        /// CameraJoueur, pour identifier un objet visible à l'écran sans avoir à deviner.
        /// </summary>
        public static void ListerObjetsCampement()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Campement.unity", OpenSceneMode.Single);
            GameObject camObj = TrouverRacine(scene, "CameraJoueur");
            Vector3 posCam = camObj != null ? camObj.transform.position : Vector3.zero;

            foreach (GameObject racine in scene.GetRootGameObjects())
            {
                float dist = Vector3.Distance(racine.transform.position, posCam);
                Debug.Log($"[ScenePreview] {racine.name,-20} pos={racine.transform.position} dist_camera={dist:F2}");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}

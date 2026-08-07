using System.Collections.Generic;
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
            DiagnostiquerScene("Campement", new[] { "Suudu_1", "Suudu_2", "FeuDeCamp", "Joueur", "Paysage", "Acacia_0", "Acacia_1", "Vegetation_Eparse", "Troupeau", "Hoggo_Anneau_1", "Hoggo_Piquet_1", "Mare" });
            DiagnostiquerScene("Concession", new[] { "Dudal", "Case_Hote", "Galle_1", "Galle_3", "Grenier", "Joueur", "Paysage", "Troupeau" });

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Inspection en lecture seule de l'état ACTUEL des deux scènes (après édition manuelle dans l'Éditeur) — n'ouvre/ne modifie rien, ne fait qu'ouvrir+lire+logger. Sert à préparer un remplacement chirurgical de modèles (joueur, feu, troupeau) sans passer par BuildCampement/BuildConcession, qui écraserait les modifications manuelles.</summary>
        [MenuItem("Wuro&Galle/Diagnostic : état actuel (joueur, feu, troupeau) des deux scènes")]
        public static void DiagnostiquerEtatActuel()
        {
            foreach (var nomScene in new[] { "Campement", "Concession" })
            {
                Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{nomScene}.unity");
                Debug.Log($"===== {nomScene} =====");

                var joueur = TrouverDansScene(scene, "Joueur");
                if (joueur != null)
                {
                    var corps = joueur.transform.Find("Corps");
                    Debug.Log($"[Etat] Joueur : position={joueur.transform.position}, rotation={joueur.transform.rotation.eulerAngles} ; Corps={(corps != null ? corps.gameObject.name : "ABSENT")}");
                }
                else Debug.Log("[Etat] Joueur : ABSENT");

                var feu = TrouverDansScene(scene, "FeuDeCamp");
                if (feu != null)
                {
                    var noms = new List<string>();
                    foreach (Transform enfant in feu.transform) noms.Add(enfant.name);
                    Debug.Log($"[Etat] FeuDeCamp : position={feu.transform.position}, enfants=[{string.Join(", ", noms)}]");
                }
                else Debug.Log("[Etat] FeuDeCamp : ABSENT");

                var troupeau = TrouverDansScene(scene, "Troupeau");
                if (troupeau == null) troupeau = TrouverDansScene(scene, "Troupeau (1)"); // renommage Unity constaté (collision de nom) sur Concession
                if (troupeau != null)
                {
                    Debug.Log($"[Etat] Troupeau : {troupeau.transform.childCount} têtes");
                    foreach (Transform enfant in troupeau.transform)
                    {
                        var mf = enfant.GetComponent<MeshFilter>() ?? enfant.GetComponentInChildren<MeshFilter>();
                        Debug.Log($"[Etat]   {enfant.name} : position={enfant.position}, rotation={enfant.rotation.eulerAngles}, scale={enfant.localScale}");
                    }
                }
                else Debug.Log("[Etat] Troupeau : ABSENT");
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Hauteur du sol réel (raycast depuis le ciel) sous le Joueur, comparée à la position Y du Joueur et aux bounds de son Corps — pour diagnostiquer un flottement/enfoncement par rapport au terrain.</summary>
        [MenuItem("Wuro&Galle/Diagnostic : joueur vs hauteur du terrain")]
        public static void DiagnostiquerHauteurJoueur()
        {
            foreach (var nomScene in new[] { "Campement", "Concession" })
            {
                Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{nomScene}.unity");
                var joueur = TrouverDansScene(scene, "Joueur");
                if (joueur == null) { Debug.LogWarning($"[Diagnostic] {nomScene}: Joueur introuvable."); continue; }

                Vector3 pos = joueur.transform.position;
                // Décalé de 3m en X : évite de re-toucher la propre capsule du CharacterController
                // du Joueur (rayon 0.3) ou son Corps — sinon le raycast mesure le joueur lui-même,
                // pas le sol (bug constaté : le sol "remontait" d'exactement le déplacement du Joueur).
                Vector3 origineSonde = pos + new Vector3(3f, 50f, 0f);
                float solY = float.NaN;
                if (Physics.Raycast(origineSonde, Vector3.down, out var hit, 200f))
                    solY = hit.point.y;

                var corps = joueur.transform.Find("Corps");
                string boundsStr = "ABSENT";
                if (corps != null)
                {
                    var renderers = corps.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        Bounds b = renderers[0].bounds;
                        foreach (var r in renderers) b.Encapsulate(r.bounds);
                        boundsStr = $"min.y={b.min.y:F3}, max.y={b.max.y:F3}, center={b.center}";
                    }
                }

                var cc = joueur.GetComponent<CharacterController>();
                string ccStr = cc != null ? $"height={cc.height}, center={cc.center}, radius={cc.radius}" : "ABSENT";

                var cam = joueur.transform.Find("CameraJoueur");
                string camStr = cam != null ? $"localPosition={cam.localPosition}" : "ABSENT";

                Debug.Log($"[Diagnostic] {nomScene} : Joueur.position={pos}, sol (raycast)={solY}, Corps bounds : {boundsStr}, CharacterController : {ccStr}, CameraJoueur : {camStr}");
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Logue les matériaux réels (couleur/metallic/smoothness/texture) de chaque tête du Troupeau — pour vérifier si le correctif metallic=0 de zebu_realiste.py s'est bien propagé jusqu'au matériau importé dans Unity, indépendamment de l'éclairage de la scène.</summary>
        [MenuItem("Wuro&Galle/Diagnostic : matériaux du troupeau")]
        public static void DiagnostiquerMateriauxTroupeau()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Campement.unity");
            var troupeau = TrouverDansScene(scene, "Troupeau");
            if (troupeau == null) { Debug.LogWarning("[Diagnostic] Troupeau introuvable."); }
            else
            {
                foreach (Transform enfant in troupeau.transform)
                {
                    var renderer = enfant.GetComponent<Renderer>();
                    if (renderer == null || renderer.sharedMaterial == null) continue;
                    var mat = renderer.sharedMaterial;
                    var shader = mat.shader;
                    var props = new System.Text.StringBuilder();
                    int n = UnityEditor.ShaderUtil.GetPropertyCount(shader);
                    for (int i = 0; i < n; i++)
                    {
                        string nom = UnityEditor.ShaderUtil.GetPropertyName(shader, i);
                        var type = UnityEditor.ShaderUtil.GetPropertyType(shader, i);
                        string val = type switch
                        {
                            UnityEditor.ShaderUtil.ShaderPropertyType.Color => mat.GetColor(nom).ToString(),
                            UnityEditor.ShaderUtil.ShaderPropertyType.Float => mat.GetFloat(nom).ToString(),
                            UnityEditor.ShaderUtil.ShaderPropertyType.Range => mat.GetFloat(nom).ToString(),
                            UnityEditor.ShaderUtil.ShaderPropertyType.TexEnv => (mat.GetTexture(nom) != null ? mat.GetTexture(nom).name : "null"),
                            _ => "?"
                        };
                        props.Append($"{nom}={val}; ");
                    }
                    Debug.Log($"[Diagnostic] {enfant.name} : shader={shader.name} :: {props}");
                }
            }
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

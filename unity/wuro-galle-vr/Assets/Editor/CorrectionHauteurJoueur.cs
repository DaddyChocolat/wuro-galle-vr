using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WuroGalle.Editor
{
    /// <summary>
    /// Corrige deux bugs constatés via DiagnosticTailles.DiagnostiquerHauteurJoueur :
    ///  1) Le Joueur est spawné à Y=0 alors que le sol réel (raycast) est à Y≈1.62 —
    ///     décalage venant du remplacement du terrain plat par un vrai Terrain Unity
    ///     (AjouterTerrainSceneActive) sans réajuster le spawn du Joueur en conséquence.
    ///  2) Le Corps (GuerrierAfricain.glb) mesure ~5,0 m de haut, pieds à Y=-2,45 (au
    ///     lieu de 1,78 m / pieds à 0 visé par guerrier_africain.py) — l'échelle
    ///     appliquée dans Blender n'est manifestement pas arrivée correctement jusqu'à
    ///     la scène. Corrigé ici directement sur l'instance (mesure réelle -> échelle et
    ///     offset calculés, pas de valeur codée en dur), plus rapide qu'un aller-retour
    ///     Blender vu le temps disponible.
    /// </summary>
    public static class CorrectionHauteurJoueur
    {
        const float HauteurCibleCorps = 1.78f;

        [MenuItem("Wuro&Galle/Corriger hauteur du joueur (sol + échelle du corps)")]
        public static void Corriger()
        {
            foreach (var nomScene in new[] { "Campement", "Concession" })
            {
                Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{nomScene}.unity", OpenSceneMode.Single);
                var joueur = TrouverDansScene(scene, "Joueur");
                if (joueur == null) { Debug.LogWarning($"[CorrectionHauteurJoueur] {nomScene}: Joueur introuvable."); continue; }

                // 1) Aligner le Joueur sur le sol réel (raycast vertical depuis le ciel).
                // Décalé de 3m en X : un raycast pile au-dessus du Joueur touche sa PROPRE
                // capsule/son Corps avant le sol — bug constaté au premier passage (le sol
                // mesuré "remontait" exactement du déplacement appliqué au Joueur, la vraie
                // hauteur du sol ici est ~0, pas 1.62).
                Vector3 pos = joueur.transform.position;
                if (Physics.Raycast(pos + new Vector3(3f, 50f, 0f), Vector3.down, out var hit, 200f))
                {
                    float ancienY = pos.y;
                    joueur.transform.position = new Vector3(pos.x, hit.point.y, pos.z);
                    Debug.Log($"[CorrectionHauteurJoueur] {nomScene} : Joueur.y {ancienY:F3} -> {hit.point.y:F3} (sol réel).");
                }
                else
                {
                    Debug.LogWarning($"[CorrectionHauteurJoueur] {nomScene} : raycast sol échoué, position Y du Joueur inchangée.");
                }

                // 2) Recalibrer l'échelle/position du Corps à partir de ses bounds RÉELS
                // (pas d'une valeur supposée) pour qu'il fasse HauteurCibleCorps, pieds à
                // Y local 0 — quelle que soit l'échelle bugguée actuellement en place.
                var corps = joueur.transform.Find("Corps");
                if (corps == null) { Debug.LogWarning($"[CorrectionHauteurJoueur] {nomScene} : 'Corps' introuvable."); continue; }

                var renderers = corps.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) { Debug.LogWarning($"[CorrectionHauteurJoueur] {nomScene} : Corps sans Renderer, échelle non corrigée."); continue; }

                Bounds b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                float hauteurActuelle = b.size.y;
                if (hauteurActuelle <= 0.001f) { Debug.LogWarning($"[CorrectionHauteurJoueur] {nomScene} : bounds Corps dégénérés."); continue; }

                float facteurCorrectif = HauteurCibleCorps / hauteurActuelle;
                Vector3 echelleAvant = corps.localScale;
                corps.localScale = echelleAvant * facteurCorrectif;

                // Après ce nouveau scale, les bounds min.y sont mis à l'échelle d'autant
                // (mise à l'échelle centrée sur le pivot du Corps, pas sur ses bounds) —
                // on recalcule pour repositionner les pieds exactement à 0 plutôt que de
                // supposer une simple proportionnalité, plus fiable si le pivot n'est pas
                // exactement au centre du mesh.
                var renderers2 = corps.GetComponentsInChildren<Renderer>();
                Bounds b2 = renderers2[0].bounds;
                foreach (var r in renderers2) b2.Encapsulate(r.bounds);
                float minYApres = b2.min.y - joueur.transform.position.y; // relatif au Joueur (repère local le long de Y, rotation identity supposée)
                corps.localPosition -= new Vector3(0f, minYApres, 0f);

                Debug.Log($"[CorrectionHauteurJoueur] {nomScene} : Corps hauteur {hauteurActuelle:F3} -> {HauteurCibleCorps:F3} (x{facteurCorrectif:F4}), pieds recalés à 0 (offset {-minYApres:F3}).");

                EditorSceneManager.SaveScene(scene);
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static GameObject TrouverEnfant(Transform parent, string nom)
        {
            if (parent.name == nom) return parent.gameObject;
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
                var trouve = TrouverEnfant(racine.transform, nom);
                if (trouve != null) return trouve;
            }
            return null;
        }
    }
}

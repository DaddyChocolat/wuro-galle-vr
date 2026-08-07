using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WuroGalle.Editor
{
    /// <summary>
    /// Remplacement CHIRURGICAL de modèles sur les scènes TELLES QUE SAUVEGARDÉES
    /// actuellement (édition manuelle de l'utilisateur dans l'Éditeur : positions et
    /// couleurs du troupeau, etc.) — n'appelle JAMAIS SceneBuilder.BuildCampement/
    /// BuildConcession/ExecuterPipelineComplet, qui écraseraient ce travail. Ouvre
    /// chaque scène (OpenSceneMode.Single charge le contenu sauvegardé, ne le vide
    /// pas — contrairement à NewScene), modifie seulement les objets ciblés, sauvegarde.
    ///
    /// Trois remplacements demandés :
    ///  - Corps du joueur -> "African Warrior" (avenirdesign, Sketchfab, CC-BY-4.0)
    ///  - Modèle du troupeau -> "Ankole Cattle" (kenchoo, Sketchfab — licence à confirmer)
    ///  - Flammes du foyer -> "Animated fire" (Yannick Deharo, Sketchfab, CC-BY-4.0),
    ///    FBX importé nativement par Unity (pas de passage par Blender : l'animation
    ///    "flipbook" à 64 objets se lit directement comme un seul clip "Take 001" côté
    ///    FBX, alors qu'un aller-retour par glTF l'aurait éclatée en 64 clips séparés).
    /// </summary>
    public static class RemplacementModeles
    {
        const string PathGuerrier = "Assets/Models/Personnage/GuerrierAfricain.glb";
        const string PathPersonnageOriginal = "Assets/Models/Personnage/Personnage.glb";
        const string PathAnkole = "Assets/Models/Troupeau/AnkoleCattle.glb";
        const string PathFeuAnime = "Assets/Models/Feu/Animated_fire.fbx";

        /// <summary>
        /// Restaure l'interaction "caresser" (avec son, voir ActionCaresserTroupeau.cs)
        /// sur les têtes de bétail actuelles — perdue quand RemplacerTroupeau a détruit
        /// les anciennes têtes (zébu, avec leur composant ActionCaresserTroupeau déjà
        /// posé par AjouterInteractionsSceneActive) pour les remplacer par des Ankole,
        /// sans réappliquer cette interaction sur les nouvelles. Puis assigne les lignes
        /// PNJ (audio ré-enregistré par l'utilisateur) — même besoin : PNJ_Berger/
        /// PNJ_Femme recréés depuis par ExecuterPipelineComplet, avec un tableau de
        /// lignes vide à chaque fois.
        /// </summary>
        [MenuItem("Wuro&Galle/Restaurer caresser (son) + assigner voix PNJ")]
        public static void RestaurerCaresserEtVoixPNJ()
        {
            foreach (var nomScene in new[] { "Campement", "Concession" })
            {
                Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{nomScene}.unity", OpenSceneMode.Single);
                SceneBuilder.AjouterActionCaresserTroupeau(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[RemplacementModeles] {nomScene} : interaction 'caresser' (avec son) restaurée sur le troupeau actuel.");
            }

            AssignerDialoguesPNJ.AssignerToutes();

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Wuro&Galle/Remplacer les modèles (guerrier, ankole, feu) sur les scènes actuelles")]
        public static void RemplacerModeles()
        {
            foreach (var nomScene in new[] { "Campement", "Concession" })
            {
                Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{nomScene}.unity", OpenSceneMode.Single);
                Debug.Log($"[RemplacementModeles] ===== {nomScene} =====");

                RemplacerCorpsJoueur(scene);
                RemplacerFlammesFeu(scene);
                RemplacerTroupeau(scene);

                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[RemplacementModeles] {nomScene} sauvegardée.");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>
        /// Retour en arrière sur le corps du joueur : le remplacement par GuerrierAfricain.glb
        /// (échelle bugguée, corrigée après coup à la main, cf. CorrectionHauteurJoueur.cs)
        /// est abandonné sur demande — remet Personnage.glb (base CC0, celle qui fonctionnait :
        /// squelette hips/spine/chest/thigh.L/shin.L... déjà posé correctement par
        /// CorpsJoueur.cs, hauteur/pieds au sol corrects dès l'origine). Ne touche PAS à la
        /// position Y du Joueur (corrigée séparément — le sol réel était déjà à 0, cohérent
        /// avec le spawn d'origine).
        /// </summary>
        [MenuItem("Wuro&Galle/Rétablir le corps du joueur d'origine (Personnage.glb)")]
        public static void RetablirCorpsOriginal()
        {
            foreach (var nomScene in new[] { "Campement", "Concession" })
            {
                Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{nomScene}.unity", OpenSceneMode.Single);
                var joueur = TrouverDansScene(scene, "Joueur");
                if (joueur == null) { Debug.LogWarning($"[RemplacementModeles] {nomScene}: 'Joueur' introuvable."); continue; }

                var ancienCorps = joueur.transform.Find("Corps");
                if (ancienCorps != null) Object.DestroyImmediate(ancienCorps.gameObject);

                var nouveauCorps = Instancier(PathPersonnageOriginal, "Corps");
                if (nouveauCorps == null) continue;
                nouveauCorps.transform.SetParent(joueur.transform);
                nouveauCorps.transform.localPosition = Vector3.zero;
                nouveauCorps.transform.localRotation = Quaternion.identity;
                nouveauCorps.transform.localScale = Vector3.one; // au cas où une échelle serait restée de la correction précédente

                // BUG corrigé ici : RemplacerCorpsJoueur (plus bas) avait ajouté CorpsJoueur
                // sur "joueur" (la racine) au lieu de "Corps" — exactement le bug documenté
                // dans SceneBuilder.CreerJoueur : Awake() de CorpsJoueur fait
                // gameObject.SetActive(false), et sur la racine ça désactive AUSSI
                // CameraJoueur/FirstPersonController (tous ses enfants) -> "Display 1 no
                // camera rendering" en Play mode. Retiré de la racine, ajouté sur Corps.
                var corpsJoueurSurRacine = joueur.GetComponent<CorpsJoueur>();
                if (corpsJoueurSurRacine != null) Object.DestroyImmediate(corpsJoueurSurRacine);
                if (nouveauCorps.GetComponent<CorpsJoueur>() == null) nouveauCorps.AddComponent<CorpsJoueur>();

                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[RemplacementModeles] {nomScene} : Corps -> Personnage.glb (rétabli), CorpsJoueur recalé sur Corps (pas sur Joueur).");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static GameObject Instancier(string cheminAsset, string nom)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(cheminAsset);
            if (source == null)
            {
                Debug.LogError($"[RemplacementModeles] Asset introuvable : {cheminAsset}");
                return null;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = nom;
            return instance;
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

        static void RemplacerCorpsJoueur(Scene scene)
        {
            var joueur = TrouverDansScene(scene, "Joueur");
            if (joueur == null) { Debug.LogWarning("[RemplacementModeles] 'Joueur' introuvable — corps non remplacé."); return; }

            var ancienCorps = joueur.transform.Find("Corps");
            if (ancienCorps != null) Object.DestroyImmediate(ancienCorps.gameObject);

            var nouveauCorps = Instancier(PathGuerrier, "Corps");
            if (nouveauCorps == null) return;
            nouveauCorps.transform.SetParent(joueur.transform);
            nouveauCorps.transform.localPosition = Vector3.zero;
            nouveauCorps.transform.localRotation = Quaternion.identity;

            // CorpsJoueur pilote le squelette par nom d'os (voir CorpsJoueur.cs, recherche
            // Contains() maintenant plutôt qu'égalité stricte, pour ce genre de rig externe).
            // SUR "Corps", PAS SUR "joueur" : Awake() de CorpsJoueur fait
            // gameObject.SetActive(false) — sur la racine ça désactive aussi CameraJoueur/
            // FirstPersonController (bug réel vécu : "Display 1 no camera rendering").
            // Voir aussi le même commentaire dans SceneBuilder.CreerJoueur.
            if (nouveauCorps.GetComponent<CorpsJoueur>() == null) nouveauCorps.AddComponent<CorpsJoueur>();

            Debug.Log("[RemplacementModeles] Corps du joueur -> GuerrierAfricain.glb.");
        }

        /// <summary>
        /// Le FBX (65 objets, chacun avec sa propre petite action de 2 images — technique
        /// "flipbook" par échelle, voir docs de l'auteur) importe par défaut en Animation
        /// Type "Generic" côté Unity, qui exigerait un Animator + AnimatorController pour
        /// être joué. Forcé en "Legacy" : Unity ajoute alors directement un composant
        /// Animation jouable sans configuration supplémentaire (adapté ici puisqu'il n'y a
        /// qu'un seul clip combiné "Take 001", pas de squelette à retargeter).
        /// </summary>
        static void ForcerImportAnimationLegacy(string cheminModele)
        {
            var importer = AssetImporter.GetAtPath(cheminModele) as ModelImporter;
            if (importer == null) return;
            if (importer.animationType == ModelImporterAnimationType.Legacy) return;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.SaveAndReimport();
        }

        static void RemplacerFlammesFeu(Scene scene)
        {
            var feu = TrouverDansScene(scene, "FeuDeCamp");
            if (feu == null) { Debug.LogWarning("[RemplacementModeles] 'FeuDeCamp' introuvable — flammes non remplacées."); return; }
            AppliquerFlammeAnimee(feu);
        }

        /// <summary>Remplace/ajoute le maillage de flamme animée sur un FeuDeCamp donné (déjà existant ou tout juste créé) — factorisé pour être réutilisé par RemplacerFlammesFeu (Campement) et AjouterFeuConcession (nouveau foyer).</summary>
        static void AppliquerFlammeAnimee(GameObject feu)
        {
            ForcerImportAnimationLegacy(PathFeuAnime);

            // Seules les FLAMMES sont remplacées par le maillage animé : fumée/braises
            // (textures Kenney réelles ajoutées récemment) restent des particules —
            // l'asset "Animated fire" ne contient que la flamme, pas de fumée.
            var particulesFlammes = feu.transform.Find("Particules_Flammes");
            if (particulesFlammes != null) Object.DestroyImmediate(particulesFlammes.gameObject);

            var nouveauFeu = Instancier(PathFeuAnime, "Flamme_Animee");
            if (nouveauFeu == null) return;
            nouveauFeu.transform.SetParent(feu.transform);
            nouveauFeu.transform.localPosition = Vector3.zero;
            nouveauFeu.transform.localRotation = Quaternion.identity;
            // Taille d'origine du modèle (~0.85 m de haut, voir inspect_assets.log) proche
            // de l'ancien système de particules (startSize 0.3-0.6, sizeOverLifetime x1) —
            // laissé à l'échelle 1 par défaut, à ajuster si trop grand/petit à l'écran.

            var anim = nouveauFeu.GetComponentInChildren<Animation>();
            if (anim != null && anim.clip != null)
            {
                anim.wrapMode = WrapMode.Loop;
                var clip = anim.clip;
                clip.wrapMode = WrapMode.Loop;
                anim.playAutomatically = true;
            }
            else
            {
                Debug.LogWarning("[RemplacementModeles] Animated_fire.fbx importé sans composant Animation détecté — vérifier le Model Importer (Animation Type doit être 'Legacy' pour ce composant, ou adapter en Animator si 'Generic'/'Humanoid').");
            }

            Debug.Log("[RemplacementModeles] Flammes du foyer -> Animated_fire.fbx.");
        }

        /// <summary>
        /// Ajoute un foyer à la Concession, qui n'en avait aucun (constaté : "FeuDeCamp
        /// introuvable" lors du remplacement précédent — la Concession n'a jamais eu de feu,
        /// contrairement à ce qu'on pensait au départ). Près du Grenier (0,0,1), décalé au
        /// sud-ouest pour ne chevaucher ni le Grenier lui-même (empreinte ~1,5x1,5 m) ni le
        /// mobilier des cases (~1.5-1.8, 2.8-3.25) plus à l'est.
        /// </summary>
        [MenuItem("Wuro&Galle/Ajouter un foyer à la Concession (près du Grenier)")]
        public static void AjouterFeuConcession()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Concession.unity", OpenSceneMode.Single);

            if (TrouverDansScene(scene, "FeuDeCamp") != null)
            {
                Debug.LogWarning("[RemplacementModeles] La Concession a déjà un 'FeuDeCamp' — rien ajouté (relancer 'Remplacer les modèles' si c'est lui qu'il faut mettre à jour).");
                if (Application.isBatchMode) EditorApplication.Exit(0);
                return;
            }

            var feu = SceneBuilder.CreerFeuDeCamp(new Vector3(-1.4f, 0f, 0.3f));
            SceneBuilder.AdoucirTexturesParticulesFeu(scene); // texture Kenney réelle sur fumée/braises, comme au Campement
            AppliquerFlammeAnimee(feu);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[RemplacementModeles] Foyer ajouté à la Concession, près du Grenier (-1.4, 0, 0.3).");

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void RemplacerTroupeau(Scene scene)
        {
            var troupeau = TrouverDansScene(scene, "Troupeau");
            if (troupeau == null) troupeau = TrouverDansScene(scene, "Troupeau (1)"); // renommage Unity constaté sur Concession
            if (troupeau == null) { Debug.LogWarning("[RemplacementModeles] 'Troupeau' introuvable — bêtes non remplacées."); return; }

            // Copie la liste AVANT de modifier (on détruit/recrée des enfants pendant
            // l'itération, une foreach directe sur troupeau.transform serait invalidée).
            var enfants = new List<Transform>();
            foreach (Transform enfant in troupeau.transform) enfants.Add(enfant);

            int rng_i = 0;
            var rng = new System.Random(17);
            foreach (var enfant in enfants)
            {
                Vector3 position = enfant.position;
                // Rotation : PAS conservée telle quelle. L'inclinaison X/Y insolite observée
                // sur le zébu (ex. 87.76°, 158.41°, 165.62°) est une correction manuelle de
                // l'utilisateur spécifique à l'orientation du maillage zébu — l'appliquer tel
                // quel à l'Ankole (dont l'aperçu Blender direct montre une orientation debout
                // déjà correcte, sans correction nécessaire) le ferait probablement basculer
                // au sol. Nouvelle rotation Y aléatoire simple, comme le placement d'origine.
                float rotationY = (float)(rng.NextDouble() * 360.0);

                Object.DestroyImmediate(enfant.gameObject);

                var nouvelle = Instancier(PathAnkole, $"Troupeau_Ankole_{rng_i}");
                rng_i++;
                if (nouvelle == null) continue;
                nouvelle.transform.SetParent(troupeau.transform);
                nouvelle.transform.position = position;
                nouvelle.transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            }

            Debug.Log($"[RemplacementModeles] Troupeau : {enfants.Count} tête(s) -> AnkoleCattle.glb (positions conservées, rotation ré-appliquée en Y simple).");
        }
    }
}

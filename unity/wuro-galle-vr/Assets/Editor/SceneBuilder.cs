using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;

namespace WuroGalle.Editor
{
    /// <summary>
    /// Assemble automatiquement les scènes "Campement" et "Concession" à partir des modules
    /// GLTF déjà importés dans Assets/Models/. Évite le placement manuel objet par objet.
    ///
    /// Usage : menu Unity > Wuro&Galle > Construire scène Campement / Construire scène Concession.
    /// Les scènes générées sont sauvegardées dans Assets/Scenes/ et peuvent être rouvertes,
    /// ajustées à la main puis re-générées (le script écrase la scène existante à chaque appel).
    /// </summary>
    public static class SceneBuilder
    {
        // Chemins des assets GLTF importés (relatifs à Assets/). glTFast expose directement
        // le fichier .glb comme un GameObject utilisable via AssetDatabase, pas besoin de préfab séparé.
        const string PathSuudu = "Assets/Models/Suudu/Suudu.glb";
        const string PathGalle1 = "Assets/Models/Galle/galle_1.glb";
        const string PathGalle2 = "Assets/Models/Galle/galle_2.glb";
        const string PathGalle3 = "Assets/Models/Galle/galle_3.glb";
        const string PathPaysage = "Assets/Models/Paysage/Paysage.glb"; // inclut déjà l'enclos (hoggo) et la mare
        const string PathTroupeauBlanc = "Assets/Models/Troupeau/troupeau_robe_blanche.glb";
        const string PathTroupeauRoux = "Assets/Models/Troupeau/troupeau_robe_rousse.glb";
        const string PathDudal = "Assets/Models/Galle/dudal.glb";
        const string PathGrenier = "Assets/Models/Galle/grenier.glb";
        const string PathCalebasse = "Assets/Models/Mobilier/Calebasse.glb";
        const string PathNatte = "Assets/Models/Mobilier/Natte.glb";
        const string PathMortier = "Assets/Models/Mobilier/Mortier.glb";
        const string PathPilon = "Assets/Models/Mobilier/Pilon.glb";
        // Acacia parasol (blender/paysage/vegetation_sahelienne.py) — un seul module,
        // réutilisé plusieurs fois par CreerVegetationEparse. Import requis avant de
        // relancer BuildCampement/BuildConcession/le pipeline complet, sinon
        // Instancier() crée un placeholder "MANQUANT_" visible mais inoffensif.
        const string PathAcacia = "Assets/Models/Paysage/Acacia.glb";

        // Canari (jarre à eau, blender/mobilier/canari.py) : manquait jusqu'ici alors
        // qu'il porte l'interaction "boire" (voir AjouterInteractionsSceneActive).
        const string PathCanari = "Assets/Models/Mobilier/Canari.glb";

        // Corps du joueur (blender/personnage/personnage_joueur.py) : silhouette
        // articulée modélisée, remplace les capsules générées en code — voir
        // CreerJoueur et Assets/Scripts/CorpsJoueur.cs.
        const string PathPersonnage = "Assets/Models/Personnage/Personnage.glb";

        // Sons libres de droits déjà présents dans Assets/Audio/ (voir note-ethique.md
        // pour les critères de sélection des sources).
        const string AudioFeu = "Assets/Audio/637523__kyles__fire-small-campfire-crackling-short-air-tone.flac";
        const string AudioCowBells = "Assets/Audio/359125__schmutz__cow-bells.wav";
        const string AudioGrazingCows = "Assets/Audio/651517__davorl__20180812-grazing-cows-in-valcomasine.ogg";
        const string AudioWind = "Assets/Audio/156414__felixblume__wind-blowing-into-some-cactus-spine-on-the-top-of-the-mountain-in-the-desert-of-atacama-chile.wav";
        const string AudioAppelPriere = "Assets/Audio/329857__martineerok__call-for-prayer-ramallah.wav";
        const string AudioMouton = "Assets/Audio/23725__jppi_stu__sw_fair_sheep_1.flac";

        /// <summary>
        /// Ajoute l'audio 3D à la scène ACTUELLEMENT OUVERTE, sans la reconstruire —
        /// pour ne pas écraser un agencement déjà ajusté à la main. Reconnaît la scène
        /// par son nom ("Campement" ou "Concession"). Peut être relancé sans problème :
        /// supprime d'abord les sources du même nom avant de les recréer.
        /// </summary>
        [MenuItem("Wuro&Galle/Ajouter l'audio à la scène active")]
        public static void AjouterAudioSceneActive()
        {
            Scene scene = EditorSceneManager.GetActiveScene();

            if (scene.name == "Campement")
            {
                RemplacerSourceAudio(scene, "Audio_Feu", AudioFeu, new Vector3(0f, 0.3f, 0f), 0.7f, 6f);
                RemplacerSourceAudio(scene, "Audio_Clochettes", AudioCowBells, new Vector3(-10.5f, 0.5f, 0f), 0.6f, 12f);
                RemplacerSourceAudio(scene, "Audio_Paturage", AudioGrazingCows, new Vector3(-10.5f, 0.5f, 0f), 0.5f, 12f);
                RemplacerSourceAudio(scene, "Audio_Vent", AudioWind, new Vector3(0f, 2f, 4f), 0.35f, 30f, spatialBlend: 0.2f);
                Debug.Log("[SceneBuilder] Audio ajouté à la scène Campement.");
            }
            else if (scene.name == "Concession")
            {
                RemplacerSourceAudio(scene, "Audio_AppelPriere", AudioAppelPriere, new Vector3(5.5f, 1.5f, -2.5f), 0.5f, 15f);
                RemplacerSourceAudio(scene, "Audio_Clochettes", AudioCowBells, new Vector3(-10.5f, 0.5f, 0f), 0.5f, 12f);
                RemplacerSourceAudio(scene, "Audio_Mouton", AudioMouton, new Vector3(-10.5f, 0.5f, 0f), 0.5f, 12f);
                RemplacerSourceAudio(scene, "Audio_Vent", AudioWind, new Vector3(0f, 2f, -4f), 0.35f, 30f, spatialBlend: 0.2f);
                Debug.Log("[SceneBuilder] Audio ajouté à la scène Concession.");
            }
            else
            {
                Debug.LogWarning($"[SceneBuilder] Scène active '{scene.name}' non reconnue " +
                    "(attendu \"Campement\" ou \"Concession\") — rien n'a été ajouté.");
                return;
            }

            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Ajoute un Post-Process Volume global (Bloom, Color Grading, Ambient Occlusion,
        /// Vignette) + anti-aliasing FXAA sur la caméra, à la scène ACTUELLEMENT OUVERTE.
        /// Non destructif : peut être relancé sans dupliquer quoi que ce soit (réutilise le
        /// même profil et le même volume s'ils existent déjà).
        /// Nécessite que le joueur (CreerJoueur) soit déjà dans la scène.
        /// </summary>
        [MenuItem("Wuro&Galle/Ajouter post-processing à la scène active")]
        public static void AjouterPostProcessingSceneActive()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Campement" && scene.name != "Concession")
            {
                Debug.LogWarning($"[SceneBuilder] Scène active '{scene.name}' non reconnue " +
                    "(attendu \"Campement\" ou \"Concession\") — rien n'a été ajouté.");
                return;
            }

            GameObject camObj = TrouverDansScene(scene, "CameraJoueur");
            if (camObj == null)
            {
                Debug.LogError("[SceneBuilder] CameraJoueur introuvable — ajoute d'abord le joueur (CreerJoueur) à la scène.");
                return;
            }

            // Layer "Default" (0) pour le volume global : pas besoin de créer un layer custom.
            PostProcessLayer ppLayer = camObj.GetComponent<PostProcessLayer>();
            if (ppLayer == null) ppLayer = camObj.AddComponent<PostProcessLayer>();
            ppLayer.volumeLayer = 1 << 0;
            ppLayer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;

            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");

            const string cheminProfil = "Assets/Settings/Profil_PostProcess.asset";
            PostProcessProfile profil = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(cheminProfil);
            if (profil == null)
            {
                profil = ScriptableObject.CreateInstance<PostProcessProfile>();
                AssetDatabase.CreateAsset(profil, cheminProfil);
            }

            ConfigurerBloom(profil);
            ConfigurerColorGrading(profil);
            ConfigurerAmbientOcclusion(profil);
            ConfigurerVignette(profil);
            EditorUtility.SetDirty(profil);
            AssetDatabase.SaveAssets();

            GameObject existant = TrouverDansScene(scene, "Global_PostProcess_Volume");
            if (existant != null) Object.DestroyImmediate(existant);

            GameObject volumeObj = new GameObject("Global_PostProcess_Volume");
            volumeObj.layer = 0;
            PostProcessVolume volume = volumeObj.AddComponent<PostProcessVolume>();
            volume.isGlobal = true;
            volume.sharedProfile = profil;

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneBuilder] Post-processing ajouté à la scène active (voir Assets/Settings/Profil_PostProcess.asset).");
        }

        /// <summary>Récupère un module d'effet du profil s'il existe déjà, sinon l'ajoute (évite les doublons si on relance le menu).</summary>
        private static T ObtenirOuAjouterSettings<T>(PostProcessProfile profil) where T : PostProcessEffectSettings
        {
            if (!profil.TryGetSettings<T>(out T settings))
                settings = profil.AddSettings<T>();
            return settings;
        }

        private static void ConfigurerBloom(PostProcessProfile profil)
        {
            var bloom = ObtenirOuAjouterSettings<Bloom>(profil);
            bloom.enabled.overrideState = true; bloom.enabled.value = true;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.3f;
            bloom.threshold.overrideState = true; bloom.threshold.value = 1.1f;
            bloom.softKnee.overrideState = true; bloom.softKnee.value = 0.5f;
        }

        private static void ConfigurerColorGrading(PostProcessProfile profil)
        {
            var cg = ObtenirOuAjouterSettings<ColorGrading>(profil);
            cg.enabled.overrideState = true; cg.enabled.value = true;
            cg.tonemapper.overrideState = true; cg.tonemapper.value = Tonemapper.ACES;
            // Légèrement chaud (ambiance sahélienne, pas un simple filtre "carte postale")
            cg.temperature.overrideState = true; cg.temperature.value = 8f;
            cg.saturation.overrideState = true; cg.saturation.value = 5f;
            cg.contrast.overrideState = true; cg.contrast.value = 5f;
        }

        private static void ConfigurerAmbientOcclusion(PostProcessProfile profil)
        {
            var ao = ObtenirOuAjouterSettings<AmbientOcclusion>(profil);
            ao.enabled.overrideState = true; ao.enabled.value = true;
            ao.intensity.overrideState = true; ao.intensity.value = 0.4f;
            ao.thicknessModifier.overrideState = true; ao.thicknessModifier.value = 1f;
        }

        private static void ConfigurerVignette(PostProcessProfile profil)
        {
            var vig = ObtenirOuAjouterSettings<Vignette>(profil);
            vig.enabled.overrideState = true; vig.enabled.value = true;
            vig.intensity.overrideState = true; vig.intensity.value = 0.25f;
            vig.smoothness.overrideState = true; vig.smoothness.value = 0.4f;
        }

        /// <summary>
        /// Corrige un oubli dans CreerLumiereDirectionnelle : un Light créé par script n'a
        /// AUCUNE ombre par défaut (LightShadows.None), d'où les ombres manquantes au sol
        /// sous les cases/galle malgré un soleil bien visible. Non destructif : modifie
        /// juste le composant Light existant sur "Soleil_Zenith" dans la scène active.
        /// </summary>
        [MenuItem("Wuro&Galle/Corriger : activer les ombres du soleil sur la scène active")]
        public static void ActiverOmbresSoleilSceneActive()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            GameObject soleilGO = TrouverDansScene(scene, "Soleil_Zenith");
            if (soleilGO == null)
            {
                Debug.LogError("[SceneBuilder] \"Soleil_Zenith\" introuvable dans la scène active.");
                return;
            }

            Light lumiere = soleilGO.GetComponent<Light>();
            if (lumiere == null)
            {
                Debug.LogError("[SceneBuilder] \"Soleil_Zenith\" n'a pas de composant Light.");
                return;
            }

            lumiere.shadows = LightShadows.Soft;
            lumiere.shadowStrength = 0.8f;

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneBuilder] Ombres activées sur Soleil_Zenith — vérifie en Play mode.");
        }

        /// <summary>Supprime l'objet du même nom (dans la scène donnée) s'il existe déjà, puis recrée la source audio.</summary>
        static void RemplacerSourceAudio(Scene scene, string nom, string cheminClip, Vector3 position,
            float volume, float portee, float spatialBlend = 1f)
        {
            var existant = TrouverDansScene(scene, nom);
            if (existant != null) Object.DestroyImmediate(existant);
            CreerSourceAudio(nom, cheminClip, position, volume, portee, spatialBlend);
        }

        /// <summary>
        /// Ajoute un PNJ placeholder (silhouette capsule, sans traits — le vrai modèle
        /// stylisé viendra plus tard) à la scène ACTUELLEMENT OUVERTE, avec sa zone de
        /// dialogue déclenché (PNJDialogue.cs). Aucune ligne audio assignée pour
        /// l'instant : le tableau "Lignes" est vide, à remplir dans l'Inspector une fois
        /// les voix enregistrées (voir docs/Livrable_4). S'assure aussi que le Joueur a
        /// bien le tag "Player", requis par PNJDialogue.OnTriggerEnter.
        /// </summary>
        [MenuItem("Wuro&Galle/Ajouter un PNJ (placeholder) à la scène active")]
        public static void AjouterPNJSceneActive()
        {
            Scene scene = EditorSceneManager.GetActiveScene();

            var joueur = TrouverDansScene(scene, "Joueur");
            if (joueur != null && !joueur.CompareTag("Player"))
                joueur.tag = "Player";

            if (scene.name == "Campement")
            {
                RemplacerPNJ(scene, "PNJ_Berger", new Vector3(-1f, 0f, 1f), Quaternion.Euler(0, -30, 0));
                Debug.Log("[SceneBuilder] PNJ_Berger ajouté à Campement (pas de ligne audio assignée).");
            }
            else if (scene.name == "Concession")
            {
                RemplacerPNJ(scene, "PNJ_Femme", new Vector3(0.5f, 0f, 3.5f), Quaternion.Euler(0, 160, 0));
                Debug.Log("[SceneBuilder] PNJ_Femme ajouté à Concession (pas de ligne audio assignée).");
            }
            else
            {
                Debug.LogWarning($"[SceneBuilder] Scène active '{scene.name}' non reconnue " +
                    "(attendu \"Campement\" ou \"Concession\") — rien n'a été ajouté.");
                return;
            }

            EditorSceneManager.SaveScene(scene);
        }

        static void RemplacerPNJ(Scene scene, string nom, Vector3 position, Quaternion rotation)
        {
            var existant = TrouverDansScene(scene, nom);
            if (existant != null) Object.DestroyImmediate(existant);
            CreerPNJPlaceholder(nom, position, rotation);
        }

        static GameObject CreerPNJPlaceholder(string nom, Vector3 position, Quaternion rotation)
        {
            var racine = new GameObject(nom);
            racine.transform.position = position;
            racine.transform.rotation = rotation;

            // Corps placeholder : capsule teintée sombre et neutre, sans trait — respecte
            // le principe "silhouettes sans traits individualisés" (note éthique) en
            // attendant un vrai modèle stylisé fait en Blender.
            var corps = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            corps.name = "Corps_Placeholder";
            corps.transform.SetParent(racine.transform);
            corps.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            corps.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f); // ~1,8 m de haut
            Object.DestroyImmediate(corps.GetComponent<Collider>()); // pas d'obstacle physique pour l'instant

            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.15f, 0.12f, 0.1f);
            corps.GetComponent<Renderer>().material = mat;

            // Zone de dialogue : sur la racine (non affectée par le scale du corps).
            var zone = racine.AddComponent<SphereCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 0.9f, 0f);
            zone.radius = 2.5f;

            var audioSource = racine.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 8f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;

            racine.AddComponent<PNJDialogue>(); // lignes vides : à assigner dans l'Inspector

            return racine;
        }

        /// <summary>Récupère un composant s'il existe déjà sur le GameObject, sinon l'ajoute — évite les doublons si on relance le menu (même logique que ObtenirOuAjouterSettings pour le post-processing).</summary>
        static T ObtenirOuAjouter<T>(GameObject go) where T : Component
        {
            var composant = go.GetComponent<T>();
            return composant != null ? composant : go.AddComponent<T>();
        }

        /// <summary>
        /// Attache les scripts d'interaction "active" (touche à presser, par opposition à
        /// PNJDialogue qui est passif) aux objets déjà présents dans la scène ACTUELLEMENT
        /// OUVERTE : boire (Canari), piler (Mortier+Pilon), s'asseoir (Natte), caresser
        /// (Troupeau), et pour la Concession spécifiquement prier (Natte_Priere) + examiner
        /// (Dudal, Grenier — légendes culturelles courtes). Attiser le feu est réservé au
        /// Campement (seule scène avec un FeuDeCamp actif).
        ///
        /// Non destructif : réutilise les composants déjà en place si le menu est relancé,
        /// ne recrée rien qui existe déjà. Ne fait rien pour un objet absent de la scène
        /// (log un avertissement) plutôt que de planter — utile si BuildCampement/
        /// BuildConcession n'a pas encore été relancé avec le Canari.
        /// </summary>
        [MenuItem("Wuro&Galle/Ajouter les interactions (canari, mortier, dudal, troupeau, feu, nattes) à la scène active")]
        public static void AjouterInteractionsSceneActive()
        {
            Scene scene = EditorSceneManager.GetActiveScene();

            var joueur = TrouverDansScene(scene, "Joueur");
            if (joueur != null && !joueur.CompareTag("Player"))
                joueur.tag = "Player";

            if (scene.name == "Campement")
            {
                AjouterActionBoire(scene, "Canari_Foyer");
                AjouterActionPiler(scene, "Mortier_Foyer", "Pilon_Foyer");
                AjouterActionSasseoir(scene, "Natte_Foyer");
                AjouterActionCaresserTroupeau(scene);
                AjouterActionAttiserFeu(scene);
                Debug.Log("[SceneBuilder] Interactions ajoutées à Campement (boire, piler, s'asseoir, caresser, attiser le feu).");
            }
            else if (scene.name == "Concession")
            {
                AjouterActionBoire(scene, "Canari_Entree");
                AjouterActionPiler(scene, "Mortier_Cases", "Pilon_Cases");
                AjouterActionSasseoir(scene, "Natte_Cases");
                AjouterActionPrier(scene, "Natte_Priere");
                AjouterActionCaresserTroupeau(scene);
                AjouterActionExaminer(scene, "Dudal",
                    "Le dudal : espace de prière et de rassemblement masculin, proche de l'entrée — " +
                    "zone publique, lieu de sociabilité et de médiation avec l'extérieur. (Le sens exact " +
                    "du terme fait l'objet d'un écart signalé dans le glossaire du projet.)");
                AjouterActionExaminer(scene, "Grenier",
                    "Le grenier à mil : le stockage des récoltes, en position centrale et surveillée " +
                    "plutôt qu'en périphérie — il concentre une bonne part de la sécurité économique du foyer.");
                Debug.Log("[SceneBuilder] Interactions ajoutées à Concession (boire, piler, s'asseoir, prier, caresser, examiner).");
            }
            else
            {
                Debug.LogWarning($"[SceneBuilder] Scène active '{scene.name}' non reconnue " +
                    "(attendu \"Campement\" ou \"Concession\") — rien n'a été ajouté.");
                return;
            }

            EditorSceneManager.SaveScene(scene);
        }

        static void AjouterActionBoire(Scene scene, string nomCanari)
        {
            var go = TrouverDansScene(scene, nomCanari);
            if (go == null)
            {
                Debug.LogWarning($"[SceneBuilder] '{nomCanari}' introuvable — relance BuildCampement/BuildConcession (avec Canari) avant ce menu.");
                return;
            }
            ObtenirOuAjouter<ActionBoire>(go);
        }

        static void AjouterActionPiler(Scene scene, string nomMortier, string nomPilon)
        {
            var mortierGO = TrouverDansScene(scene, nomMortier);
            var pilonGO = TrouverDansScene(scene, nomPilon);
            if (mortierGO == null || pilonGO == null)
            {
                Debug.LogWarning($"[SceneBuilder] '{nomMortier}' ou '{nomPilon}' introuvable — interaction 'piler' non ajoutée.");
                return;
            }
            var action = ObtenirOuAjouter<ActionPiler>(mortierGO);
            action.pilon = pilonGO.transform;
        }

        static void AjouterActionSasseoir(Scene scene, string nomNatte)
        {
            var go = TrouverDansScene(scene, nomNatte);
            if (go == null)
            {
                Debug.LogWarning($"[SceneBuilder] '{nomNatte}' introuvable — interaction 's'asseoir' non ajoutée.");
                return;
            }
            ObtenirOuAjouter<ActionSasseoir>(go);
        }

        static void AjouterActionPrier(Scene scene, string nomNattePriere)
        {
            var go = TrouverDansScene(scene, nomNattePriere);
            if (go == null)
            {
                Debug.LogWarning($"[SceneBuilder] '{nomNattePriere}' introuvable — interaction 'prier' non ajoutée.");
                return;
            }
            ObtenirOuAjouter<ActionPrier>(go);
        }

        /// <summary>Ajoute l'interaction 'caresser' à CHAQUE tête de bétail sous le parent "Troupeau" (voir CreerTroupeauDansEnclos) — le nombre de têtes n'est pas fixe.</summary>
        static void AjouterActionCaresserTroupeau(Scene scene)
        {
            var parent = TrouverDansScene(scene, "Troupeau");
            if (parent == null)
            {
                Debug.LogWarning("[SceneBuilder] 'Troupeau' introuvable — interaction 'caresser' non ajoutée.");
                return;
            }
            foreach (Transform tete in parent.transform)
                ObtenirOuAjouter<ActionCaresserTroupeau>(tete.gameObject);
        }

        static void AjouterActionExaminer(Scene scene, string nomObjet, string legende)
        {
            var go = TrouverDansScene(scene, nomObjet);
            if (go == null)
            {
                Debug.LogWarning($"[SceneBuilder] '{nomObjet}' introuvable — interaction 'examiner' non ajoutée.");
                return;
            }
            var action = ObtenirOuAjouter<ActionExaminer>(go);
            action.legende = legende;
        }

        static void AjouterActionAttiserFeu(Scene scene)
        {
            var feuGO = TrouverDansScene(scene, "FeuDeCamp");
            if (feuGO == null)
            {
                Debug.LogWarning("[SceneBuilder] 'FeuDeCamp' introuvable — interaction 'attiser le feu' non ajoutée.");
                return;
            }

            var lumiereGO = TrouverEnfant(feuGO.transform, "Lumiere_Feu");
            var flammesGO = TrouverEnfant(feuGO.transform, "Particules_Flammes");
            var braisesGO = TrouverEnfant(feuGO.transform, "Particules_Braises");

            var action = ObtenirOuAjouter<ActionAttiserFeu>(feuGO);
            action.scintillement = lumiereGO != null ? lumiereGO.GetComponent<FeuDeCampScintillement>() : null;

            var particules = new List<ParticleSystem>();
            if (flammesGO != null) particules.Add(flammesGO.GetComponent<ParticleSystem>());
            if (braisesGO != null) particules.Add(braisesGO.GetComponent<ParticleSystem>());
            action.particulesABooster = particules.ToArray();
        }

        /// <summary>
        /// Ajoute à la scène ACTUELLEMENT OUVERTE : montagnes lointaines + brume
        /// atmosphérique (profondeur d'horizon), végétation désertique éparse (rien
        /// n'existait avant côté végétation), et une texture douce sur les particules
        /// du feu (au lieu de quads à bords durs). Ne reconstruit rien d'existant —
        /// supprime seulement les objets du même nom avant de les recréer.
        /// </summary>
        [MenuItem("Wuro&Galle/Ajouter décor (montagnes + végétation) à la scène active")]
        public static void AjouterDecorSceneActive()
        {
            Scene scene = EditorSceneManager.GetActiveScene();

            if (scene.name != "Campement" && scene.name != "Concession")
            {
                Debug.LogWarning($"[SceneBuilder] Scène active '{scene.name}' non reconnue — rien n'a été ajouté.");
                return;
            }

            var ancien = TrouverDansScene(scene, "Montagnes_Lointaines");
            if (ancien != null) Object.DestroyImmediate(ancien);
            CreerMontagnesLointaines();

            var ancienneVege = TrouverDansScene(scene, "Vegetation_Eparse");
            if (ancienneVege != null) Object.DestroyImmediate(ancienneVege);
            // Zone évitée différente selon la scène (là où sont les structures).
            Rect zoneEvitee = scene.name == "Campement"
                ? new Rect(-13f, -4f, 22f, 12f)   // englobe suudu, feu, hoggo, mobilier
                : new Rect(-6f, -4f, 12f, 11f);   // englobe dudal, cases, grenier, mobilier
            CreerVegetationEparse(zoneEvitee);

            var ancienneMeteo = TrouverDansScene(scene, "Meteo");
            if (ancienneMeteo != null) Object.DestroyImmediate(ancienneMeteo);
            CreerMeteo();

            AdoucirTexturesParticulesFeu(scene);

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SceneBuilder] Décor ajouté à la scène {scene.name}.");
        }

        /// <summary>
        /// Habillage "climat" du paysage sahélien : poussière portée par le vent
        /// (cohérente avec Audio_Vent, déjà en boucle sur les deux scènes) et quelques
        /// nuages fins qui dérivent lentement (NuagesDerive.cs) — un ciel figé
        /// détonnait avec l'ambiance sonore de vent. Ni cycle jour/nuit ni pluie :
        /// hors budget de temps pour cette passe, laissé pour une itération suivante.
        /// </summary>
        static void CreerMeteo()
        {
            var parent = new GameObject("Meteo");

            CreerPoussiereVent(parent.transform);
            CreerNuages(parent.transform);
        }

        /// <summary>Fine brume de poussière en suspension, portée par le vent (World space, grande zone, très discrète — climat sahélien, pas une tempête de sable).</summary>
        static void CreerPoussiereVent(Transform parent)
        {
            var go = new GameObject("Vent_Poussiere");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(0f, 0.6f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 18f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startColor = new Color(0.75f, 0.68f, 0.5f, 0.05f); // très discret, s'additionne sur toute la zone
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 150;

            var emission = ps.emission;
            emission.rateOverTime = 8f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(60f, 2f, 60f); // couvre toute la zone jouable + les abords

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0.3f, 0.6f); // même direction générale que Audio_Vent
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, 0.05f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);

            go.GetComponent<ParticleSystemRenderer>().material =
                CreerMateriauParticules("Legacy Shaders/Particles/Alpha Blended");
        }

        /// <summary>Quelques nuages fins (quads texturés, dégradé radial doux) haut dans le ciel, dérivant lentement (NuagesDerive.cs).</summary>
        static void CreerNuages(Transform parent)
        {
            var texture = CreerTextureRadialeDouce(64);
            var mat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
            mat.mainTexture = texture;
            mat.color = new Color(1f, 1f, 1f, 0.5f);

            var rng = new System.Random(21);
            int nombre = 6;
            for (int i = 0; i < nombre; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"Nuage_{i}";
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.SetParent(parent);

                float x = (float)(rng.NextDouble() * 100 - 50);
                float z = (float)(rng.NextDouble() * 100 - 50);
                go.transform.position = new Vector3(x, 38f + (float)rng.NextDouble() * 6f, z);
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // face vers le sol
                float taille = 12f + (float)rng.NextDouble() * 10f;
                go.transform.localScale = new Vector3(taille * (1.4f + (float)rng.NextDouble() * 0.6f), taille, 1f);

                go.GetComponent<Renderer>().sharedMaterial = mat;

                var derive = go.AddComponent<NuagesDerive>();
                derive.vitesse = 0.3f + (float)rng.NextDouble() * 0.3f;
                derive.limite = 70f;
            }
        }

        /// <summary>
        /// Retour en arrière : supprime le Terrain_Environnement de la scène active et
        /// restaure le matériau d'origine ("Sol_Laterite", importé depuis Paysage.glb) sur
        /// le sol existant. Ne recrée PAS les anciens cônes de décor (jugés "basiques",
        /// abandon assumé — priorité donnée à l'amélioration des habitations à la place).
        /// </summary>
        [MenuItem("Wuro&Galle/Revenir en arrière : supprimer le Terrain de la scène active")]
        public static void SupprimerTerrainSceneActive()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Campement" && scene.name != "Concession")
            {
                Debug.LogWarning($"[SceneBuilder] Scène active '{scene.name}' non reconnue — rien à supprimer.");
                return;
            }

            var terrainObj = TrouverDansScene(scene, "Terrain_Environnement");
            if (terrainObj != null) Object.DestroyImmediate(terrainObj);

            GameObject paysageGO = TrouverDansScene(scene, "Paysage");
            if (paysageGO != null)
            {
                Transform solExistant = TrouverEnfant(paysageGO.transform, "Terrain");
                if (solExistant != null)
                {
                    var renderer = solExistant.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        var materiauOriginal = TrouverMateriauParNomDansAsset(PathPaysage, "Sol_Laterite");
                        if (materiauOriginal != null)
                            renderer.sharedMaterial = materiauOriginal;
                        else
                            Debug.LogWarning("[SceneBuilder] Matériau d'origine \"Sol_Laterite\" introuvable dans Paysage.glb.");
                    }
                }
            }

            RenderSettings.fog = false; // brouillard ajouté spécifiquement pour le Terrain

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SceneBuilder] Terrain supprimé de la scène {scene.name}, sol d'origine restauré.");
        }

        /// <summary>Cherche un Material par nom parmi les sous-assets d'un asset importé (ex. un .glb).</summary>
        static Material TrouverMateriauParNomDansAsset(string cheminAsset, string nomMateriau)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(cheminAsset))
            {
                if (asset is Material mat && mat.name == nomMateriau) return mat;
            }
            return null;
        }

        /// <summary>
        /// Remplace le décor "faux" (cônes Montagnes_Lointaines + Buisson_Eparse) par un
        /// vrai Unity Terrain : relief sculpté (dunes discrètes + collines à l'horizon au
        /// lieu de cônes identiques répétés) et deux textures procédurales mélangées
        /// (sable / terre sèche clairsemée), sans dépendance à un asset externe téléchargé
        /// (pas de question de licence). Non destructif envers les cases/galle/mobilier
        /// déjà en place : ne touche que les objets Montagnes_Lointaines, Vegetation_Eparse
        /// et Terrain_Environnement (supprimés puis recréés à chaque appel).
        /// Nécessite les modules Unity "com.unity.modules.terrain" / "terrainphysics"
        /// (déjà présents dans Packages/manifest.json).
        /// </summary>
        [MenuItem("Wuro&Galle/Ajouter un Terrain (sol + relief) à la scène active")]
        public static void AjouterTerrainSceneActive()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Campement" && scene.name != "Concession")
            {
                Debug.LogWarning($"[SceneBuilder] Scène active '{scene.name}' non reconnue — rien n'a été ajouté.");
                return;
            }

            // Zone évitée = tout l'espace vie habitable (reste plate + sable piétiné dedans).
            // Concession réutilise le même Paysage.glb que Campement (même Instancier(PathPaysage,
            // Vector3.zero, ...) dans les deux) donc l'enclos/la mare sont aux MÊMES coordonnées
            // locales dans les deux scènes (~x -9 à -12 d'après le placement du troupeau dans
            // BuildCampement) — l'ancien rect de Concession (-6..6) les laissait dehors, d'où le
            // sol qui restait sauvage/vallonné sous l'enclos. Élargi pour tout couvrir.
            Rect zoneEvitee = scene.name == "Campement"
                ? new Rect(-13f, -4f, 22f, 12f)
                : new Rect(-14f, -5f, 20f, 13f);

            foreach (string nomAncien in new[] { "Montagnes_Lointaines", "Vegetation_Eparse", "Terrain_Environnement", "Meteo" })
            {
                var ancien = TrouverDansScene(scene, nomAncien);
                if (ancien != null) Object.DestroyImmediate(ancien);
            }

            const int resolution = 129;   // léger, largement suffisant pour un relief discret
            const float taille = 200f;    // couvre largement au-delà de l'ancien anneau de cônes (45-65 m)
            const float hauteurMax = 12f;

            var data = new TerrainData();
            data.heightmapResolution = resolution;
            data.alphamapResolution = 256; // valeur par défaut trop basse (32) => mélange de textures trop pixelisé
            data.size = new Vector3(taille, hauteurMax, taille);
            data.SetHeights(0, 0, GenererHauteursTerrain(resolution, taille, zoneEvitee));

            // Deux tons de SABLE (même famille de couleur, chaude, cohérente avec Soleil_Zenith
            // et le color grading réchauffé) — pas de terre/végétation : le sol doit rester
            // uniforme, désertique. Le ton plus sombre représente juste le sable piétiné/tassé
            // près du campement (beaucoup de passage), pas un matériau différent.
            var coucheSable = CreerOuChargerCoucheTerrain("Sol_Sable", new Color(0.80f, 0.70f, 0.50f), new Color(0.86f, 0.77f, 0.59f));
            var coucheSablePietine = CreerOuChargerCoucheTerrain("Sol_Sable_Pietine", new Color(0.66f, 0.58f, 0.42f), new Color(0.72f, 0.64f, 0.47f));
            data.terrainLayers = new[] { coucheSable, coucheSablePietine };
            data.SetAlphamaps(0, 0, GenererAlphamapTerrain(data.alphamapWidth, data.alphamapHeight, taille, zoneEvitee));

            if (!AssetDatabase.IsValidFolder("Assets/Terrains"))
                AssetDatabase.CreateFolder("Assets", "Terrains");
            string cheminData = $"Assets/Terrains/TerrainData_{scene.name}.asset";
            AssetDatabase.DeleteAsset(cheminData); // repart propre si on relance le menu
            AssetDatabase.CreateAsset(data, cheminData);

            GameObject terrainObj = Terrain.CreateTerrainGameObject(data);
            terrainObj.name = "Terrain_Environnement";
            // Centré sur l'origine (comme le reste de la scène), légèrement sous 0 pour
            // éviter le z-fighting avec le sol existant du Paysage.glb à l'intérieur de la zone évitée.
            terrainObj.transform.position = new Vector3(-taille / 2f, -0.1f, -taille / 2f);

            RetexturerSolHabite(scene);

            // Brouillard : reprend les réglages qu'avait CreerMontagnesLointaines (les
            // collines du Terrain jouent maintenant ce rôle d'horizon qui se fond dans le ciel).
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.75f, 0.78f, 0.82f);
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 90f;

            // Végétation éparse : orpheline depuis le passage au Terrain (supprimée
            // ci-dessus avec Montagnes_Lointaines/Vegetation_Eparse mais jamais
            // recréée jusqu'ici — écart corrigé ici) + habillage climat/météo
            // (poussière portée par le vent, nuages qui dérivent — voir CreerMeteo).
            // CreerVegetationEparse crée elle-même son parent "Vegetation_Eparse".
            CreerVegetationEparse(zoneEvitee);
            CreerMeteo();

            AdoucirTexturesParticulesFeu(scene);

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SceneBuilder] Terrain ajouté à la scène {scene.name} (remplace les anciens cônes de décor).");
        }

        /// <summary>
        /// Retexture le sol existant du Paysage.glb (objet enfant "Terrain", mesh plat qui
        /// couvre en réalité TOUTE la zone habitée — cases, enclos, mare, dudal, grenier,
        /// vérifié via l'Inspector) avec la même texture procédurale de sable que le nouveau
        /// Terrain, au lieu de le masquer : ce mesh est le bon support pour toute la zone
        /// plate, il suffisait de changer son matériau (auparavant "Sol_Laterite", un
        /// matériau glTF importé sans rapport avec le sable désertique voulu).
        /// </summary>
        static void RetexturerSolHabite(Scene scene)
        {
            GameObject paysageGO = TrouverDansScene(scene, "Paysage");
            if (paysageGO == null)
            {
                Debug.LogWarning("[SceneBuilder] \"Paysage\" introuvable — ancien sol non retexturé.");
                return;
            }

            Transform solExistant = TrouverEnfant(paysageGO.transform, "Terrain");
            if (solExistant == null)
            {
                Debug.LogWarning("[SceneBuilder] Enfant \"Terrain\" introuvable sous Paysage — ancien sol non retexturé.");
                return;
            }

            var renderer = solExistant.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                Debug.LogWarning("[SceneBuilder] \"Terrain\" (sous Paysage) n'a pas de MeshRenderer — rien à retexturer.");
                return;
            }

            // Couleur unie plutôt qu'une texture : ce mesh n'a probablement pas de vraies
            // UV (jamais nécessaire avec l'ancien matériau plat "Sol_Laterite"), donc une
            // texture s'y afficherait comme un seul aplat de toute façon — autant assumer
            // une couleur unie directement (indépendante des UV, garantie uniforme).
            var matSol = new Material(Shader.Find("Standard"));
            matSol.color = new Color(0.80f, 0.70f, 0.50f); // même ton que Sol_Sable (Terrain)
            matSol.SetFloat("_Glossiness", 0.1f);
            renderer.sharedMaterial = matSol;

            // Log explicite et bruyant : si tu ne vois PAS cette ligne après avoir relancé
            // le menu, il n'a pas tourné sur la bonne scène — vérifie ça avant de chercher ailleurs.
            Debug.Log($"[SceneBuilder] ANCIEN SOL RETEXTURÉ avec succès (scène '{scene.name}', matériau = {renderer.sharedMaterial.name}).");
        }

        /// <summary>Distance (monde) entre un point et le rectangle le plus proche ; 0 si le point est dedans.</summary>
        static float DistanceHorsRect(float x, float z, Rect rect)
        {
            float dx = Mathf.Max(rect.xMin - x, 0f, x - rect.xMax);
            float dz = Mathf.Max(rect.yMin - z, 0f, z - rect.yMax);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// Hauteurs normalisées (0..1, relatives à data.size.y) : plat dans la zone évitée
        /// (ne déforme pas le sol sous les structures), puis relief qui monte en continu
        /// (courbe quadratique, jamais de plateau parfaitement plat) jusqu'au bord réel du
        /// Terrain, avec du bruit partout (même sur les hauteurs) pour éviter toute surface
        /// plane qui accrocherait la lumière rasante du soleil en une ligne dure à l'horizon
        /// (bug observé : un plateau plat trop tôt créait exactement cette ligne).
        /// </summary>
        static float[,] GenererHauteursTerrain(int resolution, float taille, Rect zoneEvitee)
        {
            var hauteurs = new float[resolution, resolution];
            const float marge = 3f;              // transition douce à la sortie de la zone évitée
            float porteeTotale = taille * 0.42f;  // la montée s'étale jusque tout près du bord réel du Terrain

            for (int iz = 0; iz < resolution; iz++)
            {
                for (int ix = 0; ix < resolution; ix++)
                {
                    float worldX = (ix / (float)(resolution - 1)) * taille - taille / 2f;
                    float worldZ = (iz / (float)(resolution - 1)) * taille - taille / 2f;

                    if (zoneEvitee.Contains(new Vector2(worldX, worldZ)))
                    {
                        hauteurs[iz, ix] = 0f;
                        continue;
                    }

                    float distance = DistanceHorsRect(worldX, worldZ, zoneEvitee);
                    float t = Mathf.Clamp01((distance - marge) / porteeTotale);
                    float montee = t * t; // montée continue jusqu'au bord, pas de palier plat

                    float bruitGrand = Mathf.PerlinNoise((worldX + 1000f) * 0.03f, (worldZ + 1000f) * 0.03f);
                    float bruitFin = Mathf.PerlinNoise((worldX + 1000f) * 0.15f, (worldZ + 1000f) * 0.15f);
                    float bruit = bruitGrand * 0.7f + bruitFin * 0.3f;

                    // Le bruit s'applique partout (pas seulement en altitude) pour qu'aucune
                    // zone ne soit parfaitement plane, mais reste discret près du campement.
                    hauteurs[iz, ix] = Mathf.Clamp01(montee * 0.75f + bruit * 0.2f * (0.3f + montee));
                }
            }
            return hauteurs;
        }

        /// <summary>
        /// Sable piétiné concentré autour de la zone habitée (rayon avec bord organique,
        /// pas un cercle parfait) puis s'estompe vers du sable uniforme au loin — reproduit
        /// le sol tassé par le passage répété plutôt qu'un patchwork de textures différentes.
        /// Poids plafonné (jamais 100%) pour que le sol reste visuellement uniforme partout.
        /// </summary>
        static float[,,] GenererAlphamapTerrain(int largeur, int hauteur, float taille, Rect zoneEvitee)
        {
            var carte = new float[hauteur, largeur, 2];
            const float porteeChemin = 22f; // rayon (m) sur lequel le sol piétiné s'estompe
            const float poidsMax = 0.55f;   // jamais 100% : garde une base sable uniforme partout

            for (int iz = 0; iz < hauteur; iz++)
            {
                for (int ix = 0; ix < largeur; ix++)
                {
                    float worldX = (ix / (float)(largeur - 1)) * taille - taille / 2f;
                    float worldZ = (iz / (float)(hauteur - 1)) * taille - taille / 2f;

                    float distance = DistanceHorsRect(worldX, worldZ, zoneEvitee);
                    // Bord organique (pas un cercle parfait) via un léger bruit sur la distance.
                    float jitter = (Mathf.PerlinNoise((worldX + 300f) * 0.05f, (worldZ + 300f) * 0.05f) - 0.5f) * 8f;
                    float poidsPietine = Mathf.Clamp01(1f - (distance + jitter) / porteeChemin) * poidsMax;

                    carte[iz, ix, 1] = poidsPietine;
                    carte[iz, ix, 0] = 1f - poidsPietine;
                }
            }
            return carte;
        }

        /// <summary>
        /// Texture procédurale "sable balayé par le vent" — bruit étiré (fréquences très
        /// différentes en X/Y) pour des stries allongées façon sable balayé plutôt que des
        /// taches rondes, contraste volontairement faible pour rester visuellement uniforme.
        /// Évite de dépendre d'une texture externe téléchargée (question de licence/source,
        /// voir note-ethique.md) alors qu'on n'a pas encore de vraie texture PBR sable.
        /// À remplacer plus tard par une vraie texture si le temps le permet.
        /// </summary>
        static Texture2D CreerTextureProceduraleSol(int taille, Color baseColor, Color variationColor)
        {
            var tex = new Texture2D(taille, taille, TextureFormat.RGBA32, false);
            for (int y = 0; y < taille; y++)
            {
                for (int x = 0; x < taille; x++)
                {
                    float stries = Mathf.PerlinNoise(x * 0.015f, y * 0.2f);   // grandes stries allongées (vent)
                    float grain = Mathf.PerlinNoise(x * 0.6f, y * 3f);        // grain fin superposé
                    float n = Mathf.Clamp01(stries * 0.75f + grain * 0.25f);
                    // Contraste réduit (n * 0.4) : variations subtiles, sol globalement uniforme.
                    tex.SetPixel(x, y, Color.Lerp(baseColor, variationColor, n * 0.4f));
                }
            }
            tex.Apply();
            return tex;
        }

        /// <summary>Crée (ou réutilise si déjà présent) un TerrainLayer avec sa texture procédurale associée.</summary>
        static TerrainLayer CreerOuChargerCoucheTerrain(string nomCouche, Color baseColor, Color variationColor)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Terrains"))
                AssetDatabase.CreateFolder("Assets", "Terrains");
            if (!AssetDatabase.IsValidFolder("Assets/Terrains/Textures"))
                AssetDatabase.CreateFolder("Assets/Terrains", "Textures");

            string cheminTexture = $"Assets/Terrains/Textures/{nomCouche}.png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(cheminTexture) == null)
            {
                Texture2D tex = CreerTextureProceduraleSol(256, baseColor, variationColor);
                System.IO.File.WriteAllBytes(cheminTexture, tex.EncodeToPNG());
                AssetDatabase.ImportAsset(cheminTexture);
            }
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(cheminTexture);

            string cheminLayer = $"Assets/Terrains/{nomCouche}.terrainlayer";
            TerrainLayer couche = AssetDatabase.LoadAssetAtPath<TerrainLayer>(cheminLayer);
            if (couche == null)
            {
                couche = new TerrainLayer { diffuseTexture = texture, tileSize = new Vector2(8f, 8f) };
                AssetDatabase.CreateAsset(couche, cheminLayer);
            }
            else
            {
                couche.diffuseTexture = texture;
            }
            return couche;
        }

        /// <summary>Génère un mesh de cône simple (Unity n'a pas de primitive Cone native).</summary>
        static Mesh CreerMeshCone(float rayon, float hauteur, int segments)
        {
            var vertices = new List<Vector3> { new Vector3(0, hauteur, 0) }; // apex, index 0
            for (int i = 0; i < segments; i++)
            {
                float angle = 2 * Mathf.PI * i / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * rayon, 0, Mathf.Sin(angle) * rayon));
            }
            int centreBase = vertices.Count;
            vertices.Add(new Vector3(0, 0, 0));

            var triangles = new List<int>();
            for (int i = 0; i < segments; i++)
            {
                int courant = i + 1;
                int suivant = (i + 1) % segments + 1;
                triangles.Add(0); triangles.Add(suivant); triangles.Add(courant);           // face latérale
                triangles.Add(centreBase); triangles.Add(courant); triangles.Add(suivant);   // base
            }

            var mesh = new Mesh { name = "ConeGenere" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Pics rocheux lointains (mesh cône généré, pas de modèle Blender) disposés en
        /// cercle autour de la zone jouable, + brouillard linéaire pour les fondre dans
        /// le ciel (profondeur d'horizon, inspiré de la référence Unreal envoyée en chat —
        /// technique équivalente, pas le même moteur).
        /// </summary>
        static void CreerMontagnesLointaines()
        {
            var parent = new GameObject("Montagnes_Lointaines");
            var rng = new System.Random(42); // seed fixe : reproductible d'une régénération à l'autre

            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.42f, 0.36f, 0.34f);
            mat.SetFloat("_Glossiness", 0.1f);

            int nombre = 10;
            for (int i = 0; i < nombre; i++)
            {
                float angle = (360f / nombre) * i + (float)(rng.NextDouble() * 20 - 10);
                float rayonDist = 45f + (float)rng.NextDouble() * 20f;
                float x = rayonDist * Mathf.Cos(angle * Mathf.Deg2Rad);
                float z = rayonDist * Mathf.Sin(angle * Mathf.Deg2Rad);
                float hauteur = 22f + (float)rng.NextDouble() * 18f;
                float rayonBase = 10f + (float)rng.NextDouble() * 10f;

                var pic = new GameObject($"Pic_{i}");
                pic.transform.SetParent(parent.transform);
                pic.transform.position = new Vector3(x, 0f, z);
                var mf = pic.AddComponent<MeshFilter>();
                mf.sharedMesh = CreerMeshCone(rayonBase, hauteur, 6 + (i % 3)); // silhouette irrégulière, pas un cône parfait
                var mr = pic.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
            }

            // Brouillard : commence après la zone jouable (~25 m), les pics (45-65 m) se
            // fondent progressivement dans le ciel plutôt que de finir en silhouette dure.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.75f, 0.78f, 0.82f);
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 90f;
        }

        /// <summary>Un arbre déjà placé (pour vérifier l'espacement avec le suivant) — voir CreerVegetationEparse.</summary>
        private readonly struct ArbrePlace
        {
            public readonly Vector2 Position;
            public readonly float RayonHouppier;
            public ArbrePlace(Vector2 position, float rayonHouppier) { Position = position; RayonHouppier = rayonHouppier; }
        }

        /// <summary>
        /// Acacias épars (silhouette réelle si le module Blender est importé, sinon
        /// cônes procéduraux de secours) sur le terrain, en évitant la zone occupée
        /// par les structures (rect en coordonnées X/Z monde).
        ///
        /// CORRIGÉ (bug constaté sur capture d'écran Play mode) : le houppier de
        /// l'acacia parasol mesure réellement ~9,5 m de diamètre à l'échelle 1 (vérifié
        /// via les bounds des instances générées) — pas un petit buisson. La première
        /// version ne testait que le POINT d'ancrage contre zoneEvitee (pas le rayon du
        /// houppier) et plaçait 45 arbres sur une zone à peine plus grande que la zone
        /// évitée elle-même, sans espacement minimum entre eux : résultat, un mur
        /// d'arbres continu qui recouvrait le foyer et les suudu/cases. Corrigé en
        /// tenant compte du rayon réel du houppier dans la marge ET dans l'espacement
        /// entre arbres, en élargissant la zone de tirage, et en réduisant la densité
        /// à un niveau réellement "épars".
        /// </summary>
        static void CreerVegetationEparse(Rect zoneEvitee)
        {
            var parent = new GameObject("Vegetation_Eparse");
            var rng = new System.Random(7);

            // Acacia parasol modélisé en Blender (blender/paysage/vegetation_sahelienne.py),
            // silhouette réelle de la savane soudano-sahélienne — utilisé en priorité s'il a
            // été exporté et importé. Sinon, retombe sur les cônes procéduraux (mieux qu'une
            // zone vide en attendant l'export, mais pas la version définitive).
            var assetAcacia = AssetDatabase.LoadAssetAtPath<GameObject>(PathAcacia);
            Material matBuissonSecours = null;
            if (assetAcacia == null)
            {
                matBuissonSecours = new Material(Shader.Find("Standard"));
                matBuissonSecours.color = new Color(0.16f, 0.20f, 0.11f);
                matBuissonSecours.SetFloat("_Glossiness", 0.05f);
                Debug.LogWarning($"[SceneBuilder] {PathAcacia} introuvable — végétation en cônes procéduraux (secours). " +
                    "Exporte vegetation_sahelienne.py en GLB, importe-le, puis relance pour la vraie silhouette d'acacia.");
            }

            const float rayonHouppierBase = 4.8f; // à l'échelle 1 (mesuré ~4.75 m sur les instances générées)
            const float margeAuDelaDuHouppier = 1.5f; // au-delà du houppier lui-même, pour ne pas juste effleurer la zone évitée
            const float espacementSupplementaire = 1f; // entre deux houppiers voisins

            int nombre = 14; // vraiment "épars" — la densité précédente (45) noyait tout sous les houppiers
            int tentatives = 0;
            var placees = new List<ArbrePlace>();

            while (placees.Count < nombre && tentatives < nombre * 40)
            {
                tentatives++;
                float x = (float)(rng.NextDouble() * 80 - 40); // zone de tirage bien plus large que la zone évitée
                float z = (float)(rng.NextDouble() * 76 - 38);
                float echelle = 0.8f + (float)rng.NextDouble() * 0.5f;
                float rayonHouppier = rayonHouppierBase * echelle;

                if (DistanceHorsRect(x, z, zoneEvitee) < rayonHouppier + margeAuDelaDuHouppier) continue;

                bool tropProche = false;
                foreach (var autre in placees)
                {
                    if (Vector2.Distance(autre.Position, new Vector2(x, z)) < rayonHouppier + autre.RayonHouppier + espacementSupplementaire)
                    { tropProche = true; break; }
                }
                if (tropProche) continue;

                placees.Add(new ArbrePlace(new Vector2(x, z), rayonHouppier));
                int index = placees.Count - 1;

                if (assetAcacia != null)
                {
                    var acacia = (GameObject)PrefabUtility.InstantiatePrefab(assetAcacia, parent.transform);
                    acacia.name = $"Acacia_{index}";
                    acacia.transform.position = new Vector3(x, 0f, z);
                    acacia.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                    // Un seul module réutilisé : la variation de taille vient de l'échelle,
                    // pas d'une nouvelle géométrie (aucun coût triangle supplémentaire).
                    acacia.transform.localScale = Vector3.one * echelle;
                }
                else
                {
                    var buisson = new GameObject($"Buisson_{index}");
                    buisson.transform.SetParent(parent.transform);
                    buisson.transform.position = new Vector3(x, 0f, z);
                    buisson.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);

                    var mf = buisson.AddComponent<MeshFilter>();
                    mf.sharedMesh = CreerMeshCone(
                        0.2f + (float)rng.NextDouble() * 0.2f,
                        0.35f + (float)rng.NextDouble() * 0.3f,
                        5);
                    var mr = buisson.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = matBuissonSecours;
                }
            }

            if (placees.Count < nombre)
                Debug.LogWarning($"[SceneBuilder] Végétation : seulement {placees.Count}/{nombre} arbres placés sans chevauchement.");
        }

        /// <summary>
        /// Disperse plusieurs têtes de bétail (mélange robe blanche/rousse, réutilisant
        /// les deux modules existants — pas de nouvelle géométrie) à l'intérieur du
        /// hoggo, avec un espacement minimum pour éviter les chevauchements visibles.
        /// Seed fixe (reproductible d'une régénération à l'autre, même logique que
        /// CreerMontagnesLointaines/CreerVegetationEparse).
        /// </summary>
        static void CreerTroupeauDansEnclos(Vector3 centreEnclos, float rayonEnclos, int nombre)
        {
            var parent = new GameObject("Troupeau");
            var rng = new System.Random(11);
            const float distanceMin = 1.1f;
            var positions = new List<Vector2>();

            int placees = 0;
            int tentatives = 0;
            while (placees < nombre && tentatives < nombre * 20)
            {
                tentatives++;
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
                float rayon = Mathf.Sqrt((float)rng.NextDouble()) * rayonEnclos; // distribution uniforme dans le disque
                var candidate = new Vector2(Mathf.Cos(angle) * rayon, Mathf.Sin(angle) * rayon);

                bool tropProche = false;
                foreach (var p in positions)
                {
                    if (Vector2.Distance(p, candidate) < distanceMin) { tropProche = true; break; }
                }
                if (tropProche) continue;

                positions.Add(candidate);
                Vector3 position = centreEnclos + new Vector3(candidate.x, 0f, candidate.y);
                Quaternion rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
                string cheminModele = rng.Next(2) == 0 ? PathTroupeauBlanc : PathTroupeauRoux;
                string nom = $"Troupeau_{(cheminModele == PathTroupeauBlanc ? "blanc" : "roux")}_{placees}";

                var instance = Instancier(cheminModele, position, rotation, nom);
                instance.transform.SetParent(parent.transform);
                placees++;
            }

            if (placees < nombre)
                Debug.LogWarning($"[SceneBuilder] Troupeau : seulement {placees}/{nombre} têtes placées sans chevauchement (enclos trop petit pour la densité demandée).");
        }

        /// <summary>
        /// Remplace le rendu à bords durs des particules du feu (aucune texture assignée
        /// jusqu'ici) par une texture radiale douce générée en code, appliquée aux
        /// matériaux des flammes et des braises (la fumée reste diffuse, déjà correcte).
        /// </summary>
        static void AdoucirTexturesParticulesFeu(Scene scene)
        {
            var texture = CreerTextureRadialeDouce(64);

            foreach (var nomObjet in new[] { "Particules_Flammes", "Particules_Braises" })
            {
                var obj = TrouverDansScene(scene, nomObjet);
                if (obj == null) continue; // pas de FeuDeCamp dans cette scène (ex. Concession)

                var renderer = obj.GetComponent<ParticleSystemRenderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                    renderer.sharedMaterial.mainTexture = texture;
            }
        }

        /// <summary>Génère une texture en dégradé radial doux (blanc opaque au centre, transparent au bord).</summary>
        static Texture2D CreerTextureRadialeDouce(int taille)
        {
            var tex = new Texture2D(taille, taille, TextureFormat.RGBA32, false);
            Vector2 centre = new Vector2(taille / 2f, taille / 2f);
            float rayonMax = taille / 2f;

            for (int y = 0; y < taille; y++)
            {
                for (int x = 0; x < taille; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), centre) / rayonMax;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2f); // dégradé adouci
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        [MenuItem("Wuro&Galle/Construire scène Campement")]
        public static void BuildCampement()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Terrain de base
            AjouterColliderSol(Instancier(PathPaysage, Vector3.zero, Quaternion.identity, "Paysage"));

            // Deux suudu (rayon réel ~1.3 m) délimitant un espace central commun,
            // avec le foyer équidistant entre les deux — logique documentée dans
            // docs/Livrable_1/schema-annote-suudu-galle.md ("Wuro"). Rotation :
            // sans effet visuel, l'armature est symétrique (6 arches régulières en
            // cercle, voir blender/suudu/README.md) — identity partout.
            Instancier(PathSuudu, new Vector3(-2.5f, 0f, 0f), Quaternion.identity, "Suudu_1");
            Instancier(PathSuudu, new Vector3(2.5f, 0f, 0f), Quaternion.identity, "Suudu_2");

            // Le hoggo (enclos, 14 piquets + 2 anneaux) et la mare sont déjà inclus dans
            // Paysage.glb (bakés par enclos_paysage.py) : centrés à ~(-10.5, 0, 0), rayon
            // ~4 m. Pas d'Instancier(PathEnclos, ...) ici — ça créerait un second enclos
            // en doublon, ailleurs, comme observé précédemment. Le troupeau (plusieurs
            // têtes, pas juste une bête de chaque robe : un troupeau se doit d'en avoir
            // plusieurs) est placé à l'intérieur du vrai enclos, pas à côté d'un doublon.
            CreerTroupeauDansEnclos(new Vector3(-10.5f, 0f, 0f), 3.3f, nombre: 7);

            // Calebasse près de l'enclos : évoque la traite du matin (zone de traite
            // documentée comme proche du hoggo, pas modélisée comme espace à part).
            Instancier(PathCalebasse, new Vector3(-10.5f, 0f, 3f), Quaternion.identity, "Calebasse_Traite");

            // Mobilier domestique dans l'espace central commun, entre le foyer et le
            // joueur : vie quotidienne autour du feu (cuisine, couchage) plutôt qu'un
            // centre vide.
            Instancier(PathNatte, new Vector3(0.8f, 0f, 1.3f), Quaternion.Euler(0, 15, 0), "Natte_Foyer");
            Instancier(PathMortier, new Vector3(-0.8f, 0f, 1.3f), Quaternion.identity, "Mortier_Foyer");
            Instancier(PathPilon, new Vector3(-0.8f, 0f, 1.55f), Quaternion.identity, "Pilon_Foyer");
            Instancier(PathCalebasse, new Vector3(1.1f, 0f, 1f), Quaternion.identity, "Calebasse_Foyer");

            // Canari à l'ombre de la Suudu_1 (côté opposé au Mortier/Pilon pour ne
            // rien chevaucher) : porte l'interaction "boire" (voir AjouterInteractionsSceneActive).
            // CORRIGÉ : la position d'origine (-1.7,0.7) était à ~1,06 m du centre de
            // Suudu_1, alors que la tente mesure réellement ~1,32 m de rayon (bounds
            // mesurés) — le canari se retrouvait donc clippé sous la toile, pas "à
            // l'ombre" à côté. Repoussé côté opposé au Mortier/Pilon (toujours hors de
            // l'espace central du foyer), à ~1,56 m du centre de la tente cette fois —
            // hors du dôme, avec une marge de sécurité.
            Instancier(PathCanari, new Vector3(-1.4f, 0f, -1.1f), Quaternion.identity, "Canari_Foyer");

            // Feu de camp central, équidistant des deux suudu : Particle System classique
            // (flammes, fumée, braises) + lumière ponctuelle scintillante — choix fait
            // pour rester en Built-in Render Pipeline sans migrer vers URP/HDRP (requis
            // par VFX Graph).
            CreerFeuDeCamp(new Vector3(0f, 0f, 0f));

            // Audio spatialisé : feu localisé (petite portée), troupeau/clochettes près
            // du hoggo (baké dans Paysage, ~(-10.5,0,0)), vent en ambiance diffuse
            // (spatialBlend faible : audible partout, pas localisé à un point).
            CreerSourceAudio("Audio_Feu", AudioFeu, new Vector3(0f, 0.3f, 0f), 0.7f, 6f);
            CreerSourceAudio("Audio_Clochettes", AudioCowBells, new Vector3(-10.5f, 0.5f, 0f), 0.6f, 12f);
            CreerSourceAudio("Audio_Paturage", AudioGrazingCows, new Vector3(-10.5f, 0.5f, 0f), 0.5f, 12f);
            CreerSourceAudio("Audio_Vent", AudioWind, new Vector3(0f, 2f, 4f), 0.35f, 30f, spatialBlend: 0.2f);

            CreerLumiereDirectionnelle("Soleil_Zenith", new Color(1f, 0.98f, 0.9f));

            // Joueur : en face du foyer, entre les deux suudu (contrôleur desktop
            // provisoire — remplacé par un rig XR à l'étape suivante).
            CreerJoueur(new Vector3(0f, 0f, 4f), Quaternion.Euler(0, 180, 0));

            SauvegarderScene(scene, "Campement");
        }

        [MenuItem("Wuro&Galle/Construire scène Concession")]
        public static void BuildConcession()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            AjouterColliderSol(Instancier(PathPaysage, Vector3.zero, Quaternion.identity, "Paysage"));

            // Gradient entrée (public) -> intérieur (privé), logique documentée dans
            // docs/Livrable_1/schema-annote-suudu-galle.md ("Galle") : le joueur entre
            // par Z négatif, le dudal est proche de l'entrée, les cases plus profondes.

            // Dudal : espace de prière/rassemblement, zone publique proche de l'entrée —
            // mais décalé sur le côté (x=5.5, à l'opposé de l'enclos) plutôt que planté
            // au centre de l'axe d'entrée. Position d'origine (0,0,-2) mettait le dudal
            // droit dans l'axe de vue du joueur au spawn (0,0,-4), à touche-touche du
            // Canari_Entree/Case_Hote : peu compatible avec un moment de recueillement.
            // Reste dans la même zone publique proche de l'entrée (z<0, cohérent avec
            // schema-annote-suudu-galle.md) mais dans son propre coin, à l'écart du
            // passage direct et loin de l'enclos (x=-10.5, voir CreerTroupeauDansEnclos
            // ci-dessous — un dudal collé au bétail casserait l'intimité recherchée).
            Instancier(PathDudal, new Vector3(5.5f, 0f, -2.5f), Quaternion.identity, "Dudal");

            // Natte de prière posée sur le dudal — annoncé dans dudal_grenier.py comme
            // à faire "au moment de l'assemblage Unity", fait ici.
            Instancier(PathNatte, new Vector3(5.5f, 0.02f, -2.5f), Quaternion.Euler(0, 10, 0), "Natte_Priere");

            // Case d'hôte (suudu hoɓɓe) : documentée dans schema-annote-suudu-galle.md
            // ("proche de l'entrée, l'hôte est reçu sans accéder à l'espace familial
            // privé") mais absente des versions précédentes de cette méthode — écart
            // corrigé ici. Réutilise le module de case existant (galle_1.glb) : "suudu"
            // désigne ici une case en paille générique (voir glossaire), pas la tente à
            // armature du campement nomade — la case d'hôte doit donc ressembler aux
            // autres cases de la concession, pas au Wuro. Position décalée du dudal ET
            // des cases privées, sur le côté de l'axe d'entrée.
            Instancier(PathGalle1, new Vector3(3f, 0f, -1f), Quaternion.Euler(0, -160, 0), "Case_Hote");

            // Canari près de l'entrée, à l'écart du dudal et de la case d'hôte : geste
            // d'hospitalité (offrir de l'eau à l'arrivant) plutôt qu'un simple point d'eau
            // domestique — porte l'interaction "boire" (voir AjouterInteractionsSceneActive).
            Instancier(PathCanari, new Vector3(-2.2f, 0f, -3f), Quaternion.identity, "Canari_Entree");

            // Cases (rayons réels 1.6-1.9 m, écartées de 8 m en X : aucun risque de
            // chevauchement), en zone privée, plus profondément dans la concession.
            // Porte du modèle : côté +Y en Blender = -Z une fois exporté en glTF/Unity
            // (blender/galle/README.md). Rotation calculée pour que chaque porte
            // regarde vers l'espace commun/l'entrée, pas au hasard :
            //   Galle_1 (-4,2) -> porte vers +X (le centre)      => -90°
            //   Galle_2 (4,2)  -> porte vers -X (le centre)      => +90°
            //   Galle_3 (0,5)  -> porte vers -Z (l'entrée/dudal) => 0° (déjà la direction par défaut)
            Instancier(PathGalle1, new Vector3(-4f, 0f, 2f), Quaternion.Euler(0, -90, 0), "Galle_1");
            Instancier(PathGalle2, new Vector3(4f, 0f, 2f), Quaternion.Euler(0, 90, 0), "Galle_2");
            Instancier(PathGalle3, new Vector3(0f, 0f, 5f), Quaternion.identity, "Galle_3");

            // Grenier : position centrale/surveillée, proche des cases sans les chevaucher.
            // Pas de porte (rempli par le haut, voir dudal_grenier.py) : rotation sans effet.
            Instancier(PathGrenier, new Vector3(0f, 0f, 1f), Quaternion.identity, "Grenier");

            // Mobilier domestique associé aux cases (foyer/cuisine documenté comme
            // proche des cases, zone privée) : entre le grenier et Galle_3, à l'écart
            // du dudal (zone publique) pour garder la partition public/privé lisible.
            Instancier(PathMortier, new Vector3(1.5f, 0f, 3f), Quaternion.identity, "Mortier_Cases");
            Instancier(PathPilon, new Vector3(1.5f, 0f, 3.25f), Quaternion.identity, "Pilon_Cases");
            Instancier(PathCalebasse, new Vector3(1.8f, 0f, 2.8f), Quaternion.identity, "Calebasse_Cases");
            Instancier(PathNatte, new Vector3(-1.5f, 0f, 3f), Quaternion.Euler(0, -20, 0), "Natte_Cases");

            // Bétail dans l'enclos (hoggo) : l'enclos existe déjà (baké dans Paysage.glb,
            // partagé avec Campement) mais restait vide — écart corrigé ici. Effectif
            // plus modeste qu'au Campement (le gros du troupeau part en transhumance
            // avec le campement mobile ; la concession garde un noyau domestique plutôt
            // que le troupeau complet).
            CreerTroupeauDansEnclos(new Vector3(-10.5f, 0f, 0f), 3.3f, nombre: 4);

            // Audio spatialisé : appel à la prière au-dessus du dudal, mouton + clochettes
            // près de l'enclos (Paysage, mêmes coordonnées bakées ~(-10.5,0,0), cohérent
            // avec le bétail maintenant présent), vent en ambiance diffuse. Pas de son
            // d'ambiance de village générique : le seul fichier disponible
            // (uganda-village-at-night) est explicitement nocturne, incohérent avec
            // l'éclairage de zénith de la scène — écarté plutôt qu'utilisé à tort (voir
            // note-ethique.md sur la cohérence des sources).
            CreerSourceAudio("Audio_AppelPriere", AudioAppelPriere, new Vector3(5.5f, 1.5f, -2.5f), 0.5f, 15f);
            CreerSourceAudio("Audio_Clochettes", AudioCowBells, new Vector3(-10.5f, 0.5f, 0f), 0.5f, 12f);
            CreerSourceAudio("Audio_Mouton", AudioMouton, new Vector3(-10.5f, 0.5f, 0f), 0.5f, 12f);
            CreerSourceAudio("Audio_Vent", AudioWind, new Vector3(0f, 2f, -4f), 0.35f, 30f, spatialBlend: 0.2f);

            CreerLumiereDirectionnelle("Soleil_Zenith", new Color(1f, 0.98f, 0.9f));

            // Joueur : positionné côté entrée, face à l'intérieur de la concession.
            CreerJoueur(new Vector3(0f, 0f, -4f), Quaternion.identity);

            SauvegarderScene(scene, "Concession");
        }

        /// <summary>
        /// Orchestrateur : reconstruit les deux scènes de zéro et applique toutes les
        /// passes non-destructives (audio, post-processing, ombres, PNJ) sans qu'il soit
        /// nécessaire de cliquer sur chaque entrée de menu une par une.
        ///
        /// Pensé pour être appelé aussi bien depuis le menu Unity (Editor ouvert) que depuis
        /// la ligne de commande en mode batch, ex. sur le PC GPU :
        ///
        ///   Unity.exe -batchmode -nographics -quit ^
        ///     -projectPath "T:\ProjetEddy\wuro-galle-vr\unity\wuro-galle-vr" ^
        ///     -executeMethod WuroGalle.Editor.SceneBuilder.ExecuterPipelineComplet ^
        ///     -logFile "T:\ProjetEddy\build_log.txt"
        ///
        /// ATTENTION : BuildCampement()/BuildConcession() ÉCRASENT la scène existante
        /// (NewScene en mode Single) — tout ajustement fait à la main directement dans
        /// l'éditeur (hors scripts) sera perdu. À lancer seulement après un import propre
        /// des .glb, pas en remplacement d'un ajustement manuel en cours.
        /// </summary>
        [MenuItem("Wuro&Galle/Pipeline complet : reconstruire + tout appliquer")]
        public static void ExecuterPipelineComplet()
        {
            BuildCampement();
            AjouterTerrainSceneActive();
            AjouterAudioSceneActive();
            AjouterPostProcessingSceneActive();
            ActiverOmbresSoleilSceneActive();
            AjouterPNJSceneActive();
            AjouterInteractionsSceneActive();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[SceneBuilder] Campement : reconstruite + terrain/météo + audio + post-processing + ombres + PNJ + interactions.");

            BuildConcession();
            AjouterTerrainSceneActive();
            AjouterAudioSceneActive();
            AjouterPostProcessingSceneActive();
            ActiverOmbresSoleilSceneActive();
            AjouterPNJSceneActive();
            AjouterInteractionsSceneActive();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[SceneBuilder] Concession : reconstruite + terrain/météo + audio + post-processing + ombres + PNJ + interactions.");

            Debug.Log("[SceneBuilder] Pipeline complet terminé.");

            // Ne quitte l'éditeur que si on tourne réellement en batch (CLI) — sinon un
            // appel depuis le menu fermerait l'Editor ouvert de l'utilisateur.
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>
        /// Instancie un module GLTF importé à une position/rotation donnée. Si l'asset est
        /// introuvable (mauvais chemin, pas encore importé), crée un objet vide nommé
        /// "MANQUANT_..." à la bonne position plutôt que de planter — visible immédiatement
        /// dans la Hierarchy et dans la Scene view.
        /// </summary>
        static GameObject Instancier(string cheminAsset, Vector3 position, Quaternion rotation, string nom)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(cheminAsset);
            if (source == null)
            {
                Debug.LogError($"[SceneBuilder] Asset introuvable : {cheminAsset} — vérifie le chemin d'import.");
                var vide = new GameObject($"MANQUANT_{nom}");
                vide.transform.position = position;
                return vide;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = nom;
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            return instance;
        }

        static void CreerLumiereDirectionnelle(string nom, Color couleur)
        {
            var lumiereGO = new GameObject(nom);
            var lumiere = lumiereGO.AddComponent<Light>();
            lumiere.type = LightType.Directional;
            lumiere.intensity = 0.85f; // réduit (était 1.15) : combiné à l'ambiance du skybox, ça sur-exposait la scène
            lumiere.color = couleur;
            // Par défaut un Light créé par script n'a AUCUNE ombre (LightShadows.None) —
            // c'était la cause des ombres manquantes au sol sous les cases.
            lumiere.shadows = LightShadows.Soft;
            lumiere.shadowStrength = 0.8f;
            lumiereGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f); // angle de zénith approximatif

            // Lumière ambiante du skybox par défaut (Built-in RP) : s'additionne à la
            // lumière directionnelle et au feu, contribuait à la sur-exposition signalée.
            RenderSettings.ambientIntensity = 0.6f;
        }

        /// <summary>
        /// Construit le feu de camp : trois systèmes de particules (flammes, fumée,
        /// braises) + une lumière ponctuelle scintillante, regroupés sous un parent.
        /// </summary>
        static GameObject CreerFeuDeCamp(Vector3 position)
        {
            var parent = new GameObject("FeuDeCamp");
            parent.transform.position = position;

            CreerParticulesFlammes(parent.transform);
            CreerParticulesFumee(parent.transform);
            CreerParticulesBraises(parent.transform);
            CreerLumiereFeu(parent.transform);

            return parent;
        }

        static void CreerLumiereFeu(Transform parent)
        {
            var lumiereGO = new GameObject("Lumiere_Feu");
            lumiereGO.transform.SetParent(parent);
            lumiereGO.transform.localPosition = new Vector3(0f, 0.4f, 0f);

            var lumiere = lumiereGO.AddComponent<Light>();
            lumiere.type = LightType.Point;
            lumiere.color = new Color(1f, 0.55f, 0.2f);
            lumiere.range = 5f;      // réduit (était 8) : n'éclaire que les abords immédiats du feu
            lumiere.intensity = 1.2f; // réduit (était 2.5) : trop combiné au soleil de zénith + à l'ambiance

            // Script runtime (Assets/Scripts/FeuDeCampScintillement.cs) : fait varier
            // l'intensité pour un effet de scintillement, visible seulement en Play mode.
            // Valeurs alignées sur l'intensité de base ci-dessus (sinon Update() écrase
            // avec ses propres valeurs par défaut à la première frame de Play).
            var scintillement = lumiereGO.AddComponent<FeuDeCampScintillement>();
            scintillement.intensiteBase = 1.2f;
            scintillement.amplitude = 0.3f;
        }

        /// <summary>
        /// Matériau de particules à partir d'un shader legacy intégré à Unity
        /// (Built-in Render Pipeline). Repli sur Sprites/Default si introuvable.
        /// </summary>
        static Material CreerMateriauParticules(string nomShader)
        {
            var shader = Shader.Find(nomShader);
            if (shader == null)
            {
                Debug.LogWarning($"[SceneBuilder] Shader introuvable : {nomShader} — repli sur Sprites/Default.");
                shader = Shader.Find("Sprites/Default");
            }
            return new Material(shader);
        }

        static void CreerParticulesFlammes(Transform parent)
        {
            var go = new GameObject("Particules_Flammes");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 1.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;

            var emission = ps.emission;
            emission.rateOverTime = 40f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.15f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradientFlammes = new Gradient();
            gradientFlammes.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f),
                    new GradientColorKey(new Color(1f, 0.3f, 0.05f), 0.6f),
                    new GradientColorKey(new Color(0.3f, 0.1f, 0.05f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.6f, 0.7f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradientFlammes;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.2f));

            go.GetComponent<ParticleSystemRenderer>().material =
                CreerMateriauParticules("Legacy Shaders/Particles/Additive");
        }

        static void CreerParticulesFumee(Transform parent)
        {
            var go = new GameObject("Particules_Fumee");
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 3f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;

            var emission = ps.emission;
            emission.rateOverTime = 8f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.1f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradientFumee = new Gradient();
            gradientFumee.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 0f),
                    new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0.35f, 0f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradientFumee;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.5f, 1, 1.5f));

            go.GetComponent<ParticleSystemRenderer>().material =
                CreerMateriauParticules("Legacy Shaders/Particles/Alpha Blended");
        }

        static void CreerParticulesBraises(Transform parent)
        {
            var go = new GameObject("Particules_Braises");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 0.8f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;

            var emission = ps.emission;
            emission.rateOverTime = 6f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f;
            shape.radius = 0.15f;

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            // Les 3 axes doivent être dans le même mode (ici TwoConstants) — Y explicite
            // à (0,0), sinon Unity refuse (mode par défaut différent) : erreur "Particle
            // Velocity curves must all be in the same mode".
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradientBraises = new Gradient();
            gradientBraises.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.7f, 0.2f), 0f),
                    new GradientColorKey(new Color(0.6f, 0.2f, 0.05f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = gradientBraises;

            go.GetComponent<ParticleSystemRenderer>().material =
                CreerMateriauParticules("Legacy Shaders/Particles/Additive");
        }

        /// <summary>
        /// Crée le joueur : un GameObject avec CharacterController + FirstPersonController
        /// (script existant, Assets/Scripts/FirstPersonController.cs), et une caméra enfant
        /// à hauteur des yeux, taguée MainCamera. Sans ça, le Game view affiche
        /// "No cameras rendering" — aucune scène générée par ce script n'en avait avant.
        /// Provisoire : sera remplacé par un rig XR Origin à l'étape VR (casque).
        /// </summary>
        static GameObject CreerJoueur(Vector3 position, Quaternion rotation)
        {
            var joueur = new GameObject("Joueur");
            joueur.tag = "Player"; // requis par PNJDialogue.OnTriggerEnter
            joueur.transform.position = position;
            joueur.transform.rotation = rotation;

            var controller = joueur.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.radius = 0.3f;

            var cameraGO = new GameObject("CameraJoueur");
            cameraGO.transform.SetParent(joueur.transform);
            cameraGO.transform.localPosition = new Vector3(0f, 1.6f, 0f); // hauteur des yeux
            cameraGO.transform.localRotation = Quaternion.identity;

            var cam = cameraGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cameraGO.AddComponent<AudioListener>();

            var fpc = joueur.AddComponent<FirstPersonController>();
            fpc.vueCamera = cameraGO.transform;

            // Filet de sécurité (Assets/Scripts/SecuriteChute.cs) : si le joueur tombe
            // sous y=-20 (collision manquante, bord non couvert), il est replacé au
            // point de départ au lieu de tomber indéfiniment.
            joueur.AddComponent<SecuriteChute>();

            // Corps du joueur : silhouette modélisée en Blender (voir
            // blender/personnage/personnage_joueur.py), pas des capsules brutes.
            // Instancié à plat (Personnage.glb contient Bassin/Torse/Tete/Bras_G/
            // Bras_D/Jambe_G/Jambe_D comme enfants directs, sans hiérarchie —
            // reparentés ici en Bassin > Torse > Tête/Bras, Bassin > Jambes avec
            // worldPositionStays=true : la pose debout définie dans le script
            // Blender est préservée telle quelle, pas besoin de recalculer les
            // offsets à la main). CorpsJoueur.cs (Assets/Scripts/) retrouve ensuite
            // ces transforms par nom et gère les poses (Awake() désactive
            // gameObject, invisible en vue FPS normale, affiché seulement pendant
            // les séquences 3e personne — voir CameraTierceUtils).
            // SUR UN ENFANT DÉDIÉ ("Corps"), PAS sur le Joueur lui-même :
            // CorpsJoueur.Awake() fait gameObject.SetActive(false), et l'appliquer
            // directement sur la racine Joueur désactiverait aussi CameraJoueur/
            // FirstPersonController/CameraTierce (tous ses enfants) — bug réel
            // observé (plus de caméra active du tout dès le premier frame, "no
            // audio listener" en boucle en Play mode).
            var corpsGO = Instancier(PathPersonnage, Vector3.zero, Quaternion.identity, "Corps");
            corpsGO.transform.SetParent(joueur.transform);
            corpsGO.transform.localPosition = Vector3.zero;
            corpsGO.transform.localRotation = Quaternion.identity;

            // Décroche complètement l'instance du prefab Personnage.glb AVANT de
            // reparenter ses parties entre elles : sans ça, reparenter un enfant
            // (Torse) sous un autre enfant (Bassin) DU MÊME prefab instance
            // s'applique bien dans la session Éditeur en cours, mais ne survit PAS
            // à un SaveScene/reload — au rechargement, Unity resynchronise
            // l'instance sur la structure plate d'origine du prefab et la
            // reparenté est silencieusement perdue (bug réel constaté : Torse/
            // Jambe_G revenaient enfants directs de "Corps" après rechargement,
            // AppliquerPose ne trouvait plus rien et ne faisait donc plus rien).
            PrefabUtility.UnpackPrefabInstance(corpsGO, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var bassinT = TrouverEnfant(corpsGO.transform, "Bassin");
            if (bassinT == null)
            {
                Debug.LogError("[SceneBuilder] 'Bassin' introuvable dans Personnage.glb — corps du joueur incomplet.");
            }
            else
            {
                var torseT = TrouverEnfant(corpsGO.transform, "Torse");
                var teteT = TrouverEnfant(corpsGO.transform, "Tete");
                var brasGT = TrouverEnfant(corpsGO.transform, "Bras_G");
                var brasDT = TrouverEnfant(corpsGO.transform, "Bras_D");
                var jambeGT = TrouverEnfant(corpsGO.transform, "Jambe_G");
                var jambeDT = TrouverEnfant(corpsGO.transform, "Jambe_D");

                if (torseT != null) torseT.SetParent(bassinT, true);
                if (teteT != null && torseT != null) teteT.SetParent(torseT, true);
                if (brasGT != null && torseT != null) brasGT.SetParent(torseT, true);
                if (brasDT != null && torseT != null) brasDT.SetParent(torseT, true);
                if (jambeGT != null) jambeGT.SetParent(bassinT, true);
                if (jambeDT != null) jambeDT.SetParent(bassinT, true);
            }

            corpsGO.AddComponent<CorpsJoueur>();

            // Caméra 3e personne : légèrement en retrait et en hauteur, cadrée sur le
            // corps. Désactivée par défaut (CameraTierceUtils l'active pendant la
            // séquence). AudioListener présent mais désactivé aussi : un seul listener
            // actif à la fois (Unity n'aime pas en avoir plusieurs simultanément).
            var camTierceGO = new GameObject("CameraTierce");
            camTierceGO.transform.SetParent(joueur.transform);
            camTierceGO.transform.localPosition = new Vector3(0f, 1.7f, -2.4f);
            camTierceGO.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            var camTierce = camTierceGO.AddComponent<Camera>();
            camTierce.enabled = false;
            var ecouteurTierce = camTierceGO.AddComponent<AudioListener>();
            ecouteurTierce.enabled = false;

            return joueur;
        }

        /// <summary>
        /// Crée une source audio 3D à une position donnée. spatialBlend proche de 1 =
        /// son localisé (feu, cloches) ; plus proche de 0 = ambiance diffuse (vent).
        /// L'écoute se fait via l'AudioListener du joueur (CreerJoueur).
        /// </summary>
        static void CreerSourceAudio(string nom, string cheminClip, Vector3 position,
            float volume, float portee, float spatialBlend = 1f, bool loop = true)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(cheminClip);
            if (clip == null)
            {
                Debug.LogError($"[SceneBuilder] Clip audio introuvable : {cheminClip}");
                return;
            }

            var go = new GameObject(nom);
            go.transform.position = position;

            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = true;
            source.volume = volume;
            source.spatialBlend = spatialBlend;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.maxDistance = portee;
        }

        /// <summary>
        /// Ajoute un MeshCollider sur l'objet "Terrain" (enfant du Paysage instancié) :
        /// sans ça, les modèles importés par glTFast n'ont aucun collider et le
        /// CharacterController du joueur traverse le sol en chute libre indéfinie.
        /// </summary>
        static void AjouterColliderSol(GameObject paysageRacine)
        {
            var terrain = TrouverEnfant(paysageRacine.transform, "Terrain");
            if (terrain == null)
            {
                Debug.LogError("[SceneBuilder] Objet 'Terrain' introuvable sous Paysage — " +
                    "aucun collider de sol ajouté, le joueur va tomber indéfiniment.");
                return;
            }
            if (terrain.GetComponent<Collider>() == null)
                terrain.gameObject.AddComponent<MeshCollider>();
        }

        /// <summary>Recherche récursive d'un enfant par nom exact (glTFast imbrique les nœuds).</summary>
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

        /// <summary>
        /// Cherche un GameObject par nom UNIQUEMENT dans la scène donnée — contrairement à
        /// GameObject.Find qui cherche dans TOUTES les scènes chargées dans l'Éditeur.
        /// Important ici : Campement et Concession sont presque toujours ouvertes en même
        /// temps, et partagent des noms d'objets identiques ("Paysage", "Soleil_Zenith",
        /// "Joueur", "Terrain_Environnement"...). Sans ce filtrage, un menu "non destructif"
        /// pouvait silencieusement modifier l'objet de L'AUTRE scène — bug réel observé
        /// (fix de sol appliqué à la mauvaise scène alors que les deux étaient chargées).
        /// </summary>
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

        static void SauvegarderScene(Scene scene, string nomScene)
        {
            const string dossier = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(dossier))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            string chemin = $"{dossier}/{nomScene}.unity";
            bool ok = EditorSceneManager.SaveScene(scene, chemin);
            if (ok)
                Debug.Log($"[SceneBuilder] Scène sauvegardée : {chemin}");
            else
                Debug.LogError($"[SceneBuilder] Échec de sauvegarde de la scène : {chemin}");
        }
    }
}

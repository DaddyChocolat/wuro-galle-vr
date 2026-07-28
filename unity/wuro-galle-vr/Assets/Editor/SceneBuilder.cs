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
                RemplacerSourceAudio("Audio_Feu", AudioFeu, new Vector3(0f, 0.3f, 0f), 0.7f, 6f);
                RemplacerSourceAudio("Audio_Clochettes", AudioCowBells, new Vector3(-10.5f, 0.5f, 0f), 0.6f, 12f);
                RemplacerSourceAudio("Audio_Paturage", AudioGrazingCows, new Vector3(-10.5f, 0.5f, 0f), 0.5f, 12f);
                RemplacerSourceAudio("Audio_Vent", AudioWind, new Vector3(0f, 2f, 4f), 0.35f, 30f, spatialBlend: 0.2f);
                Debug.Log("[SceneBuilder] Audio ajouté à la scène Campement.");
            }
            else if (scene.name == "Concession")
            {
                RemplacerSourceAudio("Audio_AppelPriere", AudioAppelPriere, new Vector3(0f, 1.5f, -2f), 0.5f, 15f);
                RemplacerSourceAudio("Audio_Mouton", AudioMouton, new Vector3(-10.5f, 0.5f, 0f), 0.5f, 12f);
                RemplacerSourceAudio("Audio_Vent", AudioWind, new Vector3(0f, 2f, -4f), 0.35f, 30f, spatialBlend: 0.2f);
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

            GameObject camObj = GameObject.Find("CameraJoueur");
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

            GameObject existant = GameObject.Find("Global_PostProcess_Volume");
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
            GameObject soleilGO = GameObject.Find("Soleil_Zenith");
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

        /// <summary>Supprime l'objet du même nom s'il existe déjà, puis recrée la source audio.</summary>
        static void RemplacerSourceAudio(string nom, string cheminClip, Vector3 position,
            float volume, float portee, float spatialBlend = 1f)
        {
            var existant = GameObject.Find(nom);
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

            var joueur = GameObject.Find("Joueur");
            if (joueur != null && !joueur.CompareTag("Player"))
                joueur.tag = "Player";

            if (scene.name == "Campement")
            {
                RemplacerPNJ("PNJ_Berger", new Vector3(-1f, 0f, 1f), Quaternion.Euler(0, -30, 0));
                Debug.Log("[SceneBuilder] PNJ_Berger ajouté à Campement (pas de ligne audio assignée).");
            }
            else if (scene.name == "Concession")
            {
                RemplacerPNJ("PNJ_Femme", new Vector3(0.5f, 0f, 3.5f), Quaternion.Euler(0, 160, 0));
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

        static void RemplacerPNJ(string nom, Vector3 position, Quaternion rotation)
        {
            var existant = GameObject.Find(nom);
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

            var ancien = GameObject.Find("Montagnes_Lointaines");
            if (ancien != null) Object.DestroyImmediate(ancien);
            CreerMontagnesLointaines();

            var ancienneVege = GameObject.Find("Vegetation_Eparse");
            if (ancienneVege != null) Object.DestroyImmediate(ancienneVege);
            // Zone évitée différente selon la scène (là où sont les structures).
            Rect zoneEvitee = scene.name == "Campement"
                ? new Rect(-13f, -4f, 22f, 12f)   // englobe suudu, feu, hoggo, mobilier
                : new Rect(-6f, -4f, 12f, 11f);   // englobe dudal, cases, grenier, mobilier
            CreerVegetationEparse(zoneEvitee);

            AdoucirTexturesParticulesFeu();

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SceneBuilder] Décor ajouté à la scène {scene.name}.");
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

            // Zone évitée (même convention que AjouterDecorSceneActive) : reste plate
            // dans cette zone pour ne pas déformer le sol sous les structures existantes.
            Rect zoneEvitee = scene.name == "Campement"
                ? new Rect(-13f, -4f, 22f, 12f)
                : new Rect(-6f, -4f, 12f, 11f);

            foreach (string nomAncien in new[] { "Montagnes_Lointaines", "Vegetation_Eparse", "Terrain_Environnement" })
            {
                var ancien = GameObject.Find(nomAncien);
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

            // Brouillard : reprend les réglages qu'avait CreerMontagnesLointaines (les
            // collines du Terrain jouent maintenant ce rôle d'horizon qui se fond dans le ciel).
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.75f, 0.78f, 0.82f);
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 90f;

            AdoucirTexturesParticulesFeu();

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SceneBuilder] Terrain ajouté à la scène {scene.name} (remplace les anciens cônes de décor).");
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

        /// <summary>
        /// Buissons épars (petits cônes bas et sombres) sur le terrain, en évitant la
        /// zone occupée par les structures (rect en coordonnées X/Z monde). Rien
        /// n'existait côté végétation jusqu'ici.
        /// </summary>
        static void CreerVegetationEparse(Rect zoneEvitee)
        {
            var parent = new GameObject("Vegetation_Eparse");
            var rng = new System.Random(7);

            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.16f, 0.20f, 0.11f);
            mat.SetFloat("_Glossiness", 0.05f);

            int nombre = 45;
            int placees = 0;
            int tentatives = 0;
            while (placees < nombre && tentatives < nombre * 6)
            {
                tentatives++;
                float x = (float)(rng.NextDouble() * 26 - 13);
                float z = (float)(rng.NextDouble() * 24 - 12);
                if (zoneEvitee.Contains(new Vector2(x, z))) continue;

                var buisson = new GameObject($"Buisson_{placees}");
                buisson.transform.SetParent(parent.transform);
                buisson.transform.position = new Vector3(x, 0f, z);
                buisson.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);

                var mf = buisson.AddComponent<MeshFilter>();
                mf.sharedMesh = CreerMeshCone(
                    0.2f + (float)rng.NextDouble() * 0.2f,
                    0.35f + (float)rng.NextDouble() * 0.3f,
                    5);
                var mr = buisson.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;

                placees++;
            }
        }

        /// <summary>
        /// Remplace le rendu à bords durs des particules du feu (aucune texture assignée
        /// jusqu'ici) par une texture radiale douce générée en code, appliquée aux
        /// matériaux des flammes et des braises (la fumée reste diffuse, déjà correcte).
        /// </summary>
        static void AdoucirTexturesParticulesFeu()
        {
            var texture = CreerTextureRadialeDouce(64);

            foreach (var nomObjet in new[] { "Particules_Flammes", "Particules_Braises" })
            {
                var obj = GameObject.Find(nomObjet);
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
            // en doublon, ailleurs, comme observé précédemment. Le troupeau est placé
            // à l'intérieur du vrai enclos, pas à côté d'un doublon.
            Instancier(PathTroupeauBlanc, new Vector3(-12f, 0f, 1f), Quaternion.Euler(0, 20, 0), "Troupeau_blanc");
            Instancier(PathTroupeauRoux, new Vector3(-9f, 0f, -1f), Quaternion.Euler(0, -20, 0), "Troupeau_roux");

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

            // Dudal : espace de prière/rassemblement, zone publique proche de l'entrée.
            // Forme radialement symétrique (simple disque) : rotation sans effet.
            Instancier(PathDudal, new Vector3(0f, 0f, -2f), Quaternion.identity, "Dudal");

            // Natte de prière posée sur le dudal — annoncé dans dudal_grenier.py comme
            // à faire "au moment de l'assemblage Unity", fait ici.
            Instancier(PathNatte, new Vector3(0f, 0.02f, -2f), Quaternion.Euler(0, 10, 0), "Natte_Priere");

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

            // Audio spatialisé : appel à la prière au-dessus du dudal, mouton près de
            // l'enclos (Paysage, mêmes coordonnées bakées ~(-10.5,0,0)), vent en ambiance
            // diffuse. Pas de son d'ambiance de village générique : le seul fichier
            // disponible (uganda-village-at-night) est explicitement nocturne, incohérent
            // avec l'éclairage de zénith de la scène — écarté plutôt qu'utilisé à tort
            // (voir note-ethique.md sur la cohérence des sources).
            CreerSourceAudio("Audio_AppelPriere", AudioAppelPriere, new Vector3(0f, 1.5f, -2f), 0.5f, 15f);
            CreerSourceAudio("Audio_Mouton", AudioMouton, new Vector3(-10.5f, 0.5f, 0f), 0.5f, 12f);
            CreerSourceAudio("Audio_Vent", AudioWind, new Vector3(0f, 2f, -4f), 0.35f, 30f, spatialBlend: 0.2f);

            CreerLumiereDirectionnelle("Soleil_Zenith", new Color(1f, 0.98f, 0.9f));

            // Joueur : positionné côté entrée, face à l'intérieur de la concession.
            CreerJoueur(new Vector3(0f, 0f, -4f), Quaternion.identity);

            SauvegarderScene(scene, "Concession");
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

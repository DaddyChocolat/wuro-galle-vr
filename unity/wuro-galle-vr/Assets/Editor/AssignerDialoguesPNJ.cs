using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WuroGalle.Editor
{
    /// <summary>
    /// Assigne automatiquement les clips audio enregistrés (voix PNJ, voir
    /// docs/Livrable_4/script-narration.md) aux composants PNJDialogue des scènes,
    /// sans passer par un glisser-déposer manuel dans l'Inspector à chaque fois
    /// que les scènes sont reconstruites (SceneBuilder recrée PNJ_Berger/PNJ_Femme
    /// à chaque BuildCampement/BuildConcession, avec un tableau "lignes" vide).
    ///
    /// Convention de nommage attendue (voir script-narration.md) : un clip par
    /// ligne, préfixe + numéro à deux chiffres, dans l'ordre de lecture voulu —
    /// "berger_01", "berger_02", "berger_03" pour PNJ_Berger (Campement),
    /// "femme_01", "femme_02", "femme_03" pour PNJ_Femme (Concession). L'extension
    /// (.wav/.mp3/.flac/.ogg) n'a pas d'importance : la recherche se fait par type
    /// d'asset (AudioClip) + préfixe de nom, pas par chemin exact.
    /// </summary>
    public static class AssignerDialoguesPNJ
    {
        [MenuItem("Wuro&Galle/Assigner les lignes PNJ (Campement + Concession)")]
        public static void AssignerToutes()
        {
            bool ok1 = AssignerScene("Campement", "PNJ_Berger", "berger_");
            bool ok2 = AssignerScene("Concession", "PNJ_Femme", "femme_");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(ok1 && ok2 ? 0 : 1);
            }
        }

        static bool AssignerScene(string nomScene, string nomPNJ, string prefixeClip)
        {
            Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{nomScene}.unity", OpenSceneMode.Single);

            GameObject pnj = scene.GetRootGameObjects().FirstOrDefault(r => r.name == nomPNJ);
            if (pnj == null)
            {
                Debug.LogError($"[AssignerDialoguesPNJ] {nomPNJ} introuvable dans {nomScene} — lance d'abord BuildCampement/BuildConcession.");
                return false;
            }

            var dialogue = pnj.GetComponent<PNJDialogue>();
            if (dialogue == null)
            {
                Debug.LogError($"[AssignerDialoguesPNJ] {nomPNJ} n'a pas de composant PNJDialogue.");
                return false;
            }

            AudioClip[] clips = TrouverClipsParPrefixe(prefixeClip);
            if (clips.Length == 0)
            {
                Debug.LogWarning($"[AssignerDialoguesPNJ] Aucun clip '{prefixeClip}*' trouvé dans Assets/Audio/ — " +
                    $"{nomPNJ} reste sans ligne assignée. Enregistre les lignes (voir script-narration.md) puis relance.");
                return false;
            }

            dialogue.lignes = clips;
            EditorUtility.SetDirty(pnj);
            EditorSceneManager.SaveScene(scene);

            Debug.Log($"[AssignerDialoguesPNJ] {nomPNJ} ({nomScene}) : {clips.Length} ligne(s) assignée(s) -> " +
                string.Join(", ", clips.Select(c => c.name)));
            return true;
        }

        /// <summary>
        /// Cherche tous les AudioClip dont le nom commence par le préfixe donné,
        /// triés par nom (donc par numéro si la convention "prefixe_01" est
        /// respectée) — peu importe le sous-dossier ou l'extension d'origine.
        /// </summary>
        static AudioClip[] TrouverClipsParPrefixe(string prefixe)
        {
            var guids = AssetDatabase.FindAssets("t:AudioClip");
            var clips = new List<AudioClip>();

            foreach (var guid in guids)
            {
                string chemin = AssetDatabase.GUIDToAssetPath(guid);
                string nomFichier = System.IO.Path.GetFileNameWithoutExtension(chemin);
                if (!nomFichier.StartsWith(prefixe, StringComparison.OrdinalIgnoreCase)) continue;

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(chemin);
                if (clip != null) clips.Add(clip);
            }

            return clips.OrderBy(c => c.name, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}

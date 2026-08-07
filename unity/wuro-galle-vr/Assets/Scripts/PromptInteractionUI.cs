using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Petit HUD texte, créé au runtime (pas besoin de Canvas pré-posé dans les
/// scènes) : une ligne persistante en bas de l'écran ("Appuyez sur E pour
/// boire") tant que le joueur est dans une zone InteractionProximite, et une
/// ligne de confirmation temporaire ("Vous buvez une gorgée d'eau fraîche.")
/// affichée juste après l'action, au-dessus de la première.
///
/// Singleton paresseux : le premier appel à Afficher()/AfficherMessage() crée
/// le Canvas s'il n'existe pas encore. Fonctionne dans Campement ET Concession
/// sans rien à assigner dans l'Inspector.
/// </summary>
public static class PromptInteractionUI
{
    private static Text texteConsigne;
    private static Text texteMessage;
    private static InteractionProximite proprietaireActuel;
    private static CoroutineRunner runner;

    private static void SAssurerCreee()
    {
        if (texteConsigne != null) return;

        var racineCanvas = new GameObject("Canvas_InteractionHUD");
        Object.DontDestroyOnLoad(racineCanvas);
        var canvas = racineCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        racineCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        racineCanvas.AddComponent<GraphicRaycaster>();

        texteConsigne = CreerTexte(racineCanvas.transform, "Texte_Consigne", ancrageY: 60f, taille: 24, couleur: Color.white);
        texteMessage = CreerTexte(racineCanvas.transform, "Texte_Message", ancrageY: 110f, taille: 22, couleur: new Color(1f, 0.92f, 0.75f));

        runner = racineCanvas.AddComponent<CoroutineRunner>();

        texteConsigne.text = "";
        texteMessage.text = "";
    }

    private static Text CreerTexte(Transform parent, string nom, float ancrageY, int taille, Color couleur)
    {
        var go = new GameObject(nom);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, ancrageY);
        rect.sizeDelta = new Vector2(900f, 40f);

        var texte = go.AddComponent<Text>();
        texte.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texte.fontSize = taille;
        texte.alignment = TextAnchor.MiddleCenter;
        texte.color = couleur;
        texte.text = "";

        var ombre = go.AddComponent<Shadow>();
        ombre.effectColor = new Color(0f, 0f, 0f, 0.8f);
        ombre.effectDistance = new Vector2(1.5f, -1.5f);

        return texte;
    }

    /// <summary>Affiche la consigne persistante tant que le joueur reste dans la zone donnée.</summary>
    public static void Afficher(InteractionProximite proprietaire, string message)
    {
        SAssurerCreee();
        proprietaireActuel = proprietaire;
        texteConsigne.text = message;
    }

    /// <summary>N'efface la consigne que si c'est bien la zone qui l'a affichée (évite qu'une zone
    /// n'efface par erreur le message d'une autre zone chevauchante).</summary>
    public static void Masquer(InteractionProximite proprietaire)
    {
        if (texteConsigne == null || proprietaireActuel != proprietaire) return;
        texteConsigne.text = "";
        proprietaireActuel = null;
    }

    /// <summary>Message de confirmation ponctuel ("Vous buvez..."), qui s'efface tout seul après `duree` secondes.</summary>
    public static void AfficherMessage(string message, float duree = 3f)
    {
        SAssurerCreee();
        runner.AfficherMessageTemporaire(texteMessage, message, duree);
    }

    /// <summary>MonoBehaviour minimal pour porter la coroutine de disparition — les méthodes statiques ne peuvent pas en lancer directement.</summary>
    private class CoroutineRunner : MonoBehaviour
    {
        private Coroutine enCours;

        public void AfficherMessageTemporaire(Text cible, string message, float duree)
        {
            if (enCours != null) StopCoroutine(enCours);
            enCours = StartCoroutine(Routine(cible, message, duree));
        }

        private System.Collections.IEnumerator Routine(Text cible, string message, float duree)
        {
            cible.text = message;
            yield return new WaitForSeconds(duree);
            if (cible.text == message) cible.text = "";
        }
    }
}

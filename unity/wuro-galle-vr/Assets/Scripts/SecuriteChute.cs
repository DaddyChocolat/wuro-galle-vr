using UnityEngine;

/// <summary>
/// Filet de sécurité : si le joueur tombe sous une hauteur donnée (collision
/// manquante quelque part, bord du terrain non couvert...), le replace à sa
/// position de départ plutôt que de le laisser tomber indéfiniment.
/// Ne remplace pas un vrai collider de sol — complément, pas la solution
/// principale (voir SceneBuilder.cs : AjouterColliderSol).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SecuriteChute : MonoBehaviour
{
    public float hauteurLimite = -20f;

    private Vector3 positionDepart;
    private CharacterController controller;

    void Start()
    {
        positionDepart = transform.position;
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (transform.position.y < hauteurLimite)
        {
            // Désactiver puis réactiver le CharacterController avant de forcer sa
            // position : sinon il "résiste" au déplacement direct de son transform.
            controller.enabled = false;
            transform.position = positionDepart;
            controller.enabled = true;
        }
    }
}

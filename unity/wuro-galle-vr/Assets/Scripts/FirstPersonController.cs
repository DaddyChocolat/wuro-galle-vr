using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    public float vitesseDeplacement = 3.0f; // m/s, marche normale
    public float sensibiliteSouris = 2.0f;
    public Transform vueCamera; // référence à la caméra enfant

    private CharacterController controller;
    private float rotationVerticale = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked; // capture la souris pour la vue FPS
    }

    void Update()
    {
        // Rotation horizontale : tourne tout le corps du joueur
        float rotationHorizontale = Input.GetAxis("Mouse X") * sensibiliteSouris;
        transform.Rotate(0, rotationHorizontale, 0);

        // Rotation verticale : tourne seulement la caméra, pas le corps
        rotationVerticale -= Input.GetAxis("Mouse Y") * sensibiliteSouris;
        rotationVerticale = Mathf.Clamp(rotationVerticale, -80f, 80f); // évite de se retourner
        vueCamera.localRotation = Quaternion.Euler(rotationVerticale, 0, 0);

        // Déplacement WASD
        float avant = Input.GetAxis("Vertical") * vitesseDeplacement;
        float lateral = Input.GetAxis("Horizontal") * vitesseDeplacement;
        Vector3 deplacement = transform.right * lateral + transform.forward * avant;
        controller.SimpleMove(deplacement); // gère aussi la gravité automatiquement
    }
}
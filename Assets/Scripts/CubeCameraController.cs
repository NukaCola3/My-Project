using UnityEngine;
using UnityEngine.InputSystem;


// ==================================================
// CUBE CAMERA CONTROLLER
// ==================================================

public class CubeCameraController : MonoBehaviour
{
    // ==================================================
    // REFERENZEN
    // ==================================================

    [Header("Target")]

    public Transform target;


    // ==================================================
    // KAMERA ROTATION
    // ==================================================

    [Header("Camera Rotation")]

    public float rotationSpeed = 0.3f;

    public float minVerticalAngle = -80f;

    public float maxVerticalAngle = 80f;


    // ==================================================
    // ZOOM
    // ==================================================

    [Header("Zoom")]

    public float zoomSpeed = 2f;

    public float minDistance = 3f;

    public float maxDistance = 12f;


    // ==================================================
    // INTERNE VARIABLEN
    // ==================================================

    private float horizontalAngle;

    private float verticalAngle;

    private float distance;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning(
                "CubeCameraController: Target fehlt."
            );

            return;
        }


        // --------------------------------------------------
        // DISTANZ ZUM WÜRFEL
        // --------------------------------------------------

        Vector3 direction =
            transform.position -
            target.position;


        distance =
            direction.magnitude;


        // --------------------------------------------------
        // AKTUELLE KAMERARICHTUNG
        // --------------------------------------------------

        Vector3 normalizedDirection =
            direction.normalized;


        horizontalAngle =
            Mathf.Atan2(
                normalizedDirection.x,
                normalizedDirection.z
            )
            * Mathf.Rad2Deg;


        verticalAngle =
            Mathf.Asin(
                normalizedDirection.y
            )
            * Mathf.Rad2Deg;


        verticalAngle =
            Mathf.Clamp(
                verticalAngle,
                minVerticalAngle,
                maxVerticalAngle
            );


        UpdateCameraPosition();
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        if (target == null)
        {
            return;
        }


        HandleOrbit();

        HandleZoom();

        UpdateCameraPosition();
    }


    // ==================================================
    // KAMERA UM DEN WÜRFEL DREHEN
    // ==================================================

    private void HandleOrbit()
    {
        if (Mouse.current == null)
        {
            return;
        }


        // --------------------------------------------------
        // RECHTE MAUSTASTE
        // --------------------------------------------------

        if (
            Mouse.current.rightButton.isPressed
        )
        {
            Vector2 mouseDelta =
                Mouse.current.delta.ReadValue();


            // --------------------------------------------------
            // HORIZONTAL
            //
            // Maus nach rechts
            // -> Kamera nach rechts
            // --------------------------------------------------

            horizontalAngle +=
                mouseDelta.x *
                rotationSpeed;


            // --------------------------------------------------
            // VERTIKAL
            //
            // Maus nach oben
            // -> Kamera nach oben
            // --------------------------------------------------

            verticalAngle +=
                mouseDelta.y *
                rotationSpeed;


            // --------------------------------------------------
            // KAMERA NICHT ÜBER KOPF DREHEN
            // --------------------------------------------------

            verticalAngle =
                Mathf.Clamp(
                    verticalAngle,
                    minVerticalAngle,
                    maxVerticalAngle
                );
        }
    }


    // ==================================================
    // ZOOM
    // ==================================================

    private void HandleZoom()
    {
        if (Mouse.current == null)
        {
            return;
        }


        Vector2 scroll =
            Mouse.current.scroll.ReadValue();


        if (
            Mathf.Abs(scroll.y) >
            0.01f
        )
        {
            distance -=
                scroll.y *
                zoomSpeed *
                0.01f;


            distance =
                Mathf.Clamp(
                    distance,
                    minDistance,
                    maxDistance
                );
        }
    }


    // ==================================================
    // KAMERA POSITIONIEREN
    // ==================================================

    private void UpdateCameraPosition()
    {
        Quaternion rotation =
            Quaternion.Euler(
                verticalAngle,
                horizontalAngle,
                0f
            );


        Vector3 offset =
            rotation *
            Vector3.forward *
            distance;


        transform.position =
            target.position +
            offset;


        // --------------------------------------------------
        // IMMER AUF DEN WÜRFEL SCHAUEN
        // --------------------------------------------------

        transform.LookAt(
            target.position
        );
    }
}
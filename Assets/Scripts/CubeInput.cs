using UnityEngine;
using UnityEngine.InputSystem;


// ==================================================
// CUBE INPUT
// ==================================================

public class CubeInput : MonoBehaviour
{
    // ==================================================
    // REFERENZEN
    // ==================================================

    [Header("References")]

    public RubiksCube rubiksCube;

    public Camera cubeCamera;


    // ==================================================
    // MAUS
    // ==================================================

    [Header("Mouse")]

    public float dragThreshold = 20f;


    // ==================================================
    // INTERNE VARIABLEN
    // ==================================================

    private bool isDragging = false;

    private Vector2 dragStartPosition;

    private Cubie selectedCubie;

    private Vector3 selectedNormal;


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        if (rubiksCube != null && rubiksCube.IsInputLocked)
        {
            EndMouseDrag();
            return;
        }

        if (Mouse.current == null)
        {
            return;
        }


        // --------------------------------------------------
        // MAUSTASTE GEDRÜCKT
        // --------------------------------------------------

        if (
            Mouse.current.leftButton.wasPressedThisFrame
        )
        {
            StartMouseDrag();
        }


        // --------------------------------------------------
        // DRAG
        // --------------------------------------------------

        if (
            isDragging &&
            Mouse.current.leftButton.isPressed
        )
        {
            CheckMouseDrag();
        }


        // --------------------------------------------------
        // MAUSTASTE LOSGELASSEN
        // --------------------------------------------------

        if (
            isDragging &&
            Mouse.current.leftButton.wasReleasedThisFrame
        )
        {
            EndMouseDrag();
        }
    }


    // ==================================================
    // DRAG STARTEN
    // ==================================================

    private void StartMouseDrag()
    {
        if (rubiksCube == null)
        {
            Debug.LogWarning(
                "CubeInput: RubiksCube fehlt."
            );

            return;
        }


        // --------------------------------------------------
        // KAMERA
        // --------------------------------------------------

        if (cubeCamera == null)
        {
            cubeCamera = Camera.main;
        }


        if (cubeCamera == null)
        {
            Debug.LogWarning(
                "CubeInput: Keine Kamera gefunden."
            );

            return;
        }


        // --------------------------------------------------
        // MAUSPOSITION
        // --------------------------------------------------

        Vector2 mousePosition =
            Mouse.current.position.ReadValue();


        // --------------------------------------------------
        // RAYCAST
        // --------------------------------------------------

        Ray ray =
            cubeCamera.ScreenPointToRay(
                mousePosition
            );


        RaycastHit hit;


        if (
            !Physics.Raycast(
                ray,
                out hit
            )
        )
        {
            return;
        }


        // --------------------------------------------------
        // CUBIE FINDEN
        // --------------------------------------------------

        Cubie cubie =
            hit.collider.GetComponentInParent<Cubie>();


        if (cubie == null)
        {
            return;
        }


        // --------------------------------------------------
        // AUSGEWÄHLTEN CUBIE SPEICHERN
        // --------------------------------------------------

        selectedCubie =
            cubie;


        // --------------------------------------------------
        // FLÄCHENNORMALE SPEICHERN
        // --------------------------------------------------

        selectedNormal =
            hit.normal.normalized;


        // --------------------------------------------------
        // STARTPOSITION
        // --------------------------------------------------

        dragStartPosition =
            mousePosition;


        isDragging = true;
    }


    // ==================================================
    // DRAG PRÜFEN
    // ==================================================

    private void CheckMouseDrag()
    {
        if (selectedCubie == null)
        {
            return;
        }


        Vector2 currentMousePosition =
            Mouse.current.position.ReadValue();


        Vector2 drag =
            currentMousePosition -
            dragStartPosition;


        // --------------------------------------------------
        // MINDESTBEWEGUNG
        // --------------------------------------------------

        if (
            drag.magnitude <
            dragThreshold
        )
        {
            return;
        }


        // --------------------------------------------------
        // MOVE ERZEUGEN
        // --------------------------------------------------

        CubeMove move;


        if (
            TryCreateIntuitiveMove(
                selectedCubie,
                selectedNormal,
                drag,
                out move
            )
        )
        {
            rubiksCube.ExecuteInputMove(
                move
            );


            isDragging = false;

            selectedCubie = null;
        }
    }


    // ==================================================
    // DRAG BEENDEN
    // ==================================================

    private void EndMouseDrag()
    {
        isDragging = false;

        selectedCubie = null;
    }


    // ==================================================
    // INTUITIVE BEWEGUNG
    // ==================================================

    private bool TryCreateIntuitiveMove(
        Cubie cubie,
        Vector3 normal,
        Vector2 drag,
        out CubeMove move)
    {
        move =
            new CubeMove();


        // ==================================================
        // 1.
        // MAUSBEWEGUNG IN DIE 3D-WELT ÜBERTRAGEN
        // ==================================================

        Vector3 worldDrag =
            cubeCamera.transform.right *
            drag.x
            +
            cubeCamera.transform.up *
            drag.y;


        // ==================================================
        // 2.
        // BEWEGUNG AUF DIE ANGEKLICKTE FLÄCHE
        // PROJIZIEREN
        // ==================================================

        Vector3 surfaceDrag =
            Vector3.ProjectOnPlane(
                worldDrag,
                normal
            );


        if (surfaceDrag.sqrMagnitude < 0.001f)
        {
            return false;
        }


        surfaceDrag.Normalize();


        // ==================================================
        // 3.
        // ROTATIONSACHSE BESTIMMEN
        //
        // normal × Bewegungsrichtung
        //
        // Dadurch ergibt sich die Achse, um die der
        // angeklickte Cubie bewegt werden muss.
        // ==================================================

        Vector3 rotationAxis =
            Vector3.Cross(
                normal,
                surfaceDrag
            );


        rotationAxis.Normalize();


        // ==================================================
        // 4.
        // STÄRKSTE WELTACHSE ERMITTELN
        // ==================================================

        float absX =
            Mathf.Abs(rotationAxis.x);

        float absY =
            Mathf.Abs(rotationAxis.y);

        float absZ =
            Mathf.Abs(rotationAxis.z);


        RotationAxis axis;

        int direction;


        if (
            absX >= absY &&
            absX >= absZ
        )
        {
            axis =
                RotationAxis.X;

            direction =
                rotationAxis.x >= 0
                    ? 1
                    : -1;
        }
        else if (
            absY >= absX &&
            absY >= absZ
        )
        {
            axis =
                RotationAxis.Y;

            direction =
                rotationAxis.y >= 0
                    ? 1
                    : -1;
        }
        else
        {
            axis =
                RotationAxis.Z;

            direction =
                rotationAxis.z >= 0
                    ? 1
                    : -1;
        }


        // ==================================================
        // 5.
        // SCHICHT BESTIMMEN
        // ==================================================

        int layer = 0;


        switch (axis)
        {
            case RotationAxis.X:

                layer =
                    cubie.logicalPosition.x;

                break;


            case RotationAxis.Y:

                layer =
                    cubie.logicalPosition.y;

                break;


            case RotationAxis.Z:

                layer =
                    cubie.logicalPosition.z;

                break;
        }


        // ==================================================
        // 6.
        // MOVE ERZEUGEN
        // ==================================================

        move =
            new CubeMove(
                axis,
                layer,
                direction
            );


        return true;
    }
}
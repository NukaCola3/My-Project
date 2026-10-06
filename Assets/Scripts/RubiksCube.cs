using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


// ==================================================
// ROTATIONSACHSE
// ==================================================

public enum RotationAxis
{
    X,
    Y,
    Z
}


// ==================================================
// RUBIKS CUBE
// ==================================================

public class RubiksCube : MonoBehaviour
{
    // ==================================================
    // CUBIES
    // ==================================================

    public List<Cubie> cubies =
        new List<Cubie>();

    public CubeState cubeState = new CubeState();


    // ==================================================
    // TESTER
    // ==================================================

    public bool IsCurrentlyRotating()
    {
        return isRotating;
    }


    // ==================================================
    // EINSTELLUNGEN
    // ==================================================

    [Header("Rotation")]
    public float rotationSpeed = 180f;

    [Header("Scramble")]
    public int scrambleLength = 20;


    // ==================================================
    // ZUGVERLAUF
    // ==================================================

    private List<CubeMove> moveHistory =
        new List<CubeMove>();

    private List<CubeMove> redoHistory =
        new List<CubeMove>();


    // ==================================================
    // INTERNE VARIABLEN
    // ==================================================

    private bool isRotating = false;

    private GameObject rotationPivot;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        FindCubies();

        ValidateCube();

        cubeState.Build(cubies);

        cubeState.Print();
    }


    // ==================================================
    // UPDATE
    // ==================================================

    private void Update()
    {
        if (isRotating)
        {
            return;
        }


        // ==================================================
        // U - OBERE EBENE
        // ==================================================

        if (Keyboard.current.uKey.wasPressedThisFrame)
        {
            MakeMove(
                new CubeMove(
                    RotationAxis.Y,
                    1,
                    1
                )
            );
        }


        // ==================================================
        // D - UNTERE EBENE
        // ==================================================

        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            MakeMove(
                new CubeMove(
                    RotationAxis.Y,
                    -1,
                    -1
                )
            );
        }


        // ==================================================
        // R - RECHTE EBENE
        // ==================================================

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            MakeMove(
                new CubeMove(
                    RotationAxis.X,
                    1,
                    1
                )
            );
        }


        // ==================================================
        // L - LINKE EBENE
        // ==================================================

        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            MakeMove(
                new CubeMove(
                    RotationAxis.X,
                    -1,
                    -1
                )
            );
        }


        // ==================================================
        // F - VORDERE EBENE
        // ==================================================

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            MakeMove(
                new CubeMove(
                    RotationAxis.Z,
                    1,
                    -1
                )
            );
        }


        // ==================================================
        // B - HINTERE EBENE
        // ==================================================

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            MakeMove(
                new CubeMove(
                    RotationAxis.Z,
                    -1,
                    1
                )
            );
        }


        // ==================================================
        // SCRAMBLE
        // ==================================================

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            StartScramble();
        }


        // ==================================================
        // UNDO
        // ==================================================

        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            UndoMove();
        }


        // ==================================================
        // REDO
        // ==================================================

        if (Keyboard.current.yKey.wasPressedThisFrame)
        {
            RedoMove();
        }


        // ==================================================
        // DEBUG - CUBE STATE
        // ==================================================

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            cubeState.Build(cubies);
            cubeState.Print();
        }


        // ==================================================
        // GELÖSTEN ZUSTAND PRÜFEN
        // ==================================================

        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            CheckSolved();
        }
    }


    // ==================================================
    // NORMALEN ZUG AUSFÜHREN
    // ==================================================

    private void MakeMove(CubeMove move)
    {
        if (isRotating)
        {
            return;
        }

        moveHistory.Add(move);

        redoHistory.Clear();

        StartLayerRotation(
            move.axis,
            move.layer,
            move.direction
        );
    }


    // ==================================================
    // INPUT MOVE
    // ==================================================

    public void ExecuteInputMove(CubeMove move)
    {
        MakeMove(move);
    }


    // ==================================================
    // UNDO
    // ==================================================

    private void UndoMove()
    {
        if (isRotating)
        {
            return;
        }

        if (moveHistory.Count == 0)
        {
            Debug.Log(
                "Keine Züge zum Rückgängigmachen vorhanden."
            );

            return;
        }

        CubeMove lastMove =
            moveHistory[
                moveHistory.Count - 1
            ];

        moveHistory.RemoveAt(
            moveHistory.Count - 1
        );

        redoHistory.Add(
            lastMove
        );

        CubeMove undoMove =
            new CubeMove(
                lastMove.axis,
                lastMove.layer,
                -lastMove.direction
            );

        Debug.Log(
            "Undo: " +
            MoveToString(lastMove) +
            " -> " +
            MoveToString(undoMove)
        );

        StartLayerRotation(
            undoMove.axis,
            undoMove.layer,
            undoMove.direction
        );
    }


    // ==================================================
    // REDO
    // ==================================================

    private void RedoMove()
    {
        if (isRotating)
        {
            return;
        }

        if (redoHistory.Count == 0)
        {
            Debug.Log(
                "Keine Züge zum Wiederholen vorhanden."
            );

            return;
        }

        CubeMove move =
            redoHistory[
                redoHistory.Count - 1
            ];

        redoHistory.RemoveAt(
            redoHistory.Count - 1
        );

        moveHistory.Add(
            move
        );

        Debug.Log(
            "Redo: " +
            MoveToString(move)
        );

        StartLayerRotation(
            move.axis,
            move.layer,
            move.direction
        );
    }


    // ==================================================
    // SCRAMBLE STARTEN
    // ==================================================

    private void StartScramble()
    {
        if (isRotating)
        {
            return;
        }

        StartCoroutine(
            ScrambleCoroutine()
        );
    }


    // ==================================================
    // SCRAMBLE
    // ==================================================

    private IEnumerator ScrambleCoroutine()
    {
        Debug.Log(
            "========== SCRAMBLE =========="
        );

        redoHistory.Clear();

        List<CubeMove> scrambleMoves =
            new List<CubeMove>();

        RotationAxis lastAxis =
            RotationAxis.X;

        bool hasLastAxis = false;

        for (int i = 0; i < scrambleLength; i++)
        {
            CubeMove move;

            do
            {
                move =
                    GenerateRandomMove();
            }
            while (
                hasLastAxis &&
                move.axis == lastAxis
            );

            scrambleMoves.Add(move);

            lastAxis = move.axis;

            hasLastAxis = true;
        }


        string scrambleText = "";

        foreach (CubeMove move in scrambleMoves)
        {
            scrambleText +=
                MoveToString(move) +
                " ";
        }

        Debug.Log(
            "Scramble: " +
            scrambleText
        );


        foreach (CubeMove move in scrambleMoves)
        {
            moveHistory.Add(move);

            StartLayerRotation(
                move.axis,
                move.layer,
                move.direction
            );

            while (isRotating)
            {
                yield return null;
            }

            yield return null;
        }

        Debug.Log(
            "========== SCRAMBLE FERTIG =========="
        );
    }


    // ==================================================
    // ZUFÄLLIGEN ZUG ERZEUGEN
    // ==================================================

    private CubeMove GenerateRandomMove()
    {
        RotationAxis axis =
            (RotationAxis)
            Random.Range(
                0,
                3
            );

        int layer;

        if (Random.value < 0.5f)
        {
            layer = -1;
        }
        else
        {
            layer = 1;
        }

        int direction =
            Random.value < 0.5f
                ? -1
                : 1;

        return new CubeMove(
            axis,
            layer,
            direction
        );
    }


    // ==================================================
    // ZUG -> STRING
    // ==================================================

    private string MoveToString(
        CubeMove move)
    {
        string moveName = "";

        if (move.axis ==
            RotationAxis.X)
        {
            if (move.layer == 1)
            {
                moveName = "R";
            }
            else
            {
                moveName = "L";
            }
        }

        else if (move.axis ==
                 RotationAxis.Y)
        {
            if (move.layer == 1)
            {
                moveName = "U";
            }
            else
            {
                moveName = "D";
            }
        }

        else if (move.axis ==
                 RotationAxis.Z)
        {
            if (move.layer == 1)
            {
                moveName = "F";
            }
            else
            {
                moveName = "B";
            }
        }

        if (move.direction == -1)
        {
            moveName += "'";
        }

        return moveName;
    }


    // ==================================================
    // CUBIES FINDEN
    // ==================================================

    private void FindCubies()
    {
        cubies.Clear();

        Cubie[] foundCubies =
            GetComponentsInChildren<Cubie>();

        foreach (Cubie cubie in foundCubies)
        {
            cubies.Add(cubie);
        }

        Debug.Log(
            "Gefundene Cubies: " +
            cubies.Count
        );
    }


    // ==================================================
    // WÜRFEL ÜBERPRÜFEN
    // ==================================================

    private void ValidateCube()
    {
        if (cubies.Count != 26)
        {
            Debug.LogWarning(
                "Der Rubik's Cube sollte 26 Cubies besitzen. " +
                "Gefunden: " +
                cubies.Count
            );

            return;
        }

        Debug.Log(
            "Rubik's Cube erfolgreich erkannt."
        );
    }


    // ==================================================
    // EBENEN-DREHUNG STARTEN
    // ==================================================

    private void StartLayerRotation(
        RotationAxis axis,
        int layer,
        int direction)
    {
        List<Cubie> rotatingCubies =
            new List<Cubie>();


        // --------------------------------------------------
        // CUBIES DER EBENE FINDEN
        // --------------------------------------------------

        foreach (Cubie cubie in cubies)
        {
            Vector3Int position =
                cubie.logicalPosition;

            bool belongsToLayer = false;

            switch (axis)
            {
                case RotationAxis.X:

                    belongsToLayer =
                        position.x == layer;

                    break;


                case RotationAxis.Y:

                    belongsToLayer =
                        position.y == layer;

                    break;


                case RotationAxis.Z:

                    belongsToLayer =
                        position.z == layer;

                    break;
            }

            if (belongsToLayer)
            {
                rotatingCubies.Add(cubie);
            }
        }


        // --------------------------------------------------
        // SICHERHEITSPRÜFUNG
        // --------------------------------------------------

        if (
            rotatingCubies.Count != 8 &&
            rotatingCubies.Count != 9
        )
        {
            Debug.LogWarning(
                "Es wurden " +
                rotatingCubies.Count +
                " Cubies gefunden. " +
                "Erwartet werden 8 oder 9."
            );

            return;
        }


        isRotating = true;


        // --------------------------------------------------
        // PIVOT ERZEUGEN
        // --------------------------------------------------

        rotationPivot =
            new GameObject(
                "RotationPivot"
            );

        rotationPivot.transform.SetParent(
            transform,
            false
        );

        rotationPivot.transform.localPosition =
            Vector3.zero;

        rotationPivot.transform.localRotation =
            Quaternion.identity;

        rotationPivot.transform.localScale =
            Vector3.one;


        // --------------------------------------------------
        // CUBIES AN PIVOT HÄNGEN
        // --------------------------------------------------

        foreach (Cubie cubie in rotatingCubies)
        {
            cubie.transform.SetParent(
                rotationPivot.transform,
                true
            );
        }


        // --------------------------------------------------
        // DREHACHSE BESTIMMEN
        // --------------------------------------------------

        Vector3 rotationAxis =
            Vector3.zero;

        switch (axis)
        {
            case RotationAxis.X:

                rotationAxis =
                    Vector3.right;

                break;


            case RotationAxis.Y:

                rotationAxis =
                    Vector3.up;

                break;


            case RotationAxis.Z:

                rotationAxis =
                    Vector3.forward;

                break;
        }


        StartCoroutine(
            AnimateRotation(
                rotatingCubies,
                axis,
                direction,
                rotationAxis
            )
        );
    }


    // ==================================================
    // DREHUNG ANIMIEREN
    // ==================================================

    private IEnumerator AnimateRotation(
        List<Cubie> rotatingCubies,
        RotationAxis axis,
        int direction,
        Vector3 rotationAxis)
    {
        float rotated = 0f;

        while (rotated < 90f)
        {
            float rotationThisFrame =
                rotationSpeed *
                Time.deltaTime;

            float remaining =
                90f - rotated;

            rotationThisFrame =
                Mathf.Min(
                    rotationThisFrame,
                    remaining
                );

            rotationPivot.transform.Rotate(
                rotationAxis,
                rotationThisFrame * direction,
                Space.Self
            );

            rotated += rotationThisFrame;

            yield return null;
        }


        // --------------------------------------------------
        // LOGISCHEN ZUSTAND AKTUALISIEREN
        // --------------------------------------------------

        UpdateLogicalState(
            rotatingCubies,
            axis,
            direction
        );


        // --------------------------------------------------
        // CUBIES WIEDER UNTER RUBIKSCUBE HÄNGEN
        // --------------------------------------------------

        foreach (Cubie cubie in rotatingCubies)
        {
            cubie.transform.SetParent(
                transform,
                false
            );

            cubie.ApplyLogicalPosition();

            cubie.ApplyLogicalRotation();

            cubie.UpdateOrientation();
        }


        Destroy(
            rotationPivot
        );

        rotationPivot = null;

        isRotating = false;
    }


    // ==================================================
    // LOGISCHEN ZUSTAND AKTUALISIEREN
    // ==================================================

    private void UpdateLogicalState(
        List<Cubie> rotatingCubies,
        RotationAxis axis,
        int direction)
    {
        Vector3 rotationAxis =
            Vector3.zero;


        // --------------------------------------------------
        // DREHACHSE BESTIMMEN
        // --------------------------------------------------

        switch (axis)
        {
            case RotationAxis.X:

                rotationAxis =
                    Vector3.right;

                break;


            case RotationAxis.Y:

                rotationAxis =
                    Vector3.up;

                break;


            case RotationAxis.Z:

                rotationAxis =
                    Vector3.forward;

                break;
        }


        // --------------------------------------------------
        // JEDEN CUBIE AKTUALISIEREN
        // --------------------------------------------------

        foreach (Cubie cubie in rotatingCubies)
        {
            Vector3Int oldPosition =
                cubie.logicalPosition;


            // ==================================================
            // ORIENTATION INKREMENTELL AKTUALISIEREN
            // ==================================================
            //
            // WICHTIG:
            // oldPosition muss hier noch die Position VOR
            // dem Zug enthalten.
            //
            // Corner:
            // benötigt Achse, Richtung und alte Position.
            //
            // Edge:
            // benötigt für unsere Konvention nur die Achse.
            // Da rotatingCubies ausschließlich Cubies der
            // gedrehten Ebene enthält, flippen bei Z-Zügen
            // automatisch nur die vier betroffenen Edges.
            // ==================================================


            int x =
                oldPosition.x;

            int y =
                oldPosition.y;

            int z =
                oldPosition.z;


            // ==========================================
            // X-ACHSE
            // ==========================================

            if (axis == RotationAxis.X)
            {
                if (direction == 1)
                {
                    y = -oldPosition.z;
                    z = oldPosition.y;
                }
                else
                {
                    y = oldPosition.z;
                    z = -oldPosition.y;
                }
            }


            // ==========================================
            // Y-ACHSE
            // ==========================================

            else if (axis == RotationAxis.Y)
            {
                if (direction == 1)
                {
                    x = oldPosition.z;
                    z = -oldPosition.x;
                }
                else
                {
                    x = -oldPosition.z;
                    z = oldPosition.x;
                }
            }


            // ==========================================
            // Z-ACHSE
            // ==========================================

            else if (axis == RotationAxis.Z)
            {
                if (direction == 1)
                {
                    x = -oldPosition.y;
                    y = oldPosition.x;
                }
                else
                {
                    x = oldPosition.y;
                    y = -oldPosition.x;
                }
            }


            // ------------------------------------------
            // NEUE POSITION
            // ------------------------------------------

            cubie.logicalPosition =
                new Vector3Int(
                    x,
                    y,
                    z
                );


            // ------------------------------------------
            // ROTATION
            // ------------------------------------------

            cubie.UpdateLogicalRotation(
                rotationAxis,
                direction
            );


            // ------------------------------------------
            // STICKER
            // ------------------------------------------

            cubie.RotateStickers(
                axis,
                direction
            );


            // ------------------------------------------
            // CORNER-ORIENTATION
            // ------------------------------------------
            //
            // oldPosition ist bewusst die Position VOR dem Zug.
            //
            if (cubie.Type == CubieType.Corner)
            {
                cubie.UpdateCornerOrientationForMove(
                    axis,
                    direction,
                    oldPosition
                );
            }


            // ------------------------------------------
            // EDGE-ORIENTATION
            // ------------------------------------------
            //
            // Corners werden weiterhin nach der Rotation
            // über UpdateOrientation() aus ihren Stickern
            // bestimmt.
            //
            // Edges verwenden die Solver-Konvention:
            // F/B (Z-Achse) flippt, U/D/R/L nicht.
            //
            if (cubie.Type == CubieType.Edge)
            {
                cubie.UpdateEdgeOrientationForMove(
                    axis
                );
            }
        }
    }


    // ==================================================
    // DEBUG: CUBE STATE AUSGEBEN
    // ==================================================

    private void PrintCubeState()
    {
        Debug.Log(
            "========== CUBE STATE =========="
        );

        foreach (Cubie cubie in cubies)
        {
            Debug.Log(
                cubie.name +
                " | " +
                cubie.GetState()
            );
        }

        Debug.Log(
            "================================"
        );
    }


    // ==================================================
    // GELÖSTEN ZUSTAND PRÜFEN
    // ==================================================

    public bool IsSolved()
    {
        foreach (Cubie cubie in cubies)
        {
            if (cubie.logicalPosition !=
                cubie.originalPosition)
            {
                return false;
            }

            foreach (CubieSticker sticker in cubie.stickers)
            {
                if (sticker.currentDirection !=
                    sticker.originalDirection)
                {
                    return false;
                }
            }
        }

        return true;
    }


    // ==================================================
    // GELÖSTEN ZUSTAND AUSGEBEN
    // ==================================================

    private void CheckSolved()
    {
        if (IsSolved())
        {
            Debug.Log(
                "================================"
            );

            Debug.Log(
                "          CUBE GELÖST!           "
            );

            Debug.Log(
                "================================"
            );
        }
        else
        {
            Debug.Log(
                "Cube ist NICHT gelöst."
            );
        }
    }
}
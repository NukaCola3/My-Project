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
    // Tester erweitert
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

    // Bereits ausgeführte Züge
    private List<CubeMove> moveHistory =
        new List<CubeMove>();


    // Rückgängig gemachte Züge
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
        // Während einer Drehung keine neue
        // Bewegung zulassen.
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


        // --------------------------------------------------
        // ZUG SPEICHERN
        // --------------------------------------------------

        moveHistory.Add(move);


        // --------------------------------------------------
        // REDO-HISTORY LÖSCHEN
        // --------------------------------------------------

        // Sobald ein neuer Zug gemacht wird,
        // nachdem man einen Undo ausgeführt hat,
        // ist die bisherige Redo-History ungültig!!!

        redoHistory.Clear();


        // --------------------------------------------------
        // DREHUNG STARTEN
        // --------------------------------------------------

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
        // --------------------------------------------------
        // PRÜFEN, OB BEREITS EINE DREHUNG LÄUFT
        // --------------------------------------------------

        if (isRotating)
        {
            return;
        }


        // --------------------------------------------------
        // PRÜFEN, OB ES ZÜGE GIBT
        // --------------------------------------------------

        if (moveHistory.Count == 0)
        {
            Debug.Log(
                "Keine Züge zum Rückgängigmachen vorhanden."
            );

            return;
        }


        // --------------------------------------------------
        // LETZTEN ZUG HOLEN
        // --------------------------------------------------

        CubeMove lastMove =
            moveHistory[
                moveHistory.Count - 1
            ];


        // --------------------------------------------------
        // AUS NORMALER HISTORY ENTFERNEN
        // --------------------------------------------------

        moveHistory.RemoveAt(
            moveHistory.Count - 1
        );


        // --------------------------------------------------
        // IN REDO-HISTORY SPEICHERN
        // --------------------------------------------------

        redoHistory.Add(
            lastMove
        );


        // --------------------------------------------------
        // GEGENZUG ERZEUGEN
        // --------------------------------------------------

        CubeMove undoMove =
            new CubeMove(
                lastMove.axis,
                lastMove.layer,
                -lastMove.direction
            );


        // --------------------------------------------------
        // DEBUG
        // --------------------------------------------------

        Debug.Log(
            "Undo: " +
            MoveToString(lastMove) +
            " -> " +
            MoveToString(undoMove)
        );


        // --------------------------------------------------
        // GEGENZUG AUSFÜHREN
        // --------------------------------------------------

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
        // --------------------------------------------------
        // PRÜFEN, OB BEREITS EINE DREHUNG LÄUFT
        // --------------------------------------------------

        if (isRotating)
        {
            return;
        }


        // --------------------------------------------------
        // PRÜFEN, OB ES REDO-ZÜGE GIBT
        // --------------------------------------------------

        if (redoHistory.Count == 0)
        {
            Debug.Log(
                "Keine Züge zum Wiederholen vorhanden."
            );

            return;
        }


        // --------------------------------------------------
        // LETZTEN UNDO-ZUG HOLEN
        // --------------------------------------------------

        CubeMove move =
            redoHistory[
                redoHistory.Count - 1
            ];


        // --------------------------------------------------
        // AUS REDO-HISTORY ENTFERNEN
        // --------------------------------------------------

        redoHistory.RemoveAt(
            redoHistory.Count - 1
        );


        // --------------------------------------------------
        // WIEDER IN NORMALE HISTORY
        // --------------------------------------------------

        moveHistory.Add(
            move
        );


        // --------------------------------------------------
        // DEBUG
        // --------------------------------------------------

        Debug.Log(
            "Redo: " +
            MoveToString(move)
        );


        // --------------------------------------------------
        // ORIGINALEN ZUG ERNEUT AUSFÜHREN
        // --------------------------------------------------

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


        // --------------------------------------------------
        // ALTE REDO-HISTORY LÖSCHEN
        // --------------------------------------------------

        redoHistory.Clear();


        List<CubeMove> scrambleMoves =
            new List<CubeMove>();


        RotationAxis lastAxis =
            RotationAxis.X;


        bool hasLastAxis = false;


        // --------------------------------------------------
        // ZÜGE ERZEUGEN
        // --------------------------------------------------

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


        // --------------------------------------------------
        // SCRAMBLE AUSGEBEN
        // --------------------------------------------------

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


        // --------------------------------------------------
        // ZÜGE AUSFÜHREN
        // --------------------------------------------------

        foreach (CubeMove move in scrambleMoves)
        {
            // Zug speichern
            moveHistory.Add(move);


            // Bewegung starten
            StartLayerRotation(
                move.axis,
                move.layer,
                move.direction
            );


            // Warten, bis die Animation
            // abgeschlossen ist.
            while (isRotating)
            {
                yield return null;
            }


            // Ein Frame Pause
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


        // --------------------------------------------------
        // NUR AUSSENEBENEN
        // --------------------------------------------------

        int layer;

        if (Random.value < 0.5f)
        {
            layer = -1;
        }
        else
        {
            layer = 1;
        }


        // --------------------------------------------------
        // RICHTUNG
        // --------------------------------------------------

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


        // --------------------------------------------------
        // X-ACHSE
        // --------------------------------------------------

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


        // --------------------------------------------------
        // Y-ACHSE
        // --------------------------------------------------

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


        // --------------------------------------------------
        // Z-ACHSE
        // --------------------------------------------------

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


        // --------------------------------------------------
        // RICHTUNG
        // --------------------------------------------------

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


        // --------------------------------------------------
        // DREHUNG STARTEN
        // --------------------------------------------------

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


        // --------------------------------------------------
        // ANIMATION STARTEN
        // --------------------------------------------------

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


        // --------------------------------------------------
        // 90 GRAD ANIMIEREN
        // --------------------------------------------------

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


        // --------------------------------------------------
        // PIVOT LÖSCHEN
        // --------------------------------------------------

        Destroy(
            rotationPivot
        );

        rotationPivot = null;


        // --------------------------------------------------
        // NEUE DREHUNG ERLAUBEN
        // --------------------------------------------------

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
            // ------------------------------------------
            // POSITION PRÜFEN
            // ------------------------------------------

            if (cubie.logicalPosition !=
                cubie.originalPosition)
            {
                return false;
            }


            // ------------------------------------------
            // STICKER-ORIENTIERUNG PRÜFEN
            // ------------------------------------------

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
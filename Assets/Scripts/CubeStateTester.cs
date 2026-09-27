using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeStateTester : MonoBehaviour
{
    [Header("Referenz")]
    public RubiksCube rubiksCube;

    [Header("Test")]
    public float delayBetweenMoves = 0.5f;


    // ==================================================
    // START
    // ==================================================

    private void Start()
    {
        if (rubiksCube == null)
        {
            rubiksCube =
                FindFirstObjectByType<RubiksCube>();
        }

        if (rubiksCube == null)
        {
            Debug.LogError(
                "CubeStateTester: Kein RubiksCube gefunden!"
            );

            return;
        }

        StartCoroutine(
            RunOrientationTest()
        );
    }


    // ==================================================
    // HAUPTTEST
    // ==================================================

    private IEnumerator RunOrientationTest()
    {
        Debug.Log("");
        Debug.Log("================================================");
        Debug.Log("       CORNER ORIENTATION DIAGNOSE START");
        Debug.Log("================================================");


        // ==================================================
        // TEST 1
        // ==================================================

        Debug.Log("");
        Debug.Log("############################");
        Debug.Log("# TEST 1: SOLVED");
        Debug.Log("############################");

        PrintCornerState(
            "SOLVED"
        );

        yield return new WaitForSeconds(
            delayBetweenMoves
        );
        

        // ==================================================
        // TEST 2: R
        // ==================================================
        Debug.Log("");
        Debug.Log("############################");
        Debug.Log("# TEST 2: R U");
        Debug.Log("############################");

        yield return ExecuteAndWait(
            RotationAxis.X,
            1,
            1,
            "R"
        );

        PrintCornerState(
            "NACH R"
        );

        yield return ExecuteAndWait(
            RotationAxis.Y,
            1,
            1,
            "U"
        );

        PrintCornerState(
            "NACH U"
        );

        yield return new WaitForSeconds(
            delayBetweenMoves
        );

/*
        // ==================================================
        // TEST 3: R'
        // ==================================================

        Debug.Log("");
        Debug.Log("############################");
        Debug.Log("# TEST 3: R'");
        Debug.Log("############################");

        yield return ExecuteAndWait(
            RotationAxis.X,
            1,
            -1,
            "R'"
        );

        PrintCornerState(
            "NACH R'"
        );

        yield return new WaitForSeconds(
            delayBetweenMoves
        );

        // ==================================================
        // TEST 4: F
        // ==================================================

        Debug.Log("");
        Debug.Log("############################");
        Debug.Log("# TEST 4: F");
        Debug.Log("############################");

        yield return ExecuteAndWait(
            RotationAxis.Z,
            1,
            -1,
            "F"
        );

        PrintCornerState(
            "NACH F"
        );

        yield return new WaitForSeconds(
            delayBetweenMoves
        );


        // ==================================================
        // TEST 5: F'
        // ==================================================

        Debug.Log("");
        Debug.Log("############################");
        Debug.Log("# TEST 5: F'");
        Debug.Log("############################");

        yield return ExecuteAndWait(
            RotationAxis.Z,
            1,
            1,
            "F'"
        );

        PrintCornerState(
            "NACH F'"
        );

        yield return new WaitForSeconds(
            delayBetweenMoves
        );


        
        // ==================================================
        // TEST 6
        // ==================================================

        Debug.Log("");
        Debug.Log("############################");
        Debug.Log("# TEST 6: R U R' U'");
        Debug.Log("############################");

        Debug.Log(
            "Führe Sequenz aus: R U R' U'"
        );


        yield return ExecuteAndWait(
            RotationAxis.X,
            1,
            1,
            "R"
        );

        PrintCornerState(
            "NACH R"
        );


        yield return ExecuteAndWait(
            RotationAxis.Y,
            1,
            1,
            "U"
        );

        PrintCornerState(
            "NACH R U"
        );


        yield return ExecuteAndWait(
            RotationAxis.X,
            1,
            -1,
            "R'"
        );

        PrintCornerState(
            "NACH R U R'"
        );


        yield return ExecuteAndWait(
            RotationAxis.Y,
            1,
            -1,
            "U'"
        );

        PrintCornerState(
            "NACH R U R' U'"
        );


        yield return new WaitForSeconds(
            delayBetweenMoves
        );

        // ==================================================
        // TEST 7
        // ==================================================

        Debug.Log("");
        Debug.Log("############################");
        Debug.Log("# TEST 7: (R U R' U') x 2");
        Debug.Log("############################");

        Debug.Log(
            "Führe Sequenz aus:"
        );

        Debug.Log(
            "R U R' U' R U R' U'"
        );


        yield return ExecuteAndWait(
            RotationAxis.X,
            1,
            1,
            "R"
        );

        PrintCornerState(
            "NACH R"
        );


        yield return ExecuteAndWait(
            RotationAxis.Y,
            1,
            1,
            "U"
        );

        PrintCornerState(
            "NACH R U"
        );


        yield return ExecuteAndWait(
            RotationAxis.X,
            1,
            -1,
            "R'"
        );

        PrintCornerState(
            "NACH R U R'"
        );


        yield return ExecuteAndWait(
            RotationAxis.Y,
            1,
            -1,
            "U'"
        );

        PrintCornerState(
            "NACH R U R' U'"
        );


        yield return ExecuteAndWait(
            RotationAxis.X,
            1,
            1,
            "R"
        );

        PrintCornerState(
            "NACH R U R' U' R"
        );


        yield return ExecuteAndWait(
            RotationAxis.Y,
            1,
            1,
            "U"
        );

        PrintCornerState(
            "NACH R U R' U' R U"
        );


        yield return ExecuteAndWait(
            RotationAxis.X,
            1,
            -1,
            "R'"
        );

        PrintCornerState(
            "NACH R U R' U' R U R'"
        );


        yield return ExecuteAndWait(
            RotationAxis.Y,
            1,
            -1,
            "U'"
        );

        PrintCornerState(
            "NACH (R U R' U') x 2"
        );


*/        
        Debug.Log("");
        Debug.Log("================================================");
        Debug.Log("        CORNER ORIENTATION DIAGNOSE ENDE");
        Debug.Log("================================================");
    }


    // ==================================================
    // ZUG AUSFÜHREN UND AUF ABSCHLUSS WARTEN
    // ==================================================

    private IEnumerator ExecuteAndWait(
        RotationAxis axis,
        int layer,
        int direction,
        string moveName
    )
    {
        Debug.Log(
            ">>> Starte Zug: " + moveName
        );

        CubeMove move =
            new CubeMove(
                axis,
                layer,
                direction
            );

        rubiksCube.ExecuteInputMove(
            move
        );


        // --------------------------------------------------
        // WARTEN, BIS DIE ROTATION BEGINNT
        // --------------------------------------------------

        while (
            !rubiksCube.IsCurrentlyRotating()
        )
        {
            yield return null;
        }


        Debug.Log(
            "    Rotation läuft: " + moveName
        );


        // --------------------------------------------------
        // WARTEN, BIS DIE ROTATION FERTIG IST
        // --------------------------------------------------

        while (
            rubiksCube.IsCurrentlyRotating()
        )
        {
            yield return null;
        }


        Debug.Log(
            "<<< Zug fertig: " + moveName
        );


        // Einen zusätzlichen Frame warten,
        // damit Unity alle Zustandsänderungen
        // verarbeitet hat.

        yield return null;
    }


    // ==================================================
    // CORNER STATE AUSGEBEN
    // ==================================================

    private void PrintCornerState(
        string testName
    )
    {
        if (rubiksCube == null)
        {
            Debug.LogError(
                "CubeStateTester: RubiksCube fehlt!"
            );

            return;
        }


        List<Cubie> corners =
            new List<Cubie>();


        foreach (Cubie cubie in rubiksCube.cubies)
        {
            if (cubie == null)
                continue;

            if (
                cubie.Type ==
                CubieType.Corner
            )
            {
                corners.Add(cubie);
            }
        }


        corners.Sort(
            (a, b) =>
                string.Compare(
                    a.pieceID,
                    b.pieceID,
                    System.StringComparison.Ordinal
                )
        );


        Debug.Log("");
        Debug.Log("----------------------------------------------");
        Debug.Log(
            "CORNER STATE: " +
            testName
        );
        Debug.Log("----------------------------------------------");


        Debug.Log(
            "PieceID        Position        Orientation"
        );

        Debug.Log(
            "----------------------------------------------"
        );


        int orientationSum = 0;


        foreach (Cubie corner in corners)
        {
            orientationSum +=
                corner.orientation;


            Debug.Log(
                string.Format(
                    "{0,-14} {1,-15} {2}",
                    corner.pieceID,
                    corner.logicalPosition,
                    corner.orientation
                )
            );
        }


        Debug.Log("");
        Debug.Log(
            "Orientierungs-Summe: " +
            orientationSum
        );


        Debug.Log(
            "Orientierungs-Summe MOD 3: " +
            (
                orientationSum % 3
            )
        );


        if (
            orientationSum % 3 == 0
        )
        {
            Debug.Log(
                "OK: Orientierungs-Summe ist durch 3 teilbar."
            );
        }
        else
        {
            Debug.LogWarning(
                "FEHLER: Orientierungs-Summe ist NICHT durch 3 teilbar!"
            );
        }


        Debug.Log("");
        Debug.Log(
            "DETAILLIERTE STICKER-INFORMATION:"
        );


        foreach (Cubie corner in corners)
        {
            PrintCorner(
                corner
            );
        }


        Debug.Log(
            "----------------------------------------------"
        );
    }


    // ==================================================
    // EINZELNEN CORNER AUSGEBEN
    // ==================================================

    private void PrintCorner(
        Cubie corner
    )
    {
        Debug.Log("");
        Debug.Log(
            "===== CORNER " +
            corner.pieceID +
            " ====="
        );


        Debug.Log(
            "Original Position: " +
            corner.originalPosition
        );


        Debug.Log(
            "Aktuelle Position: " +
            corner.logicalPosition
        );


        Debug.Log(
            "Orientation: " +
            corner.orientation
        );


        CubieSticker udSticker =
            null;


        foreach (
            CubieSticker sticker
            in corner.stickers
        )
        {
            if (
                sticker.originalDirection ==
                    FaceDirection.PositiveY
                ||
                sticker.originalDirection ==
                    FaceDirection.NegativeY
            )
            {
                udSticker = sticker;
                break;
            }
        }


        if (udSticker != null)
        {
            Debug.Log(
                "Original U/D Sticker: " +
                udSticker.originalDirection
            );


            Debug.Log(
                "Aktuelle Sticker-Richtung: " +
                udSticker.currentDirection
            );
        }
        else
        {
            Debug.LogWarning(
                "Keine U/D-Sticker gefunden!"
            );
        }


        Debug.Log(
            "Sticker:"
        );


        foreach (
            CubieSticker sticker
            in corner.stickers
        )
        {
            Debug.Log(
                "   " +
                sticker.originalDirection +
                " -> " +
                sticker.currentDirection
            );
        }
    }


    // ==================================================
    // CONTEXT MENU: R
    // ==================================================

    [ContextMenu("Test R")]
    public void TestR()
    {
        if (rubiksCube == null)
        {
            rubiksCube =
                FindFirstObjectByType<RubiksCube>();
        }


        if (
            !rubiksCube.IsCurrentlyRotating()
        )
        {
            rubiksCube.ExecuteInputMove(
                new CubeMove(
                    RotationAxis.X,
                    1,
                    1
                )
            );
        }
    }


    // ==================================================
    // CONTEXT MENU: R'
    // ==================================================

    [ContextMenu("Test R Prime")]
    public void TestRPrime()
    {
        if (rubiksCube == null)
        {
            rubiksCube =
                FindFirstObjectByType<RubiksCube>();
        }


        if (
            !rubiksCube.IsCurrentlyRotating()
        )
        {
            rubiksCube.ExecuteInputMove(
                new CubeMove(
                    RotationAxis.X,
                    1,
                    -1
                )
            );
        }
    }


    // ==================================================
    // CONTEXT MENU: F
    // ==================================================

    [ContextMenu("Test F")]
    public void TestF()
    {
        if (rubiksCube == null)
        {
            rubiksCube =
                FindFirstObjectByType<RubiksCube>();
        }


        if (
            !rubiksCube.IsCurrentlyRotating()
        )
        {
            rubiksCube.ExecuteInputMove(
                new CubeMove(
                    RotationAxis.Z,
                    1,
                    -1
                )
            );
        }
    }


    // ==================================================
    // CONTEXT MENU: F'
    // ==================================================

    [ContextMenu("Test F Prime")]
    public void TestFPrime()
    {
        if (rubiksCube == null)
        {
            rubiksCube =
                FindFirstObjectByType<RubiksCube>();
        }


        if (
            !rubiksCube.IsCurrentlyRotating()
        )
        {
            rubiksCube.ExecuteInputMove(
                new CubeMove(
                    RotationAxis.Z,
                    1,
                    1
                )
            );
        }
    }


    // ==================================================
    // MANUELLER STATE-TEST
    // ==================================================

    [ContextMenu("Corner State ausgeben")]
    public void PrintCurrentCorners()
    {
        if (rubiksCube == null)
        {
            rubiksCube =
                FindFirstObjectByType<RubiksCube>();
        }


        if (rubiksCube == null)
        {
            Debug.LogError(
                "CubeStateTester: RubiksCube fehlt."
            );

            return;
        }


        PrintCornerState(
            "MANUELLER TEST"
        );
    }
}
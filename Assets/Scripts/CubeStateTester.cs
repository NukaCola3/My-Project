using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// ==========================================================
// CUBE STATE TESTER
// ==========================================================
//
// Testet:
//
// 1. Corner-/Edge-Orientation
// 2. Solver-State-Struktur
// 3. Kopieren Unity -> SolverState
// 4. SolverState-Move-Simulation
//
// Beim Paralleltest gilt:
//
// Unity führt einen Move aus.
// SolverState führt denselben Move unabhängig aus.
// Danach werden beide Zustände verglichen.
//
// Der SolverState wird NICHT nach jedem Move neu aus Unity
// erzeugt.
// ==========================================================

public class CubeStateTester : MonoBehaviour
{
    // ======================================================
    // REFERENCES
    // ======================================================

    public RubiksCube rubiksCube;


    // ======================================================
    // TEST SETTINGS
    // ======================================================

    [Header("Structured Tests")]

    public string[] testSequences =
    {
        "R R'",
        "L L'",
        "U U'",
        "D D'",
        "F F'",
        "B B'",

        "R U U' R'",
        "F U U' F'",
        "R F F' R'",
        "F R R' F'",

        "R U R' U' U R U' R'",
        "F R U U' R' F'",
        "R U F F' U' R'",
        "R U F L L' F' U' R'",
        "R U F L D B B' D' L' F' U' R'"
    };


    [Header("Timing")]

    public float waitAfterMove = 0.1f;

    [Header("Random Stress Test")]

    public int randomTestMoveCount = 1000;

    public float randomTestWaitAfterMove = 0f;


    // ======================================================
    // SOLVER TEST STATE
    // ======================================================

    private SolverState simulatedSolverState;


    // ======================================================
    // VALID POSITIONS
    // ======================================================

    private readonly HashSet<Vector3Int>
        validCornerPositions =
            new HashSet<Vector3Int>()
            {
                new Vector3Int( 1,  1,  1),
                new Vector3Int( 1,  1, -1),
                new Vector3Int(-1,  1,  1),
                new Vector3Int(-1,  1, -1),

                new Vector3Int( 1, -1,  1),
                new Vector3Int( 1, -1, -1),
                new Vector3Int(-1, -1,  1),
                new Vector3Int(-1, -1, -1)
            };


    private readonly HashSet<Vector3Int>
        validEdgePositions =
            new HashSet<Vector3Int>()
            {
                // U
                new Vector3Int( 0,  1,  1),
                new Vector3Int( 1,  1,  0),
                new Vector3Int( 0,  1, -1),
                new Vector3Int(-1,  1,  0),

                // D
                new Vector3Int( 0, -1,  1),
                new Vector3Int( 1, -1,  0),
                new Vector3Int( 0, -1, -1),
                new Vector3Int(-1, -1,  0),

                // Middle
                new Vector3Int( 1,  0,  1),
                new Vector3Int(-1,  0,  1),
                new Vector3Int( 1,  0, -1),
                new Vector3Int(-1,  0, -1)
            };


    // ======================================================
    // START STRUCTURED TESTS
    // ======================================================

    [ContextMenu("Run Structured Tests")]
    public void RunStructuredTests()
    {
        if (rubiksCube == null)
        {
            Debug.LogError(
                "CubeStateTester: RubiksCube fehlt."
            );

            return;
        }


        if (rubiksCube.cubies == null)
        {
            Debug.LogError(
                "CubeStateTester: Cubie-Liste fehlt."
            );

            return;
        }


        StopAllCoroutines();

        StartCoroutine(
            RunStructuredTestsCoroutine()
        );
    }


    // ======================================================
    // STRUCTURED TEST COROUTINE
    // ======================================================

    private IEnumerator RunStructuredTestsCoroutine()
    {
        Debug.Log(
            "========================================"
        );

        Debug.Log(
            "START SOLVER PARALLELTEST"
        );

        Debug.Log(
            "========================================"
        );


        // ==================================================
        // SolverState wird genau EINMAL aus Unity erzeugt.
        //
        // Danach wird er ausschließlich durch seine eigene
        // ApplyMove()-Methode verändert.
        // ==================================================

        simulatedSolverState =
            new SolverState(
                rubiksCube.cubies
            );


        if (!simulatedSolverState.IsValid())
        {
            Debug.LogError(
                "Initialer SolverState ist ungültig."
            );

            yield break;
        }


        // Ausgangszustand vergleichen
        if (!CompareUnityWithSimulatedSolver(
            "START"
        ))
        {
            Debug.LogError(
                "Ausgangszustand stimmt nicht überein."
            );

            yield break;
        }


        int totalMoveCount = 0;


        // ==================================================
        // TESTSEQUENZEN
        // ==================================================

        for (
            int sequenceIndex = 0;
            sequenceIndex < testSequences.Length;
            sequenceIndex++
        )
        {
            string sequence =
                testSequences[sequenceIndex];


            if (string.IsNullOrWhiteSpace(
                sequence
            ))
            {
                continue;
            }


            Debug.Log(
                "----------------------------------------"
            );

            Debug.Log(
                "TEST " +
                (sequenceIndex + 1) +
                "/" +
                testSequences.Length +
                ": " +
                sequence
            );


            string[] moves =
                sequence.Split(
                    ' ',
                    System.StringSplitOptions
                        .RemoveEmptyEntries
                );


            // ==============================================
            // MOVES DER SEQUENZ
            // ==============================================

            for (
                int moveIndex = 0;
                moveIndex < moves.Length;
                moveIndex++
            )
            {
                string move =
                    moves[moveIndex];


                totalMoveCount++;


                Debug.Log(
                    "Move " +
                    (moveIndex + 1) +
                    "/" +
                    moves.Length +
                    ": " +
                    move
                );


                // ==========================================
                // 1. UNITY FÜHRT MOVE AUS
                // ==========================================

                yield return
                    ExecuteAndWait(move);


                // ==========================================
                // 2. SOLVER FÜHRT DENSELBEN MOVE AUS
                // ==========================================

                bool solverMoveExecuted =
                    simulatedSolverState
                        .ApplyMove(move);


                if (!solverMoveExecuted)
                {
                    Debug.LogError(
                        "SolverState konnte Move nicht " +
                        "ausführen: " +
                        move
                    );

                    yield break;
                }


                // ==========================================
                // 3. UNITY INTERN VALIDIEREN
                // ==========================================

                bool unityStateValid =
                    ValidateState();


                if (!unityStateValid)
                {
                    Debug.LogError(
                        "UNITY-STATE FEHLER"
                    );

                    Debug.LogError(
                        "Sequenz: " +
                        sequence
                    );

                    Debug.LogError(
                        "Move: " +
                        move
                    );

                    Debug.LogError(
                        "Move-Index: " +
                        moveIndex
                    );

                    yield break;
                }


                // ==========================================
                // 4. SOLVER INTERN VALIDIEREN
                // ==========================================

                if (!simulatedSolverState.IsValid())
                {
                    Debug.LogError(
                        "SIMULIERTER SOLVERSTATE UNGÜLTIG"
                    );

                    Debug.LogError(
                        "Sequenz: " +
                        sequence
                    );

                    Debug.LogError(
                        "Move: " +
                        move
                    );

                    Debug.LogError(
                        "Move-Index: " +
                        moveIndex
                    );

                    simulatedSolverState.Print();

                    yield break;
                }


                // ==========================================
                // 5. UNITY UND SOLVER VERGLEICHEN
                // ==========================================

                string context =
                    "Sequenz=\"" +
                    sequence +
                    "\" | Move=" +
                    move +
                    " | Schritt=" +
                    (moveIndex + 1);


                bool statesEqual =
                    CompareUnityWithSimulatedSolver(
                        context
                    );


                if (!statesEqual)
                {
                    Debug.LogError(
                        "PARALLELTEST FEHLGESCHLAGEN"
                    );

                    Debug.LogError(
                        context
                    );

                    yield break;
                }


                Debug.Log(
                    "PARALLELTEST OK: " +
                    context
                );
            }


            Debug.Log(
                "TEST BESTANDEN: " +
                sequence
            );
        }


        // ==================================================
        // ALLES BESTANDEN
        // ==================================================

        Debug.Log(
            "========================================"
        );

        Debug.Log(
            "SOLVER-PARALLELTEST: " +
            "ALLE PRÜFUNGEN BESTANDEN"
        );

        Debug.Log(
            "Getestete Moves insgesamt: " +
            totalMoveCount
        );

        Debug.Log(
            "Unity und SolverState sind nach jedem " +
            "getesteten Move identisch."
        );

        Debug.Log(
            "========================================"
        );
    }


    // ======================================================
    // UNITY <-> SIMULIERTER SOLVER VERGLEICH
    // ======================================================

    private bool CompareUnityWithSimulatedSolver(
        string context)
    {
        if (simulatedSolverState == null)
        {
            Debug.LogError(
                "Simulierter SolverState ist null."
            );

            return false;
        }


        // ==================================================
        // CORNERS
        // ==================================================

        foreach (
            Cubie cubie
            in rubiksCube.cubies
        )
        {
            if (
                cubie == null ||
                cubie.Type != CubieType.Corner
            )
            {
                continue;
            }


            bool found = false;


            foreach (
                SolverPieceState solverCorner
                in simulatedSolverState.corners
            )
            {
                if (
                    solverCorner.pieceID !=
                    cubie.pieceID
                )
                {
                    continue;
                }


                found = true;


                // ==========================================
                // POSITION
                // ==========================================

                if (
                    solverCorner.position !=
                    cubie.logicalPosition
                )
                {
                    Debug.LogError(
                        "CORNER POSITION UNTERSCHIED"
                    );

                    Debug.LogError(
                        "Context: " +
                        context
                    );

                    Debug.LogError(
                        "Piece: " +
                        cubie.pieceID
                    );

                    Debug.LogError(
                        "Unity Position: " +
                        cubie.logicalPosition
                    );

                    Debug.LogError(
                        "Solver Position: " +
                        solverCorner.position
                    );

                    return false;
                }


                // ==========================================
                // ORIENTATION
                // ==========================================

                if (
                    solverCorner.orientation !=
                    cubie.orientation
                )
                {
                    Debug.LogError(
                        "CORNER ORIENTATION UNTERSCHIED"
                    );

                    Debug.LogError(
                        "Context: " +
                        context
                    );

                    Debug.LogError(
                        "Piece: " +
                        cubie.pieceID
                    );

                    Debug.LogError(
                        "Unity Orientation: " +
                        cubie.orientation
                    );

                    Debug.LogError(
                        "Solver Orientation: " +
                        solverCorner.orientation
                    );

                    return false;
                }


                break;
            }


            if (!found)
            {
                Debug.LogError(
                    "CORNER FEHLT IM SOLVERSTATE"
                );

                Debug.LogError(
                    "Context: " +
                    context
                );

                Debug.LogError(
                    "Piece: " +
                    cubie.pieceID
                );

                return false;
            }
        }


        // ==================================================
        // EDGES
        // ==================================================

        foreach (
            Cubie cubie
            in rubiksCube.cubies
        )
        {
            if (
                cubie == null ||
                cubie.Type != CubieType.Edge
            )
            {
                continue;
            }


            bool found = false;


            foreach (
                SolverPieceState solverEdge
                in simulatedSolverState.edges
            )
            {
                if (
                    solverEdge.pieceID !=
                    cubie.pieceID
                )
                {
                    continue;
                }


                found = true;


                // ==========================================
                // POSITION
                // ==========================================

                if (
                    solverEdge.position !=
                    cubie.logicalPosition
                )
                {
                    Debug.LogError(
                        "EDGE POSITION UNTERSCHIED"
                    );

                    Debug.LogError(
                        "Context: " +
                        context
                    );

                    Debug.LogError(
                        "Piece: " +
                        cubie.pieceID
                    );

                    Debug.LogError(
                        "Unity Position: " +
                        cubie.logicalPosition
                    );

                    Debug.LogError(
                        "Solver Position: " +
                        solverEdge.position
                    );

                    return false;
                }


                // ==========================================
                // ORIENTATION
                // ==========================================

                if (
                    solverEdge.orientation !=
                    cubie.orientation
                )
                {
                    Debug.LogError(
                        "EDGE ORIENTATION UNTERSCHIED"
                    );

                    Debug.LogError(
                        "Context: " +
                        context
                    );

                    Debug.LogError(
                        "Piece: " +
                        cubie.pieceID
                    );

                    Debug.LogError(
                        "Unity Orientation: " +
                        cubie.orientation
                    );

                    Debug.LogError(
                        "Solver Orientation: " +
                        solverEdge.orientation
                    );

                    return false;
                }


                break;
            }


            if (!found)
            {
                Debug.LogError(
                    "EDGE FEHLT IM SOLVERSTATE"
                );

                Debug.LogError(
                    "Context: " +
                    context
                );

                Debug.LogError(
                    "Piece: " +
                    cubie.pieceID
                );

                return false;
            }
        }


        return true;
    }


    // ======================================================
    // COMPLETE UNITY STATE VALIDATION
    // ======================================================

    private bool ValidateState()
    {
        bool orientationValid =
            ValidateOrientationInternal();


        bool solverStateValid =
            ValidateSolverStateInternal();


        bool solverCopyValid =
            ValidateSolverStateCopy();


        bool valid =
            orientationValid &&
            solverStateValid &&
            solverCopyValid;


        string status =
            valid
                ? "OK"
                : "FEHLER";


        Debug.Log(
            "CubeStateTester: " +
            status
        );


        if (!orientationValid)
        {
            Debug.LogError(
                "Orientation-Prüfung fehlgeschlagen."
            );
        }


        if (!solverStateValid)
        {
            Debug.LogError(
                "Solver-State-Strukturprüfung " +
                "fehlgeschlagen."
            );
        }


        if (!solverCopyValid)
        {
            Debug.LogError(
                "Cubie-State und SolverState " +
                "stimmen nicht überein."
            );
        }


        return valid;
    }


    // ======================================================
    // ORIENTATION VALIDATION
    // ======================================================

    private bool ValidateOrientationInternal()
    {
        int cornerCount = 0;
        int edgeCount = 0;

        int cornerOrientationSum = 0;
        int edgeOrientationSum = 0;


        foreach (
            Cubie cubie
            in rubiksCube.cubies
        )
        {
            if (cubie == null)
                continue;


            if (
                cubie.Type ==
                CubieType.Corner
            )
            {
                cornerCount++;


                if (
                    cubie.orientation < 0 ||
                    cubie.orientation > 2
                )
                {
                    Debug.LogError(
                        "Ungültige Corner-Orientation: " +
                        cubie.pieceID +
                        " = " +
                        cubie.orientation
                    );

                    return false;
                }


                cornerOrientationSum +=
                    cubie.orientation;
            }


            else if (
                cubie.Type ==
                CubieType.Edge
            )
            {
                edgeCount++;


                if (
                    cubie.orientation < 0 ||
                    cubie.orientation > 1
                )
                {
                    Debug.LogError(
                        "Ungültige Edge-Orientation: " +
                        cubie.pieceID +
                        " = " +
                        cubie.orientation
                    );

                    return false;
                }


                edgeOrientationSum +=
                    cubie.orientation;
            }
        }


        if (cornerCount != 8)
        {
            Debug.LogError(
                "Corner-Anzahl falsch: " +
                cornerCount
            );

            return false;
        }


        if (edgeCount != 12)
        {
            Debug.LogError(
                "Edge-Anzahl falsch: " +
                edgeCount
            );

            return false;
        }


        if (
            cornerOrientationSum % 3 != 0
        )
        {
            Debug.LogError(
                "Corner-Orientierungssumme " +
                "nicht durch 3 teilbar: " +
                cornerOrientationSum
            );

            return false;
        }


        if (
            edgeOrientationSum % 2 != 0
        )
        {
            Debug.LogError(
                "Edge-Orientierungssumme " +
                "nicht durch 2 teilbar: " +
                edgeOrientationSum
            );

            return false;
        }


        return true;
    }


    // ======================================================
    // SOLVER STRUCTURE VALIDATION
    // ======================================================

    private bool ValidateSolverStateInternal()
    {
        List<Cubie> corners =
            new List<Cubie>();

        List<Cubie> edges =
            new List<Cubie>();


        foreach (
            Cubie cubie
            in rubiksCube.cubies
        )
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


            else if (
                cubie.Type ==
                CubieType.Edge
            )
            {
                edges.Add(cubie);
            }
        }


        if (corners.Count != 8)
        {
            Debug.LogError(
                "Solver-State: " +
                "Corner-Anzahl falsch: " +
                corners.Count
            );

            return false;
        }


        if (edges.Count != 12)
        {
            Debug.LogError(
                "Solver-State: " +
                "Edge-Anzahl falsch: " +
                edges.Count
            );

            return false;
        }


        HashSet<string> cornerIDs =
            new HashSet<string>();

        HashSet<string> edgeIDs =
            new HashSet<string>();

        HashSet<Vector3Int> cornerPositions =
            new HashSet<Vector3Int>();

        HashSet<Vector3Int> edgePositions =
            new HashSet<Vector3Int>();


        // ==================================================
        // CORNERS
        // ==================================================

        foreach (
            Cubie corner
            in corners
        )
        {
            if (
                string.IsNullOrEmpty(
                    corner.pieceID
                )
            )
            {
                Debug.LogError(
                    "Corner ohne Piece-ID."
                );

                return false;
            }


            if (!cornerIDs.Add(
                corner.pieceID
            ))
            {
                Debug.LogError(
                    "Doppelte Corner-ID: " +
                    corner.pieceID
                );

                return false;
            }


            if (!validCornerPositions.Contains(
                corner.logicalPosition
            ))
            {
                Debug.LogError(
                    "Ungültige Corner-Position: " +
                    corner.pieceID +
                    " = " +
                    corner.logicalPosition
                );

                return false;
            }


            if (!cornerPositions.Add(
                corner.logicalPosition
            ))
            {
                Debug.LogError(
                    "Doppelte Corner-Position: " +
                    corner.logicalPosition
                );

                return false;
            }


            if (
                corner.orientation < 0 ||
                corner.orientation > 2
            )
            {
                Debug.LogError(
                    "Ungültige Corner-Orientation: " +
                    corner.pieceID
                );

                return false;
            }
        }


        // ==================================================
        // EDGES
        // ==================================================

        foreach (
            Cubie edge
            in edges
        )
        {
            if (
                string.IsNullOrEmpty(
                    edge.pieceID
                )
            )
            {
                Debug.LogError(
                    "Edge ohne Piece-ID."
                );

                return false;
            }


            if (!edgeIDs.Add(
                edge.pieceID
            ))
            {
                Debug.LogError(
                    "Doppelte Edge-ID: " +
                    edge.pieceID
                );

                return false;
            }


            if (!validEdgePositions.Contains(
                edge.logicalPosition
            ))
            {
                Debug.LogError(
                    "Ungültige Edge-Position: " +
                    edge.pieceID +
                    " = " +
                    edge.logicalPosition
                );

                return false;
            }


            if (!edgePositions.Add(
                edge.logicalPosition
            ))
            {
                Debug.LogError(
                    "Doppelte Edge-Position: " +
                    edge.logicalPosition
                );

                return false;
            }


            if (
                edge.orientation < 0 ||
                edge.orientation > 1
            )
            {
                Debug.LogError(
                    "Ungültige Edge-Orientation: " +
                    edge.pieceID
                );

                return false;
            }
        }


        // ==================================================
        // ALLE POSITIONEN BELEGT?
        // ==================================================

        foreach (
            Vector3Int position
            in validCornerPositions
        )
        {
            if (!cornerPositions.Contains(
                position
            ))
            {
                Debug.LogError(
                    "Corner-Position nicht belegt: " +
                    position
                );

                return false;
            }
        }


        foreach (
            Vector3Int position
            in validEdgePositions
        )
        {
            if (!edgePositions.Contains(
                position
            ))
            {
                Debug.LogError(
                    "Edge-Position nicht belegt: " +
                    position
                );

                return false;
            }
        }


        return true;
    }


    // ======================================================
    // UNITY -> SOLVERSTATE COPY TEST
    // ======================================================

    private bool ValidateSolverStateCopy()
    {
        SolverState solverState =
            new SolverState(
                rubiksCube.cubies
            );


        if (!solverState.IsValid())
        {
            Debug.LogError(
                "SolverState konnte nicht " +
                "gültig erzeugt werden."
            );

            return false;
        }


        // ==================================================
        // CORNERS
        // ==================================================

        foreach (
            Cubie cubie
            in rubiksCube.cubies
        )
        {
            if (
                cubie == null ||
                cubie.Type != CubieType.Corner
            )
            {
                continue;
            }


            bool found = false;


            foreach (
                SolverPieceState solverCorner
                in solverState.corners
            )
            {
                if (
                    solverCorner.pieceID ==
                    cubie.pieceID
                )
                {
                    found = true;


                    if (
                        solverCorner.position !=
                        cubie.logicalPosition
                    )
                    {
                        Debug.LogError(
                            "SolverState Corner-Position " +
                            "stimmt nicht: " +
                            cubie.pieceID +
                            " | Cubie=" +
                            cubie.logicalPosition +
                            " | Solver=" +
                            solverCorner.position
                        );

                        return false;
                    }


                    if (
                        solverCorner.orientation !=
                        cubie.orientation
                    )
                    {
                        Debug.LogError(
                            "SolverState Corner-Orientation " +
                            "stimmt nicht: " +
                            cubie.pieceID +
                            " | Cubie=" +
                            cubie.orientation +
                            " | Solver=" +
                            solverCorner.orientation
                        );

                        return false;
                    }


                    break;
                }
            }


            if (!found)
            {
                Debug.LogError(
                    "Corner fehlt im SolverState: " +
                    cubie.pieceID
                );

                return false;
            }
        }


        // ==================================================
        // EDGES
        // ==================================================

        foreach (
            Cubie cubie
            in rubiksCube.cubies
        )
        {
            if (
                cubie == null ||
                cubie.Type != CubieType.Edge
            )
            {
                continue;
            }


            bool found = false;


            foreach (
                SolverPieceState solverEdge
                in solverState.edges
            )
            {
                if (
                    solverEdge.pieceID ==
                    cubie.pieceID
                )
                {
                    found = true;


                    if (
                        solverEdge.position !=
                        cubie.logicalPosition
                    )
                    {
                        Debug.LogError(
                            "SolverState Edge-Position " +
                            "stimmt nicht: " +
                            cubie.pieceID +
                            " | Cubie=" +
                            cubie.logicalPosition +
                            " | Solver=" +
                            solverEdge.position
                        );

                        return false;
                    }


                    if (
                        solverEdge.orientation !=
                        cubie.orientation
                    )
                    {
                        Debug.LogError(
                            "SolverState Edge-Orientation " +
                            "stimmt nicht: " +
                            cubie.pieceID +
                            " | Cubie=" +
                            cubie.orientation +
                            " | Solver=" +
                            solverEdge.orientation
                        );

                        return false;
                    }


                    break;
                }
            }


            if (!found)
            {
                Debug.LogError(
                    "Edge fehlt im SolverState: " +
                    cubie.pieceID
                );

                return false;
            }
        }


        return true;
    }


    // ======================================================
    // EXECUTE UNITY MOVE AND WAIT
    // ======================================================

    private IEnumerator ExecuteAndWait(
        string move)
    {
        bool executed =
            ExecuteUnityMove(move);


        if (!executed)
        {
            Debug.LogError(
                "Unity-Move konnte nicht " +
                "ausgeführt werden: " +
                move
            );

            yield break;
        }


        // ==================================================
        // Auf Ende der ersten Rotation warten
        // ==================================================

        while (
            rubiksCube.IsCurrentlyRotating()
        )
        {
            yield return null;
        }


        // ==================================================
        // Bei 180°-Moves wurde die zweite Rotation als
        // Coroutine gestartet.
        //
        // Einen Frame warten, damit diese starten kann.
        // ==================================================

        if (
            move.Contains("2")
        )
        {
            yield return null;


            while (
                rubiksCube.IsCurrentlyRotating()
            )
            {
                yield return null;
            }
        }


        if (waitAfterMove > 0f)
        {
            yield return
                new WaitForSeconds(
                    waitAfterMove
                );
        }
    }


    // ======================================================
    // UNITY MOVE PARSER
    // ======================================================

    private bool ExecuteUnityMove(
        string move)
    {
        if (string.IsNullOrWhiteSpace(
            move
        ))
        {
            return false;
        }


        string token =
            move
                .Trim()
                .ToUpperInvariant();


        bool prime =
            token.EndsWith("'");


        bool twice =
            token.EndsWith("2") ||
            token.EndsWith("2'");


        string baseMove =
            token
                .Replace("'", "")
                .Replace("2", "");


        RotationAxis axis;
        int layer;
        int direction;


        switch (baseMove)
        {
            case "U":
                axis = RotationAxis.Y;
                layer = 1;
                direction = 1;
                break;


            case "D":
                axis = RotationAxis.Y;
                layer = -1;
                direction = -1;
                break;


            case "R":
                axis = RotationAxis.X;
                layer = 1;
                direction = 1;
                break;


            case "L":
                axis = RotationAxis.X;
                layer = -1;
                direction = -1;
                break;


            case "F":
                axis = RotationAxis.Z;
                layer = 1;
                direction = -1;
                break;


            case "B":
                axis = RotationAxis.Z;
                layer = -1;
                direction = 1;
                break;


            default:
                Debug.LogError(
                    "Unbekannter Move: " +
                    move
                );

                return false;
        }


        if (prime)
        {
            direction *= -1;
        }


        // ==================================================
        // Öffentliche Schnittstelle von RubiksCube benutzen
        // ==================================================

        CubeMove cubeMove =
            new CubeMove(
                axis,
                layer,
                direction
            );


        rubiksCube.ExecuteInputMove(
            cubeMove
        );


        // ==================================================
        // 180° = denselben Move zweimal
        // ==================================================

        if (twice)
        {
            StartCoroutine(
                ExecuteSecondHalfTurn(
                    axis,
                    layer,
                    direction
                )
            );
        }


        return true;
    }


    // ======================================================
    // SECOND HALF OF 180° MOVE
    // ======================================================

    private IEnumerator ExecuteSecondHalfTurn(
        RotationAxis axis,
        int layer,
        int direction)
    {
        // Erste 90°-Drehung abwarten
        while (
            rubiksCube.IsCurrentlyRotating()
        )
        {
            yield return null;
        }


        // Zweite 90°-Drehung über die öffentliche
        // Schnittstelle ausführen
        CubeMove secondMove =
            new CubeMove(
                axis,
                layer,
                direction
            );


        rubiksCube.ExecuteInputMove(
            secondMove
        );


        // Auch zweite Drehung vollständig abwarten
        while (
            rubiksCube.IsCurrentlyRotating()
        )
        {
            yield return null;
        }
    }


    // ======================================================
    // PRINT SOLVER STATE FROM UNITY
    // ======================================================

    [ContextMenu("Print Solver State")]
    public void PrintSolverState()
    {
        if (
            rubiksCube == null ||
            rubiksCube.cubies == null
        )
        {
            Debug.LogError(
                "RubiksCube oder Cubie-Liste fehlt."
            );

            return;
        }


        Debug.Log(
            "========================================"
        );

        Debug.Log(
            "SOLVER STATE"
        );

        Debug.Log(
            "========================================"
        );


        Debug.Log(
            "--- CORNERS ---"
        );


        foreach (
            Cubie cubie
            in rubiksCube.cubies
        )
        {
            if (
                cubie != null &&
                cubie.Type ==
                CubieType.Corner
            )
            {
                Debug.Log(
                    cubie.logicalPosition +
                    " -> " +
                    cubie.pieceID +
                    " | Ori=" +
                    cubie.orientation
                );
            }
        }


        Debug.Log(
            "--- EDGES ---"
        );


        foreach (
            Cubie cubie
            in rubiksCube.cubies
        )
        {
            if (
                cubie != null &&
                cubie.Type ==
                CubieType.Edge
            )
            {
                Debug.Log(
                    cubie.logicalPosition +
                    " -> " +
                    cubie.pieceID +
                    " | Ori=" +
                    cubie.orientation
                );
            }
        }


        Debug.Log(
            "========================================"
        );
    }


    // ======================================================
    // MANUAL VALIDATION
    // ======================================================

    [ContextMenu("Validate Current State")]
    public void ValidateCurrentState()
    {
        if (
            rubiksCube == null ||
            rubiksCube.cubies == null
        )
        {
            Debug.LogError(
                "RubiksCube oder Cubie-Liste fehlt."
            );

            return;
        }


        bool valid =
            ValidateState();


        if (valid)
        {
            Debug.Log(
                "SOLVER-STATE TEST: " +
                "ALLE PRÜFUNGEN BESTANDEN"
            );
        }
        else
        {
            Debug.LogError(
                "SOLVER-STATE TEST: FEHLER"
            );
        }
    }
// ======================================================
// RANDOM SOLVER STRESS TEST
// ======================================================

[ContextMenu("Run Random Stress Test")]
public void RunRandomStressTest()
{
    if (rubiksCube == null)
    {
        Debug.LogError(
            "CubeStateTester: RubiksCube fehlt."
        );

        return;
    }


    if (rubiksCube.cubies == null)
    {
        Debug.LogError(
            "CubeStateTester: Cubie-Liste fehlt."
        );

        return;
    }


    if (randomTestMoveCount <= 0)
    {
        Debug.LogError(
            "Random Test Move Count muss größer als 0 sein."
        );

        return;
    }


    StopAllCoroutines();

    StartCoroutine(
        RunRandomStressTestCoroutine()
    );
}


// ======================================================
// RANDOM STRESS TEST COROUTINE
// ======================================================

private IEnumerator RunRandomStressTestCoroutine()
{
    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "START RANDOM SOLVER STRESS TEST"
    );

    Debug.Log(
        "Anzahl Moves: " +
        randomTestMoveCount
    );

    Debug.Log(
        "========================================"
    );


    // ==================================================
    // SolverState genau EINMAL vom aktuellen Unity-Würfel
    // kopieren.
    //
    // Danach läuft er wieder komplett unabhängig.
    // ==================================================

    simulatedSolverState =
        new SolverState(
            rubiksCube.cubies
        );


    if (!simulatedSolverState.IsValid())
    {
        Debug.LogError(
            "Initialer SolverState ist ungültig."
        );

        yield break;
    }


    if (!CompareUnityWithSimulatedSolver(
        "RANDOM START"
    ))
    {
        Debug.LogError(
            "Unity und SolverState stimmen bereits " +
            "am Start nicht überein."
        );

        yield break;
    }


    // Die komplette Zufallssequenz speichern.
    //
    // Falls irgendwann ein Fehler auftritt, können wir
    // exakt sehen, welche Moves vorher ausgeführt wurden.

    List<string> executedMoves =
        new List<string>();


    string previousMoveBase = "";


    for (
        int i = 0;
        i < randomTestMoveCount;
        i++
    )
    {
        // ==============================================
        // ZUFÄLLIGEN MOVE ERZEUGEN
        // ==============================================

        string move =
            GenerateRandomTestMove(
                previousMoveBase
            );


        string currentMoveBase =
            GetBaseMove(move);


        previousMoveBase =
            currentMoveBase;


        executedMoves.Add(move);


        // ==============================================
        // UNITY
        // ==============================================

        yield return
            ExecuteAndWait(move);


        // ==============================================
        // SOLVERSTATE
        // ==============================================

        bool solverMoveExecuted =
            simulatedSolverState
                .ApplyMove(move);


        if (!solverMoveExecuted)
        {
            Debug.LogError(
                "SolverState konnte Random-Move " +
                "nicht ausführen: " +
                move
            );

            PrintRandomSequence(
                executedMoves
            );

            yield break;
        }


        // ==============================================
        // SOLVER INTERN VALIDIEREN
        // ==============================================

        if (!simulatedSolverState.IsValid())
        {
            Debug.LogError(
                "SOLVERSTATE UNGÜLTIG"
            );

            Debug.LogError(
                "Random Move Nummer: " +
                (i + 1)
            );

            Debug.LogError(
                "Move: " +
                move
            );

            PrintRandomSequence(
                executedMoves
            );

            simulatedSolverState.Print();

            yield break;
        }


        // ==============================================
        // UNITY INTERN VALIDIEREN
        // ==============================================

        if (!ValidateState())
        {
            Debug.LogError(
                "UNITY-STATE UNGÜLTIG"
            );

            Debug.LogError(
                "Random Move Nummer: " +
                (i + 1)
            );

            Debug.LogError(
                "Move: " +
                move
            );

            PrintRandomSequence(
                executedMoves
            );

            yield break;
        }


        // ==============================================
        // UNITY <-> SOLVER VERGLEICH
        // ==============================================

        string context =
            "Random Move " +
            (i + 1) +
            "/" +
            randomTestMoveCount +
            " | " +
            move;


        if (!CompareUnityWithSimulatedSolver(
            context
        ))
        {
            Debug.LogError(
                "RANDOM PARALLELTEST FEHLGESCHLAGEN"
            );

            Debug.LogError(
                "Fehler bei Move Nummer: " +
                (i + 1)
            );

            Debug.LogError(
                "Move: " +
                move
            );

            PrintRandomSequence(
                executedMoves
            );

            yield break;
        }


        // ==============================================
        // FORTSCHRITT
        // ==============================================

        if (
            (i + 1) % 100 == 0 ||
            i == 0
        )
        {
            Debug.Log(
                "Random Stress Test: " +
                (i + 1) +
                "/" +
                randomTestMoveCount +
                " Moves OK"
            );
        }


        if (randomTestWaitAfterMove > 0f)
        {
            yield return
                new WaitForSeconds(
                    randomTestWaitAfterMove
                );
        }
    }


    // ==================================================
    // TEST BESTANDEN
    // ==================================================

    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "RANDOM SOLVER STRESS TEST BESTANDEN"
    );

    Debug.Log(
        randomTestMoveCount +
        " ZUFÄLLIGE MOVES OHNE ABWEICHUNG"
    );

    Debug.Log(
        "Unity und SolverState waren nach jedem Move identisch."
    );

    Debug.Log(
        "========================================"
    );
}


// ======================================================
// RANDOM MOVE GENERATOR
// ======================================================

private string GenerateRandomTestMove(
    string previousMoveBase)
{
    string[] baseMoves =
    {
        "U",
        "D",
        "R",
        "L",
        "F",
        "B"
    };


    string selectedBase;


    // Nicht unmittelbar dieselbe Fläche erneut wählen.
    //
    // Dadurch bekommen wir interessantere Sequenzen
    // statt z.B. R R R R.

    do
    {
        selectedBase =
            baseMoves[
                Random.Range(
                    0,
                    baseMoves.Length
                )
            ];
    }
    while (
        selectedBase ==
        previousMoveBase
    );


    int variant =
        Random.Range(
            0,
            2
        );


    if (variant == 0)
    {
        return selectedBase;
    }


    return selectedBase + "'";
}


// ======================================================
// BASE MOVE
// ======================================================

private string GetBaseMove(
    string move)
{
    if (string.IsNullOrWhiteSpace(
        move
    ))
    {
        return "";
    }


    return move
        .Replace("'", "")
        .Replace("2", "")
        .Trim()
        .ToUpperInvariant();
}


    // ======================================================
    // RANDOM SEQUENCE AUSGEBEN
    // ======================================================

    private void PrintRandomSequence(
        List<string> moves)
    {
        string sequence =
            string.Join(
                " ",
                moves
            );


        Debug.LogError(
            "========================================"
        );

        Debug.LogError(
            "SEQUENZ BIS ZUM FEHLER:"
        );

        Debug.LogError(
            sequence
        );

        Debug.LogError(
            "========================================"
        );
    }

// ======================================================
// SOLVERSTATE CLONE TEST
// ======================================================

[ContextMenu("Run Clone Test")]
public void RunCloneTest()
{
    if (rubiksCube == null)
    {
        Debug.LogError(
            "CLONE TEST: RubiksCube fehlt."
        );

        return;
    }


    if (rubiksCube.cubies == null)
    {
        Debug.LogError(
            "CLONE TEST: Cubie-Liste fehlt."
        );

        return;
    }


    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "START SOLVERSTATE CLONE TEST"
    );

    Debug.Log(
        "========================================"
    );


    // ==================================================
    // 1. ORIGINAL AUS UNITY ERZEUGEN
    // ==================================================

    SolverState original =
        new SolverState(
            rubiksCube.cubies
        );


    if (!original.IsValid())
    {
        Debug.LogError(
            "CLONE TEST: Original ist ungültig."
        );

        return;
    }


    // ==================================================
    // 2. ORIGINAL NOCH EINMAL KOPIEREN
    //
    // Diese Kopie dient nur als unveränderter
    // Referenzzustand für den späteren Vergleich.
    // ==================================================

    SolverState originalBefore =
        original.Clone();


    // ==================================================
    // 3. EIGENTLICHEN CLONE ERZEUGEN
    // ==================================================

    SolverState clone =
        original.Clone();


    // ==================================================
    // 4. PRÜFEN:
    // ORIGINAL UND CLONE MÜSSEN ZUNÄCHST IDENTISCH SEIN
    // ==================================================

    if (!AreSolverStatesEqual(
        original,
        clone
    ))
    {
        Debug.LogError(
            "CLONE TEST FEHLER: " +
            "Clone stimmt direkt nach Clone() " +
            "nicht mit Original überein."
        );

        return;
    }


    Debug.Log(
        "Clone entspricht dem Original: OK"
    );


    // ==================================================
    // 5. NUR DEN CLONE VERÄNDERN
    // ==================================================

    bool moveExecuted =
        clone.ApplyMove("R");


    if (!moveExecuted)
    {
        Debug.LogError(
            "CLONE TEST FEHLER: " +
            "R konnte auf dem Clone " +
            "nicht ausgeführt werden."
        );

        return;
    }


    // ==================================================
    // 6. CLONE MUSS WEITERHIN GÜLTIG SEIN
    // ==================================================

    if (!clone.IsValid())
    {
        Debug.LogError(
            "CLONE TEST FEHLER: " +
            "Clone ist nach R ungültig."
        );

        return;
    }


    Debug.Log(
        "Clone nach R weiterhin gültig: OK"
    );


    // ==================================================
    // 7. ORIGINAL DARF SICH NICHT VERÄNDERT HABEN
    // ==================================================

    if (!AreSolverStatesEqual(
        original,
        originalBefore
    ))
    {
        Debug.LogError(
            "CLONE TEST FEHLER: " +
            "Änderung am Clone hat das " +
            "Original verändert."
        );

        return;
    }


    Debug.Log(
        "Original blieb unverändert: OK"
    );


    // ==================================================
    // 8. CLONE MUSS JETZT ANDERS ALS ORIGINAL SEIN
    // ==================================================

    if (AreSolverStatesEqual(
        original,
        clone
    ))
    {
        Debug.LogError(
            "CLONE TEST FEHLER: " +
            "Clone ist nach R noch immer " +
            "identisch mit dem Original."
        );

        return;
    }


    Debug.Log(
        "Clone ist nach R unabhängig verändert: OK"
    );


    // ==================================================
    // 9. ORIGINAL MUSS WEITERHIN GÜLTIG SEIN
    // ==================================================

    if (!original.IsValid())
    {
        Debug.LogError(
            "CLONE TEST FEHLER: " +
            "Original ist nach Änderung des " +
            "Clones ungültig."
        );

        return;
    }


    // ==================================================
    // 10. UNITY DARF NICHT VERÄNDERT WORDEN SEIN
    //
    // Wir erzeugen jetzt einen neuen SolverState direkt
    // aus Unity und vergleichen ihn mit unserem Original.
    // ==================================================

    SolverState unityStateAfter =
        new SolverState(
            rubiksCube.cubies
        );


    if (!AreSolverStatesEqual(
        originalBefore,
        unityStateAfter
    ))
    {
        Debug.LogError(
            "CLONE TEST FEHLER: " +
            "Unity-Würfel wurde durch die " +
            "Solver-Simulation verändert."
        );

        return;
    }


    Debug.Log(
        "Unity-Würfel blieb unverändert: OK"
    );


    // ==================================================
    // TEST BESTANDEN
    // ==================================================

    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "SOLVERSTATE CLONE TEST BESTANDEN"
    );

    Debug.Log(
        "Clone ist vollständig unabhängig."
    );

    Debug.Log(
        "========================================"
    );
}


// ======================================================
// SOLVERSTATE VERGLEICH
// ======================================================

private bool AreSolverStatesEqual(
    SolverState a,
    SolverState b)
{
    if (
        a == null ||
        b == null
    )
    {
        return false;
    }


    if (
        a.corners.Count !=
        b.corners.Count
    )
    {
        return false;
    }


    if (
        a.edges.Count !=
        b.edges.Count
    )
    {
        return false;
    }


    // ==================================================
    // CORNERS
    // ==================================================

    foreach (
        SolverPieceState pieceA
        in a.corners
    )
    {
        bool found = false;


        foreach (
            SolverPieceState pieceB
            in b.corners
        )
        {
            if (
                pieceA.pieceID !=
                pieceB.pieceID
            )
            {
                continue;
            }


            found = true;


            if (
                pieceA.position !=
                pieceB.position
            )
            {
                return false;
            }


            if (
                pieceA.orientation !=
                pieceB.orientation
            )
            {
                return false;
            }


            break;
        }


        if (!found)
        {
            return false;
        }
    }


    // ==================================================
    // EDGES
    // ==================================================

    foreach (
        SolverPieceState pieceA
        in a.edges
    )
    {
        bool found = false;


        foreach (
            SolverPieceState pieceB
            in b.edges
        )
        {
            if (
                pieceA.pieceID !=
                pieceB.pieceID
            )
            {
                continue;
            }


            found = true;


            if (
                pieceA.position !=
                pieceB.position
            )
            {
                return false;
            }


            if (
                pieceA.orientation !=
                pieceB.orientation
            )
            {
                return false;
            }


            break;
        }


        if (!found)
        {
            return false;
        }
    }


    return true;
}

// ======================================================
// SOLVER STATE KEY / COMPARISON TEST
// ======================================================

[ContextMenu("Run State Key Test")]
public void RunStateKeyTest()
{
    if (rubiksCube == null)
    {
        Debug.LogError(
            "STATE KEY TEST: RubiksCube fehlt."
        );

        return;
    }


    if (rubiksCube.cubies == null)
    {
        Debug.LogError(
            "STATE KEY TEST: Cubie-Liste fehlt."
        );

        return;
    }


    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "START SOLVER STATE KEY TEST"
    );

    Debug.Log(
        "========================================"
    );


    // ==================================================
    // 1. AUSGANGSZUSTAND
    // ==================================================

    SolverState original =
        new SolverState(
            rubiksCube.cubies
        );


    if (!original.IsValid())
    {
        Debug.LogError(
            "STATE KEY TEST: " +
            "Originalzustand ist ungültig."
        );

        return;
    }


    string originalKey =
        original.GetStateKey();


    Debug.Log(
        "Originalzustand gültig: OK"
    );


    // ==================================================
    // 2. IDENTISCHER CLONE
    // ==================================================

    SolverState identicalClone =
        original.Clone();


    if (!original.IsSameState(
        identicalClone
    ))
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "Identischer Clone wird von " +
            "IsSameState() nicht als gleich erkannt."
        );

        return;
    }


    if (
        originalKey !=
        identicalClone.GetStateKey()
    )
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "Identischer Clone erzeugt " +
            "einen anderen State Key."
        );

        return;
    }


    Debug.Log(
        "Identischer Clone: " +
        "IsSameState + StateKey OK"
    );


    // ==================================================
    // 3. R MUSS ZUSTAND VERÄNDERN
    // ==================================================

    SolverState movedState =
        original.Clone();


    if (!movedState.ApplyMove("R"))
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "Move R konnte nicht ausgeführt werden."
        );

        return;
    }


    if (
        original.IsSameState(
            movedState
        )
    )
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "Original und R-Zustand werden " +
            "fälschlicherweise als gleich erkannt."
        );

        return;
    }


    if (
        originalKey ==
        movedState.GetStateKey()
    )
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "Original und R-Zustand besitzen " +
            "denselben State Key."
        );

        return;
    }


    Debug.Log(
        "R verändert Zustand und Key: OK"
    );


    // ==================================================
    // 4. R + R' MUSS ZUM ORIGINAL ZURÜCKKEHREN
    // ==================================================

    SolverState inverseTest =
        original.Clone();


    inverseTest.ApplyMove("R");

    inverseTest.ApplyMove("R'");


    if (!original.IsSameState(
        inverseTest
    ))
        PrintSolverStateDifferences(
        original,
        inverseTest,
        "R R'"
    );
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "R R' kehrt nicht zum " +
            "Originalzustand zurück."
        );

        return;
    }


    if (
        originalKey !=
        inverseTest.GetStateKey()
    )
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "R R' erzeugt nicht wieder " +
            "den ursprünglichen State Key."
        );

        return;
    }


    Debug.Log(
        "R R' -> Originalzustand: OK"
    );


    // ==================================================
    // 5. VIER R-ZÜGE MÜSSEN ORIGINAL ERGEBEN
    // ==================================================

    SolverState fourTurns =
        original.Clone();


    fourTurns.ApplyMove("R");
    fourTurns.ApplyMove("R");
    fourTurns.ApplyMove("R");
    fourTurns.ApplyMove("R");


    if (!original.IsSameState(
        fourTurns
    ))
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "R R R R kehrt nicht zum " +
            "Originalzustand zurück."
        );

        return;
    }


    if (
        originalKey !=
        fourTurns.GetStateKey()
    )
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "R R R R erzeugt nicht den " +
            "ursprünglichen State Key."
        );

        return;
    }


    Debug.Log(
        "R R R R -> Originalzustand: OK"
    );


    // ==================================================
    // 6. R2 MUSS GLEICH R R SEIN
    // ==================================================

    SolverState doubleMoveA =
        original.Clone();

    SolverState doubleMoveB =
        original.Clone();


    doubleMoveA.ApplyMove("R2");


    doubleMoveB.ApplyMove("R");
    doubleMoveB.ApplyMove("R");


    if (!doubleMoveA.IsSameState(
        doubleMoveB
    ))
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "R2 und R R ergeben " +
            "unterschiedliche Zustände."
        );

        return;
    }


    if (
        doubleMoveA.GetStateKey() !=
        doubleMoveB.GetStateKey()
    )
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "R2 und R R besitzen " +
            "unterschiedliche State Keys."
        );

        return;
    }


    Debug.Log(
        "R2 == R R: OK"
    );


    // ==================================================
    // 7. VERSCHIEDENE WEGE ZUM GLEICHEN ZUSTAND
    //
    // R U U' entspricht einfach R.
    // ==================================================

    SolverState pathA =
        original.Clone();

    SolverState pathB =
        original.Clone();


    pathA.ApplyMove("R");


    pathB.ApplyMove("R");
    pathB.ApplyMove("U");
    pathB.ApplyMove("U'");


    if (!pathA.IsSameState(
        pathB
    ))
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "R und R U U' werden nicht " +
            "als gleicher Zustand erkannt."
        );

        return;
    }


    if (
        pathA.GetStateKey() !=
        pathB.GetStateKey()
    )
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "R und R U U' besitzen " +
            "unterschiedliche State Keys."
        );

        return;
    }


    Debug.Log(
        "R == R U U': OK"
    );


    // ==================================================
    // 8. HASHSET TEST
    //
    // Genau das werden wir später im Solver benutzen.
    // ==================================================

    HashSet<string> visitedStates =
        new HashSet<string>();


    bool firstInsert =
        visitedStates.Add(
            original.GetStateKey()
        );


    bool duplicateInsert =
        visitedStates.Add(
            identicalClone.GetStateKey()
        );


    bool changedInsert =
        visitedStates.Add(
            movedState.GetStateKey()
        );


    if (!firstInsert)
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "Original konnte nicht in " +
            "HashSet eingefügt werden."
        );

        return;
    }


    if (duplicateInsert)
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "Identischer Zustand wurde im " +
            "HashSet als neuer Zustand erkannt."
        );

        return;
    }


    if (!changedInsert)
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "Veränderter Zustand wurde im " +
            "HashSet nicht als neu erkannt."
        );

        return;
    }


    if (visitedStates.Count != 2)
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "HashSet sollte genau 2 " +
            "verschiedene Zustände enthalten. " +
            "Tatsächlich: " +
            visitedStates.Count
        );

        return;
    }


    Debug.Log(
        "HashSet erkennt doppelte Zustände: OK"
    );


    // ==================================================
    // 9. ORIGINAL DARF NICHT VERÄNDERT WORDEN SEIN
    // ==================================================

    SolverState unityState =
        new SolverState(
            rubiksCube.cubies
        );


    if (!original.IsSameState(
        unityState
    ))
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "Unity-Zustand wurde während " +
            "des Tests verändert."
        );

        return;
    }


    if (
        originalKey !=
        unityState.GetStateKey()
    )
    {
        Debug.LogError(
            "STATE KEY TEST FEHLER: " +
            "Unity-Zustand besitzt nach dem " +
            "Test einen anderen Key."
        );

        return;
    }


    Debug.Log(
        "Unity-Würfel blieb unverändert: OK"
    );


    // ==================================================
    // ALLES BESTANDEN
    // ==================================================

    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "SOLVER STATE KEY TEST BESTANDEN"
    );

    Debug.Log(
        "IsSameState(): OK"
    );

    Debug.Log(
        "GetStateKey(): OK"
    );

    Debug.Log(
        "HashSet-Erkennung: OK"
    );

    Debug.Log(
        "========================================"
    );
}
private void PrintSolverStateDifferences(
    SolverState expected,
    SolverState actual,
    string context)
{
    Debug.Log(
        "========================================"
    );

    Debug.LogError(
        "STATE DIFFERENCES: " +
        context
    );

    Debug.Log(
        "========================================"
    );


    // ==================================================
    // CORNERS
    // ==================================================

    foreach (
        SolverPieceState expectedPiece
        in expected.corners
    )
    {
        foreach (
            SolverPieceState actualPiece
            in actual.corners
        )
        {
            if (
                expectedPiece.pieceID !=
                actualPiece.pieceID
            )
            {
                continue;
            }


            if (
                expectedPiece.position !=
                    actualPiece.position ||
                expectedPiece.orientation !=
                    actualPiece.orientation
            )
            {
                Debug.LogError(
                    "CORNER " +
                    expectedPiece.pieceID +
                    "\nExpected Position: " +
                    expectedPiece.position +
                    "\nActual Position: " +
                    actualPiece.position +
                    "\nExpected Orientation: " +
                    expectedPiece.orientation +
                    "\nActual Orientation: " +
                    actualPiece.orientation
                );
            }


            break;
        }
    }


    // ==================================================
    // EDGES
    // ==================================================

    foreach (
        SolverPieceState expectedPiece
        in expected.edges
    )
    {
        foreach (
            SolverPieceState actualPiece
            in actual.edges
        )
        {
            if (
                expectedPiece.pieceID !=
                actualPiece.pieceID
            )
            {
                continue;
            }


            if (
                expectedPiece.position !=
                    actualPiece.position ||
                expectedPiece.orientation !=
                    actualPiece.orientation
            )
            {
                Debug.LogError(
                    "EDGE " +
                    expectedPiece.pieceID +
                    "\nExpected Position: " +
                    expectedPiece.position +
                    "\nActual Position: " +
                    actualPiece.position +
                    "\nExpected Orientation: " +
                    expectedPiece.orientation +
                    "\nActual Orientation: " +
                    actualPiece.orientation
                );
            }


            break;
        }
    }


    Debug.Log(
        "========================================"
    );
}
// ======================================================
// UNITY INVERSE MOVE TEST
// ======================================================
//
// Prüft direkt am echten Unity-Würfel:
//
// R  R'
// R' R
// L  L'
// L' L
// U  U'
// U' U
// D  D'
// D' D
// F  F'
// F' F
// B  B'
// B' B
//
// Nach jedem Zugpaar muss der komplette logische Zustand
// exakt dem Zustand VOR dem Zugpaar entsprechen.
// ======================================================

[ContextMenu("Run Unity Inverse Test")]
public void RunUnityInverseTest()
{
    if (rubiksCube == null)
    {
        Debug.LogError(
            "UNITY INVERSE TEST: RubiksCube fehlt."
        );

        return;
    }


    StartCoroutine(
        RunUnityInverseTestCoroutine()
    );
}


// ======================================================
// UNITY INVERSE TEST COROUTINE
// ======================================================

private IEnumerator RunUnityInverseTestCoroutine()
{
    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "START UNITY INVERSE TEST"
    );

    Debug.Log(
        "========================================"
    );


    string[][] inverseTests =
    {
        new string[] { "R",  "R'" },
        new string[] { "R'", "R"  },

        new string[] { "L",  "L'" },
        new string[] { "L'", "L"  },

        new string[] { "U",  "U'" },
        new string[] { "U'", "U"  },

        new string[] { "D",  "D'" },
        new string[] { "D'", "D"  },

        new string[] { "F",  "F'" },
        new string[] { "F'", "F"  },

        new string[] { "B",  "B'" },
        new string[] { "B'", "B"  }
    };


    int passedTests = 0;


    foreach (
        string[] test
        in inverseTests
    )
    {
        string firstMove =
            test[0];

        string secondMove =
            test[1];

        string sequence =
            firstMove +
            " " +
            secondMove;


        Debug.Log(
            "----------------------------------------"
        );

        Debug.Log(
            "Teste: " +
            sequence
        );


        // ==============================================
        // ZUSTAND VOR DEM TEST SPEICHERN
        // ==============================================

        SolverState before =
            new SolverState(
                rubiksCube.cubies
            );


        if (!before.IsValid())
        {
            Debug.LogError(
                "UNITY INVERSE TEST: " +
                "Ausgangszustand vor " +
                sequence +
                " ist ungültig."
            );

            yield break;
        }


        // ==============================================
        // ERSTEN ZUG AM ECHTEN UNITY-WÜRFEL AUSFÜHREN
        // ==============================================

        yield return StartCoroutine(
            ExecuteAndWait(
                firstMove
            )
        );


        // ==============================================
        // ZWEITEN ZUG AUSFÜHREN
        // ==============================================

        yield return StartCoroutine(
            ExecuteAndWait(
                secondMove
            )
        );


        // ==============================================
        // NEUEN UNITY-ZUSTAND EINLESEN
        // ==============================================

        SolverState after =
            new SolverState(
                rubiksCube.cubies
            );


        // ==============================================
        // NORMALE VALIDIERUNG
        // ==============================================

        if (!after.IsValid())
        {
            Debug.LogError(
                "UNITY INVERSE TEST FEHLER: " +
                sequence +
                " erzeugt einen ungültigen Zustand."
            );

            yield break;
        }


        // ==============================================
        // EXAKTER VERGLEICH
        // ==============================================

        if (!before.IsSameState(
            after
        ))
        {
            Debug.LogError(
                "UNITY INVERSE TEST FEHLER: " +
                sequence +
                " kehrt nicht exakt zum " +
                "Ausgangszustand zurück."
            );


            PrintSolverStateDifferences(
                before,
                after,
                sequence
            );


            yield break;
        }


        // ==============================================
        // STATE KEY MUSS EBENFALLS IDENTISCH SEIN
        // ==============================================

        if (
            before.GetStateKey() !=
            after.GetStateKey()
        )
        {
            Debug.LogError(
                "UNITY INVERSE TEST FEHLER: " +
                sequence +
                " besitzt nach Rückkehr einen " +
                "anderen State Key."
            );

            yield break;
        }


        passedTests++;


        Debug.Log(
            sequence +
            " -> OK"
        );
    }


    // ==================================================
    // ALLE TESTS BESTANDEN
    // ==================================================

    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "UNITY INVERSE TEST BESTANDEN"
    );

    Debug.Log(
        passedTests +
        " / " +
        inverseTests.Length +
        " Zugpaare korrekt."
    );

    Debug.Log(
        "========================================"
    );
}

// ======================================================
// SOLVER-ONLY INVERSE TEST
// ======================================================
//
// Testet ausschließlich SolverState.
// Unity wird während der einzelnen Tests NICHT bewegt.
//
// Zusätzlich zu Position und Orientation wird auch die
// Position und Orientation geprüft.
// ======================================================

[ContextMenu("Run Solver Inverse Test")]
public void RunSolverInverseTest()
{
    if (
        rubiksCube == null ||
        rubiksCube.cubies == null
    )
    {
        Debug.LogError(
            "SOLVER INVERSE TEST: RubiksCube oder Cubie-Liste fehlt."
        );

        return;
    }


    string[] inverseTests =
    {
        "R R'",
        "R' R",

        "L L'",
        "L' L",

        "U U'",
        "U' U",

        "D D'",
        "D' D",

        "F F'",
        "F' F",

        "B B'",
        "B' B"
    };


    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "START SOLVER INVERSE TEST"
    );

    Debug.Log(
        "========================================"
    );


    SolverState sourceState =
        new SolverState(
            rubiksCube.cubies
        );


    if (!sourceState.IsValid())
    {
        Debug.LogError(
            "SOLVER INVERSE TEST: Ausgangszustand ist ungültig."
        );

        return;
    }


    int passedTests = 0;


    foreach (
        string sequence
        in inverseTests
    )
    {
        SolverState before =
            sourceState.Clone();

        SolverState after =
            sourceState.Clone();


        string[] moves =
            sequence.Split(
                ' ',
                System.StringSplitOptions.RemoveEmptyEntries
            );


        bool moveFailed = false;


        foreach (
            string move
            in moves
        )
        {
            if (!after.ApplyMove(move))
            {
                Debug.LogError(
                    "SOLVER INVERSE TEST FEHLER: " +
                    sequence +
                    " | Move konnte nicht ausgeführt werden: " +
                    move
                );

                moveFailed = true;
                break;
            }


            if (!after.IsValid())
            {
                Debug.LogError(
                    "SOLVER INVERSE TEST FEHLER: " +
                    sequence +
                    " | SolverState wurde nach Move " +
                    move +
                    " ungültig."
                );

                after.Print();

                moveFailed = true;
                break;
            }
        }


        if (moveFailed)
        {
            return;
        }


        if (!CompareSolverStatesIncludingReferenceDirection(
            before,
            after,
            sequence
        ))
        {
            Debug.LogError(
                "SOLVER INVERSE TEST FEHLGESCHLAGEN: " +
                sequence
            );

            return;
        }


        if (
            before.GetStateKey() !=
            after.GetStateKey()
        )
        {
            Debug.LogError(
                "SOLVER INVERSE TEST FEHLER: " +
                sequence +
                " erzeugt nach Rückkehr einen anderen State Key."
            );

            return;
        }


        passedTests++;


        Debug.Log(
            sequence +
            " -> OK"
        );
    }


    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "SOLVER INVERSE TEST BESTANDEN"
    );

    Debug.Log(
        passedTests +
        " / " +
        inverseTests.Length +
        " Zugpaare korrekt."
    );

    Debug.Log(
        "Position, Orientation und State Key stimmen."
    );

    Debug.Log(
        "========================================"
    );
}


// ======================================================
// SOLVER-VERGLEICH INKL. REFERENZRICHTUNG
// ======================================================

private bool CompareSolverStatesIncludingReferenceDirection(
    SolverState expected,
    SolverState actual,
    string context)
{
    if (
        expected == null ||
        actual == null
    )
    {
        Debug.LogError(
            "SOLVER INVERSE TEST: Null-State | " +
            context
        );

        return false;
    }


    foreach (
        SolverPieceState expectedPiece
        in expected.corners
    )
    {
        bool found = false;


        foreach (
            SolverPieceState actualPiece
            in actual.corners
        )
        {
            if (
                expectedPiece.pieceID !=
                actualPiece.pieceID
            )
            {
                continue;
            }


            found = true;


            if (
                expectedPiece.position !=
                actualPiece.position ||
                expectedPiece.orientation !=
                actualPiece.orientation
            )
            {
                Debug.LogError(
                    "SOLVER INVERSE CORNER UNTERSCHIED | " +
                    context +
                    " | Piece=" +
                    expectedPiece.pieceID +
                    " | Expected Pos=" +
                    expectedPiece.position +
                    " Ori=" +
                    expectedPiece.orientation +
                    " | Actual Pos=" +
                    actualPiece.position +
                    " Ori=" +
                    actualPiece.orientation
                );

                return false;
            }


            break;
        }


        if (!found)
        {
            Debug.LogError(
                "SOLVER INVERSE TEST: Corner fehlt | " +
                context +
                " | Piece=" +
                expectedPiece.pieceID
            );

            return false;
        }
    }


    foreach (
        SolverPieceState expectedPiece
        in expected.edges
    )
    {
        bool found = false;


        foreach (
            SolverPieceState actualPiece
            in actual.edges
        )
        {
            if (
                expectedPiece.pieceID !=
                actualPiece.pieceID
            )
            {
                continue;
            }


            found = true;


            if (
                expectedPiece.position !=
                actualPiece.position ||
                expectedPiece.orientation !=
                actualPiece.orientation
            )
            {
                Debug.LogError(
                    "SOLVER INVERSE EDGE UNTERSCHIED | " +
                    context +
                    " | Piece=" +
                    expectedPiece.pieceID +
                    " | Expected Pos=" +
                    expectedPiece.position +
                    " Ori=" +
                    expectedPiece.orientation +
                    " | Actual Pos=" +
                    actualPiece.position +
                    " Ori=" +
                    actualPiece.orientation
                );

                return false;
            }


            break;
        }


        if (!found)
        {
            Debug.LogError(
                "SOLVER INVERSE TEST: Edge fehlt | " +
                context +
                " | Piece=" +
                expectedPiece.pieceID
            );

            return false;
        }
    }


    return true;
}


// ======================================================
// EDGE ORIENTATION DIAGNOSE: START -> F -> U
// ======================================================
//
// Führt gezielt F und danach U in Unity aus.
// Nach START, F und U werden alle 12 Edges mit
// Position, Orientation und sämtlichen Sticker-Richtungen
// ausgegeben.
//
// Dieser Test verändert den Unity-Würfel.
// Am besten aus gelöstem Zustand starten.
// ======================================================

[ContextMenu("Run Edge Diagnose F U")]
public void RunEdgeDiagnoseFU()
{
    if (
        rubiksCube == null ||
        rubiksCube.cubies == null
    )
    {
        Debug.LogError(
            "EDGE DIAGNOSE F U: RubiksCube oder Cubie-Liste fehlt."
        );

        return;
    }

    StopAllCoroutines();
    StartCoroutine(RunEdgeDiagnoseFUCoroutine());
}


private IEnumerator RunEdgeDiagnoseFUCoroutine()
{
    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "START EDGE DIAGNOSE: F -> U"
    );

    Debug.Log(
        "========================================"
    );


    PrintAllEdgesDetailed("START");


    Debug.Log(
        "----------------------------------------"
    );

    Debug.Log(
        "EDGE DIAGNOSE: F ausführen"
    );

    yield return ExecuteAndWait("F");

    PrintAllEdgesDetailed("NACH F");


    Debug.Log(
        "----------------------------------------"
    );

    Debug.Log(
        "EDGE DIAGNOSE: U ausführen"
    );

    yield return ExecuteAndWait("U");

    PrintAllEdgesDetailed("NACH F U");


    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "EDGE DIAGNOSE F U BEENDET"
    );

    Debug.Log(
        "========================================"
    );
}


// ======================================================
// ALLE EDGES DETAILLIERT AUSGEBEN
// ======================================================

private void PrintAllEdgesDetailed(
    string label)
{
    int edgeCount = 0;
    int orientationSum = 0;


    Debug.Log(
        "========== " +
        label +
        " =========="
    );


    foreach (
        Cubie cubie
        in rubiksCube.cubies
    )
    {
        if (
            cubie == null ||
            cubie.Type != CubieType.Edge
        )
        {
            continue;
        }


        edgeCount++;
        orientationSum += cubie.orientation;


        string stickerText = "";


        foreach (
            CubieSticker sticker
            in cubie.stickers
        )
        {
            if (stickerText.Length > 0)
            {
                stickerText += " | ";
            }


            stickerText +=
                sticker.originalDirection +
                " -> " +
                sticker.currentDirection;
        }


        Debug.Log(
            "EDGE " +
            cubie.pieceID +
            " | Pos=" +
            cubie.logicalPosition +
            " | Ori=" +
            cubie.orientation +
            " | Stickers: " +
            stickerText
        );
    }


    Debug.Log(
        label +
        " | Edge-Anzahl=" +
        edgeCount +
        " | Orientation-Summe=" +
        orientationSum +
        " | Parität=" +
        (
            orientationSum % 2 == 0
                ? "GERADE / OK"
                : "UNGERADE / FEHLER"
        )
    );
}


// ======================================================
// CORNER ORIENTATION DIAGNOSE: START -> R -> U -> R'
// ======================================================
//
// Führt gezielt R, U und danach R' in Unity aus.
// Nach jedem Schritt werden alle 8 Corners mit
// Position, Orientation und Sticker-Richtungen ausgegeben.
//
// Dieser Test verändert den Unity-Würfel.
// Am besten aus gelöstem Zustand starten.
// ======================================================

[ContextMenu("Run Corner Diagnose R U R'")]
public void RunCornerDiagnoseRURPrime()
{
    if (
        rubiksCube == null ||
        rubiksCube.cubies == null
    )
    {
        Debug.LogError(
            "CORNER DIAGNOSE R U R': RubiksCube oder Cubie-Liste fehlt."
        );

        return;
    }

    StopAllCoroutines();
    StartCoroutine(RunCornerDiagnoseRURPrimeCoroutine());
}


private IEnumerator RunCornerDiagnoseRURPrimeCoroutine()
{
    Debug.Log("========================================");
    Debug.Log("START CORNER DIAGNOSE: R -> U -> R'");
    Debug.Log("========================================");

    PrintAllCornersDetailed("START");

    Debug.Log("----------------------------------------");
    Debug.Log("CORNER DIAGNOSE: R ausführen");
    yield return ExecuteAndWait("R");
    PrintAllCornersDetailed("NACH R");

    Debug.Log("----------------------------------------");
    Debug.Log("CORNER DIAGNOSE: U ausführen");
    yield return ExecuteAndWait("U");
    PrintAllCornersDetailed("NACH R U");

    Debug.Log("----------------------------------------");
    Debug.Log("CORNER DIAGNOSE: R' ausführen");
    yield return ExecuteAndWait("R'");
    PrintAllCornersDetailed("NACH R U R'");

    Debug.Log("========================================");
    Debug.Log("CORNER DIAGNOSE R U R' BEENDET");
    Debug.Log("========================================");
}


// ======================================================
// ALLE CORNERS DETAILLIERT AUSGEBEN
// ======================================================

private void PrintAllCornersDetailed(
    string label)
{
    int cornerCount = 0;
    int orientationSum = 0;

    Debug.Log(
        "========== " +
        label +
        " =========="
    );

    foreach (
        Cubie cubie
        in rubiksCube.cubies
    )
    {
        if (
            cubie == null ||
            cubie.Type != CubieType.Corner
        )
        {
            continue;
        }

        cornerCount++;
        orientationSum += cubie.orientation;

        string stickerText = "";
        string referenceText = "NICHT GEFUNDEN";

        foreach (
            CubieSticker sticker
            in cubie.stickers
        )
        {
            if (stickerText.Length > 0)
            {
                stickerText += " | ";
            }

            stickerText +=
                sticker.originalDirection +
                " -> " +
                sticker.currentDirection;

            if (
                sticker.originalDirection ==
                    FaceDirection.PositiveY ||
                sticker.originalDirection ==
                    FaceDirection.NegativeY
            )
            {
                referenceText =
                    sticker.originalDirection +
                    " -> " +
                    sticker.currentDirection;
            }
        }

        Debug.Log(
            "CORNER " +
            cubie.pieceID +
            " | Pos=" +
            cubie.logicalPosition +
            " | Ori=" +
            cubie.orientation +
            " | UD-Ref=" +
            referenceText +
            " | Stickers: " +
            stickerText
        );
    }

    Debug.Log(
        label +
        " | Corner-Anzahl=" +
        cornerCount +
        " | Orientation-Summe=" +
        orientationSum +
        " | Mod3=" +
        (orientationSum % 3) +
        " | " +
        (
            orientationSum % 3 == 0
                ? "OK"
                : "FEHLER"
        )
    );
}
// ======================================================
// CUBE SOLVER PHASE 1 TEST
// ======================================================

[ContextMenu("Run Cube Solver Test")]
public void RunCubeSolverTest()
{
    if (rubiksCube == null)
    {
        Debug.LogError(
            "CUBE SOLVER TEST: RubiksCube fehlt."
        );

        return;
    }


    if (rubiksCube.cubies == null)
    {
        Debug.LogError(
            "CUBE SOLVER TEST: Cubie-Liste fehlt."
        );

        return;
    }


    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "START CUBE SOLVER TEST"
    );

    Debug.Log(
        "========================================"
    );


    // ==================================================
    // AKTUELLEN UNITY-ZUSTAND KOPIEREN
    // ==================================================

    SolverState startState =
        new SolverState(
            rubiksCube.cubies
        );


    // ==================================================
    // STATE VALIDIEREN
    // ==================================================

    if (!startState.IsValid())
    {
        Debug.LogError(
            "CUBE SOLVER TEST: StartState ist ungültig."
        );

        return;
    }


    Debug.Log(
        "StartState gültig: OK"
    );


    // ==================================================
    // SOLVED CHECK
    // ==================================================

    bool alreadySolved =
        CubeSolver.IsSolved(
            startState
        );


    Debug.Log(
        "CubeSolver.IsSolved(): " +
        alreadySolved
    );


    // ==================================================
    // SOLVER STARTEN
    // ==================================================

    int maxDepth = 5;


    Debug.Log(
        "Starte Solver mit MaxDepth " +
        maxDepth
    );


    List<string> solution =
        CubeSolver.Solve(
            startState,
            maxDepth
        );


    // ==================================================
    // ERGEBNIS
    // ==================================================

    if (solution == null)
    {
        Debug.LogError(
            "CUBE SOLVER TEST: " +
            "Keine Lösung gefunden."
        );

        return;
    }


    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "CUBE SOLVER TEST ERFOLGREICH"
    );


    Debug.Log(
        "Länge: " +
        solution.Count
    );


    Debug.Log(
        "Lösung: " +
        string.Join(
            " ",
            solution
        )
    );


    Debug.Log(
        "========================================"
    );
}
// ======================================================
// R U R' SOLVER DIAGNOSE
// ======================================================
//
// Vorbereitung:
//
// Unity-Würfel manuell:
//
// R U R'
//
// Danach diesen Test starten.
//
// Der mathematisch korrekte inverse Weg ist:
//
// R U' R'
//
// Denn:
//
// (R U R')^-1
// = R U' R'
//
// ======================================================

[ContextMenu("Diagnose Solver R U R'")]
public void DiagnoseSolverRURPrime()
{
    if (rubiksCube == null)
    {
        Debug.LogError(
            "DIAGNOSE: RubiksCube fehlt."
        );

        return;
    }


    if (rubiksCube.cubies == null)
    {
        Debug.LogError(
            "DIAGNOSE: Cubie-Liste fehlt."
        );

        return;
    }


    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "START DIAGNOSE R U R'"
    );

    Debug.Log(
        "========================================"
    );


    // ==================================================
    // 1. AKTUELLEN UNITY-ZUSTAND ÜBERNEHMEN
    // ==================================================

    SolverState state =
        new SolverState(
            rubiksCube.cubies
        );


    if (!state.IsValid())
    {
        Debug.LogError(
            "DIAGNOSE: StartState ist ungültig."
        );

        return;
    }


    Debug.Log(
        "StartState gültig: OK"
    );

    PrintCornerOrientationDiagnosis(
        state,
        "START NACH UNITY R U R'"
    );


    // ==================================================
    // 2. DER START DARF NICHT GELÖST SEIN
    // ==================================================

    bool solvedAtStart =
        CubeSolver.IsSolved(
            state
        );


    Debug.Log(
        "START | IsSolved = " +
        solvedAtStart
    );


    if (solvedAtStart)
    {
        Debug.LogError(
            "DIAGNOSE FEHLER: " +
            "R U R' wird bereits als gelöst erkannt."
        );

        return;
    }


    // ==================================================
    // 3. CLONE ERZEUGEN
    // ==================================================

    SolverState testState =
        state.Clone();


    // ==================================================
    // 4. ERSTER INVERSE MOVE: R
    // ==================================================

    Debug.Log(
        "----------------------------------------"
    );

    Debug.Log(
        "DIAGNOSE MOVE 1: R"
    );


    if (!testState.ApplyMove("R"))
    {
        Debug.LogError(
            "DIAGNOSE: R konnte nicht ausgeführt werden."
        );

        return;
    }


    if (!testState.IsValid())
    {
        Debug.LogError(
            "DIAGNOSE: State nach R ungültig."
        );

        testState.Print();

        return;
    }


    PrintCornerOrientationDiagnosis(
        testState,
        "NACH SOLVER R"
    );

    Debug.Log(
        "Nach R | IsSolved = " +
        CubeSolver.IsSolved(
            testState
        )
    );


    // ==================================================
    // 5. ZWEITER INVERSE MOVE: U'
    // ==================================================

    Debug.Log(
        "----------------------------------------"
    );

    Debug.Log(
        "DIAGNOSE MOVE 2: U'"
    );


    if (!testState.ApplyMove("U'"))
    {
        Debug.LogError(
            "DIAGNOSE: U' konnte nicht ausgeführt werden."
        );

        return;
    }


    if (!testState.IsValid())
    {
        Debug.LogError(
            "DIAGNOSE: State nach R U' ungültig."
        );

        testState.Print();

        return;
    }


    PrintCornerOrientationDiagnosis(
        testState,
        "NACH SOLVER R U'"
    );

    Debug.Log(
        "Nach R U' | IsSolved = " +
        CubeSolver.IsSolved(
            testState
        )
    );


    // ==================================================
    // 6. DRITTER INVERSE MOVE: R'
    // ==================================================

    Debug.Log(
        "----------------------------------------"
    );

    Debug.Log(
        "DIAGNOSE MOVE 3: R'"
    );


    if (!testState.ApplyMove("R'"))
    {
        Debug.LogError(
            "DIAGNOSE: R' konnte nicht ausgeführt werden."
        );

        return;
    }


    if (!testState.IsValid())
    {
        Debug.LogError(
            "DIAGNOSE: State nach kompletter " +
            "Inverse-Sequenz ungültig."
        );

        testState.Print();

        return;
    }


    PrintCornerOrientationDiagnosis(
        testState,
        "NACH SOLVER R U' R'"
    );

    bool solvedAtEnd =
        CubeSolver.IsSolved(
            testState
        );


    Debug.Log(
        "========================================"
    );

    Debug.Log(
        "NACH R U' R' | IsSolved = " +
        solvedAtEnd
    );


    // ==================================================
    // 7. ENDRESULTAT
    // ==================================================

    if (solvedAtEnd)
    {
        Debug.Log(
            "DIAGNOSE BESTANDEN"
        );

        Debug.Log(
            "Die direkte inverse Sequenz " +
            "führt korrekt zum Solved-State."
        );
    }
    else
    {
        Debug.LogError(
            "DIAGNOSE FEHLGESCHLAGEN"
        );

        Debug.LogError(
            "R U' R' führt laut CubeSolver " +
            "NICHT zum Solved-State."
        );


        // ==============================================
        // Zustand vollständig ausgeben
        // ==============================================

        testState.Print();


        // ==============================================
        // Abweichungen vom erwarteten Solved-State
        // ==============================================

        PrintSolverSolvedDifferences(
            testState
        );
    }


    Debug.Log(
        "========================================"
    );
}
// ======================================================
// CORNER ORIENTATION DIAGNOSE
// ======================================================
//
// Gibt alle Corners kompakt aus:
//
// PieceID | Position | Orientation
//
// Dadurch können wir verfolgen, wie sich die
// Corner-Orientations bei jedem einzelnen Move ändern.
//
// ======================================================

private void PrintCornerOrientationDiagnosis(
    SolverState state,
    string label)
{
    Debug.Log(
        "========== " +
        label +
        " =========="
    );


    foreach (
        SolverPieceState corner
        in state.corners)
    {
        Debug.Log(
            "CORNER " +
            corner.pieceID +
            " | Pos=" +
            corner.position +
            " | Ori=" +
            corner.orientation
        );
    }


    int orientationSum = 0;


    foreach (
        SolverPieceState corner
        in state.corners)
    {
        orientationSum +=
            corner.orientation;
    }


    Debug.Log(
        "Corner Orientation Sum = " +
        orientationSum +
        " | Mod3 = " +
        (orientationSum % 3)
    );


    Debug.Log(
        "========================================"
    );
}

// ======================================================
// SOLVED-STATE DIFFERENCES
// ======================================================
//
// Gibt alle Pieces aus, die nach dem Diagnose-Test
// nicht an ihrer erwarteten Position / Orientation sind.
//
// ======================================================

private void PrintSolverSolvedDifferences(
    SolverState state)
{
    Debug.LogError(
        "========== SOLVED DIFFERENCES =========="
    );


    // ==================================================
    // CORNERS
    // ==================================================

    foreach (
        SolverPieceState corner
        in state.corners)
    {
        Vector3Int expectedPosition;

        bool known =
            TryGetExpectedCornerPositionForDiagnosis(
                corner.pieceID,
                out expectedPosition
            );


        if (!known)
        {
            Debug.LogError(
                "Unbekannte Corner-ID: " +
                corner.pieceID
            );

            continue;
        }


        if (
            corner.position != expectedPosition ||
            corner.orientation != 0)
        {
            Debug.LogError(
                "CORNER " +
                corner.pieceID +
                " | Position=" +
                corner.position +
                " | Erwartet=" +
                expectedPosition +
                " | Ori=" +
                corner.orientation +
                " | Erwartet Ori=0"
            );
        }
    }


    // ==================================================
    // EDGES
    // ==================================================

    foreach (
        SolverPieceState edge
        in state.edges)
    {
        Vector3Int expectedPosition;

        bool known =
            TryGetExpectedEdgePositionForDiagnosis(
                edge.pieceID,
                out expectedPosition
            );


        if (!known)
        {
            Debug.LogError(
                "Unbekannte Edge-ID: " +
                edge.pieceID
            );

            continue;
        }


        if (
            edge.position != expectedPosition ||
            edge.orientation != 0)
        {
            Debug.LogError(
                "EDGE " +
                edge.pieceID +
                " | Position=" +
                edge.position +
                " | Erwartet=" +
                expectedPosition +
                " | Ori=" +
                edge.orientation +
                " | Erwartet Ori=0"
            );
        }
    }


    Debug.LogError(
        "========================================"
    );
}


// ======================================================
// EXPECTED CORNER POSITIONS
// ======================================================

private bool TryGetExpectedCornerPositionForDiagnosis(
    string pieceID,
    out Vector3Int position)
{
    switch (pieceID)
    {
        case "URF":
            position = new Vector3Int(1, 1, 1);
            return true;

        case "URB":
            position = new Vector3Int(1, 1, -1);
            return true;

        case "ULF":
            position = new Vector3Int(-1, 1, 1);
            return true;

        case "ULB":
            position = new Vector3Int(-1, 1, -1);
            return true;

        case "DRF":
            position = new Vector3Int(1, -1, 1);
            return true;

        case "DRB":
            position = new Vector3Int(1, -1, -1);
            return true;

        case "DLF":
            position = new Vector3Int(-1, -1, 1);
            return true;

        case "DLB":
            position = new Vector3Int(-1, -1, -1);
            return true;
    }


    position = Vector3Int.zero;

    return false;
}


// ======================================================
// EXPECTED EDGE POSITIONS
// ======================================================

private bool TryGetExpectedEdgePositionForDiagnosis(
    string pieceID,
    out Vector3Int position)
{
    switch (pieceID)
    {
        case "UF":
            position = new Vector3Int(0, 1, 1);
            return true;

        case "UR":
            position = new Vector3Int(1, 1, 0);
            return true;

        case "UB":
            position = new Vector3Int(0, 1, -1);
            return true;

        case "UL":
            position = new Vector3Int(-1, 1, 0);
            return true;

        case "DF":
            position = new Vector3Int(0, -1, 1);
            return true;

        case "DR":
            position = new Vector3Int(1, -1, 0);
            return true;

        case "DB":
            position = new Vector3Int(0, -1, -1);
            return true;

        case "DL":
            position = new Vector3Int(-1, -1, 0);
            return true;

        case "RF":
            position = new Vector3Int(1, 0, 1);
            return true;

        case "LF":
            position = new Vector3Int(-1, 0, 1);
            return true;

        case "RB":
            position = new Vector3Int(1, 0, -1);
            return true;

        case "LB":
            position = new Vector3Int(-1, 0, -1);
            return true;
    }


    position = Vector3Int.zero;

    return false;
}

// ======================================================
// CORNER INVERSE ORIENTATION REGRESSION
// ======================================================
[ContextMenu("Run Corner Inverse Orientation Regression")]
public void RunCornerInverseOrientationRegression()
{
    if (rubiksCube == null || rubiksCube.cubies == null)
    {
        Debug.LogError("CORNER INVERSE TEST: RubiksCube/Cubies fehlen.");
        return;
    }

    SolverState baseline = new SolverState(rubiksCube.cubies);
    if (!baseline.IsValid())
    {
        Debug.LogError("CORNER INVERSE TEST: Ausgangszustand ungültig.");
        return;
    }

    string[,] tests =
    {
        { "R",  "R'" }, { "R'", "R" },
        { "L",  "L'" }, { "L'", "L" },
        { "F",  "F'" }, { "F'", "F" },
        { "B",  "B'" }, { "B'", "B" }
    };

    int failedTests = 0;
    Debug.Log("========================================");
    Debug.Log("START CORNER INVERSE ORIENTATION REGRESSION");
    Debug.Log("========================================");

    for (int i = 0; i < tests.GetLength(0); i++)
    {
        string firstMove = tests[i, 0];
        string inverseMove = tests[i, 1];
        SolverState testState = baseline.Clone();

        Debug.Log("----------------------------------------");
        Debug.Log("TEST: " + firstMove + " " + inverseMove);

        if (!testState.ApplyMove(firstMove))
        {
            Debug.LogError("Move fehlgeschlagen: " + firstMove);
            failedTests++;
            continue;
        }

        PrintCornerOrientationDiagnosis(testState, "NACH " + firstMove);

        if (!testState.ApplyMove(inverseMove))
        {
            Debug.LogError("Move fehlgeschlagen: " + inverseMove);
            failedTests++;
            continue;
        }

        PrintCornerOrientationDiagnosis(
            testState,
            "NACH " + firstMove + " " + inverseMove
        );

        bool passed = CompareCornerStateWithBaseline(
            baseline,
            testState,
            firstMove + " " + inverseMove
        );

        if (passed)
        {
            Debug.Log("PASS: " + firstMove + " " + inverseMove);
        }
        else
        {
            failedTests++;
            Debug.LogError("FAIL: " + firstMove + " " + inverseMove);
        }
    }

    Debug.Log("========================================");
    if (failedTests == 0)
        Debug.Log("CORNER INVERSE ORIENTATION REGRESSION BESTANDEN");
    else
        Debug.LogError("CORNER INVERSE ORIENTATION REGRESSION FEHLGESCHLAGEN | Fehlgeschlagene Tests: " + failedTests);
    Debug.Log("========================================");
}

private bool CompareCornerStateWithBaseline(
    SolverState baseline,
    SolverState testState,
    string label)
{
    bool passed = true;

    foreach (SolverPieceState expected in baseline.corners)
    {
        bool found = false;
        foreach (SolverPieceState actual in testState.corners)
        {
            if (actual.pieceID != expected.pieceID)
                continue;

            found = true;
            if (actual.position != expected.position ||
                actual.orientation != expected.orientation)
            {
                passed = false;
                Debug.LogError(
                    "CORNER ABWEICHUNG [" + label + "] " + expected.pieceID +
                    " | Pos=" + actual.position + " erwartet=" + expected.position +
                    " | Ori=" + actual.orientation + " erwartet=" + expected.orientation
                );
            }
            break;
        }

        if (!found)
        {
            passed = false;
            Debug.LogError("CORNER FEHLT [" + label + "]: " + expected.pieceID);
        }
    }

    return passed;
}

// ======================================================
// AUTOMATIC CUBE SOLVER SCRAMBLE TEST
// Uses independent copies; the Unity cube is not moved.
// ======================================================
[Header("Cube Solver Scramble Test")]
public string[] solverScrambleSequences =
{
    "R U R'", "F R U", "L D B'",
    "R U R' U'", "F R U B'", "L D F' R",
    "R U F L D", "F R U R' U'", "B' L D F R'"
};

[Header("Additional 6-Move Solver Tests")]
public string[] solverSixMoveSequences =
{
    "R U F L D B", "F R U R' U' F'", "B' L D F R' U"
};

[Header("Visible Unity Solver Test")]
public string unitySolverScramble = "R U R'";

private bool solverScrambleTestRunning;

[ContextMenu("Run Cube Solver Scramble Tests")]
public void RunCubeSolverScrambleTests()
{
    if (!Application.isPlaying || rubiksCube == null || rubiksCube.cubies == null)
    {
        Debug.LogError("SOLVER SCRAMBLE TEST: Im Play-Modus starten; RubiksCube/Cubies müssen vorhanden sein.");
        return;
    }
    if (solverScrambleTestRunning || rubiksCube.IsCurrentlyRotating())
    {
        Debug.LogWarning("SOLVER SCRAMBLE TEST: Ein Test oder eine Drehung läuft bereits.");
        return;
    }
    SolverState baseline = new SolverState(rubiksCube.cubies);
    if (!baseline.IsValid() || !CubeSolver.IsSolved(baseline))
    {
        Debug.LogError("SOLVER SCRAMBLE TEST: Bitte mit einem gültigen, gelösten Würfel starten.");
        return;
    }
    if (solverScrambleSequences == null || solverScrambleSequences.Length == 0)
    {
        Debug.LogError("SOLVER SCRAMBLE TEST: Keine Sequenzen eingestellt.");
        return;
    }
    StartCoroutine(RunCubeSolverScrambleTestsCoroutine(baseline));
}

private IEnumerator RunCubeSolverScrambleTestsCoroutine(SolverState baseline)
{
    solverScrambleTestRunning = true;
    int passed = 0, failed = 0;
    int[] passedByLength = new int[7];
    int[] failedByLength = new int[7];
    double totalSeconds = 0;
    long totalNodes = 0;
    int casesWithNodes = 0;
    var report = new System.Text.StringBuilder();
    Debug.Log("START CUBE SOLVER SCRAMBLE TESTS | Unity-Würfel bleibt unverändert.");
    try
    {
        // Snapshot Inspector settings for a reproducible run.
        var sequenceList = new List<string>(solverScrambleSequences);
        if (solverSixMoveSequences != null)
            sequenceList.AddRange(solverSixMoveSequences);
        string[] sequences = sequenceList.ToArray();
        foreach (string sequence in sequences)
        {
            yield return null;
            string[] moves = (sequence ?? "").Split(
                new[] { ' ', '\t', '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (moves.Length < 3 || moves.Length > 6)
            {
                failed++;
                report.AppendLine("FAIL | Ungültige Sequenz (erwartet 3–6 Züge): " + sequence);
                continue;
            }
            SolverState scrambled = baseline.Clone();
            bool valid = true;
            foreach (string move in moves)
            {
                // Quarter turns only: scramble length equals the depth bound.
                if (!IsSolverScrambleQuarterTurn(move) || !scrambled.ApplyMove(move) || !scrambled.IsValid())
                {
                    valid = false;
                    break;
                }
            }
            if (!valid)
            {
                failed++;
                failedByLength[moves.Length]++;
                report.AppendLine("FAIL | Scramble ungültig: " + sequence);
                continue;
            }
            Debug.Log("SOLVER SCRAMBLE FALL: " + sequence + " | MaxDepth=" + moves.Length);
            long nodes = -1;
            // Existing CubeSolver reports its node count through Debug.Log.
            // Capture that report without depending on an additional solver API.
            Application.LogCallback captureNodes = (message, stackTrace, type) =>
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    message, @"Untersuchte Zustände\s*:\s*([0-9.,\s]+)");
                if (!match.Success) return;
                string digits = System.Text.RegularExpressions.Regex.Replace(match.Groups[1].Value, @"\D", "");
                long parsed;
                if (long.TryParse(digits, out parsed)) nodes = parsed;
            };
            List<string> solution = null;
            string error = null;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Application.logMessageReceived += captureNodes;
            try
            {
                solution = CubeSolver.Solve(scrambled.Clone(), moves.Length);
            }
            catch (System.Exception exception)
            {
                error = exception.GetType().Name + ": " + exception.Message;
            }
            finally
            {
                Application.logMessageReceived -= captureNodes;
                timer.Stop();
            }
            totalSeconds += timer.Elapsed.TotalSeconds;
            if (nodes >= 0) { totalNodes += nodes; casesWithNodes++; }
            bool solved = solution != null && solution.Count <= moves.Length;
            SolverState verification = scrambled.Clone();
            if (solved)
            {
                foreach (string move in solution)
                {
                    if (!verification.ApplyMove(move) || !verification.IsValid())
                    {
                        solved = false;
                        break;
                    }
                }
                solved = solved && CubeSolver.IsSolved(verification);
            }
            string result = (solved ? "PASS" : "FAIL") + " | Scramble=" + sequence +
                " | Lösung=" + (solution == null ? "keine" : string.Join(" ", solution)) +
                " | Lösungslänge=" + (solution == null ? "–" : solution.Count.ToString()) +
                " | MaxDepth=" + moves.Length +
                " | Untersuchte Zustände=" + (nodes < 0 ? "nicht im Solver-Log verfügbar" : nodes.ToString()) +
                " | Suche=" + timer.Elapsed.TotalSeconds.ToString("F3") + " s" +
                (error == null ? "" : " | Ausnahme=" + error);
            report.AppendLine(result);
            if (solved)
            {
                passed++; passedByLength[moves.Length]++;
                Debug.Log(result);
            }
            else
            {
                failed++; failedByLength[moves.Length]++;
                Debug.LogError(result);
            }
        }
        report.AppendLine("GESAMT | Bestanden=" + passed + " | Fehlgeschlagen=" + failed);
        for (int length = 3; length <= 6; length++)
            report.AppendLine(length + " Züge | Bestanden=" + passedByLength[length] +
                " | Fehlgeschlagen=" + failedByLength[length]);
        report.AppendLine("Suchdauer gesamt=" + totalSeconds.ToString("F3") +
            " s | Erfasste Zustände=" + totalNodes + " (" + casesWithNodes + " Fälle)");
        if (failed == 0)
            Debug.Log("CUBE SOLVER SCRAMBLE TESTS BESTANDEN\n" + report);
        else
            Debug.LogError("CUBE SOLVER SCRAMBLE TESTS FEHLGESCHLAGEN\n" + report);
    }
    finally
    {
        solverScrambleTestRunning = false;
    }
}

private static bool IsSolverScrambleQuarterTurn(string move)
{
    return move == "R" || move == "R'" || move == "L" || move == "L'" ||
        move == "U" || move == "U'" || move == "D" || move == "D'" ||
        move == "F" || move == "F'" || move == "B" || move == "B'";
}


// Executes one scramble and its verified solution on the visible cube.
[ContextMenu("Run Unity Cube Solver Execution Test")]
public void RunUnityCubeSolverExecutionTest()
{
    if (!Application.isPlaying || rubiksCube == null || rubiksCube.cubies == null)
    {
        Debug.LogError("UNITY SOLVER TEST: Im Play-Modus mit RubiksCube/Cubies starten.");
        return;
    }
    if (solverScrambleTestRunning || rubiksCube.IsCurrentlyRotating())
    {
        Debug.LogWarning("UNITY SOLVER TEST: Ein Test oder eine Drehung läuft bereits.");
        return;
    }
    SolverState baseline = new SolverState(rubiksCube.cubies);
    if (!baseline.IsValid() || !CubeSolver.IsSolved(baseline))
    {
        Debug.LogError("UNITY SOLVER TEST: Bitte mit gültigem, gelöstem Würfel starten.");
        return;
    }
    string[] moves = (unitySolverScramble ?? "").Split(
        new[] { ' ', '\t', '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
    if (moves.Length < 3 || moves.Length > 6)
    {
        Debug.LogError("UNITY SOLVER TEST: Scramble muss 3–6 Vierteldrehungen enthalten.");
        return;
    }
    foreach (string move in moves)
    {
        if (!IsSolverScrambleQuarterTurn(move))
        {
            Debug.LogError("UNITY SOLVER TEST: Unbekannte Vierteldrehung: " + move);
            return;
        }
    }
    StartCoroutine(RunUnityCubeSolverExecutionTestCoroutine(baseline, moves));
}

private IEnumerator RunUnityCubeSolverExecutionTestCoroutine(SolverState baseline, string[] moves)
{
    solverScrambleTestRunning = true;
    try
    {
        Debug.Log("START UNITY SOLVER EXECUTION TEST | Scramble=" + string.Join(" ", moves));
        simulatedSolverState = baseline.Clone();
        foreach (string move in moves)
        {
            yield return ExecuteAndWait(move);
            if (!simulatedSolverState.ApplyMove(move) ||
                !simulatedSolverState.IsValid() || !ValidateState() ||
                !CompareUnityWithSimulatedSolver("UNITY SOLVER SCRAMBLE | " + move))
            {
                Debug.LogError("UNITY SOLVER EXECUTION TEST FEHLGESCHLAGEN beim Scramble: " + move);
                yield break;
            }
        }
        SolverState scrambled = simulatedSolverState.Clone();
        List<string> solution = null;
        string error = null;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            solution = CubeSolver.Solve(scrambled.Clone(), moves.Length);
        }
        catch (System.Exception exception)
        {
            error = exception.GetType().Name + ": " + exception.Message;
        }
        finally
        {
            timer.Stop();
        }
        if (solution == null || solution.Count > moves.Length)
        {
            Debug.LogError("UNITY SOLVER EXECUTION TEST FEHLGESCHLAGEN: Keine Lösung innerhalb MaxDepth=" +
                moves.Length + (error == null ? "" : " | " + error));
            yield break;
        }
        // Verify the entire solution before moving the visible cube.
        SolverState verification = scrambled.Clone();
        foreach (string move in solution)
        {
            if (!IsSolverScrambleQuarterTurn(move) || !verification.ApplyMove(move) || !verification.IsValid())
            {
                Debug.LogError("UNITY SOLVER EXECUTION TEST FEHLGESCHLAGEN: Lösung enthält ungültigen Zug: " + move);
                yield break;
            }
        }
        if (!CubeSolver.IsSolved(verification))
        {
            Debug.LogError("UNITY SOLVER EXECUTION TEST FEHLGESCHLAGEN: Lösung löst Zustandskopie nicht.");
            yield break;
        }
        Debug.Log("UNITY SOLVER: Führe Lösung aus: " + string.Join(" ", solution));
        foreach (string move in solution)
        {
            yield return ExecuteAndWait(move);
            if (!simulatedSolverState.ApplyMove(move) ||
                !simulatedSolverState.IsValid() || !ValidateState() ||
                !CompareUnityWithSimulatedSolver("UNITY SOLVER LÖSUNG | " + move))
            {
                Debug.LogError("UNITY SOLVER EXECUTION TEST FEHLGESCHLAGEN bei Lösung: " + move);
                yield break;
            }
        }
        SolverState finalUnityState = new SolverState(rubiksCube.cubies);
        if (!finalUnityState.IsValid() || !CubeSolver.IsSolved(finalUnityState) ||
            !CubeSolver.IsSolved(simulatedSolverState))
        {
            Debug.LogError("UNITY SOLVER EXECUTION TEST FEHLGESCHLAGEN: Endzustand nicht gelöst.");
            yield break;
        }
        Debug.Log("UNITY SOLVER EXECUTION TEST BESTANDEN | Scramble=" + string.Join(" ", moves) +
            " | Lösung=" + string.Join(" ", solution) + " | Lösungslänge=" + solution.Count +
            " | Suche=" + timer.Elapsed.TotalSeconds.ToString("F3") +
            " s | Unity und Solver nach jedem Zug identisch; Endzustand gelöst.");
    }
    finally
    {
        solverScrambleTestRunning = false;
    }
}

}

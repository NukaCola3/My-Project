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
}
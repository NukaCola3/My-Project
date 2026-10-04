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
}
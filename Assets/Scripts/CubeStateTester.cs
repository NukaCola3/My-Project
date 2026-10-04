using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeStateTester : MonoBehaviour
{
    [Header("Referenz")]
    public RubiksCube rubiksCube;

    [Header("Test")]
    public float delayBetweenMoves = 0.15f;
    public bool stopOnFirstFailure = true;
    public bool printAllEdgesOnFailure = true;
    public bool printSolverStateOnFailure = true;

    [Tooltip(
        "Jede Sequenz sollte am Ende wieder zum Ausgangszustand zurückführen. " +
        "Geprüft wird trotzdem nach JEDEM einzelnen Zug."
    )]
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


    private int totalMoves = 0;
    private int totalChecks = 0;
    private int failedChecks = 0;


    // ==================================================
    // ERLAUBTE SOLVER-POSITIONEN
    // ==================================================

    private readonly HashSet<Vector3Int> validCornerPositions =
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


    private readonly HashSet<Vector3Int> validEdgePositions =
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

            // MITTLERE EBENE
            new Vector3Int( 1,  0,  1),
            new Vector3Int(-1,  0,  1),
            new Vector3Int( 1,  0, -1),
            new Vector3Int(-1,  0, -1)
        };


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
            RunStructuredTests()
        );
    }


    // ==================================================
    // HAUPTTEST
    // ==================================================

    private IEnumerator RunStructuredTests()
    {
        Debug.Log("");
        Debug.Log(
            "============================================================"
        );
        Debug.Log(
            "       CUBE STATE - SOLVER STATE TEST"
        );
        Debug.Log(
            "============================================================"
        );


        // --------------------------------------------------
        // STARTZUSTAND
        // --------------------------------------------------

        if (!ValidateState(
            "START / SOLVED",
            "<keine Züge>"
        ))
        {
            yield break;
        }


        // --------------------------------------------------
        // TESTSEQUENZEN
        // --------------------------------------------------

        for (
            int testIndex = 0;
            testIndex < testSequences.Length;
            testIndex++
        )
        {
            string sequence =
                testSequences[testIndex];

            if (string.IsNullOrWhiteSpace(sequence))
            {
                continue;
            }


            Debug.Log("");
            Debug.Log(
                "############################################################"
            );
            Debug.Log(
                $"# TEST {testIndex + 1}: {sequence}"
            );
            Debug.Log(
                "############################################################"
            );


            List<MoveDefinition> moves =
                ParseSequence(sequence);

            List<string> executedMoves =
                new List<string>();


            if (moves.Count == 0)
            {
                Debug.LogWarning(
                    $"TEST {testIndex + 1}: Keine gültigen Züge gefunden."
                );

                continue;
            }


            // --------------------------------------------------
            // EINZELNE ZÜGE
            // --------------------------------------------------

            for (
                int moveIndex = 0;
                moveIndex < moves.Count;
                moveIndex++
            )
            {
                MoveDefinition move =
                    moves[moveIndex];


                yield return ExecuteAndWait(
                    move.axis,
                    move.layer,
                    move.direction,
                    move.name
                );


                totalMoves++;

                executedMoves.Add(
                    move.name
                );


                string executedSequence =
                    string.Join(
                        " ",
                        executedMoves
                    );


                string label =
                    $"TEST {testIndex + 1} | " +
                    $"STEP {moveIndex + 1}/{moves.Count} | " +
                    $"{move.name}";


                bool valid =
                    ValidateState(
                        label,
                        executedSequence
                    );


                if (
                    !valid &&
                    stopOnFirstFailure
                )
                {
                    Debug.LogError("");

                    Debug.LogError(
                        "============================================================"
                    );

                    Debug.LogError(
                        "TEST ABGEBROCHEN - ERSTER FEHLER GEFUNDEN"
                    );

                    Debug.LogError(
                        $"Test: {testIndex + 1}"
                    );

                    Debug.LogError(
                        $"Geplante Sequenz: {sequence}"
                    );

                    Debug.LogError(
                        $"Sequenz bis Fehler: {executedSequence}"
                    );

                    Debug.LogError(
                        $"Fehler trat nach Zug '{move.name}' auf."
                    );

                    Debug.LogError(
                        "============================================================"
                    );

                    yield break;
                }


                if (delayBetweenMoves > 0f)
                {
                    yield return new WaitForSeconds(
                        delayBetweenMoves
                    );
                }
            }


            Debug.Log(
                $"TEST {testIndex + 1} beendet: {sequence}"
            );
        }


        // --------------------------------------------------
        // ENDERGEBNIS
        // --------------------------------------------------

        Debug.Log("");
        Debug.Log(
            "============================================================"
        );

        Debug.Log(
            "                 TESTLAUF BEENDET"
        );

        Debug.Log(
            $"Ausgeführte Züge: {totalMoves}"
        );

        Debug.Log(
            $"Prüfungen: {totalChecks}"
        );

        Debug.Log(
            $"Fehlerhafte Prüfungen: {failedChecks}"
        );

        Debug.Log(
            "============================================================"
        );


        if (failedChecks == 0)
        {
            Debug.Log(
                "SOLVER-STATE TEST: ALLE PRÜFUNGEN BESTANDEN"
            );
        }
    }


    // ==================================================
    // KOMPLETTEN STATE PRÜFEN
    // ==================================================

    private bool ValidateState(
        string label,
        string executedSequence)
    {
        totalChecks++;


        bool orientationValid =
            ValidateOrientationInternal();

        bool solverStateValid =
            ValidateSolverStateInternal();


        bool valid =
            orientationValid &&
            solverStateValid;


        string status =
            valid
                ? "OK"
                : "FEHLER";


        Debug.Log(
            $"[{status}] {label}"
        );


        if (!valid)
        {
            failedChecks++;


            Debug.LogError(
                "---------------- STATE FEHLER ----------------"
            );

            Debug.LogError(
                $"Sequenz bis hier: {executedSequence}"
            );


            if (!orientationValid)
            {
                Debug.LogError(
                    "Orientation-State ist ungültig."
                );
            }


            if (!solverStateValid)
            {
                Debug.LogError(
                    "Solver-State ist ungültig."
                );
            }


            if (printAllEdgesOnFailure)
            {
                PrintEdgeState(
                    "FEHLER NACH: " +
                    executedSequence
                );
            }


            PrintCornerSummary();


            if (printSolverStateOnFailure)
            {
                PrintSolverState(
                    "FEHLER NACH: " +
                    executedSequence
                );
            }


            Debug.LogError(
                "------------------------------------------------"
            );
        }


        return valid;
    }


    // ==================================================
    // ORIENTATION PRÜFEN
    // ==================================================

    private bool ValidateOrientationInternal()
    {
        int cornerSum = 0;
        int edgeSum = 0;

        int cornerCount = 0;
        int edgeCount = 0;

        bool valueRangeValid = true;


        foreach (Cubie cubie in rubiksCube.cubies)
        {
            if (cubie == null)
            {
                continue;
            }


            // --------------------------------------------------
            // CORNER
            // --------------------------------------------------

            if (cubie.Type == CubieType.Corner)
            {
                cornerCount++;

                cornerSum +=
                    cubie.orientation;


                if (
                    cubie.orientation < 0 ||
                    cubie.orientation > 2
                )
                {
                    valueRangeValid = false;

                    Debug.LogError(
                        $"Ungültige Corner-Orientation: " +
                        $"{cubie.pieceID} = {cubie.orientation}"
                    );
                }
            }


            // --------------------------------------------------
            // EDGE
            // --------------------------------------------------

            else if (cubie.Type == CubieType.Edge)
            {
                edgeCount++;

                edgeSum +=
                    cubie.orientation;


                if (
                    cubie.orientation < 0 ||
                    cubie.orientation > 1
                )
                {
                    valueRangeValid = false;

                    Debug.LogError(
                        $"Ungültige Edge-Orientation: " +
                        $"{cubie.pieceID} = {cubie.orientation}"
                    );
                }
            }
        }


        bool cornerCountValid =
            cornerCount == 8;

        bool edgeCountValid =
            edgeCount == 12;

        bool cornerSumValid =
            cornerSum % 3 == 0;

        bool edgeSumValid =
            edgeSum % 2 == 0;


        bool valid =
            cornerCountValid &&
            edgeCountValid &&
            cornerSumValid &&
            edgeSumValid &&
            valueRangeValid;


        Debug.Log(
            $"Orientation | " +
            $"Corners: {cornerSum} " +
            $"(mod 3 = {cornerSum % 3}) | " +
            $"Edges: {edgeSum} " +
            $"(mod 2 = {edgeSum % 2}) | " +
            $"Counts C/E: {cornerCount}/{edgeCount}"
        );


        if (!cornerCountValid)
        {
            Debug.LogError(
                $"Corner-Anzahl falsch: " +
                $"{cornerCount} statt 8"
            );
        }


        if (!edgeCountValid)
        {
            Debug.LogError(
                $"Edge-Anzahl falsch: " +
                $"{edgeCount} statt 12"
            );
        }


        if (!cornerSumValid)
        {
            Debug.LogError(
                $"CORNER-SUMME UNGÜLTIG: " +
                $"{cornerSum} % 3 = {cornerSum % 3}"
            );
        }


        if (!edgeSumValid)
        {
            Debug.LogError(
                $"EDGE-SUMME UNGÜLTIG: " +
                $"{edgeSum} % 2 = {edgeSum % 2}"
            );
        }


        return valid;
    }


    // ==================================================
    // SOLVER STATE PRÜFEN
    // ==================================================

    private bool ValidateSolverStateInternal()
    {
        bool valid = true;


        HashSet<string> cornerIDs =
            new HashSet<string>();

        HashSet<string> edgeIDs =
            new HashSet<string>();


        HashSet<Vector3Int> cornerPositions =
            new HashSet<Vector3Int>();

        HashSet<Vector3Int> edgePositions =
            new HashSet<Vector3Int>();


        int cornerCount = 0;
        int edgeCount = 0;


        foreach (Cubie cubie in rubiksCube.cubies)
        {
            if (cubie == null)
            {
                continue;
            }


            // ==================================================
            // CORNERS
            // ==================================================

            if (cubie.Type == CubieType.Corner)
            {
                cornerCount++;


                // ----------------------------------------------
                // PIECE ID
                // ----------------------------------------------

                if (string.IsNullOrEmpty(cubie.pieceID))
                {
                    Debug.LogError(
                        $"Corner ohne Piece-ID: {cubie.name}"
                    );

                    valid = false;
                }
                else if (!cornerIDs.Add(cubie.pieceID))
                {
                    Debug.LogError(
                        $"Doppelte Corner-ID: {cubie.pieceID}"
                    );

                    valid = false;
                }


                // ----------------------------------------------
                // POSITION GÜLTIG?
                // ----------------------------------------------

                if (!validCornerPositions.Contains(
                    cubie.logicalPosition
                ))
                {
                    Debug.LogError(
                        $"Ungültige Corner-Position: " +
                        $"{cubie.pieceID} | " +
                        $"Pos={cubie.logicalPosition}"
                    );

                    valid = false;
                }


                // ----------------------------------------------
                // POSITION EINDEUTIG?
                // ----------------------------------------------

                if (!cornerPositions.Add(
                    cubie.logicalPosition
                ))
                {
                    Debug.LogError(
                        $"Doppelte Corner-Position: " +
                        $"{cubie.logicalPosition}"
                    );

                    valid = false;
                }


                // ----------------------------------------------
                // ORIENTATION
                // ----------------------------------------------

                if (
                    cubie.orientation < 0 ||
                    cubie.orientation > 2
                )
                {
                    Debug.LogError(
                        $"Solver: Ungültige Corner-Orientation: " +
                        $"{cubie.pieceID} = {cubie.orientation}"
                    );

                    valid = false;
                }
            }


            // ==================================================
            // EDGES
            // ==================================================

            else if (cubie.Type == CubieType.Edge)
            {
                edgeCount++;


                // ----------------------------------------------
                // PIECE ID
                // ----------------------------------------------

                if (string.IsNullOrEmpty(cubie.pieceID))
                {
                    Debug.LogError(
                        $"Edge ohne Piece-ID: {cubie.name}"
                    );

                    valid = false;
                }
                else if (!edgeIDs.Add(cubie.pieceID))
                {
                    Debug.LogError(
                        $"Doppelte Edge-ID: {cubie.pieceID}"
                    );

                    valid = false;
                }


                // ----------------------------------------------
                // POSITION GÜLTIG?
                // ----------------------------------------------

                if (!validEdgePositions.Contains(
                    cubie.logicalPosition
                ))
                {
                    Debug.LogError(
                        $"Ungültige Edge-Position: " +
                        $"{cubie.pieceID} | " +
                        $"Pos={cubie.logicalPosition}"
                    );

                    valid = false;
                }


                // ----------------------------------------------
                // POSITION EINDEUTIG?
                // ----------------------------------------------

                if (!edgePositions.Add(
                    cubie.logicalPosition
                ))
                {
                    Debug.LogError(
                        $"Doppelte Edge-Position: " +
                        $"{cubie.logicalPosition}"
                    );

                    valid = false;
                }


                // ----------------------------------------------
                // ORIENTATION
                // ----------------------------------------------

                if (
                    cubie.orientation < 0 ||
                    cubie.orientation > 1
                )
                {
                    Debug.LogError(
                        $"Solver: Ungültige Edge-Orientation: " +
                        $"{cubie.pieceID} = {cubie.orientation}"
                    );

                    valid = false;
                }
            }
        }


        // ==================================================
        // ANZAHL
        // ==================================================

        if (cornerCount != 8)
        {
            Debug.LogError(
                $"Solver-State: " +
                $"Corner-Anzahl {cornerCount} statt 8"
            );

            valid = false;
        }


        if (edgeCount != 12)
        {
            Debug.LogError(
                $"Solver-State: " +
                $"Edge-Anzahl {edgeCount} statt 12"
            );

            valid = false;
        }


        // ==================================================
        // ALLE POSITIONEN BESETZT?
        // ==================================================

        if (cornerPositions.Count !=
            validCornerPositions.Count)
        {
            Debug.LogError(
                $"Solver-State: Nur " +
                $"{cornerPositions.Count}/8 " +
                $"Corner-Positionen eindeutig belegt."
            );

            valid = false;
        }


        if (edgePositions.Count !=
            validEdgePositions.Count)
        {
            Debug.LogError(
                $"Solver-State: Nur " +
                $"{edgePositions.Count}/12 " +
                $"Edge-Positionen eindeutig belegt."
            );

            valid = false;
        }


        // ==================================================
        // ERLAUBTE POSITIONEN EXPLIZIT PRÜFEN
        // ==================================================

        foreach (
            Vector3Int position
            in validCornerPositions
        )
        {
            if (!cornerPositions.Contains(position))
            {
                Debug.LogError(
                    $"Solver-State: Corner-Position fehlt: " +
                    $"{position}"
                );

                valid = false;
            }
        }


        foreach (
            Vector3Int position
            in validEdgePositions
        )
        {
            if (!edgePositions.Contains(position))
            {
                Debug.LogError(
                    $"Solver-State: Edge-Position fehlt: " +
                    $"{position}"
                );

                valid = false;
            }
        }


        Debug.Log(
            $"Solver-State | " +
            $"Corners={cornerCount} | " +
            $"Edges={edgeCount} | " +
            $"CornerIDs={cornerIDs.Count} | " +
            $"EdgeIDs={edgeIDs.Count} | " +
            $"CornerPos={cornerPositions.Count} | " +
            $"EdgePos={edgePositions.Count}"
        );


        return valid;
    }


    // ==================================================
    // SOLVER STATE AUSGEBEN
    // ==================================================

    private void PrintSolverState(
        string title)
    {
        Debug.Log("");
        Debug.Log(
            "================ SOLVER STATE ================"
        );
        Debug.Log(title);


        List<Cubie> corners =
            new List<Cubie>();

        List<Cubie> edges =
            new List<Cubie>();


        foreach (Cubie cubie in rubiksCube.cubies)
        {
            if (cubie == null)
            {
                continue;
            }


            if (cubie.Type == CubieType.Corner)
            {
                corners.Add(cubie);
            }

            else if (cubie.Type == CubieType.Edge)
            {
                edges.Add(cubie);
            }
        }


        // --------------------------------------------------
        // CORNERS NACH POSITION SORTIEREN
        // --------------------------------------------------

        corners.Sort(
            (a, b) =>
            {
                int yCompare =
                    b.logicalPosition.y.CompareTo(
                        a.logicalPosition.y
                    );

                if (yCompare != 0)
                {
                    return yCompare;
                }


                int zCompare =
                    b.logicalPosition.z.CompareTo(
                        a.logicalPosition.z
                    );

                if (zCompare != 0)
                {
                    return zCompare;
                }


                return b.logicalPosition.x.CompareTo(
                    a.logicalPosition.x
                );
            }
        );


        Debug.Log(
            "--- SOLVER CORNERS ---"
        );


        foreach (Cubie corner in corners)
        {
            Debug.Log(
                $"Position={corner.logicalPosition} | " +
                $"Piece={corner.pieceID} | " +
                $"Orientation={corner.orientation}"
            );
        }


        // --------------------------------------------------
        // EDGES NACH POSITION SORTIEREN
        // --------------------------------------------------

        edges.Sort(
            (a, b) =>
            {
                int yCompare =
                    b.logicalPosition.y.CompareTo(
                        a.logicalPosition.y
                    );

                if (yCompare != 0)
                {
                    return yCompare;
                }


                int zCompare =
                    b.logicalPosition.z.CompareTo(
                        a.logicalPosition.z
                    );

                if (zCompare != 0)
                {
                    return zCompare;
                }


                return b.logicalPosition.x.CompareTo(
                    a.logicalPosition.x
                );
            }
        );


        Debug.Log(
            "--- SOLVER EDGES ---"
        );


        foreach (Cubie edge in edges)
        {
            Debug.Log(
                $"Position={edge.logicalPosition} | " +
                $"Piece={edge.pieceID} | " +
                $"Orientation={edge.orientation}"
            );
        }


        Debug.Log(
            "=============================================="
        );
    }


    // ==================================================
    // EDGE STATE
    // ==================================================

    private void PrintEdgeState(
        string title)
    {
        List<Cubie> edges =
            new List<Cubie>();


        foreach (Cubie cubie in rubiksCube.cubies)
        {
            if (
                cubie != null &&
                cubie.Type == CubieType.Edge
            )
            {
                edges.Add(cubie);
            }
        }


        edges.Sort(
            (a, b) =>
                string.Compare(
                    a.pieceID,
                    b.pieceID,
                    System.StringComparison.Ordinal
                )
        );


        Debug.Log("");
        Debug.Log(
            "================ EDGE STATE ================"
        );
        Debug.Log(title);


        int sum = 0;


        foreach (Cubie edge in edges)
        {
            sum +=
                edge.orientation;


            string stickerInfo = "";


            foreach (
                CubieSticker sticker
                in edge.stickers
            )
            {
                if (stickerInfo.Length > 0)
                {
                    stickerInfo += " ";
                }


                stickerInfo +=
                    sticker.originalDirection +
                    "->" +
                    sticker.currentDirection;
            }


            Debug.Log(
                $"{edge.pieceID} | " +
                $"Edge | " +
                $"Pos={edge.logicalPosition} | " +
                $"Ori={edge.orientation} | " +
                $"Stickers={stickerInfo}"
            );
        }


        Debug.Log(
            $"Edge-Summe: {sum} | " +
            $"MOD 2: {sum % 2}"
        );

        Debug.Log(
            "============================================"
        );
    }


    // ==================================================
    // CORNER STATE
    // ==================================================

    private void PrintCornerSummary()
    {
        List<Cubie> corners =
            new List<Cubie>();


        foreach (Cubie cubie in rubiksCube.cubies)
        {
            if (
                cubie != null &&
                cubie.Type == CubieType.Corner
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


        Debug.Log(
            "CORNER SUMMARY:"
        );


        foreach (Cubie corner in corners)
        {
            Debug.Log(
                $"{corner.pieceID} | " +
                $"Pos={corner.logicalPosition} | " +
                $"Ori={corner.orientation}"
            );
        }
    }


    // ==================================================
    // ZUG AUSFÜHREN UND AUF ABSCHLUSS WARTEN
    // ==================================================

    private IEnumerator ExecuteAndWait(
        RotationAxis axis,
        int layer,
        int direction,
        string moveName)
    {
        Debug.Log(
            ">>> " +
            moveName
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


        // Einen Frame geben,
        // damit die Rotation sicher starten kann.
        yield return null;


        while (
            rubiksCube.IsCurrentlyRotating()
        )
        {
            yield return null;
        }


        // Einen weiteren Frame
        // für alle logischen Updates.
        yield return null;
    }


    // ==================================================
    // SEQUENZ PARSEN
    // ==================================================

    private List<MoveDefinition> ParseSequence(
        string sequence)
    {
        List<MoveDefinition> result =
            new List<MoveDefinition>();


        string[] tokens =
            sequence.Split(
                new char[]
                {
                    ' ',
                    '\t',
                    '\r',
                    '\n'
                },
                System.StringSplitOptions.RemoveEmptyEntries
            );


        foreach (string rawToken in tokens)
        {
            string token =
                rawToken
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


            MoveDefinition move;


            if (!TryCreateMove(
                baseMove,
                prime,
                out move
            ))
            {
                Debug.LogError(
                    $"Unbekannter Zug im Tester: '{rawToken}'"
                );

                continue;
            }


            result.Add(
                move
            );


            if (twice)
            {
                result.Add(
                    move
                );
            }
        }


        return result;
    }


    // ==================================================
    // MOVE ERZEUGEN
    // ==================================================

    private bool TryCreateMove(
        string moveName,
        bool prime,
        out MoveDefinition move)
    {
        move =
            new MoveDefinition();


        RotationAxis axis;
        int layer;
        int direction;


        switch (moveName)
        {
            // ----------------------------------------------
            // U
            // ----------------------------------------------

            case "U":

                axis =
                    RotationAxis.Y;

                layer = 1;

                direction = 1;

                break;


            // ----------------------------------------------
            // D
            // ----------------------------------------------

            case "D":

                axis =
                    RotationAxis.Y;

                layer = -1;

                direction = -1;

                break;


            // ----------------------------------------------
            // R
            // ----------------------------------------------

            case "R":

                axis =
                    RotationAxis.X;

                layer = 1;

                direction = 1;

                break;


            // ----------------------------------------------
            // L
            // ----------------------------------------------

            case "L":

                axis =
                    RotationAxis.X;

                layer = -1;

                direction = -1;

                break;


            // ----------------------------------------------
            // F
            // ----------------------------------------------

            case "F":

                axis =
                    RotationAxis.Z;

                layer = 1;

                direction = -1;

                break;


            // ----------------------------------------------
            // B
            // ----------------------------------------------

            case "B":

                axis =
                    RotationAxis.Z;

                layer = -1;

                direction = 1;

                break;


            default:

                return false;
        }


        if (prime)
        {
            direction *= -1;
        }


        move =
            new MoveDefinition(
                axis,
                layer,
                direction,
                moveName +
                (prime ? "'" : "")
            );


        return true;
    }


    // ==================================================
    // MANUELLE DIAGNOSE
    // ==================================================

    [ContextMenu(
        "Aktuellen kompletten State prüfen"
    )]
    public void ValidateCurrentState()
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


        ValidateState(
            "MANUELL",
            "<manueller Zustand>"
        );
    }


    // ==================================================
    // SOLVER STATE MANUELL AUSGEBEN
    // ==================================================

    [ContextMenu(
        "Aktuellen Solver-State ausgeben"
    )]
    public void PrintCurrentSolverState()
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


        PrintSolverState(
            "MANUELLER SOLVER STATE"
        );
    }


    // ==================================================
    // EDGES MANUELL AUSGEBEN
    // ==================================================

    [ContextMenu(
        "Aktuelle Edges ausgeben"
    )]
    public void PrintCurrentEdges()
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


        PrintEdgeState(
            "MANUELLER STATE"
        );
    }


    // ==================================================
    // MOVE DEFINITION
    // ==================================================

    private struct MoveDefinition
    {
        public RotationAxis axis;

        public int layer;

        public int direction;

        public string name;


        public MoveDefinition(
            RotationAxis axis,
            int layer,
            int direction,
            string name)
        {
            this.axis =
                axis;

            this.layer =
                layer;

            this.direction =
                direction;

            this.name =
                name;
        }
    }
}
using System.Collections.Generic;
using UnityEngine;


// ==========================================================
// CUBE SOLVER
// ==========================================================
//
// Phase 1:
//
// Ein einfacher Solver zum Testen der kompletten Pipeline:
//
// SolverState
//      ↓
// Suche
//      ↓
// Move-Sequenz
//      ↓
// gelöster SolverState
//
// WICHTIG:
//
// Dieser Solver ist absichtlich noch NICHT für große
// Scrambles gedacht.
//
// Verwendet wird zunächst Breadth-First Search (BFS).
//
// ==========================================================

public static class CubeSolver
{
    // ======================================================
    // VERFÜGBARE MOVES
    // ======================================================

    private static readonly string[] Moves =
    {
        "U", "U'",
        "D", "D'",
        "R", "R'",
        "L", "L'",
        "F", "F'",
        "B", "B'"
    };


    // ======================================================
    // SOLVER NODE
    // ======================================================
    //
    // Ein Knoten enthält:
    //
    // - einen Würfelzustand
    // - den bisher verwendeten Move-Pfad
    //
    // ======================================================

    private class SearchNode
    {
        public SolverState state;

        public List<string> path;


        public SearchNode(
            SolverState state,
            List<string> path)
        {
            this.state = state;
            this.path = path;
        }
    }


    // ======================================================
    // SOLVED STATE
    // ======================================================

    public static bool IsSolved(
        SolverState state)
    {
        if (state == null)
        {
            return false;
        }


        // ==================================================
        // CORNERS
        // ==================================================

        foreach (
            SolverPieceState corner
            in state.corners)
        {
            Vector3Int expectedPosition;


            if (!TryGetSolvedCornerPosition(
                corner.pieceID,
                out expectedPosition))
            {
                Debug.LogError(
                    "CubeSolver: Unbekannte Corner-ID: " +
                    corner.pieceID
                );

                return false;
            }


            if (
                corner.position !=
                expectedPosition)
            {
                return false;
            }


            if (corner.orientation != 0)
            {
                return false;
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


            if (!TryGetSolvedEdgePosition(
                edge.pieceID,
                out expectedPosition))
            {
                Debug.LogError(
                    "CubeSolver: Unbekannte Edge-ID: " +
                    edge.pieceID
                );

                return false;
            }


            if (
                edge.position !=
                expectedPosition)
            {
                return false;
            }


            if (edge.orientation != 0)
            {
                return false;
            }
        }


        return true;
    }


    // ======================================================
    // SOLVER STARTEN
    // ======================================================
    //
    // Gibt bei Erfolg die Lösung zurück.
    //
    // Beispiel:
    //
    // R
    //
    // Ergebnis:
    //
    // R'
    //
    // ======================================================

    public static List<string> Solve(
        SolverState startState,
        int maxDepth = 5)
    {
        if (startState == null)
        {
            Debug.LogError(
                "CubeSolver: StartState ist null."
            );

            return null;
        }


        if (!startState.IsValid())
        {
            Debug.LogError(
                "CubeSolver: StartState ist ungültig."
            );

            return null;
        }


        // ==================================================
        // BEREITS GELÖST?
        // ==================================================

        if (IsSolved(startState))
        {
            Debug.Log(
                "CubeSolver: Würfel ist bereits gelöst."
            );

            return new List<string>();
        }


        // ==================================================
        // BFS INITIALISIEREN
        // ==================================================

        Queue<SearchNode> queue =
            new Queue<SearchNode>();


        HashSet<string> visited =
            new HashSet<string>();


        SolverState startClone =
            startState.Clone();


        queue.Enqueue(
            new SearchNode(
                startClone,
                new List<string>()
            )
        );


        visited.Add(
            startClone.GetStateKey()
        );


        int expandedStates = 0;


        // ==================================================
        // BFS
        // ==================================================

        while (queue.Count > 0)
        {
            SearchNode current =
                queue.Dequeue();


            expandedStates++;


            // ==============================================
            // MAX DEPTH
            // ==============================================

            if (current.path.Count >= maxDepth)
            {
                continue;
            }


            // ==============================================
            // ALLE MOVES TESTEN
            // ==============================================

            foreach (string move in Moves)
            {
                // ------------------------------------------
                // Kleines Pruning:
                //
                // Direkten Gegenmove nicht erzeugen.
                //
                // Beispiel:
                //
                // R -> R'
                //
                // würde sofort wieder zum vorherigen
                // Zustand führen.
                // ------------------------------------------

                if (
                    current.path.Count > 0 &&
                    AreInverseMoves(
                        current.path[
                            current.path.Count - 1
                        ],
                        move
                    ))
                {
                    continue;
                }


                // ------------------------------------------
                // Zustand klonen
                // ------------------------------------------

                SolverState nextState =
                    current.state.Clone();


                // ------------------------------------------
                // Move anwenden
                // ------------------------------------------

                bool moveApplied =
                    nextState.ApplyMove(
                        move
                    );


                if (!moveApplied)
                {
                    continue;
                }


                // ------------------------------------------
                // STATE KEY
                // ------------------------------------------

                string key =
                    nextState.GetStateKey();


                // ------------------------------------------
                // Bereits besucht?
                // ------------------------------------------

                if (!visited.Add(key))
                {
                    continue;
                }


                // ------------------------------------------
                // Neuen Pfad erzeugen
                // ------------------------------------------

                List<string> nextPath =
                    new List<string>(
                        current.path
                    );


                nextPath.Add(
                    move
                );


                // ------------------------------------------
                // GELÖST?
                // ------------------------------------------

                if (IsSolved(nextState))
                {
                    Debug.Log(
                        "========================================"
                    );

                    Debug.Log(
                        "CUBE SOLVER: LÖSUNG GEFUNDEN"
                    );

                    Debug.Log(
                        "Tiefe: " +
                        nextPath.Count
                    );

                    Debug.Log(
                        "Untersuchte Zustände: " +
                        expandedStates
                    );

                    Debug.Log(
                        "Lösung: " +
                        string.Join(
                            " ",
                            nextPath
                        )
                    );

                    Debug.Log(
                        "========================================"
                    );


                    return nextPath;
                }


                // ------------------------------------------
                // Weiter durchsuchen
                // ------------------------------------------

                queue.Enqueue(
                    new SearchNode(
                        nextState,
                        nextPath
                    )
                );
            }
        }


        // ==================================================
        // KEINE LÖSUNG
        // ==================================================

        Debug.LogWarning(
            "CubeSolver: Keine Lösung bis Tiefe " +
            maxDepth +
            " gefunden. Untersuchte Zustände: " +
            expandedStates
        );


        return null;
    }


    // ======================================================
    // INVERSE MOVES
    // ======================================================

    private static bool AreInverseMoves(
        string first,
        string second)
    {
        if (
            string.IsNullOrEmpty(first) ||
            string.IsNullOrEmpty(second))
        {
            return false;
        }


        return
            GetInverseMove(first) ==
            second;
    }


    // ======================================================
    // INVERSE MOVE
    // ======================================================

    public static string GetInverseMove(
        string move)
    {
        if (string.IsNullOrEmpty(move))
        {
            return "";
        }


        if (move.EndsWith("'"))
        {
            return
                move.Substring(
                    0,
                    move.Length - 1
                );
        }


        return move + "'";
    }


    // ======================================================
    // SOLVED CORNER POSITIONS
    // ======================================================

    private static bool TryGetSolvedCornerPosition(
        string pieceID,
        out Vector3Int position)
    {
        switch (pieceID)
        {
            case "URF":
                position =
                    new Vector3Int(1, 1, 1);
                return true;


            case "URB":
                position =
                    new Vector3Int(1, 1, -1);
                return true;


            case "ULF":
                position =
                    new Vector3Int(-1, 1, 1);
                return true;


            case "ULB":
                position =
                    new Vector3Int(-1, 1, -1);
                return true;


            case "DRF":
                position =
                    new Vector3Int(1, -1, 1);
                return true;


            case "DRB":
                position =
                    new Vector3Int(1, -1, -1);
                return true;


            case "DLF":
                position =
                    new Vector3Int(-1, -1, 1);
                return true;


            case "DLB":
                position =
                    new Vector3Int(-1, -1, -1);
                return true;
        }


        position =
            Vector3Int.zero;

        return false;
    }

// ======================================================
// SOLVED EDGE POSITIONS
// ======================================================

private static bool TryGetSolvedEdgePosition(
    string pieceID,
    out Vector3Int position)
{
    switch (pieceID)
    {
        // ==============================================
        // UPPER EDGES
        // ==============================================

        case "UF":
            position =
                new Vector3Int(0, 1, 1);
            return true;


        case "UR":
            position =
                new Vector3Int(1, 1, 0);
            return true;


        case "UB":
            position =
                new Vector3Int(0, 1, -1);
            return true;


        case "UL":
            position =
                new Vector3Int(-1, 1, 0);
            return true;


        // ==============================================
        // DOWN EDGES
        // ==============================================

        case "DF":
            position =
                new Vector3Int(0, -1, 1);
            return true;


        case "DR":
            position =
                new Vector3Int(1, -1, 0);
            return true;


        case "DB":
            position =
                new Vector3Int(0, -1, -1);
            return true;


        case "DL":
            position =
                new Vector3Int(-1, -1, 0);
            return true;


        // ==============================================
        // MIDDLE EDGES
        //
        // WICHTIG:
        // Das sind die tatsächlichen Piece-IDs des
        // Unity-Projekts.
        // ==============================================

        case "RF":
            position =
                new Vector3Int(1, 0, 1);
            return true;


        case "LF":
            position =
                new Vector3Int(-1, 0, 1);
            return true;


        case "RB":
            position =
                new Vector3Int(1, 0, -1);
            return true;


        case "LB":
            position =
                new Vector3Int(-1, 0, -1);
            return true;
    }


    position =
        Vector3Int.zero;

    return false;
}
    
}
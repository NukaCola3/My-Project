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
// Verwendet wird bidirektionale Breadth-First Search (BFS).
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
    // - Vorgänger, letzten Move und Tiefe (Pfad erst am Ende)
    //
    // ======================================================

    private sealed class SearchNode
    {
        public readonly SolverState state;
        public readonly SearchNode parent;
        public readonly string move;
        public readonly int depth;

        public SearchNode(SolverState state, SearchNode parent, string move)
        {
            this.state = state;
            this.parent = parent;
            this.move = move;
            depth = parent == null ? 0 : parent.depth + 1;
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

    public static List<string> Solve(SolverState startState, int maxDepth = 5)
    {
        if (startState == null || !startState.IsValid())
        {
            Debug.LogError("CubeSolver: StartState fehlt oder ist ungültig.");
            return null;
        }
        if (maxDepth < 0)
        {
            Debug.LogError("CubeSolver: MaxDepth darf nicht negativ sein.");
            return null;
        }
        if (IsSolved(startState))
        {
            Debug.Log("CubeSolver: Würfel ist bereits gelöst. Untersuchte Zustände: 0");
            return new List<string>();
        }

        // Build the goal on a clone, preserving piece IDs and collection order.
        // No changes are made to the supplied state or to Unity cubies.
        SolverState goal = startState.Clone();
        for (int i = 0; i < goal.corners.Count; i++)
        {
            SolverPieceState piece = goal.corners[i];
            Vector3Int position;
            if (!TryGetSolvedCornerPosition(piece.pieceID, out position)) return null;
            piece.position = position;
            piece.orientation = 0;
            goal.corners[i] = piece;
        }
        for (int i = 0; i < goal.edges.Count; i++)
        {
            SolverPieceState piece = goal.edges[i];
            Vector3Int position;
            if (!TryGetSolvedEdgePosition(piece.pieceID, out position)) return null;
            piece.position = position;
            piece.orientation = 0;
            goal.edges[i] = piece;
        }
        if (!goal.IsValid() || !IsSolved(goal))
        {
            Debug.LogError("CubeSolver: Gelöster Zielzustand konnte nicht erzeugt werden.");
            return null;
        }

        var startNode = new SearchNode(startState.Clone(), null, null);
        var goalNode = new SearchNode(goal, null, null);
        var forwardVisited = new Dictionary<string, SearchNode>();
        var backwardVisited = new Dictionary<string, SearchNode>();
        forwardVisited.Add(startNode.state.GetStateKey(), startNode);
        backwardVisited.Add(goal.GetStateKey(), goalNode);
        var forwardFrontier = new List<SearchNode> { startNode };
        var backwardFrontier = new List<SearchNode> { goalNode };
        int forwardDepth = 0, backwardDepth = 0;
        long expandedStates = 0;

        // Each frontier contains one complete BFS layer. If the two visited
        // balls do not intersect, no solution within their summed radii exists.
        // Expanding the smaller layer then finds a shortest quarter-turn path.
        while (forwardFrontier.Count > 0 && backwardFrontier.Count > 0 &&
            forwardDepth + backwardDepth < maxDepth)
        {
            bool forward = forwardFrontier.Count <= backwardFrontier.Count;
            List<SearchNode> frontier = forward ? forwardFrontier : backwardFrontier;
            Dictionary<string, SearchNode> own = forward ? forwardVisited : backwardVisited;
            Dictionary<string, SearchNode> other = forward ? backwardVisited : forwardVisited;
            var nextFrontier = new List<SearchNode>();
            foreach (SearchNode current in frontier)
            {
                expandedStates++;
                foreach (string move in Moves)
                {
                    if (current.move != null && AreInverseMoves(current.move, move))
                        continue;
                    SolverState nextState = current.state.Clone();
                    if (!nextState.ApplyMove(move)) continue;
                    string key = nextState.GetStateKey();
                    if (own.ContainsKey(key)) continue;
                    var next = new SearchNode(nextState, current, move);
                    own.Add(key, next);
                    SearchNode meeting;
                    if (other.TryGetValue(key, out meeting))
                    {
                        List<string> solution = BuildSolution(
                            forward ? next : meeting, forward ? meeting : next);
                        Debug.Log("CUBE SOLVER: LÖSUNG GEFUNDEN");
                        Debug.Log("Tiefe: " + solution.Count);
                        Debug.Log("Untersuchte Zustände: " + expandedStates);
                        Debug.Log("Lösung: " + string.Join(" ", solution));
                        return solution;
                    }
                    nextFrontier.Add(next);
                }
            }
            if (forward)
            {
                forwardFrontier = nextFrontier;
                forwardDepth++;
            }
            else
            {
                backwardFrontier = nextFrontier;
                backwardDepth++;
            }
        }
        Debug.LogWarning("CubeSolver: Keine Lösung bis Tiefe " + maxDepth +
            " gefunden. Untersuchte Zustände: " + expandedStates);
        return null;
    }

    private static List<string> BuildSolution(SearchNode forward, SearchNode backward)
    {
        var solution = new List<string>();
        for (SearchNode node = forward; node.parent != null; node = node.parent)
            solution.Add(node.move);
        solution.Reverse();
        // The backward tree points from the goal to the meeting state.
        // Walking toward its root requires inverse moves, in reverse order.
        for (SearchNode node = backward; node.parent != null; node = node.parent)
            solution.Add(GetInverseMove(node.move));
        return solution;
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
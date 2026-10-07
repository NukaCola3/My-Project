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
    // - einen kompakten Zustandsschlüssel
    // - Vorgänger, letzten Move und Tiefe (Pfad erst am Ende)
    //
    // ======================================================

    private sealed class SearchNode
    {
        public readonly string key;
        public readonly SearchNode parent;
        public readonly string move;
        public readonly int depth;

        public SearchNode(string key, SearchNode parent, string move)
        {
            this.key = key;
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

    public enum SearchStatus
    {
        Solved, DepthLimit, TimeLimit, StateLimit, InvalidInput
    }

    // Existing callers keep the same API, with bounded resource use.
    public static List<string> Solve(SolverState startState, int maxDepth = 5)
    {
        SearchStatus status;
        return Solve(startState, maxDepth, 5.0, 500000, out status);
    }

    public static List<string> Solve(SolverState startState, int maxDepth,
        double maxSearchSeconds, int maxStoredStates, out SearchStatus status)
    {
        status = SearchStatus.InvalidInput;
        if (maxSearchSeconds <= 0 || double.IsNaN(maxSearchSeconds) ||
            double.IsInfinity(maxSearchSeconds) || maxStoredStates < 2)
        {
            Debug.LogError("CubeSolver: Ungültiges Zeit- oder Zustandslimit.");
            return null;
        }
        var searchTimer = System.Diagnostics.Stopwatch.StartNew();
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
            status = SearchStatus.Solved;
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

        // One shared workspace, never retained in visited nodes. Lists preserve
        // the piece-ID order of this search, so each key slot identifies a piece.
        var keyBuffer = new char[20];
        if (!HasEncodablePositions(startState))
        {
            Debug.LogError("CubeSolver: Piece-Position außerhalb gültiger Corner-/Edge-Plätze.");
            return null;
        }
        string startKey = EncodeState(startState, keyBuffer);
        string goalKey = EncodeState(goal, keyBuffer);
        SolverState workspace = startState.Clone();
        DecodeStateInto(startKey, workspace);
        if (!workspace.IsSameState(startState))
        {
            Debug.LogError("CubeSolver: Kompakter Zustand konnte nicht rekonstruiert werden.");
            return null;
        }
        var startNode = new SearchNode(startKey, null, null);
        var goalNode = new SearchNode(goalKey, null, null);
        var forwardVisited = new Dictionary<string, SearchNode>();
        var backwardVisited = new Dictionary<string, SearchNode>();
        forwardVisited.Add(startNode.key, startNode);
        backwardVisited.Add(goalNode.key, goalNode);
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
                    if (searchTimer.Elapsed.TotalSeconds >= maxSearchSeconds)
                    {
                        status = SearchStatus.TimeLimit;
                        Debug.LogWarning("CubeSolver: Suche wegen Zeitlimit abgebrochen (" +
                            maxSearchSeconds + " s). Untersuchte Zustände: " + expandedStates);
                        return null;
                    }
                    if (current.move != null && AreInverseMoves(current.move, move))
                        continue;
                    DecodeStateInto(current.key, workspace);
                    if (!workspace.ApplyMove(move)) continue;
                    string key = EncodeState(workspace, keyBuffer);
                    if (own.ContainsKey(key)) continue;
                    if ((long)forwardVisited.Count + backwardVisited.Count >= maxStoredStates)
                    {
                        status = SearchStatus.StateLimit;
                        Debug.LogWarning("CubeSolver: Suche wegen Zustandslimit abgebrochen (" +
                            maxStoredStates + "). Untersuchte Zustände: " + expandedStates);
                        return null;
                    }
                    var next = new SearchNode(key, current, move);
                    own.Add(key, next);
                    SearchNode meeting;
                    if (other.TryGetValue(key, out meeting))
                    {
                        List<string> solution = BuildSolution(
                            forward ? next : meeting, forward ? meeting : next);
                        status = SearchStatus.Solved;
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
        status = SearchStatus.DepthLimit;
        Debug.LogWarning("CubeSolver: Keine Lösung bis Tiefe " + maxDepth +
            " gefunden. Untersuchte Zustände: " + expandedStates);
        return null;
    }

    // Encode 27 coordinate slots times three orientations in one char.
    // This is a lossless value key, not a hash: dictionary hash collisions
    // are resolved by full ordinal string equality. IDs are fixed per search.
    private static string EncodeState(SolverState state, char[] buffer)
    {
        for (int i = 0; i < 8; i++) buffer[i] = EncodePiece(state.corners[i]);
        for (int i = 0; i < 12; i++) buffer[8 + i] = EncodePiece(state.edges[i]);
        return new string(buffer);
    }

    private static char EncodePiece(SolverPieceState piece)
    {
        Vector3Int p = piece.position;
        int position = (p.x + 1) * 9 + (p.y + 1) * 3 + p.z + 1;
        return (char)(position * 3 + piece.orientation);
    }

    private static void DecodeStateInto(string key, SolverState state)
    {
        for (int i = 0; i < 8; i++)
        {
            SolverPieceState piece = state.corners[i];
            DecodePiece(key[i], ref piece);
            state.corners[i] = piece;
        }
        for (int i = 0; i < 12; i++)
        {
            SolverPieceState piece = state.edges[i];
            DecodePiece(key[8 + i], ref piece);
            state.edges[i] = piece;
        }
    }

    private static void DecodePiece(char value, ref SolverPieceState piece)
    {
        int position = value / 3;
        piece.orientation = value % 3;
        piece.position = new Vector3Int(position / 9 - 1,
            (position / 3) % 3 - 1, position % 3 - 1);
    }

    private static bool HasEncodablePositions(SolverState state)
    {
        foreach (SolverPieceState corner in state.corners)
        {
            Vector3Int p = corner.position;
            if ((p.x != -1 && p.x != 1) || (p.y != -1 && p.y != 1) ||
                (p.z != -1 && p.z != 1)) return false;
        }
        foreach (SolverPieceState edge in state.edges)
        {
            Vector3Int p = edge.position;
            if (p.x < -1 || p.x > 1 || p.y < -1 || p.y > 1 || p.z < -1 || p.z > 1 ||
                (p.x == 0 ? 1 : 0) + (p.y == 0 ? 1 : 0) + (p.z == 0 ? 1 : 0) != 1)
                return false;
        }
        return true;
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
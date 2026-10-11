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
        Solved, DepthLimit, TimeLimit, StateLimit, InvalidInput, Running, Cancelled
    }

    public static List<string> Solve(SolverState startState, int maxDepth = 5)
    {
        SearchStatus status;
        return Solve(startState, maxDepth, 5.0, 500000, out status);
    }

    // Synchronous regression tests drive the very same resumable session.
    public static List<string> Solve(SolverState startState, int maxDepth,
        double maxSearchSeconds, int maxStoredStates, out SearchStatus status)
    {
        using (SearchSession session = CreateSearchSession(startState, maxDepth,
            maxSearchSeconds, maxStoredStates))
        {
            while (!session.IsComplete) session.Step(1000.0, 10000);
            status = session.Status;
            return session.Solution;
        }
    }

    public static SearchSession CreateSearchSession(SolverState startState, int maxDepth = 5,
        double maxSearchSeconds = 5.0, int maxStoredStates = 500000)
    {
        return new SearchSession(startState, maxDepth, maxSearchSeconds, maxStoredStates);
    }

    public sealed class SearchSession : System.IDisposable
    {
        private IEnumerator<int> routine;
        private readonly int maxDepth;
        private readonly double maxSearchSeconds;
        private readonly int maxStoredStates;
        private readonly System.Diagnostics.Stopwatch workTimer = new System.Diagnostics.Stopwatch();

        public SearchStatus Status { get; private set; } = SearchStatus.Running;
        public List<string> Solution { get; private set; }
        public long ExpandedStates { get; private set; }
        public bool IsComplete => Status != SearchStatus.Running;
        // Waiting between frames does not consume the active search-time budget.
        public double ActiveSearchSeconds => workTimer.Elapsed.TotalSeconds;
        public int StepCount { get; private set; }

        internal SearchSession(SolverState state, int maxDepth, double seconds, int storedStates)
        {
            this.maxDepth = maxDepth;
            maxSearchSeconds = seconds;
            maxStoredStates = storedStates;
            routine = RunSearch(state == null ? null : state.Clone());
        }

        public void Step(double frameBudgetMilliseconds = 3.0, int maxWorkItems = 256)
        {
            if (IsComplete) return;
            if (frameBudgetMilliseconds <= 0 || double.IsNaN(frameBudgetMilliseconds) ||
                double.IsInfinity(frameBudgetMilliseconds) || maxWorkItems <= 0)
                throw new System.ArgumentOutOfRangeException("frameBudgetMilliseconds",
                    "Frame-Budget und maximale Arbeitsschritte müssen positiv sein.");
            StepCount++;
            var sliceTimer = System.Diagnostics.Stopwatch.StartNew();
            workTimer.Start();
            try
            {
                for (int i = 0; i < maxWorkItems && !IsComplete; i++)
                {
                    if (!routine.MoveNext())
                    {
                        if (!IsComplete) Finish(SearchStatus.InvalidInput, null);
                        break;
                    }
                    // A single move/GC can exceed the budget; this is cooperative.
                    if (sliceTimer.Elapsed.TotalMilliseconds >= frameBudgetMilliseconds) break;
                }
            }
            catch (System.Exception exception)
            {
                Finish(SearchStatus.InvalidInput, null);
                Debug.LogError("CubeSolver: Suchfehler: " + exception.GetType().Name + ": " + exception.Message);
            }
            finally
            {
                workTimer.Stop();
                if (IsComplete) ReleaseRoutine();
            }
        }

        public void Cancel()
        {
            if (IsComplete) return;
            workTimer.Stop();
            Finish(SearchStatus.Cancelled, null);
            ReleaseRoutine();
            Debug.Log("CubeSolver: Suche abgebrochen. Untersuchte Zustände: " + ExpandedStates);
        }

        public void Dispose()
        {
            if (!IsComplete) Cancel();
            ReleaseRoutine();
        }

        private void ReleaseRoutine()
        {
            if (routine == null) return;
            routine.Dispose();
            routine = null;
        }

        private void Finish(SearchStatus status, List<string> solution)
        {
            Status = status;
            Solution = solution;
        }

        private IEnumerator<int> RunSearch(SolverState startState)
        {
            SearchStatus terminal = SearchStatus.InvalidInput;
            if (maxSearchSeconds <= 0 || double.IsNaN(maxSearchSeconds) ||
                double.IsInfinity(maxSearchSeconds) || maxStoredStates < 2)
            {
                Debug.LogError("CubeSolver: Ungültiges Zeit- oder Zustandslimit.");
                Finish(terminal, null); yield break;
            }
            if (startState == null || !startState.IsValid())
            {
                Debug.LogError("CubeSolver: StartState fehlt oder ist ungültig.");
                Finish(terminal, null); yield break;
            }
            if (maxDepth < 0)
            {
                Debug.LogError("CubeSolver: MaxDepth darf nicht negativ sein.");
                Finish(terminal, null); yield break;
            }
            if (IsSolved(startState))
            {
                terminal = SearchStatus.Solved;
                Debug.Log("CubeSolver: Würfel ist bereits gelöst. Untersuchte Zustände: 0");
                Finish(terminal, new List<string>()); yield break;
            }

            // Build the goal on a clone, preserving piece IDs and collection order.
            // No changes are made to the supplied state or to Unity cubies.
            SolverState goal = startState.Clone();
            for (int i = 0; i < goal.corners.Count; i++)
            {
                SolverPieceState piece = goal.corners[i];
                Vector3Int position;
                if (!TryGetSolvedCornerPosition(piece.pieceID, out position)) { Finish(terminal, null); yield break; }
                piece.position = position;
                piece.orientation = 0;
                goal.corners[i] = piece;
            }
            for (int i = 0; i < goal.edges.Count; i++)
            {
                SolverPieceState piece = goal.edges[i];
                Vector3Int position;
                if (!TryGetSolvedEdgePosition(piece.pieceID, out position)) { Finish(terminal, null); yield break; }
                piece.position = position;
                piece.orientation = 0;
                goal.edges[i] = piece;
            }
            if (!goal.IsValid() || !IsSolved(goal))
            {
                Debug.LogError("CubeSolver: Gelöster Zielzustand konnte nicht erzeugt werden.");
                Finish(terminal, null); yield break;
            }

            // One shared workspace, never retained in visited nodes. Lists preserve
            // the piece-ID order of this search, so each key slot identifies a piece.
            var keyBuffer = new char[20];
            if (!HasEncodablePositions(startState))
            {
                Debug.LogError("CubeSolver: Piece-Position außerhalb gültiger Corner-/Edge-Plätze.");
                Finish(terminal, null); yield break;
            }
            string startKey = EncodeState(startState, keyBuffer);
            string goalKey = EncodeState(goal, keyBuffer);
            SolverState workspace = startState.Clone();
            DecodeStateInto(startKey, workspace);
            if (!workspace.IsSameState(startState))
            {
                Debug.LogError("CubeSolver: Kompakter Zustand konnte nicht rekonstruiert werden.");
                Finish(terminal, null); yield break;
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
            ExpandedStates = 0;

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
                    ExpandedStates++;
                    foreach (string move in Moves)
                    {
                        // Suspend before each move, including pruned moves. Iterator
                        // locals retain the frontier, node and move indices exactly.
                        yield return 0;
                        if (workTimer.Elapsed.TotalSeconds >= maxSearchSeconds)
                        {
                            terminal = SearchStatus.TimeLimit;
                            Debug.LogWarning("CubeSolver: Suche wegen Zeitlimit abgebrochen (" +
                                maxSearchSeconds + " s). Untersuchte Zustände: " + ExpandedStates);
                            Finish(terminal, null); yield break;
                        }
                        if (current.move != null && AreInverseMoves(current.move, move))
                            continue;
                        DecodeStateInto(current.key, workspace);
                        if (!workspace.ApplyMove(move)) continue;
                        string key = EncodeState(workspace, keyBuffer);
                        if (own.ContainsKey(key)) continue;
                        if ((long)forwardVisited.Count + backwardVisited.Count >= maxStoredStates)
                        {
                            terminal = SearchStatus.StateLimit;
                            Debug.LogWarning("CubeSolver: Suche wegen Zustandslimit abgebrochen (" +
                                maxStoredStates + "). Untersuchte Zustände: " + ExpandedStates);
                            Finish(terminal, null); yield break;
                        }
                        var next = new SearchNode(key, current, move);
                        own.Add(key, next);
                        SearchNode meeting;
                        if (other.TryGetValue(key, out meeting))
                        {
                            List<string> solution = BuildSolution(
                                forward ? next : meeting, forward ? meeting : next);
                            terminal = SearchStatus.Solved;
                            Debug.Log("CUBE SOLVER: LÖSUNG GEFUNDEN");
                            Debug.Log("Tiefe: " + solution.Count);
                            Debug.Log("Untersuchte Zustände: " + ExpandedStates);
                            Debug.Log("Lösung: " + string.Join(" ", solution));
                            Finish(terminal, solution); yield break;
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
            terminal = SearchStatus.DepthLimit;
            Debug.LogWarning("CubeSolver: Keine Lösung bis Tiefe " + maxDepth +
                " gefunden. Untersuchte Zustände: " + ExpandedStates);
            Finish(terminal, null); yield break;
        }
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
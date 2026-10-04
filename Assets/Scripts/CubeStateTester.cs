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

    [Tooltip("Jede Sequenz sollte am Ende wieder zum Ausgangszustand zurückführen. " +
             "Geprüft wird trotzdem nach JEDEM einzelnen Zug.")]
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

    private void Start()
    {
        if (rubiksCube == null)
            rubiksCube = FindFirstObjectByType<RubiksCube>();

        if (rubiksCube == null)
        {
            Debug.LogError("CubeStateTester: Kein RubiksCube gefunden!");
            return;
        }

        StartCoroutine(RunStructuredTests());
    }

    // ==================================================
    // HAUPTTEST
    // ==================================================

    private IEnumerator RunStructuredTests()
    {
        Debug.Log("");
        Debug.Log("============================================================");
        Debug.Log("       CUBE STATE - STRUKTURIERTER ORIENTATIONSTEST");
        Debug.Log("============================================================");

        if (!ValidateOrientation("START / SOLVED", "<keine Züge>"))
            yield break;

        for (int testIndex = 0; testIndex < testSequences.Length; testIndex++)
        {
            string sequence = testSequences[testIndex];

            if (string.IsNullOrWhiteSpace(sequence))
                continue;

            Debug.Log("");
            Debug.Log("############################################################");
            Debug.Log($"# TEST {testIndex + 1}: {sequence}");
            Debug.Log("############################################################");

            List<MoveDefinition> moves = ParseSequence(sequence);
            List<string> executedMoves = new List<string>();

            if (moves.Count == 0)
            {
                Debug.LogWarning($"TEST {testIndex + 1}: Keine gültigen Züge gefunden.");
                continue;
            }

            for (int moveIndex = 0; moveIndex < moves.Count; moveIndex++)
            {
                MoveDefinition move = moves[moveIndex];

                yield return ExecuteAndWait(
                    move.axis,
                    move.layer,
                    move.direction,
                    move.name
                );

                totalMoves++;
                executedMoves.Add(move.name);

                string executedSequence = string.Join(" ", executedMoves);
                string label =
                    $"TEST {testIndex + 1} | STEP {moveIndex + 1}/{moves.Count} | {move.name}";

                bool valid = ValidateOrientation(label, executedSequence);

                if (!valid && stopOnFirstFailure)
                {
                    Debug.LogError("");
                    Debug.LogError("============================================================");
                    Debug.LogError("TEST ABGEBROCHEN - ERSTER FEHLER GEFUNDEN");
                    Debug.LogError($"Test: {testIndex + 1}");
                    Debug.LogError($"Geplante Sequenz: {sequence}");
                    Debug.LogError($"Sequenz bis Fehler: {executedSequence}");
                    Debug.LogError($"Fehler trat nach Zug '{move.name}' auf.");
                    Debug.LogError("============================================================");
                    yield break;
                }

                if (delayBetweenMoves > 0f)
                    yield return new WaitForSeconds(delayBetweenMoves);
            }

            Debug.Log($"TEST {testIndex + 1} beendet: {sequence}");
        }

        Debug.Log("");
        Debug.Log("============================================================");
        Debug.Log("                 TESTLAUF BEENDET");
        Debug.Log($"Ausgeführte Züge: {totalMoves}");
        Debug.Log($"Prüfungen: {totalChecks}");
        Debug.Log($"Fehlerhafte Prüfungen: {failedChecks}");
        Debug.Log("============================================================");
    }

    // ==================================================
    // ORIENTATION PRÜFEN
    // ==================================================

    private bool ValidateOrientation(string label, string executedSequence)
    {
        totalChecks++;

        int cornerSum = 0;
        int edgeSum = 0;
        int cornerCount = 0;
        int edgeCount = 0;
        bool valueRangeValid = true;

        foreach (Cubie cubie in rubiksCube.cubies)
        {
            if (cubie == null)
                continue;

            if (cubie.Type == CubieType.Corner)
            {
                cornerCount++;
                cornerSum += cubie.orientation;

                if (cubie.orientation < 0 || cubie.orientation > 2)
                {
                    valueRangeValid = false;
                    Debug.LogError(
                        $"Ungültige Corner-Orientation: {cubie.pieceID} = {cubie.orientation}"
                    );
                }
            }
            else if (cubie.Type == CubieType.Edge)
            {
                edgeCount++;
                edgeSum += cubie.orientation;

                if (cubie.orientation < 0 || cubie.orientation > 1)
                {
                    valueRangeValid = false;
                    Debug.LogError(
                        $"Ungültige Edge-Orientation: {cubie.pieceID} = {cubie.orientation}"
                    );
                }
            }
        }

        bool cornerCountValid = cornerCount == 8;
        bool edgeCountValid = edgeCount == 12;
        bool cornerValid = cornerSum % 3 == 0;
        bool edgeValid = edgeSum % 2 == 0;

        bool valid =
            cornerCountValid &&
            edgeCountValid &&
            cornerValid &&
            edgeValid &&
            valueRangeValid;

        string status = valid ? "OK" : "FEHLER";

        Debug.Log(
            $"[{status}] {label} | " +
            $"Corners: {cornerSum} (mod 3 = {cornerSum % 3}) | " +
            $"Edges: {edgeSum} (mod 2 = {edgeSum % 2}) | " +
            $"Counts C/E: {cornerCount}/{edgeCount}"
        );

        if (!valid)
        {
            failedChecks++;

            Debug.LogError("---------------- ORIENTATION FEHLER ----------------");
            Debug.LogError($"Sequenz bis hier: {executedSequence}");

            if (!cornerCountValid)
                Debug.LogError($"Corner-Anzahl falsch: {cornerCount} statt 8");

            if (!edgeCountValid)
                Debug.LogError($"Edge-Anzahl falsch: {edgeCount} statt 12");

            if (!cornerValid)
                Debug.LogError(
                    $"CORNER-SUMME UNGÜLTIG: {cornerSum} % 3 = {cornerSum % 3}"
                );

            if (!edgeValid)
                Debug.LogError(
                    $"EDGE-SUMME UNGÜLTIG: {edgeSum} % 2 = {edgeSum % 2}"
                );

            if (printAllEdgesOnFailure)
                PrintEdgeState("FEHLER NACH: " + executedSequence);

            PrintCornerSummary();
            Debug.LogError("------------------------------------------------------");
        }

        return valid;
    }

    // ==================================================
    // EDGE STATE
    // ==================================================

    private void PrintEdgeState(string title)
    {
        List<Cubie> edges = new List<Cubie>();

        foreach (Cubie cubie in rubiksCube.cubies)
        {
            if (cubie != null && cubie.Type == CubieType.Edge)
                edges.Add(cubie);
        }

        edges.Sort(
            (a, b) => string.Compare(
                a.pieceID,
                b.pieceID,
                System.StringComparison.Ordinal
            )
        );

        Debug.Log("");
        Debug.Log("================ EDGE STATE ================");
        Debug.Log(title);

        int sum = 0;

        foreach (Cubie edge in edges)
        {
            sum += edge.orientation;

            string stickerInfo = "";
            foreach (CubieSticker sticker in edge.stickers)
            {
                if (stickerInfo.Length > 0)
                    stickerInfo += " ";

                stickerInfo +=
                    sticker.originalDirection + "->" + sticker.currentDirection;
            }

            Debug.Log(
                $"{edge.pieceID} | Edge | Pos={edge.logicalPosition} | " +
                $"Ori={edge.orientation} | Stickers={stickerInfo}"
            );
        }

        Debug.Log($"Edge-Summe: {sum} | MOD 2: {sum % 2}");
        Debug.Log("============================================");
    }

    private void PrintCornerSummary()
    {
        List<Cubie> corners = new List<Cubie>();

        foreach (Cubie cubie in rubiksCube.cubies)
        {
            if (cubie != null && cubie.Type == CubieType.Corner)
                corners.Add(cubie);
        }

        corners.Sort(
            (a, b) => string.Compare(
                a.pieceID,
                b.pieceID,
                System.StringComparison.Ordinal
            )
        );

        Debug.Log("CORNER SUMMARY:");

        foreach (Cubie corner in corners)
        {
            Debug.Log(
                $"{corner.pieceID} | Pos={corner.logicalPosition} | Ori={corner.orientation}"
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
        Debug.Log(">>> " + moveName);

        CubeMove move = new CubeMove(axis, layer, direction);
        rubiksCube.ExecuteInputMove(move);

        // Einen Frame geben, damit die Rotation sicher starten kann.
        yield return null;

        while (rubiksCube.IsCurrentlyRotating())
            yield return null;

        // Einen weiteren Frame für alle logischen Updates.
        yield return null;
    }

    // ==================================================
    // SEQUENZ PARSEN
    // ==================================================

    private List<MoveDefinition> ParseSequence(string sequence)
    {
        List<MoveDefinition> result = new List<MoveDefinition>();

        string[] tokens = sequence.Split(
            new char[] { ' ', '\t', '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries
        );

        foreach (string rawToken in tokens)
        {
            string token = rawToken.Trim().ToUpperInvariant();
            bool prime = token.EndsWith("'");
            bool twice = token.EndsWith("2") || token.EndsWith("2'");

            string baseMove = token.Replace("'", "").Replace("2", "");

            MoveDefinition move;
            if (!TryCreateMove(baseMove, prime, out move))
            {
                Debug.LogError($"Unbekannter Zug im Tester: '{rawToken}'");
                continue;
            }

            result.Add(move);

            if (twice)
                result.Add(move);
        }

        return result;
    }

    private bool TryCreateMove(
        string moveName,
        bool prime,
        out MoveDefinition move)
    {
        move = new MoveDefinition();

        RotationAxis axis;
        int layer;
        int direction;

        switch (moveName)
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
                return false;
        }

        if (prime)
            direction *= -1;

        move = new MoveDefinition(
            axis,
            layer,
            direction,
            moveName + (prime ? "'" : "")
        );

        return true;
    }

    // ==================================================
    // MANUELLE DIAGNOSE
    // ==================================================

    [ContextMenu("Aktuellen Orientation-State prüfen")]
    public void ValidateCurrentState()
    {
        if (rubiksCube == null)
            rubiksCube = FindFirstObjectByType<RubiksCube>();

        if (rubiksCube == null)
        {
            Debug.LogError("CubeStateTester: RubiksCube fehlt.");
            return;
        }

        ValidateOrientation("MANUELL", "<manueller Zustand>");
    }

    [ContextMenu("Aktuelle Edges ausgeben")]
    public void PrintCurrentEdges()
    {
        if (rubiksCube == null)
            rubiksCube = FindFirstObjectByType<RubiksCube>();

        if (rubiksCube == null)
        {
            Debug.LogError("CubeStateTester: RubiksCube fehlt.");
            return;
        }

        PrintEdgeState("MANUELLER STATE");
    }

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
            this.axis = axis;
            this.layer = layer;
            this.direction = direction;
            this.name = name;
        }
    }
}
